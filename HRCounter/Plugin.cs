using System.Diagnostics;
using HRCounter.Configuration;
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
        internal static Plugin Instance { get; private set; } = null!;
        internal static IPALogger Logger { get; private set; } = null!;
        
        // private readonly HarmonyLib.Harmony _harmony = new HarmonyLib.Harmony("com.github.qe201020335.HRCounter");
        
        internal static PluginMetadata? BSMLMeta { get; private set; } = null;
        internal static PluginMetadata? ScoreSaberMeta { get; private set; } = null;
        internal static PluginMetadata? BeatLeaderMeta { get; private set; } = null;
        
        private const string BSMLId = "BeatSaberMarkupLanguage";
        private const string ScoreSaberId = "ScoreSaber";
        private const string BeatLeaderId = "BeatLeader";


        [Init]
        public void InitWithConfig(IPALogger logger, IPA.Config.Config conf, Zenjector zenject)
        {
            Instance = this;
            Logger = logger;
            var config = conf.Generated<PluginConfig>();
            PluginConfig.Instance = config;
            Logger.Debug("Config loaded");
            zenject.UseLogger(logger);
            zenject.UseMetadataBinder<Plugin>();
            
            zenject.Install<AppInstaller>(Location.App, config);
            zenject.Install<MenuInstaller>(Location.Menu);
            zenject.Install<GameplayHearRateInstaller>(Location.Player);
            zenject.Install<Installers.GameplayCoreInstaller>(Location.Player);
            // we don't want to popup the pause menu during multiplayer, that's not gonna help anything!
            zenject.Install<GamePauseInstaller>(Location.StandardPlayer | Location.CampaignPlayer);
            
            zenject.Expose<FlyingGameHUDRotation>("Environment");
            
            Logger.Info("HRCounter initialized.");
        }
        
        [OnStart]
        public void OnEnable()
        {
            BSMLMeta = Utils.Utils.FindEnabledPluginMetadata(BSMLId);
            ScoreSaberMeta = Utils.Utils.FindEnabledPluginMetadata(ScoreSaberId);
            BeatLeaderMeta = Utils.Utils.FindEnabledPluginMetadata(BeatLeaderId);
        }
        
        [Conditional("DEBUG")]
        internal static void DebugSpam(string s)
        {
            if (PluginConfig.Instance.DebugSpam)
            {
                Logger.Trace(s);
            }
        }
    }
}
