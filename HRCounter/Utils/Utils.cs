using System.Linq;
using IPALogger = IPA.Logging.Logger;
using System.Reflection;
using HarmonyLib;
using System.Collections.Generic;
using HRCounter.Configuration;
using UnityEngine;

namespace HRCounter.Utils
{
    
    public static class Utils
    {

        internal static readonly List<object> DataSources = new object[] {"HypeRate", "WebRequest", "FitbitHRtoWS", "Pulsoid", "YUR APP", "YUR MOD"}.ToList();
        private static readonly List<string> DataSourcesRequireWebSocket = new [] {"HypeRate", "FitbitHRtoWS", "Pulsoid"}.ToList();
        
        private static readonly MethodBase ScoreSaber_playbackEnabled =
            AccessTools.Method("ScoreSaber.Core.ReplaySystem.HarmonyPatches.PatchHandleHMDUnmounted:Prefix");
        // code copied from Camera2
        internal static bool IsInReplay()
        {
            // detect whether we are in a replay
            return ScoreSaber_playbackEnabled != null && (bool) ScoreSaber_playbackEnabled.Invoke(null, null) == false;
        }
        
        internal static bool IsModEnabled(string id)
        {
            return IPA.Loader.PluginManager.EnabledPlugins.Any(x => x.Id == id);
        }

        internal static bool NeedWebSocket(string dc)
        {
            return DataSourcesRequireWebSocket.Contains(dc);
        }

        internal static Assembly GetModAssembly(string modName) => IPA.Loader.PluginManager.EnabledPlugins.ToList()
            .Find(metadata => metadata.Name == modName).Assembly;

        internal static string GetCurrentSourceLinkText()
        {
            switch (PluginConfig.Instance.DataSource)
            {
                case "WebRequest":
                    return PluginConfig.Instance.FeedLink;

                case "HypeRate":
                    return PluginConfig.Instance.HypeRateSessionID;

                case "FitbitHRtoWS":
                    return PluginConfig.Instance.FitbitWebSocket;
                
                case "Pulsoid":
                    return PluginConfig.Instance.PulsoidWidgetID;
                
                case "YUR APP":
                    return "Make sure to have your desktop YUR app running";

                default:
                    return "Unknown Data Source";
            }
        }
        
        internal static string DetermineColor(int hr)
        {
            var hrLow = PluginConfig.Instance.HRLow;
            var hrHigh = PluginConfig.Instance.HRHigh;
            var lowColor = PluginConfig.Instance.LowColor;
            var highColor = PluginConfig.Instance.HighColor;
            var midColor = PluginConfig.Instance.MidColor;
            if (hrHigh >= hrLow && hrLow > 0)
            {
                if (ColorUtility.TryParseHtmlString(highColor, out var colorHigh) &&
                    ColorUtility.TryParseHtmlString(lowColor, out var colorLow) && 
                    ColorUtility.TryParseHtmlString(midColor, out var colorMid))
                {
                    if (hr <= hrLow)
                    {
                        return lowColor.Substring(1); //the rgb color in setting are #RRGGBB, need to omit the #
                    }

                    if (hr >= hrHigh)
                    {
                        return highColor.Substring(1);
                    }

                    var ratio = (hr - hrLow) / (float) (hrHigh - hrLow) * 2;
                    var color = ratio < 1 ? Color.Lerp(colorLow, colorMid, ratio) : Color.Lerp(colorMid, colorHigh, ratio - 1);

                    return ColorUtility.ToHtmlStringRGB(color);
                }
            }
            Logger.logger.Warn("Cannot determine color, please check hr boundaries and color codes.");
            return ColorUtility.ToHtmlStringRGB(Color.white);
        }
    }
}