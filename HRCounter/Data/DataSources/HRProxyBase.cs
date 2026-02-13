using System;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Data.DataSources.Base;
using HRCounter.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using Zenject;
using Logger = IPA.Logging.Logger;
using Random = UnityEngine.Random;

namespace HRCounter.Data.DataSources;

internal abstract class HRProxyBase : DataSource
{
    private const string URL = "wss://hrproxy.fortnite.lol:2096/hrproxy";

    // private const string PONG = "{\"method\": \"pong\"}";

    [Inject]
    protected readonly PluginConfig Config = null!;

    [Inject]
    private readonly Logger _logger = null!;

    protected abstract string ReaderName { get; }

    protected abstract string EventIdentifier { get; }

    private string SubscribeJson
    {
        get
        {
            var o = new JObject
            {
                new JProperty("reader", ReaderName),
                new JProperty("identifier", EventIdentifier),
                new JProperty("service", "beatsaber")
            };

            return o.ToString();
        }
    }

    private bool _updating;

    private WebSocket? _webSocket;

    protected override void Start()
    {
        _updating = true;
        CreateAndConnectSocket();
    }

    protected override void Stop()
    {
        _updating = false;
        _webSocket?.CloseAsync();
        _webSocket = null;
    }

    private void OnSocketClose(object sender, CloseEventArgs e)
    {
        if (sender != _webSocket)
        {
            return;
        }

        if (!_updating)
        {
            // we are not updating anyways.
            return;
        }

        _logger.Warn("WebSocket is closed. Stopping HR updates");
        Stop();
    }

    private void CreateAndConnectSocket()
    {
        if (_webSocket != null && _webSocket.IsAlive)
        {
            _logger.Info("We have an old WebSocket, destroying");
            _webSocket.Close();
            _webSocket = null;
        }

        _logger.Info("Creating new WebSocket");
        _webSocket = new WebSocket(URL);
        _webSocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
        _webSocket.OnMessage += OnMessageReceive;
        _webSocket.OnError += OnSocketError;
        _webSocket.OnClose += OnSocketClose;
        _webSocket.Connect();
        SendMessage(SubscribeJson);
    }

    private async void Pong(JObject data)
    {
        var delay = Random.Range(30, 15000);
        _logger.Debug($"Random delay {delay} ms for pong.");
        await Task.Delay(delay); // random delay between 30 ms and 15 sec
        if (_updating)
        {
            _logger.Debug("Pong!");
            SendMessage(data.ToString());
        }
    }

    private void SendMessage(string s)
    {
        _logger.Debug($"Trying to send message {s}");
        if (!_updating)
        {
            _logger.Debug($"Not updating, no message sent.");
            return;
        }

        if (_webSocket == null || _webSocket.ReadyState == WebSocketState.Closed)
        {
            _logger.Critical("WebSocket is null or Closed. Terminating HR Update.");
            _logger.Notice("Server unreachable! Does your internet get disconnected?");
            Stop();
        }

        try
        {
            _webSocket?.SendAsync(s, delegate(bool b)
            {
                if (!b)
                {
                    _logger.Warn("WebSocket failed to send message");
                    Stop();
                }
                else
                {
                    _logger.Debug("Message sent successfully");
                }
            });
        }
        catch (Exception e)
        {
            _logger.Error("Error happened when sending message. Terminating HR Update.");
            _logger.Error(e.Message);
            _logger.Debug(e);
            Stop();
        }
    }

    private void OnSocketError(object sender, ErrorEventArgs e)
    {
        if (sender != _webSocket)
        {
            return;
        }

        Stop();
        _logger.Error(e.Message);
        _logger.Debug(e.Exception);
    }

    private void OnMessageReceive(object sender, MessageEventArgs e)
    {
        if (sender != _webSocket)
        {
            return;
        }

        _logger.Spam(e.Data);

        try
        {
            var json = JObject.Parse(e.Data);

            if (json["method"]?.ToString() == "ping")
            {
                _logger.Debug("Ping!");
                json["method"] = "pong";
                Pong(json);
            }
            else
            {
                // {"reader": "","identifier": "","hr": "0","timestamp": "0"}
                UpdateHR(json);
            }
        }
        catch (JsonReaderException)
        {
            _logger.Warn("Invalid json received.");
            _logger.Warn(e.Data);
        }
    }

    private void UpdateHR(JObject json)
    {
        if (json["hr"] != null)
        {
            var hr = json["hr"].ToObject<int>();
            var timestamp = json["timestamp"]?.ToObject<string>();
            if (timestamp == null)
            {
                OnHeartRateDataReceived(hr);
            }
            else
            {
                OnHeartRateDataReceived(hr, timestamp);
            }
        }
    }
}
