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

        private static readonly string WSNotInstalledStr = $"<color=#FF0000>{DataSourceUtils.WEBSOCKET_SHARP_MOD_ID} REQUIRED BUT NOT INSTALLED OR ENABLED!</color>";

        private static PluginConfig Config => PluginConfig.Instance;

        private readonly bool _needWebSocket;
        private readonly string _str;
        private readonly Func<Task<string>> _sourceLinkTextAction;
        private readonly Type _dataSoucerType;

        #region New

        private DataSourceType(string str, bool needWebSocket, Type dataSoucerType, Func<Task<string>> sourceLinkTextCallback)
        {
            _str = str;
            _needWebSocket = needWebSocket;
            _sourceLinkTextAction = sourceLinkTextCallback;
            _dataSoucerType = dataSoucerType;
        }

        private static DataSourceType NewAndAppend<T>(string str, bool needWebSocket, Func<Task<string>> sourceLinkTextCallback) where T : DataSource
        {
            var type = new DataSourceType(str, needWebSocket, typeof(T), sourceLinkTextCallback);
            _sourceTypes[str] = type;
            return type;
        }

        private static DataSourceType NewAndAppend<T>(string str, bool needWebSocket, Func<string> sourceLinkTextCallback) where T : DataSource
        {
            return NewAndAppend<T>(str, needWebSocket, () => Task.FromResult(sourceLinkTextCallback()));
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

        internal async Task<string> GetSourceLinkText()
        {
            return await _sourceLinkTextAction.Invoke();
        }

        #endregion

        #region Some Instances

        internal static DataSourceType HypeRate = NewAndAppend<HRProxy>(HYPERATE_STR, true, () =>
        {
            var s = DataSourceUtils.WebSocketSharpInstalled
                ? $"Current Session ID: {Config.HypeRateSessionID}"
                : WSNotInstalledStr;

            return s;
        });

        internal static DataSourceType Puloid = NewAndAppend<Pulsoid>(PULSOID_STR, false, async () =>
            {
                if (PluginConfig.Instance.PulsoidToken == "NotSet")
                {
                    return "Token Not Set";
                }

                var status = await DataSourceUtils.CheckPulsoidToken();
                return "Token Status: " + (status == "" ? "<color=#00FF00>OK</color>" : $"<color=#FF0000>{status}</color>");
            }
        );

        internal static DataSourceType WebRequest = NewAndAppend<WebRequest>(WEBREQUEST_STR, false,
            () => $"Current URL: {Config.FeedLink.TruncateW()}"
        );

        internal static DataSourceType FitbitHRtoWS = NewAndAppend<FitbitHRtoWS>(FITBIT_STR, true,
            () => DataSourceUtils.WebSocketSharpInstalled
                ? $"Current WebSocket Link: {Config.FitbitWebSocket.CondTrunc(30)}"
                : WSNotInstalledStr
        );

        internal static DataSourceType HRProxy = NewAndAppend<HRProxy>(HRPROXY_STR, true,
            () => DataSourceUtils.WebSocketSharpInstalled
                ? $"Current HRProxy ID: {Config.HRProxyID.CondTrunc(30)}"
                : WSNotInstalledStr
        );

        internal static DataSourceType YURApp = NewAndAppend<YURApp>(YUR_APP_STR, false,
            () => DataSourceUtils.CheckYURProcess()
                ? "YUR App seems to be running."
                : "<color=#FFFF00>YUR App does not seem to be running.</color>"
        );

        internal static DataSourceType YURMod = NewAndAppend<YURMod>(YUR_MOD_STR, false,
            () => PluginManager.GetPluginFromId(DataSourceUtils.YUR_MOD_ID) == null
                ? "<color=#FF0000>YUR MOD IS NOT INSTALLED OR ENABLED!</color>"
                : "YUR MOD Detected!"
        );

#if DEBUG

        private const string RANDOM_STR = "Random";
        internal static DataSourceType Random = NewAndAppend<RandomHR>(RANDOM_STR, false, () => "debug lul");

#endif

        #endregion
    }
}