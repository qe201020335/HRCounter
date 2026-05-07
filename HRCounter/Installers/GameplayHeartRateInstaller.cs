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

        if (!DataSourceManager.TryGetFromKey(_config.DataSource, out var dataSource))
        {
            _logger.Error($"Unknown data source: {_config.DataSource}");
            return;
        }

        if (!dataSource.PreconditionSatisfied())
        {
            _logger.Warn($"{dataSource} precondition not met! Did you set your link/id/token or install the required dependencies?");
            return;
        }

        _logger.Debug("Binding BPM Downloader");
        if (typeof(Component).IsAssignableFrom(dataSource.DataSourceType))
        {
            Container.BindInterfacesAndSelfTo(dataSource.DataSourceType)
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName($"HRCounter {dataSource.Key} Data Source")
                .AsSingle();
        }
        else
        {
            Container.BindInterfacesAndSelfTo(dataSource.DataSourceType).AsSingle();
        }

        _logger.Debug("binding hr controller");
        Container.BindInterfacesAndSelfTo<HRDataManager>().AsSingle().NonLazy();
    }
}
