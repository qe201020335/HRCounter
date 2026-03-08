using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Utils;
using HRCounter.Web.WebSocket;
using HRCounter.Web.WebSocket.EventArgs;
using IPA.Logging;
using Zenject;

namespace HRCounter.Data.DataSources.Base;

public abstract class WebSocketSource : DataSource
{
    [Inject(Id = typeof(WebSocketSource))]
    private readonly Logger _logger = null!;

    private readonly SimpleWebSocketClient _ws = new();
    private readonly CancellationTokenSource _cts = new();
    private int _running = 0;

    protected abstract string Url { get; }

    protected virtual string UserAgent { get; }

    protected CancellationToken CToken => _cts.Token;

    protected WebSocketSource()
    {
        var meta = Plugin.Instance.Metadata;
        UserAgent = $"{meta.Id}/{meta.HVersion}";
    }

    protected override void Start()
    {
        if (!Validate())
        {
            _logger.Warn("Validation failed, not starting data source");
        }

        if (Interlocked.Exchange(ref _running, 1) == 1)
        {
            _logger.Warn("WebSocket data source already running");
            return;
        }

        _logger.Debug("WebSocket data source starting");

        _ws.MessageReceived += OnMessageReceived;
        _ws.Closed += OnWebSocketClosed;

        Task.Run(async () =>
        {
            try
            {
                await Connect(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.Trace("Websocket connection cancelled");
            }
            catch (Exception e)
            {
                _logger.Error("Failed to connect to WebSocket");
                _logger.Error(e);
                _ = TryReconnect(true, _cts.Token);
            }
        }, _cts.Token);
    }

    private async Task Connect(CancellationToken token)
    {
        _logger.Info("Creating WebSocket connection");
        if (!await PrepareBeforeConnect(token))
        {
            _logger.Warn("Data source preparation did not succeed, not connecting");
            return;
        }

        await _ws.ConnectAsync(Url, token, ConfigureWebSocket);
        await _ws.StartReceive(token);
        await OnWebSocketConnected(token);
    }

    protected virtual bool Validate() => true;
    protected virtual Task<bool> PrepareBeforeConnect(CancellationToken token) => Task.FromResult(true);

    protected virtual void ConfigureWebSocket(ClientWebSocketOptions options)
    {
        options.SetRequestHeader("User-Agent", UserAgent);
    }

    protected async Task SendWebSocketMessageAsync(string message, CancellationToken token)
    {
        _logger.Spam("Sending message: " + message);
        if (Volatile.Read(ref _running) == 0)
        {
            _logger.Debug("Data source not running, not sending message");
            // not running
            return;
        }

        if (_ws.State != WebSocketState.Open)
        {
            //TODO reconnect?
            // await TryReconnect(false, token);
            return;
        }

        try
        {
            await _ws.SendMessageAsync(message, token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            _logger.Error("Failed to send WebSocket message");
            _logger.Error(e);
            await TryReconnect(true, token);
        }
    }

    protected virtual Task OnWebSocketConnected(CancellationToken token) => Task.CompletedTask;

    protected abstract void OnMessageReceived(WebSocketMessageEventArgs args);

    /// <returns>True if it should reconnect</returns>
    protected virtual bool OnWebSocketClosed(WebSocketClosedEventArgs args) => true;

    private void OnMessageReceived(object sender, WebSocketMessageEventArgs args)
    {
        if (sender != _ws)
        {
            return;
        }

        _logger.Spam(args.Message);
        OnMessageReceived(args);
    }

    private void OnWebSocketClosed(object sender, WebSocketClosedEventArgs args)
    {
        if (sender != _ws)
        {
            return;
        }

        _logger.Warn($"WebSocket closed: {args.CloseStatus} - {args.CloseStatusDescription}");
        if (OnWebSocketClosed(args))
        {
            _ = TryReconnect(false, _cts.Token);
        }
    }

    private async Task TryReconnect(bool wasError, CancellationToken token)
    {
        if (Volatile.Read(ref _running) == 0)
        {
            // not running
            return;
        }

        //TODO reconnect with exponential backoff
        Stop();
    }

    protected override void Stop()
    {
        if (Interlocked.Exchange(ref _running, 0) == 0)
        {
            return;
        }

        _logger.Debug("WebSocket data source stopping");
        _ws.MessageReceived -= OnMessageReceived;
        _ws.Closed -= OnWebSocketClosed;
        _cts.Cancel();
        _cts.Dispose();
        _ws.Dispose();
    }
}
