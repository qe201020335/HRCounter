using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace HRCounter.Data.BpmDownloaders
{
    internal sealed class YURApp: BpmDownloader
    {
        private const string HOST = "127.0.0.1";
        private const int PORT = 11010;
        private TcpClient _client = null;

        private bool _running = false;
        
        private Thread _worker;

        protected override void RefreshSettings() {}

        internal override void Start()
        {
            _running = true;
            if (_client != null)
            {
                logger.Info("We have an old tcp client, destroying");
                CloseClientMakeNull();
            }
            logger.Info("Creating new tcp connection");
            _client = new TcpClient();
            _worker = new Thread(StartClient);
            _worker.Start();

        }

        private void StartClient()
        {
            try
            {
                _client.ConnectAsync(HOST, PORT).Wait();
            }
            catch (Exception e)
            {
                logger.Warn("Cannot connect to YUR app");
                logger.Warn(e.Message);
                logger.Debug(e);
                Stop();
                return;
            }

            try
            {
                ReadMessage().Wait();
            }
            catch (Exception e)
            {
                logger.Critical("Exception occured while receiving data");
                logger.Critical(e.Message);
                logger.Debug(e);
                Stop();
            }
        }


        private async Task ReadMessage()
        {
            while (_running)
            {
                if (!_client.Connected)
                {
                    return;
                }

                try
                {
                    // steam operations feel a lot like c...
                    // first get message type
                    var type = new byte[1];
                    if (await _client.GetStream().ReadAsync(type, 0, 1) < 1)
                    {
                        throw new Exception("Cannot read message type");
                    }

                    if (type[0] == 1)
                    {
                        // ping message
                        Logger.DebugSpam("Ping!");
                        await Pong();
                    }
                    else if (type[0] == 20)
                    {
                        // we have data
                        byte[] dataBytes;
                        using (var reader = new BinaryReader(_client.GetStream(), Encoding.UTF8, true))
                        {
                            var messageSize = reader.ReadInt32();
                            dataBytes = reader.ReadBytes(messageSize);
                        }

                        HandleData(Encoding.UTF8.GetString(dataBytes));
                    }
                }
                catch (ObjectDisposedException e)
                {
                    logger.Warn("tcp client is not connected anymore");
                    return;
                }
                catch (Exception e)
                {
                    
                    logger.Critical("Exception occured while reading data");
                    logger.Critical(e.Message);
                    logger.Debug(e);
                    return;
                }
                
            }
        }


        private async Task SendMessage(byte type, string data)
        {
            byte[] dataArray;
            if (string.IsNullOrEmpty(data))
            {
                dataArray = new[] { type };
            }
            else
            {
                var dataBytes = Encoding.UTF8.GetBytes(data);

                var countBytes = BitConverter.GetBytes(dataBytes.Length);

                dataArray = new byte[dataBytes.Length + 5];
                
                countBytes.CopyTo(dataArray, 1);
                dataBytes.CopyTo(dataArray, 5);
            }

            dataArray[0] = type;

            using (var writer = new BinaryWriter(_client.GetStream(), Encoding.UTF8, true))
            {
                writer.Write(dataArray);
                writer.Flush();
            }
            await _client.GetStream().FlushAsync();
        }

        private void HandleData(string data)
        {
            
            Logger.DebugSpam(data);

            try
            {
                var json = JObject.Parse(data);
                if (json["actionType"]?.ToString() != "OverlayUpdate")
                {
                    return;
                }

                if (json["jsonData"] == null)
                {
                    return;
                }

                var osu = JObject.Parse(json["jsonData"]?.ToString());
                
                Logger.DebugSpam(osu.ToString());

                var hrToken = osu["status"]?["heartRate"]?.Type != JTokenType.Null
                    ? osu["status"]?["heartRate"]
                    : osu["status"]?["calculationMetrics"]?["estHeartRate"];

                if (hrToken != null)
                {
                    OnHearRateDataReceived(hrToken.ToObject<int>());
                }
                

            }
            catch (Exception e)
            {
                logger.Warn("Exception occured while parsing hr data");
                logger.Warn(e.Message);
                logger.Debug(e);
            }
            
        }


        private async Task Pong()
        {
            Logger.DebugSpam("Pong!");
            await SendMessage(2, null);
        }
        
        
        private void CloseClientMakeNull()
        {
            // close current one 
            try
            {
                _client?.Close();
            }
            catch (Exception e)
            {
                logger.Warn("Exception occured while closing tcp client");
                logger.Warn(e.Message);
                logger.Debug(e);
            }
            _client = null;
        }

        internal override void Stop()
        {
            _running = false;
            CloseClientMakeNull();
            _worker = null;
            logger.Info("Stopped");
        }
    }
}