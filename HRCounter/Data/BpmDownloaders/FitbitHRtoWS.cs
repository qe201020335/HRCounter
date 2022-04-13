using System;
using System.Threading;
using HRCounter.Configuration;
using WebSocketSharp;

namespace HRCounter.Data.BpmDownloaders
{
    internal sealed class FitbitHRtoWS : BpmDownloader
    {
        private Thread _worker = null;

        private WebSocket webSocket = null;
        private string _url = string.Empty;
        private bool _updating = false;
        
        private int lastHR = 0;
        private bool isDeviceConnected = false;

        internal FitbitHRtoWS() => RefreshSettings();

        private bool isSocketSecure(string url)
        {
            bool btr = false;
            try
            {
                string[] colonSplit = url.Split(':');
                btr = colonSplit[0] == "wss";
            }
            catch(Exception)
            {
                logger.Warn("WebSocket URI is not valid! Assuming insecure.");
            }

            return btr;
        }

        private void VerifyDeadThread()
        {
            if (_worker != null)
                if (_worker.IsAlive)
                    _worker.Abort();
            _worker = null;
        }

        internal override void Start()
        {
            // Start Thread
            VerifyDeadThread();
            _worker = new Thread(() =>
            {
                if (webSocket != null)
                {
                    // WebSocket is listening. Stopping it then continue.
                    Stop();
                }
                webSocket = new WebSocket(_url);
                if (isSocketSecure(_url))
                    webSocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                webSocket.OnClose += WebSocket_OnClose;
                webSocket.OnMessage += WebSocket_OnMessage;
                webSocket.Connect();
                _updating = true;
                while (_updating)
                {
                    if (webSocket != null)
                        if (webSocket.ReadyState == WebSocketState.Open)
                        {
                            webSocket.Send("getHR");
                            webSocket.Send("checkFitbitConnection");
                        }
                        else
                            logger.Warn("Failed to connect to WebSocket. Is it running?");
                    else
                        logger.Error("WebSocket is null!");
                    Thread.Sleep(1000);
                }
            });
            _worker.Start();
        }

        private void RefreshSettings()
        {
            _url = Config.FitbitWebSocket;
        }

        private void WebSocket_OnClose(object sender, CloseEventArgs e) => Stop();

        private void WebSocket_OnMessage(object sender, MessageEventArgs e)
        {
            switch (e.Data)
            {
                case "yes":
                    // This means the Fitbit is connected to the WebSocket
                    // You are free to do whatever you want with this here if you want
                    isDeviceConnected = true;
                    break;
                case "no":
                    // This means the Fitbit is not connected to the WebSocket
                    // You are free to do whatever you want with this here if you want
                    isDeviceConnected = false;
                    break;
                default:
                    // Assume it's the HeartRate
                    try { lastHR = Convert.ToInt32(e.Data); } catch (Exception) { }
                    break;
            }
            OnHearRateDataReceived(lastHR);
        }

        internal override void Stop()
        {
            if(webSocket != null)
            {
                if (webSocket.ReadyState == WebSocketState.Open)
                    webSocket.Close();
                webSocket = null;
            }
            _updating = false;
            VerifyDeadThread();
        }
    }
}
