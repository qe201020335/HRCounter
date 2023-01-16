﻿using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Data.BpmDownloaders;
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
            Logger.logger.Debug("Binding BPM Downloader");
            if (!Utils.Utils.IsModEnabled(DataSourceUtils.WEBSOCKET_SHARP_MOD_ID) && DataSourceUtils.NeedWebSocket(Config.DataSource))
            {
                Logger.logger.Error($"{DataSourceUtils.WEBSOCKET_SHARP_MOD_ID} is not installed but required for the data source {Config.DataSource}, NOT BINDING!");
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
                
                case "Pulsoid Token":
                    if (Config.PulsoidToken == "NotSet")
                    {
                        Logger.logger.Warn("Pulsoid Token not set.");
                        return;
                    }
                    Container.Bind<BpmDownloader>().To<Pulsoid>().AsSingle();
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
                
                case "HRProxy":
                    if (Config.HRProxyID == "NotSet")
                    {
                        Logger.logger.Warn("HRProxy ID not set.");
                        return;
                    }
                    Container.Bind<BpmDownloader>().To<HRProxy>().AsSingle();
                    break;
                
                case "YUR APP":
                    Container.Bind<BpmDownloader>().To<YURApp>().AsSingle();
                    break;
                
                case "YUR MOD":
                    if (!Utils.Utils.IsModEnabled(DataSourceUtils.YUR_MOD_ID))
                    {
                        Logger.logger.Error($"{DataSourceUtils.YUR_MOD_ID} is not installed but required for the data source {Config.DataSource}, NOT BINDING!");
                        return;
                    }
                    Container.Bind<BpmDownloader>().To<YURMod>().AsSingle();
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