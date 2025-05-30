﻿using System;
using System.Threading.Tasks;
using HRCounter.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using Random = UnityEngine.Random;

namespace HRCounter.Data.DataSources;

internal abstract class HRProxyBase : DataSource
{
    private const string URL = "wss://hrproxy.fortnite.lol:2096/hrproxy";

    // private const string PONG = "{\"method\": \"pong\"}";

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

        Logger.Warn("WebSocket is closed. Stopping HR updates");
        Stop();
    }

    private void CreateAndConnectSocket()
    {
        if (_webSocket != null && _webSocket.IsAlive)
        {
            Logger.Info("We have an old WebSocket, destroying");
            _webSocket.Close();
            _webSocket = null;
        }

        Logger.Info("Creating new WebSocket");
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
        Logger.Debug($"Random delay {delay} ms for pong.");
        await Task.Delay(delay); // random delay between 30 ms and 15 sec
        if (_updating)
        {
            Logger.Debug("Pong!");
            SendMessage(data.ToString());
        }
    }

    private void SendMessage(string s)
    {
        Logger.Debug($"Trying to send message {s}");
        if (!_updating)
        {
            Logger.Debug($"Not updating, no message sent.");
            return;
        }

        if (_webSocket == null || _webSocket.ReadyState == WebSocketState.Closed)
        {
            Logger.Critical("WebSocket is null or Closed. Terminating HR Update.");
            Logger.Notice("Server unreachable! Does your internet get disconnected?");
            Stop();
        }

        try
        {
            _webSocket?.SendAsync(s, delegate(bool b)
            {
                if (!b)
                {
                    Logger.Warn("WebSocket failed to send message");
                    Stop();
                }
                else
                {
                    Logger.Debug("Message sent successfully");
                }
            });
        }
        catch (Exception e)
        {
            Logger.Error("Error happened when sending message. Terminating HR Update.");
            Logger.Error(e.Message);
            Logger.Debug(e);
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
        Logger.Error(e.Message);
        Logger.Debug(e.Exception);
    }

    private void OnMessageReceive(object sender, MessageEventArgs e)
    {
        if (sender != _webSocket)
        {
            return;
        }

        Logger.Logger!.Spam(e.Data);

        try
        {
            var json = JObject.Parse(e.Data);

            if (json["method"]?.ToString() == "ping")
            {
                Logger.Debug("Ping!");
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
            Logger.Warn("Invalid json received.");
            Logger.Warn(e.Data);
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
