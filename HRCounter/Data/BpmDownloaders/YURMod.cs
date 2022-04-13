using System;
using System.Collections;
using HRCounter.Configuration;
using UnityEngine;
using YUR.Fit.Core.Models;
using YUR.Fit.Unity;

namespace HRCounter.Data.BpmDownloaders
{
    internal sealed class YURMod : BpmDownloader
    {
        internal override void Start()
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
                        OnHearRateDataReceived(Convert.ToInt32(hr));
                    }
                }
                else
                {
                    OnHearRateDataReceived(Convert.ToInt32(osu.HeartRate.Value));
                }
            }
            catch (Exception e)
            {
                // we don't want ANY uncaught exception,
                // YUR invokes the action on the freaking MAIN THREAD!!!
                logger.Critical($"Exception occured while handling YUR Mod Status Update: {e.Message}");
                logger.Critical(e);
            }
        }
        
        internal override void Stop()
        {
            CoreServiceManager.OverlayUpdateAction -= OnOverlayStatusUpdate;
        }
    }
}