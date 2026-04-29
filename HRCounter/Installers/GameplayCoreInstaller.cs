using System;
using BeatLeader.Replayer;
using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Data.Replay;
using HRCounter.Utils;
using IPA.Logging;
using Zenject;

namespace HRCounter.Installers;

public class GameplayCoreInstaller : Installer<GameplayCoreInstaller>
{
    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly UserInfoHelper _userInfoHelper = null!;

    private const string COUNTERS_PLUS_MOD_ID = "Counters+";

    public override void InstallBindings()
    {
        if (!_config.ModEnable)
        {
            return;
        }

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

        if (!_config.IgnoreCountersPlus && Utils.Utils.IsModEnabled(COUNTERS_PLUS_MOD_ID))
        {
            _logger.Info("Counters+ mod is enabled! Not binding standalone counter!");
            return;
        }

        _logger.Debug("Binding HR Counter");
        Container.BindInterfacesTo<HRCounterStandalone>().AsSingle().NonLazy();
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

        //todo
        var currenUser = _userInfoHelper.UserInfo;
        if (currenUser == null)
        {
            _logger.Warn("Current user info is null");
        }
        else
        {
            _logger.Spam($"Current user: {currenUser.userName} ({currenUser.platformUserId})");
        }

        var replayPlayer = playerData?.id;
        var currentPlayer = currenUser?.platformUserId;
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
