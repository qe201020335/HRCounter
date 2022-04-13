using System.Linq;
using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaberMarkupLanguage;
using HRCounter.Data;
using System;
using System.Reflection;
using HarmonyLib;
using HRCounter.Data.BpmDownloaders;
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
            
            if (Configuration.PluginConfig.Instance.YURModIntegration)
            {
                if (Utils.Utils.IsModEnabled("YUR.fit-BeatSaber-Mod"))
                {
                    // YUR Exists! Lets get some data!
                    Logger.logger.Log(IPALogger.Level.Info, "Found Yur Mod!");
                    Assembly YurModAssembly = Utils.Utils.GetModAssembly("YUR.fit-BeatSaber-Mod");
                    if (YurModAssembly != null)
                    {
                        // Found the YUR Mod; use Harmony to postfix ActivityViewController.OverlayUpdateAction
                        Type avcType = YurModAssembly.GetType("YUR.ViewControllers.ActivityViewController");
                        if (avcType != null)
                        {
                            Harmony harmony = new Harmony("HRCounter.YURPatch");

                            MethodInfo mOriginal = AccessTools.Method(avcType, "OverlayUpdateAction");
                            MethodInfo mPostfix = typeof(ActivityViewControllerPatch).GetMethod("OverlayUpdateAction");

                            harmony.Patch(mOriginal, postfix: new HarmonyMethod(mPostfix));
                            Logger.logger.Log(IPALogger.Level.Info, "Patched OverlayUpdateAction()!");
                        }
                        else
                            Logger.logger.Error("Failed to find the YUR Mod's ActivityViewController!");
                    }
                    else
                        Logger.logger.Error("Found the YUR Mod, but can't get it's assembly!");
                }
                else
                    Logger.logger.Warn("YURModIntegration is enabled, but failed to find the YUR Mod!");
            }
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
