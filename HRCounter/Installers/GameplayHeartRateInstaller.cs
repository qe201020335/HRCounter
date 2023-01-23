using System.Linq;
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
            Log.Logger.Debug("Binding BPM Downloader");
            if (!Utils.Utils.IsModEnabled(DataSourceUtils.WEBSOCKET_SHARP_MOD_ID) && DataSourceUtils.NeedWebSocket(Config.DataSource))
            {
                Log.Logger.Error($"{DataSourceUtils.WEBSOCKET_SHARP_MOD_ID} is not installed but required for the data source {Config.DataSource}, NOT BINDING!");
                return;
            }
            
            switch (Config.DataSource)
            {
                case "WebRequest":
                    if (Config.FeedLink == "NotSet")
                    {
                        Log.Logger.Warn("Feed link not set.");
                        return;
                    }
                    Container.Bind<DataSource>().To<WebRequest>().AsSingle();
                    break;
                
                case "Pulsoid Token":
                    if (Config.PulsoidToken == "NotSet")
                    {
                        Log.Logger.Warn("Pulsoid Token not set.");
                        return;
                    }
                    Container.Bind<DataSource>().To<Pulsoid>().AsSingle();
                    break;
                
                case "HypeRate":
                    if (Config.HypeRateSessionID == "-1")
                    {
                        Log.Logger.Warn("Hype Rate Session ID not set.");
                        return;
                    }
                    Container.Bind<DataSource>().To<HRProxy>().AsSingle();
                    break;
                    
                case "Pulsoid":
                    if (Config.PulsoidWidgetID == "NotSet")
                    {
                        Log.Logger.Warn("Pulsoid Widget ID not set.");
                        return;
                    }
                    Container.Bind<DataSource>().To<HRProxy>().AsSingle();
                    break;

                case "FitbitHRtoWS":
                    if(Config.FitbitWebSocket == string.Empty)
                    {
                        Log.Logger.Warn("FitbitWebSocket is empty.");
                        return;
                    }
                    Container.Bind<DataSource>().To<FitbitHRtoWS>().AsSingle();
                    break;
                
                case "HRProxy":
                    if (Config.HRProxyID == "NotSet")
                    {
                        Log.Logger.Warn("HRProxy ID not set.");
                        return;
                    }
                    Container.Bind<DataSource>().To<HRProxy>().AsSingle();
                    break;
                
                case "YUR APP":
                    Container.Bind<DataSource>().To<YURApp>().AsSingle();
                    break;
                
                case "YUR MOD":
                    if (!Utils.Utils.IsModEnabled(DataSourceUtils.YUR_MOD_ID))
                    {
                        Log.Logger.Error($"{DataSourceUtils.YUR_MOD_ID} is not installed but required for the data source {Config.DataSource}, NOT BINDING!");
                        return;
                    }
                    Container.Bind<DataSource>().To<YURMod>().AsSingle();
                    break;
                
                case "Random":
                    Container.Bind(new[] { typeof(DataSource) }.Concat(typeof(DataSource).GetInterfaces())).To<RandomHR>().AsSingle();
                    Container.Bind<DataSource>().To<RandomHR>().AsSingle();
                    break;

                default:
                    Log.Logger.Warn("Unknown Data Sources");
                    break;
            }

            if (Container.HasBinding<DataSource>())
            {
                Log.Logger.Debug("binding hr controller");
                Container.BindInterfacesAndSelfTo<HRDataManager>().AsSingle().NonLazy();
            }
            else
            {
                Log.Logger.Debug("no downloader bounded");
            }
        }
    }
}