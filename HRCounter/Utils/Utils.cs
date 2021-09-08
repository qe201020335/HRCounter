using System.Linq;
using IPALogger = IPA.Logging.Logger;
using System.Reflection;
using HarmonyLib;
using System.Collections.Generic;
using HRCounter.Configuration;
using UnityEngine;

namespace HRCounter.Utils
{
    
    public class Utils
    {
        // private static IPALogger _logger = Logger.logger;

        internal static List<object> DataSources = new object[] {"HypeRate", "WebRequest", "FitbitHRtoWS"}.ToList();
        
        static MethodBase ScoreSaber_playbackEnabled =
            AccessTools.Method("ScoreSaber.Core.ReplaySystem.HarmonyPatches.PatchHandleHMDUnmounted:Prefix");
        // code copied from Camera2
        internal static bool IsInReplay()
        {
            // detect whether we are in a replay
            return ScoreSaber_playbackEnabled != null && (bool) ScoreSaber_playbackEnabled.Invoke(null, null) == false;
        }
        
        internal static bool IsModInstalled(string modName)
        {
            return IPA.Loader.PluginManager.EnabledPlugins.Any(x => x.Id == modName);
        }

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

                default:
                    return "Unknown Data Source";
            }
        }
    }
}