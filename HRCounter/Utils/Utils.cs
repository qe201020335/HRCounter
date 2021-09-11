using System.Linq;
using IPALogger = IPA.Logging.Logger;
using System.Reflection;
using HarmonyLib;
using System.Collections.Generic;
using HRCounter.Configuration;

namespace HRCounter.Utils
{
    
    public static class Utils
    {
        // private static IPALogger _logger = Logger.logger;

        internal static readonly List<object> DataSources = new object[] {"HypeRate", "WebRequest", "FitbitHRtoWS"}.ToList();
        
        private static readonly MethodBase ScoreSaber_playbackEnabled =
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
                    return "FeedLink: " + PluginConfig.Instance.FeedLink;

                case "HypeRate":
                    return "SessionID: " + PluginConfig.Instance.HypeRateSessionID;

                case "FitbitHRtoWS":
                    return "WebSocket Server: " + PluginConfig.Instance.FitbitWebSocket;

                default:
                    return "Unknown Data Source";
            }
        }
    }
}