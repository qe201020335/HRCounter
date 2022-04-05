using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaberMarkupLanguage;
using IPA;
using IPA.Config.Stores;
using IPALogger = IPA.Logging.Logger;
// using UnityEngine.SceneManagement;

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
        /// <summary>
        /// Called when the plugin is first loaded by IPA (either when the game starts or when the plugin is enabled if it starts disabled).
        /// [Init] methods that use a Constructor or called before regular methods like InitWithConfig.
        /// Only use [Init] with one Constructor.
        /// </summary>
        public void Init(IPALogger logger)
        {
            Instance = this;
            Logger.logger = logger;
            Log = logger;
            Log.Info("HRCounter initialized.");
        }

        #region BSIPA Config

        [Init]
        public void InitWithConfig(IPA.Config.Config conf)
        {
            Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
            Logger.logger = Log;
            Log.Debug("Config loaded");
        }

        #endregion
        
        [OnStart]
        public void OnStart() {
            MenuButtons.instance.RegisterButton(MenuButton);
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
