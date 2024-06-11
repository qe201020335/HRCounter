using SharpOSC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HRCounter.Data.DataSources.OSCDataSource
{
    internal sealed class OSCDataSource : DataSource
    {
        private string host => Config.OSCBindIP;
        private int port => Config.OSCPort;

        private bool _updating;

        UdpClient? _client;

        public static string? errorMessage = null;
        protected override void Start()
        {
            Logger.Info("Start OSC data source");
            _updating = true;
            Task.Factory.StartNew(async () =>
            {
                Logger.Info("OSC Receive Task enabled");

                await ReadOSCData();
            });
        }


        private string usingAddress = "";
        private DateTime usingAddressReceived = DateTime.MinValue;
        void HandleHeartData(string address, int heartRate)
        {
            //lock the address to prevent multiple sources at the same time
            if(address != usingAddress)
            {
                if ((DateTime.Now - usingAddressReceived).TotalSeconds < 30)
                    return;
                usingAddress = address;
            }
            usingAddressReceived = DateTime.Now;


            OnHeartRateDataReceived(heartRate);
        }

        private async Task ReadOSCData()
        {
            try
            {
                _client = new UdpClient(new IPEndPoint(IPAddress.Parse(host), port));
            }catch(Exception e)
            {
                Logger.Error(e.ToString());
                errorMessage = $"<color=#FF0000>{e.Message}</color>";
                return;
            }

            while (_updating) {
                try
                {
                    Logger.Info("Waiting for package");
                    var pkg = await _client.ReceiveAsync();
                    Logger.Info("Got package");
                    if (pkg == null)
                    {
                        continue;
                    }

                    Regex? filter_re = null;
                    try
                    {
                        if (Config.OSCAddressFilterRegex != "")
                            filter_re = new Regex(Config.OSCAddressFilterRegex);
                    }catch (ArgumentException) {
                        //invalid address filter
                    }


                    try
                    {
                        var oscPacket = OscPacket.GetPacket(pkg.Buffer);
                        void Handle(OscMessage msg)
                        {
                            if (filter_re != null && !filter_re.IsMatch(msg.Address))
                                return;
                            Logger.Info("A package received");
                            if(msg.Arguments.Count == 1)
                            {
                                var arg = msg.Arguments[0];
                                switch(arg?.GetType()?.ToString() ?? "null") {
                                    case "System.Int32":
                                        HandleHeartData(msg.Address, (int)arg);
                                        break;
                                    case "System.Int64":
                                        HandleHeartData(msg.Address, (int)(Int64)arg);
                                        break;
                                    case "System.UInt64":
                                        HandleHeartData(msg.Address, (int)(UInt64)arg);
                                        break;
                                    case "System.Single":
                                        HandleHeartData(msg.Address, (int)(.5f + 255 * (float)arg));
                                        break;
                                    case "System.Double":
                                        HandleHeartData(msg.Address, (int)(.5 + 255 * (double)arg));
                                        break;
                                }
                            }
                        }
                        void HandleBundle(OscBundle bundle)
                        {
                            foreach (var message in bundle.Messages)
                            {
                                Handle(message);
                            }
                        }
                        if(oscPacket is OscBundle) 
                            HandleBundle((OscBundle)oscPacket);
                        if(oscPacket is OscMessage)
                            Handle((OscMessage)oscPacket);
                    }catch (Exception ex)
                    {
                        //Invalid package received. add small delay to prevent possible ddos attack
                        await Task.Delay(50);
                    }

                }
                catch (ObjectDisposedException) { 
                    await Task.Delay(100);
                }
                catch (SocketException ex)
                {
                    Logger.Info(ex.ToString());
                    //Don't make cpu busy if we have trouble
                    await Task.Delay(1000);
                }


            }
        }

        protected override void Stop()
        {
            _updating = false;
            _client?.Dispose();
            _client = null;
        }
    }
}
