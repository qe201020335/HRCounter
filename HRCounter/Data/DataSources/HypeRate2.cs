using System;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Utils;
using HRCounter.Web.WebSocket;
using HRCounter.Web.WebSocket.EventArgs;
using IPA.Loader;
using Newtonsoft.Json.Linq;
using SiraUtil.Zenject;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace HRCounter.Data.DataSources;

internal class HypeRate2 : DataSource
{
    private const string URL = "wss://hrcounter.skyqe.net/proxy/hyperate";
    // private const string URL = "ws://127.0.0.1:8787/proxy/hyperate";

    private const string USER_PLATFORM_HEADER = "X-User-Platform";
    private const string USER_ID_HEADER = "X-User-ID";
    private const string USER_TOKEN_HEADER = "X-User-Token";
    private const string HYPERATE_ID_HEADER = "X-HypeRate-ID";

    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly IPlatformUserModel _platformUserModel = null!;

    private readonly string _userAgent;

    private readonly SimpleWebSocketClient _ws = new();

    private CancellationTokenSource? _cts;

    public HypeRate2(UBinder<Plugin, PluginMetadata> metadataBinder)
    {
        var meta = metadataBinder.Value;
        _userAgent = $"{meta.Id}/{meta.HVersion}";
    }

    private async Task Connect(string hyperateId, CancellationToken token)
    {
        _logger.Info("Creating HypeRate WebSocket connection");
        var userInfo = await _platformUserModel.GetUserInfo(token);
        var authToken = await _platformUserModel.GetUserAuthToken();
        string platform;
        string ticket;
        switch (userInfo.platform)
        {
            case UserInfo.Platform.Steam:
                platform = "steam";
                ticket = authToken.token?.Replace("-", "") ?? "";
                break;
            case UserInfo.Platform.Oculus:
                platform = "oculus";
                ticket = authToken.token ?? "";
                break;
            default:
                _logger.Notice($"Unknown platform: {userInfo.platform}");
                return;
        }

        await _ws.ConnectAsync(new Uri(URL), token, options =>
        {
            // options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.SetRequestHeader("User-Agent", _userAgent);
            options.SetRequestHeader(USER_ID_HEADER, userInfo.platformUserId);
            options.SetRequestHeader(USER_PLATFORM_HEADER, platform);
            options.SetRequestHeader(USER_TOKEN_HEADER, ticket);
            options.SetRequestHeader(HYPERATE_ID_HEADER, hyperateId);
        });
        await _ws.StartReceive(token);
    }

    protected override void Start()
    {
        if (string.IsNullOrWhiteSpace(Config.HypeRateSessionID))
        {
            _logger.Warn("HypeRate Session ID is not set, not starting HypeRate data source");
            return;
        }

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
                await Connect(Config.HypeRateSessionID, cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.Trace("HypeRate connection cancelled");
            }
            catch (Exception e)
            {
                _logger.Error("Failed to connect to HypeRate WebSocket");
                _logger.Error(e);
            }
        }, cts.Token);
    }

    private void OnMessageReceived(object sender, WebSocketMessageEventArgs args)
    {
        _logger.Spam(args.Message);
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

    private void OnWebSocketClosed(object sender, WebSocketClosedEventArgs args)
    {
        _logger.Warn($"HypeRate WebSocket closed: {args.CloseStatus} - {args.CloseStatusDescription}");
        //TODO auto-reconnect
    }

    protected override void Stop()
    {
        _logger.Info("Stopping HypeRate data source");
        _ws.MessageReceived -= OnMessageReceived;
        _ws.Closed -= OnWebSocketClosed;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _ws.Dispose();
    }
}
