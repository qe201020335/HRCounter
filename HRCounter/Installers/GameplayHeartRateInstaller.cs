using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Data.DataSources;
using HRCounter.Utils;
using Zenject;

namespace HRCounter.Installers
{
    public class GameplayHearRateInstaller : Installer<GameplayHearRateInstaller>
    {
        private static PluginConfig Config => PluginConfig.Instance;

        public override void InstallBindings()
        {
            if (!PluginConfig.Instance.ModEnable)
            {
                return;
            }

            var dataSource = DataSourceType.GetFromStr(Config.DataSource);
            if (dataSource == null)
            {
                Log.Logger.Error($"Unknown data source: {Config.DataSource}");
                return;
            }

            if (dataSource.NeedWebSocket && !DataSourceUtils.WebSocketSharpInstalled)
            {
                Log.Logger.Error(
                    $"{DataSourceUtils.WEBSOCKET_SHARP_MOD_ID} is not installed but required for the data source {dataSource}, NOT BINDING!");
                return;
            }

            if (!dataSource.PreconditionMet())
            {
                Log.Logger.Warn($"{dataSource} precondition not met! Did you set your link/id/token ?");
                return;
            }

            Log.Logger.Debug("Binding BPM Downloader");
            Container.Bind<DataSource>().To(dataSource.DataSourcerType).AsSingle();
            Log.Logger.Debug("binding hr controller");
            Container.BindInterfacesAndSelfTo<HRDataManager>().AsSingle().NonLazy();
        }
    }
}