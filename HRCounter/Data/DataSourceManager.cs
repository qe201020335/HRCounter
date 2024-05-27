using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Data.DataSources;
using HRCounter.Utils;
using IPA.Loader;

#if DEBUG
using HRCounter.Data.DataSources.DebugSource;
#endif

namespace HRCounter.Data
{
    public class DataSourceManager
    {
        private const string HYPERATE_STR = "HypeRate";
        private const string PULSOID_STR = "Pulsoid";
        private const string WEBREQUEST_STR = "WebRequest";
        private const string FITBIT_STR = "FitbitHRtoWS";
        private const string HRPROXY_STR = "HRProxy";
        private const string YUR_APP_STR = "YUR APP";
        private const string YUR_MOD_STR = "YUR MOD";

        private static readonly string WSNotInstalledStr =
            $"<color=#FF0000>{DataSourceUtils.WEBSOCKET_SHARP_MOD_ID} REQUIRED BUT NOT INSTALLED OR ENABLED!</color>";

        private static PluginConfig Config => PluginConfig.Instance;

        private static DataSourceInfo NewAndAppend<T>(string str, Func<Task<string>> sourceLinkTextCallback,
            Func<bool> precondition) where T : IHRDataSource
        {
            var type = new DataSourceInfo(str, typeof(T), sourceLinkTextCallback, precondition);
            _sourceTypes[str] = type;
            return type;
        }

        private static DataSourceInfo NewAndAppend<T>(string str, Func<string> sourceLinkTextCallback, Func<bool> precondition)
            where T : IHRDataSource
        {
            return NewAndAppend<T>(str, () => Task.FromResult(sourceLinkTextCallback()), precondition);
        }
        
        private static readonly Dictionary<string, DataSourceInfo> _sourceTypes = new Dictionary<string, DataSourceInfo>();
        internal static IReadOnlyDictionary<string, DataSourceInfo> DataSourceTypes => _sourceTypes;

        public static bool TryGetFromStr(string str, out DataSourceInfo outInfo)
        {
            var found =  DataSourceTypes.TryGetValue(str, out var value);
            outInfo = found ? value : default;
            return found;
        }

        internal static string MigrateStr(string old)
        {
            if (DataSourceTypes.ContainsKey(old)) return old;
            if (old.ToLower().Contains("pulsoid")) return Puloid.Key;
            return old;
        }

        #region Some Instances

        internal static DataSourceInfo HypeRate = NewAndAppend<HRProxy>(HYPERATE_STR,
            () => DataSourceUtils.WebSocketSharpInstalled ? $"Current Session ID: {Config.HypeRateSessionID}" : WSNotInstalledStr,
            () => GenericPreconditionWithWS(Config.HypeRateSessionID)
        );

        internal static DataSourceInfo Puloid = NewAndAppend<Pulsoid>(PULSOID_STR, async () =>
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

        internal static DataSourceInfo WebRequest = NewAndAppend<WebRequest>(WEBREQUEST_STR,
            () => $"Current URL: {Config.FeedLink.TruncateW()}",
            () => GenericPrecondition(Config.FeedLink)
        );

        internal static DataSourceInfo FitbitHRtoWS = NewAndAppend<FitbitHRtoWS>(FITBIT_STR,
            () => DataSourceUtils.WebSocketSharpInstalled
                ? $"Current WebSocket Link: {Config.FitbitWebSocket.TruncateW()}"
                : WSNotInstalledStr,
            () => GenericPreconditionWithWS(Config.FitbitWebSocket)
        );

        internal static DataSourceInfo HRProxy = NewAndAppend<HRProxy>(HRPROXY_STR,
            () => DataSourceUtils.WebSocketSharpInstalled
                ? $"Current HRProxy ID: {Config.HRProxyID.TruncateW()}"
                : WSNotInstalledStr,
            () => GenericPreconditionWithWS(Config.HRProxyID)
        );

        internal static DataSourceInfo YURApp = NewAndAppend<YURApp>(YUR_APP_STR,
            () => DataSourceUtils.CheckYURProcess()
                ? "YUR App seems to be running."
                : "<color=#FFFF00>YUR App does not seem to be running.</color>",
            () => true
        );

        internal static DataSourceInfo YURMod = NewAndAppend<YURMod>(YUR_MOD_STR,
            () => PluginManager.GetPluginFromId(DataSourceUtils.YUR_MOD_ID) == null
                ? "<color=#FF0000>YUR MOD IS NOT INSTALLED OR ENABLED!</color>"
                : "YUR MOD Detected!",
            () => PluginManager.GetPluginFromId(DataSourceUtils.YUR_MOD_ID) != null
        );

#if DEBUG

        private const string DEBUG_RANDOM_STR = "Random Debug";
        internal static DataSourceInfo Random = NewAndAppend<RandomHR>(DEBUG_RANDOM_STR, () => "debug lul", () => true);

        private const string DEBUG_SWEEP_STR = "Sweep Debug";
        internal static DataSourceInfo Sweep = NewAndAppend<SweepHR>(DEBUG_SWEEP_STR, () => "debug lulul", () => true);

        private const string DEBUG_FPS_STR = "FPS Debug";
        internal static DataSourceInfo FrameRate = NewAndAppend<FrameRateHR>(DEBUG_FPS_STR, () => "debug lululul", () => true);

#endif

        #endregion


        private static bool GenericPreconditionWithWS(string s)
        {
            return DataSourceUtils.WebSocketSharpInstalled && GenericPrecondition(s);
        }
        
        private static bool GenericPrecondition(string s)
        {
            return !string.IsNullOrWhiteSpace(s) && s != "NotSet" && s != "-1";
        }
    }
}