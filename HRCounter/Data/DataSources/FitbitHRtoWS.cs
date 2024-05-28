using System;
using System.Threading;
using WebSocketSharp;

namespace HRCounter.Data.DataSources
{
    internal sealed class FitbitHRtoWS : DataSource
    {
        private Thread? _worker;

        private WebSocket? _webSocket;
        private string _url = string.Empty;
        private bool _updating;
        
        private int _lastHR;
        private bool isDeviceConnected;

        internal FitbitHRtoWS() => RefreshSettings();

        private bool IsSocketSecure(string url)
        {
            bool btr = false;
            try
            {
                string[] colonSplit = url.Split(':');
                btr = colonSplit[0] == "wss";
            }
            catch(Exception)
            {
                Logger.Warn("WebSocket URI is not valid! Assuming insecure.");
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

        protected override void Start()
        {
            // Start Thread
            VerifyDeadThread();
            _worker = new Thread(() =>
            {
                if (_webSocket != null)
                {
                    // WebSocket is listening. Stopping it then continue.
                    Stop();
                }
                _webSocket = new WebSocket(_url);
                if (IsSocketSecure(_url))
                    _webSocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                _webSocket.OnClose += WebSocket_OnClose;
                _webSocket.OnMessage += WebSocket_OnMessage;
                _webSocket.Connect();
                _updating = true;
                while (_updating)
                {
                    if (_webSocket != null)
                        if (_webSocket.ReadyState == WebSocketState.Open)
                        {
                            _webSocket.Send("getHR");
                            _webSocket.Send("checkFitbitConnection");
                        }
                        else
                            Logger.Warn("Failed to connect to WebSocket. Is it running?");
                    else
                        Logger.Error("WebSocket is null!");
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
                    try { _lastHR = Convert.ToInt32(e.Data); } catch (Exception) { }
                    break;
            }
            OnHeartRateDataReceived(_lastHR);
        }

        protected override void Stop()
        {
            if(_webSocket != null)
            {
                if (_webSocket.ReadyState == WebSocketState.Open)
                    _webSocket.Close();
                _webSocket = null;
            }
            _updating = false;
            VerifyDeadThread();
        }
    }
}
