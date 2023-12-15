using System.Linq;
using IPALogger = IPA.Logging.Logger;
using System.Reflection;
using HarmonyLib;
using IPA.Loader;

namespace HRCounter.Utils
{
    public static class Utils
    {

        // copied from Camera2
        private static readonly MethodBase? ScoreSaber_playbackEnabled =
            AccessTools.Method("ScoreSaber.Core.ReplaySystem.HarmonyPatches.PatchHandleHMDUnmounted:Prefix");

        private static readonly MethodBase? GetBeatLeaderIsStartedAsReplay =
            AccessTools.Property(AccessTools.TypeByName("BeatLeader.Replayer.ReplayerLauncher"), "IsStartedAsReplay")?.GetGetMethod(false);

        
        internal static bool IsInReplay()
        {
            // copied from Camera2
            var ssReplay = ScoreSaber_playbackEnabled != null && (bool) ScoreSaber_playbackEnabled.Invoke(null, null) == false;

            var blReplay = GetBeatLeaderIsStartedAsReplay != null && (bool) GetBeatLeaderIsStartedAsReplay.Invoke(null, null);
            
            return ssReplay || blReplay;
        }

        internal static bool IsModEnabled(string id)
        {
            return FindEnabledPluginMetadata(id) != null;
        }

        internal static PluginMetadata? FindEnabledPluginMetadata(string id)
        {
            return PluginManager.EnabledPlugins.FirstOrDefault(x => x.Id == id);
        }
    }
}