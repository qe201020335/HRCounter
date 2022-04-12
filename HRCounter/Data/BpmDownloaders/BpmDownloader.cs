using System;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Events;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter.Data.BpmDownloaders
{
    internal abstract class BpmDownloader
    {
        private readonly BPM Bpm = BPM.Instance;
        protected readonly IPALogger logger = Logger.logger;
        protected static PluginConfig Config => PluginConfig.Instance;

        internal event EventHandler<HRUpdateEventArgs> OnHRUpdate;

        protected void OnHearRateDataReceived(int hr)
        {
            OnHearRateDataReceived(hr, DateTime.Now.ToString("HH:mm:ss"));
        }
        
        protected void OnHearRateDataReceived(int hr, string receivedAt)
        {
            Bpm.Bpm = hr;
            Bpm.ReceivedAt = receivedAt;

            if (Config.LogHR)
            {
                logger.Info(Bpm.ToString());
            }
            
            try
            {
                var handler = OnHRUpdate;
                Task.Factory.StartNew(() =>
                {
                    handler?.Invoke(this, new HRUpdateEventArgs(hr));
                });
            }
            catch (Exception e)
            {
                Logger.logger.Critical($"Exception Caught while broadcasting hr update event: {e.Message}");
                Logger.logger.Critical(e);
            }
        }

        protected abstract void RefreshSettings();

        internal abstract void Start();

        internal abstract void Stop();
        
        
        
    }
}