using System.Linq;
using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaberMarkupLanguage;
using HRCounter.Data;
using IPA;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;
using HRCounter.Installers;
using IPA.Loader;

namespace HRCounter
{

    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }

        internal static string Name => "HR Counter";

        internal MenuButton MenuButton = new MenuButton("HRCounter", "Display your heart rate in game!", OnMenuButtonClick, true);

        private UI.ConfigViewFlowCoordinator _configViewFlowCoordinator;

        [Init]
        public void InitWithConfig(IPALogger logger, IPA.Config.Config conf, Zenjector zenject)
        {
            Instance = this;
            Logger.logger = logger;
            Log = logger;
            Log.Info("HRCounter initialized.");
            Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
            Log.Debug("Config loaded");
            AssetBundleManager.LoadAssetBundle();
            zenject.Install<GameplayHearRateInstaller>(Location.Player);
            zenject.Install<Installers.GameplayCoreInstaller>(Location.Player);
            zenject.Install<GamePauseInstaller>(Location.StandardPlayer | Location.CampaignPlayer); 
            // we don't want to popup the menu during mp, that's not gonna help

            Log.Debug("Installers!");
        }

        [OnStart]
        public void OnStart() {
            MenuButtons.instance.RegisterButton(MenuButton);
            HRController.ClearThings();
        }
        
        private static void OnMenuButtonClick()
        {
            if (Instance._configViewFlowCoordinator == null)
            {
                Instance._configViewFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<UI.ConfigViewFlowCoordinator>();
            }
            BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(Instance._configViewFlowCoordinator);
        }
    }
}
