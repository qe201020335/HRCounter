using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Data.DataSources;
using HRCounter.Utils;
using IPA.Loader;

namespace HRCounter.Data
{
    public class DataSourceType
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

        private readonly bool _needWebSocket;
        private readonly string _str;
        private readonly Func<Task<string>> _sourceLinkTextAction;
        private readonly Type _dataSoucerType;
        private readonly Func<bool> _precondition;

        #region New

        private DataSourceType(string str, bool needWebSocket, Type dataSoucerType, Func<Task<string>> sourceLinkTextCallback,
            Func<bool> precondition)
        {
            _str = str;
            _needWebSocket = needWebSocket;
            _sourceLinkTextAction = sourceLinkTextCallback;
            _dataSoucerType = dataSoucerType;
            _precondition = precondition;
        }

        private static DataSourceType NewAndAppend<T>(string str, bool needWebSocket, Func<Task<string>> sourceLinkTextCallback,
            Func<bool> precondition) where T : IHRDataSource
        {
            var type = new DataSourceType(str, needWebSocket, typeof(T), sourceLinkTextCallback, precondition);
            _sourceTypes[str] = type;
            return type;
        }

        private static DataSourceType NewAndAppend<T>(string str, bool needWebSocket, Func<string> sourceLinkTextCallback, Func<bool> precondition)
            where T : IHRDataSource
        {
            return NewAndAppend<T>(str, needWebSocket, () => Task.FromResult(sourceLinkTextCallback()), precondition);
        }

        #endregion

        #region Static Stuff (should be in another class)

        private static readonly Dictionary<string, DataSourceType> _sourceTypes = new Dictionary<string, DataSourceType>();
        internal static IReadOnlyDictionary<string, DataSourceType> DataSourceTypes => _sourceTypes;

        public static DataSourceType? GetFromStr(string str)
        {
            return DataSourceTypes.TryGetValue(str, out var value) ? value : null;
        }

        internal static string MigrateStr(string old)
        {
            if (DataSourceTypes.ContainsKey(old)) return old;
            if (old.ToLower().Contains("pulsoid")) return Puloid.Str;
            return old;
        }

        #endregion

        #region Instance Stuff...

        public string Str => _str;

        public Type DataSourcerType => _dataSoucerType;

        public bool NeedWebSocket => _needWebSocket;

        public override bool Equals(object obj)
        {
            return obj.GetType() == GetType();
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }

        public override string ToString()
        {
            return Str;
        }

        internal async Task<string> GetSourceLinkText()
        {
            return await _sourceLinkTextAction();
        }

        internal bool PreconditionMet()
        {
            return _precondition();
        }

        #endregion

        #region Some Instances

        internal static DataSourceType HypeRate = NewAndAppend<HRProxy>(HYPERATE_STR, true,
            () => DataSourceUtils.WebSocketSharpInstalled ? $"Current Session ID: {Config.HypeRateSessionID}" : WSNotInstalledStr,
            () => GenericPrecondition(Config.HypeRateSessionID)
        );

        internal static DataSourceType Puloid = NewAndAppend<Pulsoid>(PULSOID_STR, false,
            async () =>
            {
                if (PluginConfig.Instance.PulsoidToken == "NotSet")
                {
                    return "Token Not Set";
                }

                var status = await DataSourceUtils.CheckPulsoidToken(PluginConfig.Instance.PulsoidToken);
                return "Token Status: " + (status == "" ? "<color=#00FF00>OK</color>" : $"<color=#FF0000>{status}</color>");
            },
            () => GenericPrecondition(Config.PulsoidToken)
        );

        internal static DataSourceType WebRequest = NewAndAppend<WebRequest>(WEBREQUEST_STR, false,
            () => $"Current URL: {Config.FeedLink.TruncateW()}",
            () => GenericPrecondition(Config.FeedLink)
        );

        internal static DataSourceType FitbitHRtoWS = NewAndAppend<FitbitHRtoWS>(FITBIT_STR, true,
            () => DataSourceUtils.WebSocketSharpInstalled
                ? $"Current WebSocket Link: {Config.FitbitWebSocket.TruncateW()}"
                : WSNotInstalledStr,
            () => GenericPrecondition(Config.FitbitWebSocket)
        );

        internal static DataSourceType HRProxy = NewAndAppend<HRProxy>(HRPROXY_STR, true,
            () => DataSourceUtils.WebSocketSharpInstalled
                ? $"Current HRProxy ID: {Config.HRProxyID.TruncateW()}"
                : WSNotInstalledStr,
            () => GenericPrecondition(Config.HRProxyID)
        );

        internal static DataSourceType YURApp = NewAndAppend<YURApp>(YUR_APP_STR, false,
            () => DataSourceUtils.CheckYURProcess()
                ? "YUR App seems to be running."
                : "<color=#FFFF00>YUR App does not seem to be running.</color>",
            () => true
        );

        internal static DataSourceType YURMod = NewAndAppend<YURMod>(YUR_MOD_STR, false,
            () => PluginManager.GetPluginFromId(DataSourceUtils.YUR_MOD_ID) == null
                ? "<color=#FF0000>YUR MOD IS NOT INSTALLED OR ENABLED!</color>"
                : "YUR MOD Detected!",
            () => PluginManager.GetPluginFromId(DataSourceUtils.YUR_MOD_ID) != null
        );

#if DEBUG

        private const string DEBUG_RANDOM_STR = "Random Debug";
        internal static DataSourceType Random = NewAndAppend<RandomHR>(DEBUG_RANDOM_STR, false, () => "debug lul", () => true);
        
        private const string DEBUG_SWEEP_STR = "Sweep Debug";
        internal static DataSourceType Sweep = NewAndAppend<SweepHR>(DEBUG_SWEEP_STR, false, () => "debug lulul", () => true);

#endif

        #endregion

        private static bool GenericPrecondition(string s)
        {
            return !string.IsNullOrWhiteSpace(s) && s != "NotSet" && s != "-1";
        }
    }
}