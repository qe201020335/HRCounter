using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using HRCounter.Configuration;
using IPA.Loader;
using Newtonsoft.Json.Linq;

namespace HRCounter.Utils
{
    public static class DataSourceUtils
    {
        internal const string WEBSOCKET_SHARP_MOD_ID = "websocket-sharp";
        internal const string YUR_MOD_ID = "YUR Fit Calorie Tracker";

        private static bool? _webSocketSharpInstalled = null;

        internal static bool WebSocketSharpInstalled
        {
            get
            {
                _webSocketSharpInstalled ??= PluginManager.GetPluginFromId(WEBSOCKET_SHARP_MOD_ID) != null;
                return _webSocketSharpInstalled.Value;
            }
        }

        internal static bool CheckYURProcess()
        {
            var processes = Process.GetProcessesByName("YUR.Fit.Windows.Service");
            return processes.Length > 0;
        }
        
        private const string PULSOID_VALIDATE = "https://dev.pulsoid.net/api/v1/token/validate";

        internal static async Task<string> CheckPulsoidToken(string token)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {token}");
            try
            {
                var res = await httpClient.GetAsync(PULSOID_VALIDATE);
                
                if (res.IsSuccessStatusCode)
                {
                    return "";
                }
                var json = JObject.Parse(await res.Content.ReadAsStringAsync());

                return $"{Convert.ToInt32(res.StatusCode)} {res.StatusCode}, {json?["error_code"]}, {json?["error_message"]}";
            }
            catch (Exception e)
            {
                Log.Logger.Error(e);
                return "Error validating token";
            }
        }
    }
}