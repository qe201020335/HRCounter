using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaberMarkupLanguage;
using HRCounter.Data;
using IPA;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;
using HRCounter.Installers;

namespace HRCounter
{

    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; } = null!;
        internal static IPALogger Log { get; private set; } = null!;
        
        private readonly MenuButton _menuButton = new MenuButton("HRCounter", "Display your heart rate in game!", OnMenuButtonClick);

        private UI.ConfigViewFlowCoordinator? _configViewFlowCoordinator;
        
        private readonly HarmonyLib.Harmony _harmony = new HarmonyLib.Harmony("com.github.qe201020335.HRCounter");


        [Init]
        public void InitWithConfig(IPALogger logger, IPA.Config.Config conf, Zenjector zenject)
        {
            Instance = this;
            global::HRCounter.Log.Logger = logger;
            Log = logger;
            Log.Info("HRCounter initialized.");
            Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
            Log.Debug("Config loaded");
            AssetBundleManager.LoadAssetBundle();
            zenject.Install<GameplayHearRateInstaller>(Location.Player);
            zenject.Install<Installers.GameplayCoreInstaller>(Location.Player);
            // we don't want to popup the pause menu during multiplayer, that's not gonna help anything!
            zenject.Install<GamePauseInstaller>(Location.StandardPlayer | Location.CampaignPlayer); 
            
            Log.Debug("Installers!");
        }
        
        [OnStart]
        public void OnStart() {
            MenuButtons.instance.RegisterButton(_menuButton);
            HRDataManager.ClearThings();
        }

        [OnExit]
        public void OnExit()
        {
            MenuButtons.instance.UnregisterButton(_menuButton);
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
