using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Data.DataSources;
using HRCounter.Utils;
using HRCounter.Web.HTTP;
using IPA.Loader;


#if DEBUG
using HRCounter.Data.DataSources.DebugSource;
#endif

namespace HRCounter.Data
{
    public class DataSourceManager
    {
        private const string HYPERATE_KEY = "HypeRate";
        private const string PULSOID_KEY = "Pulsoid";
        private const string PULSOID_WIDEGT_KEY = "PulsoidWidget";
        private const string WEBREQUEST_KEY = "WebRequest";
        private const string FITBIT_KEY = "FitbitHRtoWS";
        private const string HRPROXY_KEY = "HRProxy";
        private const string YUR_APP_KEY = "YUR APP";
        private const string YUR_MOD_KEY = "YUR MOD";
        private const string HTTP_SERVER_KEY = "HttpServer";
        private const string OSC_KEY = "OSC Protocol";

        private static readonly string WSNotInstalledStr =
            $"<color=#FF0000>{DataSourceUtils.WEBSOCKET_SHARP_MOD_ID} REQUIRED BUT NOT INSTALLED OR ENABLED!</color>";

        private static PluginConfig Config => PluginConfig.Instance;
        
        private static readonly Dictionary<string, DataSourceInfo> _sourceTypes = new Dictionary<string, DataSourceInfo>();
        internal static IReadOnlyDictionary<string, DataSourceInfo> DataSourceTypes => _sourceTypes;
        
        public static DataSourceInfo RegisterDataSource<T>(string key, Func<Task<string>> sourceLinkTextCallback,
            Func<bool> precondition) where T : IHRDataSource
        {
            if (_sourceTypes.ContainsKey(key)) throw new ArgumentException($"Key {key} already exists!");
            var type = new DataSourceInfo(key, typeof(T), sourceLinkTextCallback, precondition);
            _sourceTypes[key] = type;
            return type;
        }

        public static DataSourceInfo RegisterDataSource<T>(string key, Func<string> sourceLinkTextCallback, Func<bool> precondition)
            where T : IHRDataSource
        {
            return RegisterDataSource<T>(key, () => Task.FromResult(sourceLinkTextCallback()), precondition);
        }

        public static bool TryGetFromKey(string str, out DataSourceInfo outInfo)
        {
            var found =  DataSourceTypes.TryGetValue(str, out var value);
            outInfo = found ? value : default;
            return found;
        }

        internal static string MigrateKey(string old)
        {
            if (DataSourceTypes.ContainsKey(old)) return old;
            if (old.ToLower().StartsWith("pulsoid")) return Pulsoid.Key;
            return old;
        }
        
        private static bool GenericPreconditionWS(string s)
        {
            return DataSourceUtils.WebSocketSharpInstalled && GenericPrecondition(s);
        }
        
        private static bool GenericPrecondition(string s)
        {
            return !string.IsNullOrWhiteSpace(s) && s != "NotSet" && s != "-1";
        }

        #region Some Instances

        internal static DataSourceInfo HypeRate = RegisterDataSource<HypeRate>(HYPERATE_KEY,
            () => DataSourceUtils.WebSocketSharpInstalled ? $"Current Session ID: {Config.HypeRateSessionID}" : WSNotInstalledStr,
            () => GenericPreconditionWS(Config.HypeRateSessionID)
        );

        internal static DataSourceInfo Pulsoid = RegisterDataSource<Pulsoid>(PULSOID_KEY, async () =>
            {
                if (!GenericPrecondition(Config.PulsoidToken))
                {
                    return "Token Not Set";
                }

                var status = await DataSourceUtils.CheckPulsoidToken(PluginConfig.Instance.PulsoidToken);
                return "Token Status: " + (status == "" ? "<color=#00FF00>OK</color>" : $"<color=#FF0000>{status}</color>");
            },
            () => GenericPrecondition(Config.PulsoidToken)
        );

        internal static DataSourceInfo WebRequest = RegisterDataSource<WebRequest>(WEBREQUEST_KEY,
            () => $"Current URL: {Config.FeedLink.TruncateW()}",
            () => GenericPrecondition(Config.FeedLink)
        );

        internal static DataSourceInfo FitbitHRtoWS = RegisterDataSource<FitbitHRtoWS>(FITBIT_KEY,
            () => DataSourceUtils.WebSocketSharpInstalled
                ? $"Current WebSocket Link: {Config.FitbitWebSocket.TruncateW()}"
                : WSNotInstalledStr,
            () => GenericPreconditionWS(Config.FitbitWebSocket)
        );

        internal static DataSourceInfo HRProxy = RegisterDataSource<HRProxyCustomReader>(HRPROXY_KEY,
            () => DataSourceUtils.WebSocketSharpInstalled
                ? $"Current HRProxy ID: {Config.HRProxyID.TruncateW()}"
                : WSNotInstalledStr,
            () => GenericPreconditionWS(Config.HRProxyID)
        );

        internal static DataSourceInfo YURApp = RegisterDataSource<YURApp>(YUR_APP_KEY,
            () => DataSourceUtils.CheckYURProcess()
                ? "YUR App seems to be running."
                : "<color=#FFFF00>YUR App does not seem to be running.</color>",
            () => true
        );

        internal static DataSourceInfo YURMod = RegisterDataSource<YURMod>(YUR_MOD_KEY,
            () => PluginManager.GetPluginFromId(DataSourceUtils.YUR_MOD_ID) == null
                ? "<color=#FF0000>YUR MOD IS NOT INSTALLED OR ENABLED!</color>"
                : "YUR MOD Detected!",
            () => PluginManager.GetPluginFromId(DataSourceUtils.YUR_MOD_ID) != null
        );
        
        internal static DataSourceInfo OscServer = RegisterDataSource<OscHR>(OSC_KEY,
            () => Config.EnableOscServer 
                ? $"OSC server listening on {Config.OscBindIP}:{Config.OscPort}.\nUsing addresses below (one Int32 value only)\n  {string.Join("\n  ", Config.OscAddress)}" 
                : "<color=#FF0000>OSC Server is NOT enabled!</color>",
            () => true);
        
        internal static DataSourceInfo HttpServer = RegisterDataSource<HttpServerDataSource>(HTTP_SERVER_KEY,
            () => Config.EnableHttpServer 
                ? $"POST to: {SimpleHttpServer.PREFIX}/hr" 
                : "<color=#FF0000>HTTP Server is NOT enabled!</color>",
            () => true
        );
        
        internal static DataSourceInfo PulsoidWidget = RegisterDataSource<PulsoidWidget>(PULSOID_WIDEGT_KEY, async () =>
            {
                var status = $"Widget ID: {(GenericPrecondition(Config.PulsoidWidgetID) ? Config.PulsoidWidgetID.TruncateW(10) : "Not Set")}"; 
                return "<color=#FF5630>EXPERIMENTAL</color> "+ status;
            },
            () => GenericPrecondition(Config.PulsoidToken)
        );

#if DEBUG

        private const string DEBUG_RANDOM_KEY = "Random Debug";
        internal static DataSourceInfo Random = RegisterDataSource<RandomHR>(DEBUG_RANDOM_KEY, () => LoremIpsum, () => true);

        private const string DEBUG_SWEEP_KEY = "Sweep Debug";
        internal static DataSourceInfo Sweep = RegisterDataSource<SweepHR>(DEBUG_SWEEP_KEY, () => LoremIpsum, () => true);

        private const string DEBUG_FPS_KEY = "FPS Debug";
        internal static DataSourceInfo FrameRate = RegisterDataSource<FrameRateHR>(DEBUG_FPS_KEY, () => LoremIpsum, () => true);

        private static string LoremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer tristique posuere libero eu gravida. " +
                                           "Aenean sed urna ante. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus " +
                                           "mus. Nam nec nunc enim. Fusce porta condimentum tellus eu hendrerit. Duis semper nisl vitae euismod " +
                                           "mollis. Nullam nunc ligula, elementum vulputate viverra sed, pretium sed orci. Nullam mattis, diam ac " +
                                           "malesuada vulputate, justo leo pharetra lorem, eu varius orci augue non leo. Vivamus quis iaculis arcu. " +
                                           "Nam at est ut risus posuere sodales. Aliquam erat volutpat. Duis quis auctor orci, vel blandit mi. Sed " +
                                           "semper, lorem quis malesuada lobortis, augue magna consequat dui, sit amet blandit ante diam commodo " +
                                           "metus. Mauris eu eros at lectus commodo lacinia in vitae nulla. Suspendisse dignissim auctor dui, " +
                                           "malesuada molestie dolor mollis at.\n\nSuspendisse at lacus rutrum, semper lorem vel, consequat ipsum. " +
                                           "In hac habitasse platea dictumst. Donec dictum viverra velit, at sollicitudin odio dignissim eu. " +
                                           "Praesent congue eros turpis. Aliquam vel nisl sit amet mi vestibulum hendrerit eu maximus est. Mauris et " +
                                           "sapien at ante feugiat congue. Morbi tincidunt sagittis purus, et accumsan odio tincidunt non.";

#endif

        #endregion
    }
}