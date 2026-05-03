using System;
using BeatLeader.Models;
using BeatLeader.Replayer;
using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Data.Replay;
using HRCounter.Utils;
using IPA.Logging;
using Zenject;
using Version = Hive.Versioning.Version;

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

    private static readonly Version BeatLeaderAbstractPlayerVersion = new(0, 9, 34);

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

    private static string? GetMember(object? obj, params string[] names)
    {
        if (obj == null) return null;
        var t = obj.GetType();
        foreach (var n in names)
        {
            var p = t.GetProperty(n)?.GetValue(obj) ?? t.GetField(n)?.GetValue(obj);
            if (p != null) return p.ToString();
        }

        return null;
    }

    private byte[]? GetReplayHeartRateCustomData()
    {
        if (Plugin.Instance.BeatLeaderMeta?.HVersion is null) return null;
        var blVersion = Plugin.Instance.BeatLeaderMeta.HVersion;

        // I didn't want to use this kind of reflection mess here but BeatLeader introduced api + abi breaking changes...
        var replayData = ReplayerLauncher.LaunchData?.MainReplay.ReplayData;
        var playerData = replayData?.GetType().GetProperty(nameof(replayData.Player))?.GetValue(replayData);
        string? playerId = null;
        if (playerData == null)
        {
            _logger.Warn("BeatLeader replay player data is null");
        }
        else
        {
            string? playerName;

            if (blVersion < BeatLeaderAbstractPlayerVersion)
            {
                _logger.Debug("BeatLeader old replay player api");
                // old api
                var type = playerData.GetType();
                playerName = type.GetField("name")?.GetValue(playerData) as string;
                playerId = type.GetField("id")?.GetValue(playerData) as string;
            }
            else
            {
                _logger.Debug("BeatLeader new replay player api");
                // new api from abstracted player
                var type = typeof(IPlayer);
                playerName = type.GetProperty("Name")?.GetValue(playerData) as string;
                playerId = type.GetProperty("Id")?.GetValue(playerData) as string;
            }

            _logger.Spam($"BeatLeader replay player: {playerName} ({playerId})");
        }

        var currenUser = _userInfoHelper.UserInfo;
        if (currenUser == null)
        {
            _logger.Warn("Current user info is null");
        }
        else
        {
            _logger.Spam($"Current user: {currenUser.userName} ({currenUser.platformUserId})");
        }

        var replayPlayer = playerId;
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

        return rawData;
    }

    private ReplayHRData? GetBeatLeaderReplayHRData()
    {
        if (!_config.ReplayPlaybackSelfHr && !_config.ReplayPlaybackOthersHr) return null;

        try
        {
            _logger.Info("Loading HR data from BeatLeader replay");
            var rawData = GetReplayHeartRateCustomData();
            return rawData == null ? null : ReplayHRDataConverter.Decode(rawData);
        }
        catch (Exception e)
        {
            _logger.Error("Failed to load HR data from BeatLeader replay");
            _logger.Error(e);
            return null;
        }
    }
}
