using System;
using System.Linq;
using IPALogger = IPA.Logging.Logger;
using System.Reflection;
using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using HRCounter.Configuration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace HRCounter.Utils
{
    
    public static class Utils
    {

        internal static readonly List<object> DataSources = new object[] {"HypeRate", "Pulsoid Token", "WebRequest", "FitbitHRtoWS", "Pulsoid", "YUR APP", "YUR MOD"}.ToList();
        private static readonly List<string> DataSourcesRequireWebSocket = new [] {"HypeRate", "FitbitHRtoWS", "Pulsoid"}.ToList();
        internal const string YUR_MOD_ID = "YUR Fit Calorie Tracker";
        internal const string WEBSOCKET_SHARP_MOD_ID = "websocket-sharp";
        internal const string PULSOID_API = "https://dev.pulsoid.net/api/v1/data/heart_rate/latest";
        
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
                    return $"Current URL: {ConditionalTruncate(PluginConfig.Instance.FeedLink, 30)}";
                
                case "Pulsoid Token":
                    if (PluginConfig.Instance.PulsoidToken == "NotSet")
                    {
                        return "Token Not Set";
                    }

                    var task = CheckPulsoidToken();
                    task.Wait();
                    var status = task.Result;
                    return $"Token Status: {(status == "" ? "<color=#00FF00>OK</color>" : $"<color=#FF0000>{status}</color>)")}";

                case "HypeRate":
                    if (!IsModEnabled(WEBSOCKET_SHARP_MOD_ID))
                    {
                        return $"<color=#FF0000>{WEBSOCKET_SHARP_MOD_ID} REQUIRED BUT NOT INSTALLED OR ENABLED!</color>";
                    }
                    return $"Current Session ID: {ConditionalTruncate(PluginConfig.Instance.HypeRateSessionID, 30)}";

                case "FitbitHRtoWS":
                    if (!IsModEnabled(WEBSOCKET_SHARP_MOD_ID))
                    {
                        return $"<color=#FF0000>{WEBSOCKET_SHARP_MOD_ID} REQUIRED BUT NOT INSTALLED OR ENABLED!</color>";
                    }
                    return $"Current WebSocket Link: {ConditionalTruncate(PluginConfig.Instance.FitbitWebSocket, 30)}";
                
                case "Pulsoid":
                    if (!IsModEnabled(WEBSOCKET_SHARP_MOD_ID))
                    {
                        return $"<color=#FF0000>{WEBSOCKET_SHARP_MOD_ID} REQUIRED BUT NOT INSTALLED OR ENABLED!</color>";
                    }
                    return $"Current Widget ID: {ConditionalTruncate(PluginConfig.Instance.PulsoidWidgetID, 30)}";

                case "YUR APP":
                    if (!CheckYURProcess())
                    {
                        return "<color=#FFFF00>YUR App does not seem to be running.</color>";
                    }
                    return "YUR App seems to be running.";
                
                case "YUR MOD":
                    if (!IsModEnabled(YUR_MOD_ID))
                    {
                        return "<color=#FF0000>YUR MOD IS NOT INSTALLED OR ENABLED!</color>";
                    }
                    return "YUR MOD Detected!";

                default:
                    return "Unknown Data Source";
            }
        }

        internal static async Task<string> CheckPulsoidToken()
        {
            HttpClient HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {PluginConfig.Instance.PulsoidToken}");
            
            try
            {
                var res = await HttpClient.GetAsync(PULSOID_API);
            
                if (res.IsSuccessStatusCode)
                {
                    return "";
                }
                var json = JObject.Parse(await res.Content.ReadAsStringAsync());
                return $"{Convert.ToInt32(res.StatusCode)} {res.StatusCode}, {json["error_code"]}, {json["error_message"]}";
            }
            catch (Exception e)
            {
                Logger.logger.Error(e);
                return "Error validating token";
            }
        }

        internal static string ConditionalTruncate(string s, int length)
        {
            return s.Length <= length ? s : s.Substring(0, length) + "...";
        }

        internal static bool CheckYURProcess()
        {
            var processes = Process.GetProcessesByName("YUR.Fit.Windows.Service");
            return processes.Length > 0;
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