using System.Collections.Generic;
using HRCounter.Configuration;
using HRCounter.Utils;
using HRCounter.Web.HTTP;
using HRCounter.Web.HTTP.Handlers;
using HRCounter.Web.OSC;
using HRCounter.Web.OSC.Handlers;
using IPA.Loader;
using IPA.Logging;
using Zenject;

namespace HRCounter.Installers;

public class AppInstaller : Installer<AppInstaller>
{
    private readonly PluginConfig _config;
    private readonly Logger _logger;
    private readonly PluginMetadata _pluginMetadata;

    private readonly Dictionary<string, Logger> _loggers = new();

    private AppInstaller(PluginConfig config, Logger logger, PluginMetadata pluginMetadata)
    {
        _config = config;
        _logger = logger;
        _pluginMetadata = pluginMetadata;
    }

    public override void InstallBindings()
    {
        Container.Bind<Logger>().FromMethod(CreateChildLogger).AsTransient().When(ShouldBindLogger);

        Container.BindInstance(_config).AsSingle();
        Container.BindInterfacesAndSelfTo<AssetBundleManager>().AsSingle();
        Container.BindInterfacesAndSelfTo<IconManager>().AsSingle();
        Container.BindInterfacesAndSelfTo<UserInfoHelper>().AsSingle();

        // Web stuff
        Container.BindInterfacesAndSelfTo<SimpleHttpServer>().AsSingle();
        Container.BindInterfacesTo<HttpConfigHandler>().AsSingle();
        Container.BindInterfacesAndSelfTo<HttpHRHandler>().AsSingle();

        Container.BindInterfacesAndSelfTo<SimpleOscServer>().AsSingle();
        Container.BindInterfacesAndSelfTo<OscHRHandler>().AsSingle();
    }

    private Logger CreateChildLogger(InjectContext context)
    {
        var name = context.ObjectType.Name;
        if (_loggers.TryGetValue(name, out var logger))
        {
            _logger.Spam($"Using cached child logger for {name}");
            return logger;
        }

        _logger.Spam($"Creating child logger for {name}");
        logger = _logger.GetChildLogger(name);
        _loggers[name] = logger;
        return logger;
    }

    private bool ShouldBindLogger(InjectContext context) => context.ObjectType.Assembly == _pluginMetadata.Assembly;
}
