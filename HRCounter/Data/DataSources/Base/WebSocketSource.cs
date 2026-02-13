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
    [Inject]
    private readonly Logger _logger = null!;

    private readonly SimpleWebSocketClient _ws = new();
    private CancellationTokenSource? _cts;

    protected abstract string Url { get; }

    protected virtual string UserAgent { get; }

    protected WebSocketSource()
    {
        var meta = Plugin.Instance.Metadata;
        UserAgent = $"{meta.Id}/{meta.HVersion}";
    }

    protected override void Start()
    {
        _logger.Debug("WebSocket data source starting");
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        var cts = new CancellationTokenSource();
        _cts = cts;

        _ws.MessageReceived += OnMessageReceived;
        _ws.Closed += OnWebSocketClosed;

        Task.Run(async () =>
        {
            try
            {
                await Connect(cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.Trace("Websocket connection cancelled");
            }
            catch (Exception e)
            {
                _logger.Error("Failed to connect to WebSocket");
                _logger.Error(e);
            }
        }, cts.Token);
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

    protected virtual Task<bool> PrepareBeforeConnect(CancellationToken token) => Task.FromResult(true);

    protected virtual void ConfigureWebSocket(ClientWebSocketOptions options)
    {
        options.SetRequestHeader("User-Agent", UserAgent);
    }

    protected virtual Task OnWebSocketConnected(CancellationToken token) => Task.CompletedTask;

    protected abstract void OnMessageReceived(WebSocketMessageEventArgs args);

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
        OnWebSocketClosed(args);
        //TODO auto-reconnect
    }

    protected override void Stop()
    {
        _logger.Debug("WebSocket data source stopping");
        _ws.MessageReceived -= OnMessageReceived;
        _ws.Closed -= OnWebSocketClosed;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _ws.Dispose();
    }
}
