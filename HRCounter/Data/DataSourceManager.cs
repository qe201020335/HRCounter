using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Data.DataSources;
using HRCounter.Data.SourceDescriptors;
using HRCounter.Utils;
using IPA.Loader;
using IPA.Utilities.Async;
using JetBrains.Annotations;
using Zenject;
using Component = UnityEngine.Component;
using IPALogger = IPA.Logging.Logger;
#if DEBUG
using HRCounter.Data.DataSources.DebugSource;
using HRCounter.Data.SourceDescriptors.DebugDescriptor;
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

    [Inject]
    private readonly IPALogger _logger = null!;

    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly DiContainer _diContainer = null!;

    private readonly Dictionary<string, IDataSourceDescriptor> _sources = new(StringComparer.InvariantCultureIgnoreCase);

    internal IReadOnlyDictionary<string, IDataSourceDescriptor> DataSources => _sources;

    [Inject]
    [UsedImplicitly]
    private void Init()
    {
        RegisterInternalDataSources();
        UpdateStreamerMode();
        _config.PropertyChanged += OnConfigChanged;
    }

    void IDisposable.Dispose()
    {
        _config.PropertyChanged -= OnConfigChanged;
        foreach (var pair in _sources)
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

    private void OnConfigChanged(object? _, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(_config.StreamerMode))
        {
            //TODO really should make config raise this on the main thread
            UnityMainThreadTaskScheduler.Factory.StartNew(UpdateStreamerMode);
        }
    }

    private void UpdateStreamerMode()
    {
        var streamerMode = _config.StreamerMode;
        foreach (var pair in _sources)
        {
            try
            {
                pair.Value.StreamerMode = streamerMode;
            }
            catch (Exception exception)
            {
                _logger.Warn($"Failed to set streamer mode for data source descriptor {pair.Value.Key}");
                _logger.Warn(exception);
            }
        }
    }

    internal IDataSourceDescriptor? GetFromKey(string str) => _sources.GetValueOrDefault(str);

    public void RegisterDataSource<T>(string key, Func<CancellationToken, Task<string>> getStatusText,
        Func<bool> precondition) where T : class, IHRDataSource
    {
        var descriptor = new GenericSourceDescriptor<T>(key, getStatusText, precondition);
        RegisterDataSource(descriptor);
    }

    public void RegisterDataSource<T>(string key, Func<string> getStatusText, Func<bool> precondition)
        where T : class, IHRDataSource
    {
        RegisterDataSource<T>(key, _ => Task.FromResult(getStatusText()), precondition);
    }

    public void RegisterDataSource<T>(IDataSourceDescriptor<T> descriptor) where T : class, IHRDataSource
    {
        var key = descriptor.Key;
        if (_sources.ContainsKey(key)) throw new ArgumentException($"Key {key} already exists!", nameof(key));
        _logger.Debug("Registering data source: " + key);
        _sources.Add(descriptor.Key, descriptor);
    }

    public void RegisterDataSource<TDesc, TSource>() where TDesc : class, IDataSourceDescriptor<TSource> where TSource : class, IHRDataSource
    {
        TDesc source;
        if (typeof(Component).IsAssignableFrom(typeof(TDesc)))
        {
            _logger.Spam($"Instantiating {typeof(TDesc).Name} on new game object");
            var instance = _diContainer.InstantiateComponent(typeof(TDesc), _diContainer.CreateEmptyGameObject(typeof(TDesc).Name));
            // ReSharper disable once SuspiciousTypeConversion.Global
            source = (TDesc)(object)instance;
            instance.gameObject.name = $"HRCounter Data Source Descriptor - {source.Key}";
        }
        else
        {
            _logger.Spam($"Instantiating {typeof(TDesc).Name} as normal object");
            source = _diContainer.Instantiate<TDesc>();
        }

        RegisterDataSource(source);
    }

    #region Internal Data Sources

    private void RegisterInternalDataSources()
    {
        _logger.Debug("Registering internal data sources");
        RegisterDataSource(new SimpleSourceDescriptor<HypeRate2>(HYPERATE_KEY, "HypeRate ID",
            () => _config.HypeRateSessionID, _config, nameof(_config.HypeRateSessionID)));

        RegisterDataSource<PulsoidDescriptor, Pulsoid2>();

        RegisterDataSource(new SimpleSourceDescriptor<WebRequest>(WEBREQUEST_KEY, "Request URL",
            () => _config.FeedLink, _config, nameof(_config.FeedLink)));

        RegisterDataSource(new SimpleSourceDescriptor<HRProxyCustomReader>(HRPROXY_KEY, "HRProxy ID",
            () => _config.HRProxyID, _config, nameof(_config.HRProxyID)));

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

        RegisterDataSource<OscDescriptor, OscHR>();

        RegisterDataSource<HttpServerDescriptor, HttpServerDataSource>();

        RegisterDataSource(new SimpleSourceDescriptor<PulsoidWidget>(PULSOID_WIDEGT_KEY, "<color=#FF5630>EXPERIMENTAL</color>\nWidget ID",
            () => _config.PulsoidWidgetID, _config, nameof(_config.PulsoidWidgetID)));

#if DEBUG
        RegisterDataSource<RandomHR>(DEBUG_RANDOM_KEY, () => LOREM_IPSUM, () => true);
        RegisterDataSource<SweepHR>(DEBUG_SWEEP_KEY, () => LOREM_IPSUM, () => true);
        RegisterDataSource<FrameRateDescriptor, FrameRateHR>();
#endif
    }

#if DEBUG

    private const string DEBUG_RANDOM_KEY = "Random Debug";
    private const string DEBUG_SWEEP_KEY = "Sweep Debug";
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
