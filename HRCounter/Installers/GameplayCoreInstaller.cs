using System;
using BeatLeader.Replayer;
using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Data.Replay;
using HRCounter.Utils;
using IPA.Logging;
using OculusStudios.Platform.Core;
using Zenject;

namespace HRCounter.Installers;

public class GameplayCoreInstaller : Installer<GameplayCoreInstaller>
{
    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly IPlatform _platform = null!;

    [InjectOptional]
    private readonly GameplayCoreSceneSetupData? _sceneSetupData = null;

    private const string COUNTERS_PLUS_MOD_ID = "Counters+";

    public override void InstallBindings()
    {
        if (!_config.ModEnable)
        {
            return;
        }

        if (!_config.IgnoreCountersPlus && Utils.Utils.IsModEnabled(COUNTERS_PLUS_MOD_ID))
        {
            _logger.Info("Counters+ mod is enabled! Not binding!");
            return;
        }

        if (_sceneSetupData == null)
        {
            _logger.Warn("GameplayCoreSceneSetupData is null");
        }
        else if (_sceneSetupData.playerSpecificSettings.noTextsAndHuds)
        {
            _logger.Info("No Texts & HUDs");
        }
        else
        {
            ReplayHRData? data;
            if (Utils.Utils.IsInBLReplay() && (data = GetBeatLeaderReplayHRData()) != null)
            {
                _logger.Debug("Binding replay HR provider");
                Container.BindInstance(data).WhenInjectedInto<ReplayHRProvider>();
                Container.BindInterfacesAndSelfTo<ReplayHRProvider>().AsSingle();
            }
            else
            {
                _logger.Debug("Binding live HR provider");
                Container.BindInterfacesAndSelfTo<LiveHRProvider>().AsSingle();
            }

            _logger.Debug("Binding HR Counter");
            Container.BindInterfacesTo<HRCounterController>().AsSingle().NonLazy();
            _logger.Debug("HR Counter binded");
        }
    }

    private ReplayHRData? GetBeatLeaderReplayHRData()
    {
        if (!_config.ReplayPlaybackSelfHr && !_config.ReplayPlaybackOthersHr) return null;
        var playerData = ReplayerLauncher.LaunchData?.MainReplay.ReplayData.Player;
        if (playerData == null)
        {
            _logger.Warn("BeatLeader replay player data is null");
        }
        else
        {
            _logger.Spam($"BeatLeader replay player: {playerData.name} ({playerData.id})");
        }

        var user = _platform.user;
        if (user == null)
        {
            _logger.Warn("Current user is null");
        }
        else
        {
            _logger.Spam($"Current user: {user.displayName} ({user.userId})");
        }

        var replayPlayer = playerData?.id;
        var currentPlayer = (user?.userId)?.ToString();
        var idMatch = replayPlayer == currentPlayer;
        var shouldLoadHr = (idMatch && _config.ReplayPlaybackSelfHr) || (!idMatch && _config.ReplayPlaybackOthersHr);
        if (!shouldLoadHr)
        {
            _logger.Debug("Not loading HR data from BeatLeader replay");
            return null;
        }

        var rawData = ReplayerLauncher.GetMainReplayCustomData(ReplayHRDataConverter.DataKey);
        if (rawData == null)
        {
            _logger.Debug("No HR data in BeatLeader replay");
            return null;
        }

        _logger.Info("Loading HR data from BeatLeader replay");
        try
        {
            return ReplayHRDataConverter.Decode(rawData);
        }
        catch (Exception e)
        {
            _logger.Error("Failed to load HR data from BeatLeader replay");
            _logger.Error(e);
            return null;
        }
    }
}
