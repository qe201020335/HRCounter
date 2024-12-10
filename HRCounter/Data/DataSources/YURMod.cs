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
            var hr = osu.HeartRate ?? osu.CalculationMetrics?.EstHeartRate;
            if (hr != null)
            {
                OnHeartRateDataReceived(Convert.ToInt32(hr.Value));
            }
        }
        
        protected override void Stop()
        {
            CoreServiceManager.OverlayUpdateAction -= OnOverlayStatusUpdate;
        }
    }
}