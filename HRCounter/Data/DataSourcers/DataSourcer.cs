using System;
using System.Threading.Tasks;
using HRCounter.Configuration;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter.Data.DataSourcers
{
    internal abstract class DataSourcer
    {
        private readonly BPM Bpm = BPM.Instance;
        protected readonly IPALogger logger = Log.Logger;
        protected static PluginConfig Config => PluginConfig.Instance;

        internal event Action<int>? OnHRUpdate;

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
                Task.Factory.StartNew(() =>
                {
                    OnHRUpdate?.Invoke(hr);
                });
            }
            catch (Exception e)
            {
                Log.Logger.Critical($"Exception Caught while broadcasting hr update event: {e.Message}");
                Log.Logger.Critical(e);
            }
        }
        
        internal abstract void Start();

        internal abstract void Stop();
        
        
        
    }
}