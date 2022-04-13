using System;
using System.Threading.Tasks;
using HRCounter.Data.BpmDownloaders;
using HRCounter.Events;
using Zenject;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter.Data
{
    public class HRController : IInitializable, IDisposable
    {
        private static readonly IPALogger _logger = Logger.logger;
        private static BpmDownloader _bpmDownloader;

        internal HRController(BpmDownloader bpmDownloader)
        {
            _bpmDownloader = bpmDownloader;
        }
        
        public static event EventHandler<HRUpdateEventArgs> OnHRUpdate;
        
        internal static void ClearThings()
        {
            Logger.logger.Info("Clearing bpm downloader and all related event subscribers");
            // _bpmDownloader?.Stop();
            _bpmDownloader = null;

            OnHRUpdate = null;
        }

        public void Initialize()
        {
            _logger.Debug("HRController Init");
            if (_bpmDownloader == null)
            {
                _logger.Warn("BPM Downloader is null!");
                return;
            }
            _bpmDownloader.OnHRUpdate += OnHRUpdateInternalHandler;
            
            try
            {
                _bpmDownloader.Start();
                _logger.Info("Start updating heart rate");
            }
            catch (Exception e)
            {
                _logger.Error($"Could not start bpm downloader. {e.Message}");
                _logger.Debug(e);
                _bpmDownloader.OnHRUpdate -= OnHRUpdateInternalHandler;
                _bpmDownloader.Stop();
            }
        }

        public void Dispose()
        {
            _logger.Debug("HRController Dispose");
            if (_bpmDownloader != null)
            {
                _bpmDownloader.OnHRUpdate -= OnHRUpdateInternalHandler;
            }
            _bpmDownloader?.Stop();
            // _bpmDownloader = null;
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
    }
}