using HRCounter.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WebSocketSharp;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter.Data
{
    internal sealed class FitbitHRtoWS : BpmDownloader
    {
        private WebSocket webSocket = null;
        private string _url = string.Empty;
        private bool _updating = false;

        private bool _logHr = PluginConfig.Instance.LogHR;

        private int lastHR = 0;
        private bool isFitbitConnected = false;

        internal FitbitHRtoWS() => RefreshSettings();

        internal override void Start()
        {
            if(webSocket != null)
            {
                // WebSocket is listening. Stopping it then continue.
                Stop();
            }
            webSocket = new WebSocket(_url);
            webSocket.OnClose += WebSocket_OnClose;
            webSocket.OnMessage += WebSocket_OnMessage;
            webSocket.Connect();
            if (webSocket.IsAlive)
            {
                _updating = true;
                SharedCoroutineStarter.instance.StartCoroutine(UpdateStuff());
            }
            else
                logger.Warn("Failed to connect to WebSocket. Is it running?");
        }

        protected override void RefreshSettings()
        {
            _logHr = PluginConfig.Instance.LogHR;
            _url = PluginConfig.Instance.FitbitWebSocket;
        }

        private IEnumerator UpdateStuff()
        {
            while (_updating)
            {
                if (webSocket != null)
                {
                    if (webSocket.IsAlive)
                    {
                        webSocket.Send("getHR");
                        webSocket.Send("checkFitbitConnection");
                        yield return new WaitForSeconds(1);
                    }
                }
            }
        }

        private void WebSocket_OnClose(object sender, CloseEventArgs e)
        {
            _updating = false;
        }

        private void WebSocket_OnMessage(object sender, MessageEventArgs e)
        {
            switch (e.Data)
            {
                case "yes":
                    // This means the Fitbit is connected to the WebSocket
                    // You are free to do whatever you want with this here if you want
                    isFitbitConnected = true;
                    break;
                case "no":
                    // This means the Fitbit is not connected to the WebSocket
                    // You are free to do whatever you want with this here if you want
                    isFitbitConnected = false;
                    break;
                default:
                    // Assume it's the HeartRate
                    try { lastHR = Convert.ToInt32(e.Data); } catch (Exception) { }
                    break;
            }
            Bpm.Bpm = lastHR;
        }

        internal override void Stop()
        {
            if(webSocket != null)
            {
                if (webSocket.IsAlive)
                    webSocket.Close();
                webSocket = null;
            }
            _updating = false;
        }
    }
}
