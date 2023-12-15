using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Data.DataSources;
using HRCounter.Utils;
using SiraUtil.Logging;
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

            var dataSource = DataSourceType.GetFromStr(_config.DataSource);
            if (dataSource == null)
            {
                _logger.Error($"Unknown data source: {_config.DataSource}");
                return;
            }

            if (dataSource.NeedWebSocket && !DataSourceUtils.WebSocketSharpInstalled)
            {
                _logger.Error(
                    $"{DataSourceUtils.WEBSOCKET_SHARP_MOD_ID} is not installed but required for the data source {dataSource}, NOT BINDING!");
                return;
            }

            if (!dataSource.PreconditionMet())
            {
                _logger.Warn($"{dataSource} precondition not met! Did you set your link/id/token ?");
                return;
            }

            _logger.Debug("Binding BPM Downloader");
            Container.Bind<DataSource>().To(dataSource.DataSourcerType).AsSingle();
            _logger.Debug("binding hr controller");
            Container.BindInterfacesAndSelfTo<HRDataManager>().AsSingle().NonLazy();
        }
    }
}