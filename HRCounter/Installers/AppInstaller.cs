using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        BindLoggers();

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

    private void BindLoggers()
    {
        // Default logger binding 
        Container.Bind<Logger>().FromMethod(CreateChildLogger).AsTransient().When(ShouldBindLogger);

        // ID-ed logger binding
        var ids = _pluginMetadata.Assembly.GetTypes()
            .SelectMany(type => type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(field => field.FieldType == typeof(Logger))
            .Select(field => field.GetCustomAttribute<InjectAttribute>()?.Id)
            .Where(type => type != null)
            .Distinct();

        foreach (var id in ids)
        {
            _logger.Trace($"Binding Logger with ID {id}");
            Container.Bind<Logger>().WithId(id!).FromMethod(CreateChildLogger).AsTransient().When(ShouldBindLogger);
        }
    }

    private Logger CreateChildLogger(InjectContext context)
    {
        var id = context.Identifier;
        var name = (id as Type)?.Name ?? id?.ToString() ?? context.ObjectType.Name;

        if (_loggers.TryGetValue(name, out var logger))
        {
            _logger.Spam($"Using cached child logger for {name}");
            return logger;
        }

        _logger.Spam($"Creating child logger for {name}");
        logger = Plugin.GetChildLogger(name);
        _loggers[name] = logger;
        return logger;
    }

    private bool ShouldBindLogger(InjectContext context) => context.ObjectType.Assembly == _pluginMetadata.Assembly;
}
