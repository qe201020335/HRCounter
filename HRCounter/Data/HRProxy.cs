using System;
using System.Collections;
using System.Threading.Tasks;
using HRCounter.Configuration;
using IPALogger = IPA.Logging.Logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HRCounter.Data
{
    internal sealed class HRProxy : BpmDownloader
    {
         // Register: {"topic": "hr:1234","event": "phx_join","payload": {},"ref": 0}
        // HeartBeat: "{"topic": "phoenix","event": "heartbeat","payload": {},"ref": 123456}"
        
        private const string URL = "wss://hrproxy.fortnite.lol:2096/hrproxy";

        // private const string PONG = "{\"method\": \"pong\"}";
        
        private string _sessionJson;

        private string _reader = PluginConfig.Instance.DataSource;

        private bool _logHr = PluginConfig.Instance.LogHR;
        private string _id;
        private bool _updating;

        private WebSocket _webSocket;

        internal HRProxy()
        {
            RefreshSettings();
        }

        protected override void RefreshSettings()
        {
            _logHr = PluginConfig.Instance.LogHR;
            _reader = PluginConfig.Instance.DataSource;

            if (_reader == "HypeRate")
            {
                _id = PluginConfig.Instance.HypeRateSessionID;
            }
            else
            {
                _id = PluginConfig.Instance.PulsoidWidgetID;
            }

            JObject _subscribe = new JObject();
            _subscribe["reader"] = _reader;
            _subscribe["identifier"] = _id;
            
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
            _webSocket.Connect();
            SendMessage(_sessionJson);
        }

        private async Task Pong(JObject data)
        {
            await Task.Delay(Random.Range(30, 15000)); // random delay between 30 ms and 15 sec
            if (_updating)
            {
                logger.Debug("Pong!");
                SendMessage(data.ToString());
            }
        }

        private void SendMessage(string s)
        {
            logger.Debug($"Trying to send message {s}");
            if (_webSocket == null || _webSocket.ReadyState == WebSocketState.Closed)
            {
                logger.Critical("WebSocket is null or Closed. Terminating HR Update.");
                logger.Notice("Does your internet get disconnected?");
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

        private void OnSocketError(System.Object sender, ErrorEventArgs e)
        {
            if (sender != _webSocket)
            {
                return;
            }
            Stop();
            logger.Error(e.Message);
            logger.Debug(e.Exception);
        }
        
        private void OnMessageReceive(System.Object sender, MessageEventArgs e)
        {
            if (sender != _webSocket)
            {
                return;
            }

#if DEBUG
         
            logger.Debug(e.Data);
#endif        

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
                logger.Critical("Invalid json received.");
                logger.Critical(e.Data);
            }
        }

        private void UpdateHR(JObject json)
        {
            if (json["hr"] != null)
            {
                Bpm.Bpm = json["hr"].ToObject<int>();
                Bpm.ReceivedAt = json["timestamp"]?.ToString() ?? DateTime.Now.ToString("HH:mm:ss");
            }
            
            if (_logHr)
            {
                logger.Info(Bpm.ToString());
            }
        }
    }
}