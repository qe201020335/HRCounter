using System;
using System.Threading.Tasks;
using HRCounter.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using Random = UnityEngine.Random;

namespace HRCounter.Data.BpmDownloaders
{
    internal sealed class HRProxy : BpmDownloader
    {
        private const string URL = "wss://hrproxy.fortnite.lol:2096/hrproxy";

        // private const string PONG = "{\"method\": \"pong\"}";
        
        private string _sessionJson;

        private string _reader = Config.DataSource;

        private string _id;
        private bool _updating;

        private WebSocket _webSocket;

        internal HRProxy()
        {
            RefreshSettings();
        }

        private void RefreshSettings()
        {
            _reader = Config.DataSource;

            if (_reader == "HypeRate")
            {
                _id = Config.HypeRateSessionID;
            }
            else if(_reader == "Pulsoid")
            {
                _id = Config.PulsoidWidgetID;
            }
            else
            {
                _id = Config.HRProxyID;
            }

            JObject _subscribe = new JObject();
            _subscribe["reader"] = _reader;
            _subscribe["identifier"] = _id;
            _subscribe["service"] = "beatsaber";
            
            _sessionJson = _subscribe.ToString();
        }

        internal override void Start()
        {
            _updating = true;
            CreateAndConnectSocket();
        }

        internal override void Stop()
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
            
            logger.Warn("WebSocket is closed. Stopping HR updates");
            Stop();
        }

        private void CreateAndConnectSocket()
        {
            if (_webSocket != null && _webSocket.IsAlive)
            {
                logger.Info("We have an old WebSocket, destroying");
                _webSocket.Close();
                _webSocket = null;
            }
            logger.Info("Creating new WebSocket");
            _webSocket = new WebSocket(URL);
            _webSocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            _webSocket.OnMessage += OnMessageReceive;
            _webSocket.OnError += OnSocketError;
            _webSocket.OnClose += OnSocketClose;
            _webSocket.Connect();
            SendMessage(_sessionJson);
        }

        private async Task Pong(JObject data)
        {
            var delay = Random.Range(30, 15000);
            logger.Debug($"Random delay {delay} ms for pong.");
            await Task.Delay(delay); // random delay between 30 ms and 15 sec
            if (_updating)
            {
                logger.Debug("Pong!");
                SendMessage(data.ToString());
            }
        }

        private void SendMessage(string s)
        {
            logger.Debug($"Trying to send message {s}");
            if (!_updating)
            {
                logger.Debug($"Not updating, no message sent.");
                return;
            }
            if (_webSocket == null || _webSocket.ReadyState == WebSocketState.Closed)
            {
                logger.Critical("WebSocket is null or Closed. Terminating HR Update.");
                logger.Notice("Server unreachable! Does your internet get disconnected?");
                Stop();
            }
            try
            {
                _webSocket?.SendAsync(s, delegate(bool b)
                {
                    if (!b)
                    {
                        logger.Warn("WebSocket failed to send message");
                        Stop();
                    }
                    else
                    {
                        logger.Debug("Message sent successfully");
                    }
                });
            }
            catch (Exception e)
            {
                logger.Error("Error happened when sending message. Terminating HR Update.");
                logger.Error(e.Message);
                logger.Debug(e);
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
            logger.Error(e.Message);
            logger.Debug(e.Exception);
        }
        
        private void OnMessageReceive(object sender, MessageEventArgs e)
        {
            if (sender != _webSocket)
            {
                return;
            }
            
            Logger.DebugSpam(e.Data);

            try
            {
                var json = JObject.Parse(e.Data);

                if (json["method"]?.ToString() == "ping")
                {
                    logger.Debug("Ping!");
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
                logger.Warn("Invalid json received.");
                logger.Warn(e.Data);
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
                    OnHearRateDataReceived(hr);
                }
                else
                {
                    OnHearRateDataReceived(hr, timestamp);
                }
            }
        }
    }
}