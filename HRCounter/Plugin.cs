using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaberMarkupLanguage;
using HRCounter.Data;
using IPA;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;
using HRCounter.Installers;
using HarmonyLib;
using HRCounter.Harmony;
using YUR.ViewControllers;

namespace HRCounter
{

    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        private const string YUR_MOD_ID = "YUR Fit Calorie Tracker";
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }

        internal static string Name => "HR Counter";

        internal MenuButton MenuButton = new MenuButton("HRCounter", "Display your heart rate in game!", OnMenuButtonClick, true);

        private UI.ConfigViewFlowCoordinator _configViewFlowCoordinator;
        
        private readonly HarmonyLib.Harmony _harmony = new HarmonyLib.Harmony("com.github.qe201020335.HRCounter");


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
            PatchYURMod();
        }

        [OnExit]
        public void OnExit()
        {
            MenuButtons.instance.UnregisterButton(MenuButton);
            UnPatchYURMod();
        }
        
        private static void OnMenuButtonClick()
        {
            if (Instance._configViewFlowCoordinator == null)
            {
                Instance._configViewFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<UI.ConfigViewFlowCoordinator>();
            }
            BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(Instance._configViewFlowCoordinator);
        }

        private void PatchYURMod()
        {
            if (!Utils.Utils.IsModEnabled(YUR_MOD_ID))
            {
                Logger.logger.Info("YUR Mod is not enabled");
                return;
            }

            var mOriginal =
                typeof(ActivityViewController).GetMethod(nameof(ActivityViewController.OverlayUpdateAction));
            var mPostfix =
                typeof(YURActivityViewControllerPatch).GetMethod(
                    nameof(YURActivityViewControllerPatch.OverlayUpdateAction),
                    BindingFlags.Static | BindingFlags.NonPublic);

            if (mOriginal == null || mPostfix == null)
            {
                Logger.logger.Error("At least one of patch methods is null! Not Patching YUR MOD!");
                Logger.logger.Error($"{mOriginal}, {mPostfix}");
            }
            else
            {
                _harmony.Patch(mOriginal, postfix: new HarmonyMethod(mPostfix));

            }
        }

        private void UnPatchYURMod()
        {
            if (!Utils.Utils.IsModEnabled(YUR_MOD_ID))
            {
                Logger.logger.Info("YUR Mod is not enabled");
                return;
            }
            var mOriginal =
                typeof(ActivityViewController).GetMethod(nameof(ActivityViewController.OverlayUpdateAction));

            if (mOriginal == null)
            {
                Logger.logger.Error("Original method is null! Cannot unpatch!");
            }
            else
            {
                _harmony.Unpatch(mOriginal, HarmonyPatchType.All);

            }
        }
    }
}
