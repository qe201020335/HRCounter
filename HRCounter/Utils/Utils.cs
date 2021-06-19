using System.Linq;
using IPALogger = IPA.Logging.Logger;
using System.Reflection;
using HarmonyLib;


namespace HRCounter.Utils
{
    
    public class Utils
    {
        // private static IPALogger _logger = Logger.logger;
        
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
    }
}