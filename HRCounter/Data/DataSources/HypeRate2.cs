using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Data.DataSources.Base;
using HRCounter.Web.WebSocket.EventArgs;
using Newtonsoft.Json.Linq;
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
    private readonly IPlatformUserModel _platformUserModel = null!;

    protected override string Url => URL;

    private string _platform = "";
    private string _userId = "";
    private string _ticket = "";
    private string _hyperateId = "";

    protected override void Start()
    {
        if (string.IsNullOrWhiteSpace(_config.HypeRateSessionID))
        {
            _logger.Warn("HypeRate Session ID is not set, not starting HypeRate data source");
            return;
        }

        _hyperateId = _config.HypeRateSessionID;
        base.Start();
    }

    protected override async Task<bool> PrepareBeforeConnect(CancellationToken token)
    {
        var userInfo = await _platformUserModel.GetUserInfo(token);
        var authToken = await _platformUserModel.GetUserAuthToken();
        _userId = userInfo.platformUserId;
        switch (userInfo.platform)
        {
            case UserInfo.Platform.Steam:
                _platform = "steam";
                _ticket = authToken.token?.Replace("-", "") ?? "";
                break;
            case UserInfo.Platform.Oculus:
                _platform = "oculus";
                _ticket = authToken.token ?? "";
                break;
            default:
                _logger.Notice($"Unsupported platform: {userInfo.platform}");
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
        catch (Exception e)
        {
            _logger.Warn("Failed to parse HypeRate message");
            _logger.Warn(e);
        }
    }
}
