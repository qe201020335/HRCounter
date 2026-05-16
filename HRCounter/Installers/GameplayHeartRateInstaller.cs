using HRCounter.Configuration;
using HRCounter.Data;
using UnityEngine;
using Zenject;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter.Installers;

public class GameplayHeartRateInstaller : Installer<GameplayHeartRateInstaller>
{
    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly IPALogger _logger = null!;

    public override void InstallBindings()
    {
        if (!_config.ModEnable)
        {
            return;
        }

        var source = DataSourceManager.GetFromKey(_config.DataSource);
        if (source is null)
        {
            _logger.Error($"Unknown data source: {_config.DataSource}");
            return;
        }

        if (!source.PreconditionMet())
        {
            _logger.Warn($"{source} precondition not met! Did you set your link/id/token or install the required dependencies?");
            return;
        }

        _logger.Debug("Binding BPM Downloader");
        if (typeof(Component).IsAssignableFrom(source.DataSourceType))
        {
            Container.BindInterfacesAndSelfTo(source.DataSourceType)
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName($"HRCounter {source.Key} Data Source")
                .AsSingle();
        }
        else
        {
            Container.BindInterfacesAndSelfTo(source.DataSourceType).AsSingle();
        }

        _logger.Debug("Binding hr controller");
        Container.BindInterfacesAndSelfTo<HRDataManager>().AsSingle().NonLazy();
    }
}
