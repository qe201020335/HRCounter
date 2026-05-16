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

namespace HRCounter.Data;

public class DataSourceManager
{
    private const string HYPERATE_KEY = "HypeRate";
    private const string PULSOID_KEY = "Pulsoid";
    private const string PULSOID_WIDEGT_KEY = "PulsoidWidget";
    private const string WEBREQUEST_KEY = "WebRequest";
    private const string HRPROXY_KEY = "HRProxy";
    private const string YUR_APP_KEY = "YUR APP";
    private const string YUR_MOD_KEY = "YUR MOD";
    private const string HTTP_SERVER_KEY = "HttpServer";
    private const string OSC_KEY = "OSC Protocol";

    private static PluginConfig Config => Plugin.Config;

    private static readonly Dictionary<string, IDataSourceDescriptor> SourceTypes = new(StringComparer.InvariantCultureIgnoreCase);

    internal static IReadOnlyDictionary<string, IDataSourceDescriptor> DataSourceTypes => SourceTypes;

    public static IDataSourceDescriptor RegisterDataSource<T>(string key, Func<Task<string>> getStatusText,
        Func<bool> precondition) where T : class, IHRDataSource
    {
        var descriptor = new GenericSourceDescriptor<T>(key, getStatusText, precondition);
        RegisterDataSource(descriptor);
        return descriptor;
    }

    public static IDataSourceDescriptor RegisterDataSource<T>(string key, Func<string> getStatusText, Func<bool> precondition)
        where T : class, IHRDataSource
    {
        return RegisterDataSource<T>(key, () => Task.FromResult(getStatusText()), precondition);
    }

    public static void RegisterDataSource(IDataSourceDescriptor descriptor)
    {
        var key = descriptor.Key;
        if (SourceTypes.ContainsKey(key)) throw new ArgumentException($"Key {key} already exists!", nameof(key));
        SourceTypes.Add(descriptor.Key, descriptor);
    }

    internal static IDataSourceDescriptor? GetFromKey(string str) => DataSourceTypes.GetValueOrDefault(str, null);

    internal static string MigrateKey(string key)
    {
        if (DataSourceTypes.ContainsKey(key)) return key;
        return key.StartsWith("pulsoid", StringComparison.InvariantCultureIgnoreCase) ? Pulsoid.Key : key;
    }

    private static bool GenericPrecondition(string s)
    {
        return !string.IsNullOrWhiteSpace(s) && s != "NotSet" && s != "-1";
    }

    #region Some Instances

    internal static IDataSourceDescriptor HypeRate = RegisterDataSource<HypeRate2>(HYPERATE_KEY,
        () => $"Current Session ID: {(Config.StreamerMode ? "********" : Config.HypeRateSessionID)}",
        () => GenericPrecondition(Config.HypeRateSessionID)
    );

    internal static IDataSourceDescriptor Pulsoid = RegisterDataSource<Pulsoid2>(PULSOID_KEY, async () =>
        {
            if (!GenericPrecondition(Config.PulsoidToken))
            {
                return "Token Not Set";
            }

            var status = await DataSourceUtils.CheckPulsoidToken(Config.PulsoidToken);
            return "Token Status: " + (status == "" ? "<color=#00FF00>OK</color>" : $"<color=#FF0000>{status}</color>");
        },
        () => GenericPrecondition(Config.PulsoidToken)
    );

    internal static IDataSourceDescriptor WebRequest = RegisterDataSource<WebRequest>(WEBREQUEST_KEY,
        () => $"Current URL: {(Config.StreamerMode ? "********" : Config.FeedLink)}",
        () => GenericPrecondition(Config.FeedLink)
    );

    internal static IDataSourceDescriptor HRProxy = RegisterDataSource<HRProxyCustomReader>(HRPROXY_KEY,
        () => $"Current HRProxy ID: {(Config.StreamerMode ? "********" : Config.HRProxyID)}",
        () => GenericPrecondition(Config.HRProxyID)
    );

    internal static IDataSourceDescriptor YURApp = RegisterDataSource<YURApp>(YUR_APP_KEY,
        () => DataSourceUtils.CheckYURProcess()
            ? "YUR App seems to be running."
            : "<color=#FFFF00>YUR App does not seem to be running.</color>",
        () => true
    );

    internal static IDataSourceDescriptor YURMod = RegisterDataSource<YURMod>(YUR_MOD_KEY,
        () => PluginManager.GetPluginFromId(DataSourceUtils.YUR_MOD_ID) == null
            ? "<color=#FF0000>YUR MOD IS NOT INSTALLED OR ENABLED!</color>"
            : "YUR MOD Detected!",
        () => PluginManager.GetPluginFromId(DataSourceUtils.YUR_MOD_ID) != null
    );

    internal static IDataSourceDescriptor OscServer = RegisterDataSource<OscHR>(OSC_KEY,
        () => Config.EnableOscServer
            ? $"Use addresses below (one Int32 value only)\n  {string.Join("\n  ", Config.OscAddress)}"
            : "<color=#FF0000>OSC Server is NOT enabled!</color>",
        () => true);

    internal static IDataSourceDescriptor HttpServer = RegisterDataSource<HttpServerDataSource>(HTTP_SERVER_KEY,
        () => Config.EnableHttpServer
            ? "POST to the <color=#00FF00>/hr</color> endpoint"
            : "<color=#FF0000>HTTP Server is NOT enabled!</color>",
        () => true
    );

    internal static IDataSourceDescriptor PulsoidWidget = RegisterDataSource<PulsoidWidget>(PULSOID_WIDEGT_KEY, async () =>
        {
            var status =
                $"Widget ID: {(GenericPrecondition(Config.PulsoidWidgetID) ? Config.StreamerMode ? "********" : Config.PulsoidWidgetID : "Not Set")}";
            return "<color=#FF5630>EXPERIMENTAL</color>\n" + status;
        },
        () => GenericPrecondition(Config.PulsoidToken)
    );

#if DEBUG

    private const string DEBUG_RANDOM_KEY = "Random Debug";
    internal static IDataSourceDescriptor Random = RegisterDataSource<RandomHR>(DEBUG_RANDOM_KEY, () => LOREM_IPSUM, () => true);

    private const string DEBUG_SWEEP_KEY = "Sweep Debug";
    internal static IDataSourceDescriptor Sweep = RegisterDataSource<SweepHR>(DEBUG_SWEEP_KEY, () => LOREM_IPSUM, () => true);

    private const string DEBUG_FPS_KEY = "FPS Debug";
    internal static IDataSourceDescriptor FrameRate = RegisterDataSource<FrameRateHR>(DEBUG_FPS_KEY, () => LOREM_IPSUM, () => true);

    private const string LOREM_IPSUM = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer tristique posuere libero eu gravida. " +
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
