using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Utils;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace HRCounter.Installers
{
    public class GameplayHearRateInstaller : Installer<GameplayHearRateInstaller>
    {
        [Inject] private readonly PluginConfig _config = null!;
        [Inject] private readonly SiraLog _logger = null!;

        public override void InstallBindings()
        {
            if (!_config.ModEnable)
            {
                return;
            }

            if (!DataSourceManager.TryGetFromStr(_config.DataSource, out var dataSource))
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
                Container.BindInterfacesTo(dataSource.DataSourceType).AsSingle();
            }
            
            _logger.Debug("binding hr controller");
            Container.BindInterfacesAndSelfTo<HRDataManager>().AsSingle().NonLazy();
        }
    }
}