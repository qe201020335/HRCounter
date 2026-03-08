using System;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Data.DataSources.Base;
using HRCounter.Utils;
using HRCounter.Web.WebSocket.EventArgs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace HRCounter.Data.DataSources;

internal abstract class HRProxyBase : WebSocketSource
{
    private const string URL = "wss://hrproxy.fortnite.lol:2096/hrproxy";

    [Inject]
    protected readonly PluginConfig Config = null!;

    [Inject(Id = typeof(HRProxyBase))]
    private readonly Logger _logger = null!;

    private readonly Random _random = new();

    protected override string Url => URL;

    protected abstract string ReaderName { get; }
    protected abstract string ConfigName { get; }
    protected abstract string EventIdentifier { get; }

    protected override bool Validate()
    {
        if (string.IsNullOrWhiteSpace(EventIdentifier))
        {
            _logger.Warn($"{ConfigName} not set");
            return false;
        }

        return true;
    }

    protected override Task OnWebSocketConnected(CancellationToken token)
    {
        // reader subscribe
        var sub = $$"""{"reader": "{{ReaderName}}","identifier": "{{EventIdentifier}}","service": "beatsaber"}""";
        return SendWebSocketMessageAsync(sub, token);
    }

    private async Task Pong(string data, CancellationToken token)
    {
        var delay = _random.Next(100, 15000); // random delay between 100 ms and 15 sec
        _logger.Spam($"Random delay {delay} ms for pong.");
        await Task.Delay(delay, token);
        _logger.Trace("Pong!");
        await SendWebSocketMessageAsync(data, token);
    }

    protected override void OnMessageReceived(WebSocketMessageEventArgs args)
    {
        try
        {
            var json = JObject.Parse(args.Message);
            int? hr;
            if (json["method"]?.ToString() == "ping")
            {
                // {"method": "ping","pingId": "xyz"}
                var id = json["pingId"]?.ToObject<string?>();
                _logger.Trace("Ping!");
                var pong = $$"""{"method": "pong","pingId": "{{id}}"}""";
                _ = Pong(pong, CToken);
            }
            else if ((hr = json["hr"]?.ToObject<int?>()) != null)
            {
                // {"reader": "","identifier": "","hr": "0","timestamp": "0"}
                var timestamp = json["timestamp"]?.ToObject<string>();
                OnHeartRateDataReceived(hr.Value, timestamp);
            }
            else
            {
                _logger.Warn("Unsupported message received");
                _logger.Debug(args.Message);
            }
        }
        catch (JsonException e)
        {
            _logger.Warn("Failed to parse HRProxy json message");
            _logger.Warn(e);
            _logger.Debug(args.Message);
        }
        catch (Exception e)
        {
            _logger.Warn("Failed to handle HRProxy message");
            _logger.Warn(e);
        }
    }
}
