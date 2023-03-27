using System;
using System.Linq;
using IPALogger = IPA.Logging.Logger;
using System.Reflection;
using HarmonyLib;
using HRCounter.Configuration;
using UnityEngine;
using BeatLeader.Replayer;
using IPA.Loader;
using Version = Hive.Versioning.Version;

namespace HRCounter.Utils
{
    public static class Utils
    {
        private const string BEATLEADER_MOD_ID = "BeatLeader";

        private static bool? _beatleaderHasReplay = null;

        internal static bool BeatLeaderHasReplay
        {
            get
            {
                _beatleaderHasReplay ??= (FindEnabledPluginMetadata(BEATLEADER_MOD_ID)?.HVersion ?? new Version(0, 0, 0)) >= new Version(0, 5, 0);
                return _beatleaderHasReplay.Value;
            }
        }

        // copied from Camera2
        private static readonly MethodBase ScoreSaber_playbackEnabled =
            AccessTools.Method("ScoreSaber.Core.ReplaySystem.HarmonyPatches.PatchHandleHMDUnmounted:Prefix");

        internal static bool IsInReplay()
        {
            // copied from Camera2
            var ssReplay = ScoreSaber_playbackEnabled != null && (bool)ScoreSaber_playbackEnabled.Invoke(null, null) == false;

            var blReplay = BeatLeaderHasReplay && ReplayerLauncher.IsStartedAsReplay;

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

                    var ratio = (hr - hrLow) / (float)(hrHigh - hrLow) * 2;
                    var color = ratio < 1 ? Color.Lerp(colorLow, colorMid, ratio) : Color.Lerp(colorMid, colorHigh, ratio - 1);

                    return ColorUtility.ToHtmlStringRGB(color);
                }
            }

            Log.Logger.Warn("Cannot determine color, please check hr boundaries and color codes.");
            return ColorUtility.ToHtmlStringRGB(Color.white);
        }
    }
}