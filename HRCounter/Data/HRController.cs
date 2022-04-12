using System;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Data.BpmDownloaders;
using HRCounter.Events;
using JetBrains.Annotations;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter.Data
{
    internal class HRController
    {
        private static readonly IPALogger _logger = Logger.logger;
        [CanBeNull] private static BpmDownloader _bpmDownloader;
        
        public static event EventHandler<HRUpdateEventArgs> OnHRUpdate;
        
        internal static void ClearThings()
        {
            Logger.logger.Info("Clearing bpm downloader and all related event subscribers");
            _bpmDownloader?.Stop();
            _bpmDownloader = null;

            OnHRUpdate = null;
        }

        internal static bool InitAndStartDownloader()
        {
            _bpmDownloader?.Stop();
            _bpmDownloader = null;
            _logger.Info("Initializing BPM Downloader");
            switch (PluginConfig.Instance.DataSource)
            {
                case "WebRequest":
                    if (PluginConfig.Instance.FeedLink == "NotSet")
                    {
                        _logger.Warn("Feed link not set.");
                        return false;
                    }
                    _bpmDownloader = new WebRequest();
                    break;
                
                case "HypeRate":
                    if (PluginConfig.Instance.HypeRateSessionID == "-1")
                    {
                        _logger.Warn("Hype Rate Session ID not set.");
                        return false;
                    }

                    _bpmDownloader = new HRProxy();
                    break;
                    
                case "Pulsoid":
                    if (PluginConfig.Instance.PulsoidWidgetID == "NotSet")
                    {
                        _logger.Warn("Pulsoid Widget ID not set.");
                        return false;
                    }

                    _bpmDownloader = new HRProxy();
                    break;

                case "FitbitHRtoWS":
                    if(PluginConfig.Instance.FitbitWebSocket == string.Empty)
                    {
                        _logger.Warn("FitbitWebSocket is empty.");
                        return false;
                    }
                    _bpmDownloader = new FitbitHRtoWS();
                    break;
                
                case "YUR APP":
                    _bpmDownloader = new YURApp();
                    break;
                
                case "Random":
                    _bpmDownloader = new RandomHR();
                    break;

                default:
                    _bpmDownloader = null;
                    _logger.Warn("Unknown Data Sources");
                    return false;
            }

            _bpmDownloader.OnHRUpdate += OnHRUpdateInternalHandler;
            
            try
            {
                _bpmDownloader.Start();
                _logger.Info("Start updating heart rate");
                return true;
            }
            catch (Exception e)
            {
                _logger.Error($"Could not start bpm downloader. {e.Message}");
                _logger.Debug(e);
                _bpmDownloader.OnHRUpdate -= OnHRUpdateInternalHandler;
                _bpmDownloader.Stop();
                return false;
            }
        }

        private static void OnHRUpdateInternalHandler(object sender, HRUpdateEventArgs args)
        {
            if (sender != _bpmDownloader)
            {
                return;
            }
            try
            {
                var handler = OnHRUpdate;
                Task.Factory.StartNew(() =>
                {
                    handler?.Invoke(sender, args);
                });
            }
            catch (Exception e)
            {
                Logger.logger.Critical($"Exception Caught while broadcasting hr update event: {e.Message}");
                Logger.logger.Critical(e);
            }
        }

        internal static void Stop()
        {
            if (_bpmDownloader != null)
            {
                _bpmDownloader.OnHRUpdate -= OnHRUpdateInternalHandler;
            }
            _bpmDownloader?.Stop();
            _bpmDownloader = null;
        }
    }
}