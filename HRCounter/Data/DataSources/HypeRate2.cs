using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Data.DataSources.Base;
using HRCounter.Web.WebSocket.EventArgs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OculusStudios.Platform.Core;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace HRCounter.Data.DataSources;

internal class HypeRate2 : WebSocketSource
{
    private const string URL = "wss://hrcounter.skyqe.net/proxy/hyperate";
    // private const string URL = "ws://127.0.0.1:8787/proxy/hyperate";

    private const string USER_PLATFORM_HEADER = "X-User-Platform";
    private const string USER_ID_HEADER = "X-User-ID";
    private const string USER_TOKEN_HEADER = "X-User-Token";
    private const string HYPERATE_ID_HEADER = "X-HypeRate-ID";

    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly IPlatform _platformModel = null!;

    protected override string Url => URL;

    private string _platform = "";
    private string _userId = "";
    private string _ticket = "";
    private string _hyperateId = "";

    protected override bool Validate()
    {
        if (string.IsNullOrWhiteSpace(_config.HypeRateSessionID))
        {
            _logger.Warn("HypeRate Session ID not set");
            return false;
        }

        _hyperateId = _config.HypeRateSessionID;
        return true;
    }

    protected override async Task<bool> PrepareBeforeConnect(CancellationToken token)
    {
        var userInfo = _platformModel.user;
        var authToken = await userInfo.GetAccessTokenAsync() ?? "";
        _userId = userInfo.userId.ToString();
        switch (_platformModel.vendor)
        {
            case Vendor.Valve:
                _platform = "steam";
                _ticket = authToken.Replace("-", "");
                break;
            case Vendor.Meta:
                _platform = "oculus";
                _ticket = authToken;
                break;
            default:
                _logger.Notice($"Unsupported platform: {_platformModel.vendor}");
                return false;
        }

        return true;
    }

    protected override void ConfigureWebSocket(ClientWebSocketOptions options)
    {
        base.ConfigureWebSocket(options);
        // options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.SetRequestHeader(USER_ID_HEADER, _userId);
        options.SetRequestHeader(USER_PLATFORM_HEADER, _platform);
        options.SetRequestHeader(USER_TOKEN_HEADER, _ticket);
        options.SetRequestHeader(HYPERATE_ID_HEADER, _hyperateId);
    }

    protected override void OnMessageReceived(WebSocketMessageEventArgs args)
    {
        try
        {
            //{"topic":"hr:6956","event":"hr_update","payload":{"hr":88},"ref":null}
            var obj = JObject.Parse(args.Message);
            if (obj["event"]?.ToString() == "hr_update")
            {
                var hr = obj["payload"]?["hr"]?.ToObject<int>();
                if (hr != null)
                {
                    OnHeartRateDataReceived(hr.Value);
                }
                else
                {
                    _logger.Warn("HypeRate message missing hr field");
                    _logger.Debug(args.Message);
                }
            }
        }
        catch (JsonException e)
        {
            _logger.Warn("Failed to parse HypeRate json message");
            _logger.Warn(e);
            _logger.Debug(args.Message);
        }
        catch (Exception e)
        {
            _logger.Warn("Failed to handle HypeRate message");
            _logger.Warn(e);
        }
    }
}
