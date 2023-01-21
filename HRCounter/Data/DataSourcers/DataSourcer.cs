using System;
using System.Threading.Tasks;
using HRCounter.Configuration;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter.Data.DataSourcers
{
    internal abstract class DataSourcer
    {
        protected readonly IPALogger Logger = Log.Logger;
        protected static PluginConfig Config => PluginConfig.Instance;

        internal event Action<int>? OnHRUpdate;

        protected void OnHearRateDataReceived(int hr, string? receivedAt = null)
        {
            BPM.Set(hr, receivedAt);

            if (Config.LogHR)
            {
                Logger.Info(BPM.Str);
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