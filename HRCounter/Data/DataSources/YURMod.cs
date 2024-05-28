using System;
using YUR.Fit.Core.Models;
using YUR.Fit.Unity;

namespace HRCounter.Data.DataSources
{
    internal sealed class YURMod : DataSource
    {
        protected override void Start()
        {
            CoreServiceManager.OverlayUpdateAction += OnOverlayStatusUpdate;
        }

        private void OnOverlayStatusUpdate(OverlayStatusUpdate osu)
        {
            try
            {
                if (osu.HeartRate == null)
                {
                    var calMetric = typeof(OverlayStatusUpdate).GetProperty("CalculationMetrics")?.GetValue(osu);
                    var hr = calMetric?.GetType().GetProperty("EstHeartRate")?.GetValue(calMetric);
                    if (hr != null)
                    {
                        OnHeartRateDataReceived(Convert.ToInt32(hr));
                    }
                }
                else
                {
                    OnHeartRateDataReceived(Convert.ToInt32(osu.HeartRate.Value));
                }
            }
            catch (Exception e)
            {
                // we don't want ANY uncaught exception,
                // YUR invokes the action on the freaking MAIN THREAD!!!
                Logger.Critical($"Exception occured while handling YUR Mod Status Update: {e.Message}");
                Logger.Critical(e);
            }
        }
        
        protected override void Stop()
        {
            CoreServiceManager.OverlayUpdateAction -= OnOverlayStatusUpdate;
        }
    }
}