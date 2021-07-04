using System;
using SystemObject = System.Object;
using System.Collections;
using HRCounter.Configuration;
using IPALogger = IPA.Logging.Logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using UnityEngine;

namespace HRCounter.Data
{
    internal sealed class HypeRate : BpmDownloader
    {
        // Register: {"topic": "hr:1234","event": "phx_join","payload": {},"ref": 0}
        // HeartBeat: "{"topic": "phoenix","event": "heartbeat","payload": {},"ref": 123456}"
        
        private const string URL = "wss://app.hyperate.io/socket/websocket";

        private const string HeartBeatJson =
            "{\"topic\": \"phoenix\",\"event\": \"heartbeat\",\"payload\": {},\"ref\": 123456}";
        private string _sessionJson;

        private bool _logHr = PluginConfig.Instance.LogHR;
        private string _sessionID = PluginConfig.Instance.HypeRateSessionID;
        private bool _updating;

        private WebSocket _webSocket;

        internal HypeRate()
        {
            RefreshSettings();
        }

        protected override void RefreshSettings()
        {
            _logHr = PluginConfig.Instance.LogHR;
            _sessionID = PluginConfig.Instance.HypeRateSessionID;

            _sessionJson = "{\"topic\": \"hr:" + _sessionID + "\",\"event\": \"phx_join\",\"payload\": {},\"ref\": 0}";
        }

        internal override void Start()
        {
            _updating = true;
            CreateAndConnectSocket();
            SharedCoroutineStarter.instance.StartCoroutine(HeartBeating());
        }

        internal override void Stop()
        {
            _updating = false;
            _webSocket?.Close();
            _webSocket = null;
        }

        private void CreateAndConnectSocket()
        {
            if (_webSocket == null || _webSocket.ReadyState == WebSocketState.Closed ||
                _webSocket.ReadyState != WebSocketState.Closing)
            {
                logger.Debug("Creating WebSocket");
                _webSocket = new WebSocket(URL);
                _webSocket.OnMessage += OnMessageReceive;
                _webSocket.Connect();
                _webSocket.Send(_sessionJson);
            }
        }

        private IEnumerator HeartBeating()
        {
            logger.Info("WebSocket HeartBeating!");
            while (_updating)
            {
                SendMessage(HeartBeatJson);
                yield return new WaitForSecondsRealtime(15);
            }
        }

        private void SendMessage(string s)
        {
            _webSocket?.Send(s);
        }
        
        private void OnMessageReceive(SystemObject sender, MessageEventArgs e)
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
                JObject json = JObject.Parse(e.Data);
                
                if (json["event"] != null && json["event"].ToObject<string>() == "hr_update")
                {
                    // {"event":"hr_update","payload":{"hr":89,"id":"2340"},"ref":null,"topic":"hr:2340"}
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
            if (json["payload"] != null && json["payload"]["hr"] != null)
            {
                Bpm.Bpm = json["payload"]["hr"].ToObject<int>();
                Bpm.ReceivedAt = DateTime.Now.ToString("HH:mm:ss");
            }
        }
    }
}