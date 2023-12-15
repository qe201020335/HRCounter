using System;
using System.Threading.Tasks;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter.Data.DataSources
{
    public abstract class DataSource
    {
        internal event Action<int>? OnHRUpdate;

        protected void OnHearRateDataReceived(int hr, string? receivedAt = null)
        {
            BPM.Set(hr, receivedAt);

            // if (Config.LogHR)
            // {
            //     Logger.Info(BPM.Str);
            // }
            
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
        
        protected internal abstract void Start();

        protected internal abstract void Stop();
        
    }
}