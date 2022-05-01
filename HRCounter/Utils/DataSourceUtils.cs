using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using HRCounter.Configuration;
using Newtonsoft.Json.Linq;

namespace HRCounter.Utils
{
    public static class DataSourceUtils
    {
        internal const string YUR_MOD_ID = "YUR Fit Calorie Tracker";
        internal const string WEBSOCKET_SHARP_MOD_ID = "websocket-sharp";
        internal const string PULSOID_API = "https://dev.pulsoid.net/api/v1/data/heart_rate/latest";
        internal static readonly List<object> DataSources = new object[] {"HypeRate", "Pulsoid Token", "WebRequest", "FitbitHRtoWS", "Pulsoid", "YUR APP", "YUR MOD"}.ToList();
        private static readonly List<string> DataSourcesRequireWebSocket = new [] {"HypeRate", "FitbitHRtoWS", "Pulsoid"}.ToList();

        internal static bool NeedWebSocket(string dc)
        {
            return DataSourcesRequireWebSocket.Contains(dc);
        }

        internal static Assembly GetModAssembly(string modName) => IPA.Loader.PluginManager.EnabledPlugins.ToList()
            .Find(metadata => metadata.Name == modName).Assembly;

        internal static async Task<string> GetCurrentSourceLinkText()
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
                    
                    var status = await CheckPulsoidToken();
                    return $"Token Status: {(status == "" ? "<color=#00FF00>OK</color>" : $"<color=#FF0000>{status}</color>")}";

                case "HypeRate":
                    if (!Utils.IsModEnabled(WEBSOCKET_SHARP_MOD_ID))
                    {
                        return $"<color=#FF0000>{WEBSOCKET_SHARP_MOD_ID} REQUIRED BUT NOT INSTALLED OR ENABLED!</color>";
                    }
                    return $"Current Session ID: {ConditionalTruncate(PluginConfig.Instance.HypeRateSessionID, 30)}";

                case "FitbitHRtoWS":
                    if (!Utils.IsModEnabled(WEBSOCKET_SHARP_MOD_ID))
                    {
                        return $"<color=#FF0000>{WEBSOCKET_SHARP_MOD_ID} REQUIRED BUT NOT INSTALLED OR ENABLED!</color>";
                    }
                    return $"Current WebSocket Link: {ConditionalTruncate(PluginConfig.Instance.FitbitWebSocket, 30)}";
                
                case "Pulsoid":
                    var s =
                        "<color=#FFFF00>EXPERIMENTAL, MAY NOT WORK IN THE FUTURE. PLEASE USE TOKEN INSTEAD.</color>\n";
                    if (!Utils.IsModEnabled(WEBSOCKET_SHARP_MOD_ID))
                    {
                        return s + $"<color=#FF0000>{WEBSOCKET_SHARP_MOD_ID} REQUIRED BUT NOT INSTALLED OR ENABLED!</color>";
                    }
                    return s + $"Current Widget ID: {ConditionalTruncate(PluginConfig.Instance.PulsoidWidgetID, 30)}";

                case "YUR APP":
                    if (!CheckYURProcess())
                    {
                        return "<color=#FFFF00>YUR App does not seem to be running.</color>";
                    }
                    return "YUR App seems to be running.";
                
                case "YUR MOD":
                    if (!Utils.IsModEnabled(YUR_MOD_ID))
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
    }
}