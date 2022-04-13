using System;
using HRCounter.Data.BpmDownloaders;
using YUR.Fit.Core.Models;

namespace HRCounter.Harmony
{
    // no harmony attributes here, this patch needs to be manually applied
    internal class YURActivityViewControllerPatch
    {
        internal static void OverlayUpdateAction(OverlayStatusUpdate status)
        {
            Logger.logger.Debug($"YUR MOD OSU: {status.HeartRate}");
            // postfix
            try
            {
                YURData.SetHR(status.HeartRate ?? 0);
            }
            catch(Exception e){}
        }
    }
}