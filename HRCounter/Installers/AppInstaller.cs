using HRCounter.Configuration;
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

        // Web stuff
        Container.BindInterfacesTo<SimpleHttpServer>().AsSingle();
        Container.BindInterfacesTo<HttpConfigHandler>().AsSingle();
        Container.BindInterfacesAndSelfTo<HttpHRHandler>().AsSingle();

        Container.BindInterfacesTo<SimpleOscServer>().AsSingle();
        Container.BindInterfacesAndSelfTo<OscHRHandler>().AsSingle();
    }

    private Logger CreateChildLogger(InjectContext context)
    {
        return _logger.GetChildLogger(context.ObjectType.Name);
    }

    private bool ShouldBindLogger(InjectContext context)
    {
        return context.ObjectType.Assembly == _pluginMetadata.Assembly;
    }
}
