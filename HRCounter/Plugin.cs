using System;
using System.Reflection;
using HarmonyLib;
using HRCounter.Data;
using IPA;
using IPA.Config;
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

            /*
            HRCounter.ScoreSaberInstalled = Utils.Utils.IsModInstalled("ScoreSaber");
            if (HRCounter.ScoreSaberInstalled)
            {
                logger.Info("ScoreSaber Detected.");
            }
            */
        }

        #region BSIPA Config

        [Init]
        public void InitWithConfig(Config conf)
        {
            Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
            Logger.logger = Log;
            Log.Debug("Config loaded");
        }

        #endregion
        [OnStart]
        public void OnApplicationStart() 
        {
            //SceneManager.activeSceneChanged += Utils.Utils.OnActiveSceneChanged;
            if (Configuration.PluginConfig.Instance.YURModIntegration)
            {
                if (Utils.Utils.IsModInstalled("YUR.fit-BeatSaber-Mod"))
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
    }
}
