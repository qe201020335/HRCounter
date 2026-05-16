using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Data.DataSources;
using HRCounter.Utils;
using IPA.Loader;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;
using Logger = IPA.Logging.Logger;
#if DEBUG
using HRCounter.Data.DataSources.DebugSource;
#endif

namespace HRCounter.Data;

public sealed class DataSourceManager : IDisposable
{
    private const string HYPERATE_KEY = "HypeRate";
    private const string PULSOID_WIDEGT_KEY = "PulsoidWidget";
    private const string WEBREQUEST_KEY = "WebRequest";
    private const string HRPROXY_KEY = "HRProxy";
    private const string YUR_APP_KEY = "YUR APP";
    private const string YUR_MOD_KEY = "YUR MOD";
    private const string HTTP_SERVER_KEY = "HttpServer";
    private const string OSC_KEY = "OSC Protocol";

    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly DiContainer _diContainer = null!;

    private readonly Dictionary<string, IDataSourceDescriptor> SourceTypes = new(StringComparer.InvariantCultureIgnoreCase);

    internal IReadOnlyDictionary<string, IDataSourceDescriptor> DataSourceTypes => SourceTypes;

    [Inject]
    [UsedImplicitly]
    private void Init()
    {
        RegisterInternalDataSources();
    }

    public IDataSourceDescriptor RegisterDataSource<T>(string key, Func<CancellationToken, Task<string>> getStatusText,
        Func<bool> precondition) where T : class, IHRDataSource
    {
        var descriptor = new GenericSourceDescriptor<T>(key, getStatusText, precondition);
        RegisterDataSource(descriptor);
        return descriptor;
    }

    public IDataSourceDescriptor RegisterDataSource<T>(string key, Func<string> getStatusText, Func<bool> precondition)
        where T : class, IHRDataSource
    {
        return RegisterDataSource<T>(key, _ => Task.FromResult(getStatusText()), precondition);
    }

    public void RegisterDataSource<T>(IDataSourceDescriptor<T> descriptor) where T : class, IHRDataSource
    {
        var key = descriptor.Key;
        if (SourceTypes.ContainsKey(key)) throw new ArgumentException($"Key {key} already exists!", nameof(key));
        _logger.Debug("Registering data source: " + key);
        SourceTypes.Add(descriptor.Key, descriptor);
    }

    public void RegisterDataSource<TDesc, TSource>() where TDesc : class, IDataSourceDescriptor<TSource> where TSource : class, IHRDataSource
    {
        TDesc source;
        if (typeof(Component).IsAssignableFrom(typeof(TDesc)))
        {
            var instance = _diContainer.InstantiateComponent(typeof(TDesc), _diContainer.CreateEmptyGameObject(typeof(TDesc).Name));
            // ReSharper disable once SuspiciousTypeConversion.Global
            source = (TDesc)(object)instance;
            instance.gameObject.name = $"HRCounter Data Source Descriptor - {source.Key}";
        }
        else
        {
            source = _diContainer.Instantiate<TDesc>();
        }

        RegisterDataSource(source);
    }

    internal IDataSourceDescriptor? GetFromKey(string str) => SourceTypes.GetValueOrDefault(str);

    void IDisposable.Dispose()
    {
        foreach (var pair in SourceTypes)
        {
            try
            {
                pair.Value.Dispose();
            }
            catch (Exception e)
            {
                _logger.Warn($"Failed to dispose data source descriptor for {pair.Value.Key}: {e}");
                _logger.Warn(e);
            }
        }
    }

    private static bool GenericPrecondition(string s)
    {
        return !string.IsNullOrWhiteSpace(s) && s != "NotSet" && s != "-1";
    }

    #region Internal Data Sources

    private void RegisterInternalDataSources()
    {
        _logger.Debug("Registering internal data sources");
        // register internal sources
        RegisterDataSource<HypeRate2>(HYPERATE_KEY,
            () => $"Current Session ID: {(_config.StreamerMode ? "********" : _config.HypeRateSessionID)}",
            () => GenericPrecondition(_config.HypeRateSessionID)
        );

        RegisterDataSource<PulsoidSourceDescriptor, Pulsoid2>();

        RegisterDataSource<WebRequest>(WEBREQUEST_KEY,
            () => $"Current URL: {(_config.StreamerMode ? "********" : _config.FeedLink)}",
            () => GenericPrecondition(_config.FeedLink)
        );

        RegisterDataSource<HRProxyCustomReader>(HRPROXY_KEY,
            () => $"Current HRProxy ID: {(_config.StreamerMode ? "********" : _config.HRProxyID)}",
            () => GenericPrecondition(_config.HRProxyID)
        );

        RegisterDataSource<YURApp>(YUR_APP_KEY,
            () => DataSourceUtils.CheckYURProcess()
                ? "YUR App seems to be running."
                : "<color=#FFFF00>YUR App does not seem to be running.</color>",
            () => true
        );

        RegisterDataSource<YURMod>(YUR_MOD_KEY,
            () => PluginManager.GetPluginFromId(DataSourceUtils.YUR_MOD_ID) == null
                ? "<color=#FF0000>YUR MOD IS NOT INSTALLED OR ENABLED!</color>"
                : "YUR MOD Detected!",
            () => PluginManager.GetPluginFromId(DataSourceUtils.YUR_MOD_ID) != null
        );

        RegisterDataSource<OscHR>(OSC_KEY,
            () => _config.EnableOscServer
                ? $"Use addresses below (one Int32 value only)\n  {string.Join("\n  ", _config.OscAddress)}"
                : "<color=#FF0000>OSC Server is NOT enabled!</color>",
            () => true);

        RegisterDataSource<HttpServerDataSource>(HTTP_SERVER_KEY,
            () => _config.EnableHttpServer
                ? "POST to the <color=#00FF00>/hr</color> endpoint"
                : "<color=#FF0000>HTTP Server is NOT enabled!</color>",
            () => true
        );

        RegisterDataSource<PulsoidWidget>(PULSOID_WIDEGT_KEY, () =>
            {
                var status =
                    $"Widget ID: {(GenericPrecondition(_config.PulsoidWidgetID) ? _config.StreamerMode ? "********" : _config.PulsoidWidgetID : "Not Set")}";
                return "<color=#FF5630>EXPERIMENTAL</color>\n" + status;
            },
            () => GenericPrecondition(_config.PulsoidToken)
        );

#if DEBUG
        RegisterDataSource<RandomHR>(DEBUG_RANDOM_KEY, () => LOREM_IPSUM, () => true);
        RegisterDataSource<SweepHR>(DEBUG_SWEEP_KEY, () => LOREM_IPSUM, () => true);
        RegisterDataSource<FrameRateHR>(DEBUG_FPS_KEY, () => LOREM_IPSUM, () => true);
#endif
    }

#if DEBUG

    private const string DEBUG_RANDOM_KEY = "Random Debug";
    private const string DEBUG_SWEEP_KEY = "Sweep Debug";
    private const string DEBUG_FPS_KEY = "FPS Debug";
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
