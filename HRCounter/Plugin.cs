using HRCounter.Configuration;
using HRCounter.Installers;
using IPA;
using IPA.Loader;
using IPA.Logging;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter;

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
    public Plugin(IPALogger logger, IPA.Config.Config conf, PluginMetadata metadata, Zenjector zenject)
    {
        Instance = this;
        Logger = logger;
        var config = PluginConfig.Initialize(logger.GetChildLogger(nameof(PluginConfig)), conf);

        zenject.UseLogger(logger);
        zenject.UseMetadataBinder<Plugin>();

        zenject.Install<AppInstaller>(Location.App, config, logger, metadata);
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
}
