using HRCounter.Configuration;
using HRCounter.Installers;
using IPA;
using IPA.Loader;
using IPA.Logging;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter;

[Plugin(RuntimeOptions.SingleStartInit)]
[NoEnableDisable]
public class Plugin
{
    internal static Plugin Instance { get; private set; } = null!;
    internal static IPALogger Logger { get; private set; } = null!;
    internal static PluginConfig Config { get; private set; } = null!;

    // private readonly HarmonyLib.Harmony _harmony = new HarmonyLib.Harmony("com.github.qe201020335.HRCounter");

    internal PluginMetadata Metadata { get; }
    internal PluginMetadata? BSMLMeta { get; }
    internal PluginMetadata? ScoreSaberMeta { get; }
    internal PluginMetadata? BeatLeaderMeta { get; }

    private const string BSMLId = "BeatSaberMarkupLanguage";
    private const string ScoreSaberId = "ScoreSaber";
    private const string BeatLeaderId = "BeatLeader";

    internal string UserAgent { get; }

    [Init]
    public Plugin(IPALogger logger, IPA.Config.Config conf, PluginMetadata metadata, Zenjector zenject)
    {
        Instance = this;
        Logger = logger;
        var config = PluginConfig.Initialize(GetChildLogger(nameof(PluginConfig)), conf);
        Config = config;

        Metadata = metadata;
        BSMLMeta = Utils.Utils.FindEnabledPluginMetadata(BSMLId);
        ScoreSaberMeta = Utils.Utils.FindEnabledPluginMetadata(ScoreSaberId);
        BeatLeaderMeta = Utils.Utils.FindEnabledPluginMetadata(BeatLeaderId);

        UserAgent = $"{metadata.Id}/{metadata.HVersion}";

        zenject.UseMetadataBinder<Plugin>();

        zenject.Install<AppInstaller>(Location.App, config, GetChildLogger(nameof(AppInstaller)), metadata);
        zenject.Install<MenuInstaller>(Location.Menu);
        zenject.Install<GameplayHeartRateInstaller>(Location.Player);
        zenject.Install<GameInstaller>(Location.Player);
        // we don't want to popup the pause menu during multiplayer, that's not gonna help anything!
        zenject.Install<GamePauseInstaller>(Location.StandardPlayer | Location.CampaignPlayer);

        if (BeatLeaderMeta != null) zenject.Install<ReplayRecorderInstaller>(Location.Player);

        zenject.Expose<FlyingGameHUDRotation>("Environment");

        Logger.Info("HRCounter initialized.");
    }

    internal static IPALogger GetChildLogger(string name) => Logger.GetChildLogger(name);
}
