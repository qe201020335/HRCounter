using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Data.BpmDownloaders;
using Zenject;

namespace HRCounter.Installers
{
    public class GameplayHearRateInstaller : Installer<GameplayHearRateInstaller>
    {
        private static PluginConfig Config => PluginConfig.Instance;

        public override void InstallBindings()
        {
            Logger.logger.Debug("Binding BPM Downloader");
            if (!Utils.Utils.IsModEnabled("websocket-sharp") && Utils.Utils.NeedWebSocket(Config.DataSource))
            {
                Logger.logger.Error($"websocket-sharp is not installed but required for the data source {Config.DataSource}, NOT BINDING!");
                return;
            }
            
            switch (Config.DataSource)
            {
                case "WebRequest":
                    if (Config.FeedLink == "NotSet")
                    {
                        Logger.logger.Warn("Feed link not set.");
                        return;
                    }
                    Container.Bind<BpmDownloader>().To<WebRequest>().AsSingle();
                    break;
                
                case "HypeRate":
                    if (Config.HypeRateSessionID == "-1")
                    {
                        Logger.logger.Warn("Hype Rate Session ID not set.");
                        return;
                    }
                    Container.Bind<BpmDownloader>().To<HRProxy>().AsSingle();
                    break;
                    
                case "Pulsoid":
                    if (Config.PulsoidWidgetID == "NotSet")
                    {
                        Logger.logger.Warn("Pulsoid Widget ID not set.");
                        return;
                    }
                    Container.Bind<BpmDownloader>().To<HRProxy>().AsSingle();
                    break;

                case "FitbitHRtoWS":
                    if(Config.FitbitWebSocket == string.Empty)
                    {
                        Logger.logger.Warn("FitbitWebSocket is empty.");
                        return;
                    }
                    Container.Bind<BpmDownloader>().To<FitbitHRtoWS>().AsSingle();
                    break;
                
                case "YUR APP":
                    Container.Bind<BpmDownloader>().To<YURApp>().AsSingle();
                    break;
                
                case "Random":
                    Container.Bind<BpmDownloader>().To<RandomHR>().AsSingle();
                    break;

                default:
                    Logger.logger.Warn("Unknown Data Sources");
                    break;
            }

            if (Container.HasBinding<BpmDownloader>())
            {
                Logger.logger.Debug("binding hr controller");
                Container.BindInterfacesAndSelfTo<HRController>().AsSingle().NonLazy();
            }
            else
            {
                Logger.logger.Debug("no downloader bounded");

            }
        }
    }
}