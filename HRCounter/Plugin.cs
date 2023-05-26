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
        
        // private readonly HarmonyLib.Harmony _harmony = new HarmonyLib.Harmony("com.github.qe201020335.HRCounter");


        [Init]
        public void InitWithConfig(IPALogger logger, IPA.Config.Config conf, Zenjector zenject)
        {
            Instance = this;
            global::HRCounter.Log.Logger = logger;
            Log = logger;
            var config = conf.Generated<Configuration.PluginConfig>();
            Configuration.PluginConfig.Instance = config;
            Log.Debug("Config loaded");
            zenject.UseLogger(logger);
            zenject.UseMetadataBinder<Plugin>();
            
            zenject.Install<AppInstaller>(Location.App, config);
            zenject.Install<MenuInstaller>(Location.Menu);
            zenject.Install<GameplayHearRateInstaller>(Location.Player);
            zenject.Install<Installers.GameplayCoreInstaller>(Location.Player);
            // we don't want to popup the pause menu during multiplayer, that's not gonna help anything!
            zenject.Install<GamePauseInstaller>(Location.StandardPlayer | Location.CampaignPlayer); 
            
            Log.Info("HRCounter initialized.");
        }
    }
}
