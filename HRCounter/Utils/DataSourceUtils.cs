using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace HRCounter.Utils;

public static class DataSourceUtils
{
    internal const string YUR_MOD_ID = "YUR Fit Calorie Tracker";

    internal static bool CheckYURProcess()
    {
        var processes = Process.GetProcessesByName("YUR.Fit.Windows.Service");
        return processes.Length > 0;
    }

    private const string PULSOID_VALIDATE = "https://dev.pulsoid.net/api/v1/token/validate";

    //TODO migrate to use PulsoidAuthenticator
    internal static async Task<string> CheckPulsoidToken(string token)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {token}");
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Plugin.Instance.UserAgent);
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
            Plugin.Logger.Error(e);
            return "Error validating token";
        }
    }
}
