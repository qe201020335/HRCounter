using System;
using System.Linq;
using IPALogger = IPA.Logging.Logger;
using System.Reflection;
using HarmonyLib;
using IPA.Loader;

namespace HRCounter.Utils
{
    public static class Utils
    {
        #region replay check
        private static readonly Lazy<MethodBase?> ScoreSaber_playbackEnabled = new Lazy<MethodBase?>(() =>
        {
            var meta = Plugin.ScoreSaberMeta;
            if (meta == null)
            {
                Plugin.Logger.Info("ScoreSaber is not installed or disabled");
                return null;
            }

            var method = meta.Assembly.GetType("ScoreSaber.Core.ReplaySystem.HarmonyPatches.PatchHandleHMDUnmounted")
                ?.GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                Plugin.Logger.Warn("ScoreSaber replay check method not found");
                return null;
            }

            return method;
        });

        private static readonly Lazy<MethodBase?> GetBeatLeaderIsStartedAsReplay = new Lazy<MethodBase?>(() =>
        {
            var meta = Plugin.BeatLeaderMeta;
            if (meta == null)
            {
                Plugin.Logger.Info("BeatLeader is not installed or disabled");
                return null;
            }

            var method = meta.Assembly.GetType("BeatLeader.Replayer.ReplayerLauncher")
                ?.GetProperty("IsStartedAsReplay", BindingFlags.Static | BindingFlags.Public)?.GetGetMethod(false);

            if (method == null)
            {
                Plugin.Logger.Warn("BeatLeader ReplayerLauncher.IsStartedAsReplay not found");
                return null;
            }

            return method;
        });
        
        internal static bool IsInReplay()
        {
            var ssReplay = ScoreSaber_playbackEnabled.Value != null && (bool) ScoreSaber_playbackEnabled.Value.Invoke(null, null) == false;

            var blReplay = GetBeatLeaderIsStartedAsReplay.Value != null && (bool) GetBeatLeaderIsStartedAsReplay.Value.Invoke(null, null);
            
            return ssReplay || blReplay;
        }
        #endregion

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