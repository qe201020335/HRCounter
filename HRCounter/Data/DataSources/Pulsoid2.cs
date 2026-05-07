using System.Net.WebSockets;
using HRCounter.Configuration;
using HRCounter.Data.DataSources.Base;
using HRCounter.Web.WebSocket.EventArgs;
using IPA.Logging;
using Zenject;

namespace HRCounter.Data.DataSources;

public class Pulsoid2 : WebSocketSource
{
    private const string URL = "wss://pulsoid.net/api/v1/data/real_time?response_mode=text_plain_only_heart_rate";

    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly Logger _logger = null!;

    protected override string Url => URL;

    protected override bool Validate()
    {
        if (string.IsNullOrWhiteSpace(_config.PulsoidToken))
        {
            _logger.Warn("PulsoidToken not set");
            return false;
        }

        return true;
    }

    protected override void ConfigureWebSocket(ClientWebSocketOptions options)
    {
        base.ConfigureWebSocket(options);
        options.SetRequestHeader("Authorization", $"Bearer {_config.PulsoidToken}");
    }

    protected override void OnMessageReceived(WebSocketMessageEventArgs args)
    {
        if (int.TryParse(args.Message, out var hr))
        {
            OnHeartRateDataReceived(hr);
        }
        else
        {
            _logger.Warn($"Failed to parse hr data: {args.Message}");
        }
    }
}
