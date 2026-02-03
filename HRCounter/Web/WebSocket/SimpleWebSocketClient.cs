using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Utils;
using HRCounter.Web.WebSocket.EventArgs;
using IPA.Logging;

namespace HRCounter.Web.WebSocket;

public class SimpleWebSocketClient(long maxMessageSize = 4096) : IDisposable
{
    private readonly Logger _logger = Plugin.GetChildLogger(nameof(SimpleWebSocketClient));

    private readonly byte[] _rxBuffer = new byte[maxMessageSize];

    public event EventHandler<WebSocketMessageEventArgs>? MessageReceived;

    /// <summary>
    ///     Raised when the WebSocket connection is closed not by calling <see cref="CloseAsync" />
    /// </summary>
    public event EventHandler<WebSocketClosedEventArgs>? Closed;

    private ClientWebSocket? _ws;
    private Task? _receiveLoopTask;
    private CancellationTokenSource? _receiveTokenSource;

    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private int _disposed = 0;

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0) throw new ObjectDisposedException(nameof(SimpleWebSocketClient));
    }

    /// <param name="uri">Uri to connect to</param>
    /// <param name="token">Cancellation token</param>
    /// <param name="configureOptions">A callback to configure websocket options. Do not call other methods in this callback</param>
    public Task ConnectAsync(string uri, CancellationToken token, Action<ClientWebSocketOptions>? configureOptions = null) =>
        ConnectAsync(new Uri(uri), token, configureOptions);

    /// <param name="uri">Uri to connect to</param>
    /// <param name="token">Cancellation token</param>
    /// <param name="configureOptions">A callback to configure websocket options. Do not call other methods in this callback</param>
    public async Task ConnectAsync(Uri uri, CancellationToken token, Action<ClientWebSocketOptions>? configureOptions = null)
    {
        ThrowIfDisposed();
        _logger.Debug($"Connecting to {uri}");
        await _lifecycle.WaitAsync(token);
        try
        {
            if (_ws is not null)
            {
                // stop the previous one if it exists
                _receiveTokenSource?.Cancel();
                _receiveTokenSource?.Dispose();
                _receiveTokenSource = null;
                _ws.Abort();
                _ws.Dispose();
                _ws = null;
            }

            _ws = new ClientWebSocket();
            configureOptions?.Invoke(_ws.Options);
            await _ws.ConnectAsync(uri, token);
            _logger.Trace("Websocket connected");
        }
        catch (OperationCanceledException)
        {
            _logger.Trace("WebSocket connect cancelled");
        }
        catch (Exception e)
        {
            _logger.Error("Failed to connect WebSocket");
            _logger.Error(e);
            throw;
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    public async Task StartReceive(CancellationToken token)
    {
        ThrowIfDisposed();
        _logger.Debug("Starting receive loop");
        await _lifecycle.WaitAsync(token);
        var ws = _ws;
        try
        {
            if (ws is null or { State: not WebSocketState.Open })
            {
                throw new InvalidOperationException("WebSocket is not connected");
            }

            if (_receiveLoopTask?.IsCompleted == false)
            {
                _logger.Warn("Receive loop is already running");
                return;
            }

            _receiveTokenSource?.Cancel();
            _receiveTokenSource?.Dispose();
            var tokenSource = new CancellationTokenSource();
            _receiveLoopTask = Task.Run(() => ReceiveLoop(ws, tokenSource.Token), CancellationToken.None);
            _receiveTokenSource = tokenSource;
        }
        catch (OperationCanceledException)
        {
            _logger.Trace("Start receive cancelled");
        }
        catch (Exception e)
        {
            _logger.Critical("Failed to start receive loop");
            _logger.Critical(e);
            throw;
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    private async Task ReceiveLoop(ClientWebSocket ws, CancellationToken token)
    {
        while (!token.IsCancellationRequested && ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                var result = await ReceiveMessage(ws, token);
                if (!result) break;
            }
            catch (ObjectDisposedException)
            {
                // WebSocket may have been aborted and disposed
                _logger.Trace("ReceiveLoop ObjectDisposedException");
                break;
            }
            catch (OperationCanceledException)
            {
                _logger.Trace("ReceiveLoop cancelled");
                break;
            }
            catch (Exception e)
            {
                _logger.Error("Exception in ReceiveLoop: " + e.Message);
                _logger.Error(e);
                await CloseAsync(WebSocketCloseStatus.InternalServerError, "Unexpected exception during receive", CancellationToken.None);
                RaiseClosed(WebSocketCloseStatus.InternalServerError, "Unexpected exception during receive", false, e);
                break;
            }
        }

        _logger.Trace($"Receive loop exited, cancelled: {token.IsCancellationRequested}");
    }

    private async Task<bool> ReceiveMessage(ClientWebSocket ws, CancellationToken token)
    {
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(_rxBuffer), token);
        if (result.MessageType == WebSocketMessageType.Close)
        {
            _logger.Spam($"Received WebSocket close message ({result.CloseStatus}): {result.CloseStatusDescription}, State: {ws.State}");
            if (ws.State is not (WebSocketState.Closed or WebSocketState.CloseSent or WebSocketState.Aborted))
            {
                _logger.Debug($"WebSocket closed by server ({result.CloseStatus}): {result.CloseStatusDescription}");
                await CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                RaiseClosed(result.CloseStatus ?? WebSocketCloseStatus.Empty, result.CloseStatusDescription, true);
            }

            return false;
        }

        if (!result.EndOfMessage)
        {
            _logger.Warn("Received message too big to fit in buffer, disconnecting");
            await CloseAsync(WebSocketCloseStatus.MessageTooBig, "Message too big", CancellationToken.None);
            RaiseClosed(WebSocketCloseStatus.MessageTooBig, "Message too big");
            return false;
        }

        if (result.MessageType != WebSocketMessageType.Text)
        {
            _logger.Warn("Received non-text message, ignoring");
            return true;
        }

        var message = Encoding.UTF8.GetString(_rxBuffer, 0, result.Count);
        // Process message
        OnMessageReceived(message);
        return true;
    }

    private void OnMessageReceived(string message)
    {
        try
        {
            var handlers = MessageReceived;
            handlers?.Invoke(this, new WebSocketMessageEventArgs(message));
        }
        catch (Exception e)
        {
            _logger.Error("Exception occured while raising MessageReceived event");
            _logger.Error(e);
        }
    }

    public async Task SendMessageAsync(string message, CancellationToken token)
    {
        ThrowIfDisposed();
        _logger.Spam("Sending message: " + message);
        ClientWebSocket ws;
        await _lifecycle.WaitAsync(token);
        try
        {
            if (_ws is null or { State: not WebSocketState.Open })
            {
                throw new InvalidOperationException("WebSocket is not connected");
            }

            ws = _ws;
        }
        finally
        {
            _lifecycle.Release();
        }

        await _sendLock.WaitAsync(token);
        try
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
        }
        catch (Exception e)
        {
            _logger.Error("Failed to send WebSocket message");
            _logger.Error(e);
            throw;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private void RaiseClosed(WebSocketCloseStatus closeStatus, string closeStatusDescription, bool isWebSocketMessage = false,
        Exception? exception = null)
    {
        var args = new WebSocketClosedEventArgs(closeStatus, closeStatusDescription, isWebSocketMessage, exception);
        try
        {
            var handlers = Closed;
            handlers?.Invoke(this, args);
        }
        catch (Exception e)
        {
            _logger.Error("Exception occured while raising Closed event");
            _logger.Error(e);
        }
    }

    /// <summary>
    ///     Close the WebSocket connection, will not raise Closed event
    /// </summary>
    public async Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken token = default)
    {
        ThrowIfDisposed();
        _logger.Debug("Closing WebSocket");
        await _lifecycle.WaitAsync(token);
        try
        {
            if (_ws is null or { State: WebSocketState.CloseSent or WebSocketState.Closed or WebSocketState.Aborted })
            {
                _logger.Debug("WebSocket already closed");
                return;
            }

            await _ws.CloseOutputAsync(closeStatus, statusDescription, token);

            // cancel the receive loop after gracefully closing otherwise the WebSocket will be aborted
            _receiveTokenSource?.Cancel();
            _receiveTokenSource?.Dispose();
            _receiveTokenSource = null;
        }
        catch (ObjectDisposedException)
        {
            // WebSocket may have been aborted and disposed
        }
        catch (OperationCanceledException)
        {
            _logger.Trace("CloseAsync cancelled, aborting");
            _ws?.Abort();
        }
        catch (Exception e)
        {
            _logger.Warn("Failed to close WebSocket, aborting");
            _logger.Warn(e);
            _ws?.Abort();
        }
        finally
        {
            _ws?.Dispose();
            _ws = null;
            _lifecycle.Release();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _lifecycle.Wait();
        try
        {
            var cts = _receiveTokenSource;
            _receiveTokenSource = null;
            cts?.Cancel();
            cts?.Dispose();
            _ws?.Abort(); // we can't gracefully close here
            _ws?.Dispose();
        }
        finally
        {
            _lifecycle.Release();
        }
    }
}
