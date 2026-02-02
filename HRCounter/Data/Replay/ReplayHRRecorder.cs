using System;
using System.Collections.Generic;
using BeatLeader;
using IPA.Loader;
using SiraUtil.Zenject;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace HRCounter.Data.Replay;

public class ReplayHRRecorder : IInitializable, IDisposable
{
    private const float MIN_DATA_INTERVAL = 0.25f;

    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly AudioTimeSyncController _audioTimeSyncController = null!;

    [Inject]
    private readonly ReplayRecorder _replayRecorder = null!;

    [InjectOptional]
    private readonly HRDataManager? _hrDataManager = null;

    private readonly PluginMetadata _pMetadata;

    private readonly List<ReplayHR> _data = new(0);

    private ReplayHR _prev = new() { SongTime = -1f };

    private ReplayHRRecorder(UBinder<Plugin, PluginMetadata> metadataBinder) => _pMetadata = metadataBinder.Value;

    void IInitializable.Initialize()
    {
        _logger.Trace("ReplayHRRecorder Initialize");
        if (_hrDataManager is null || !_hrDataManager.AllowReplayRecording) return;
        _logger.Debug("Data source allows replay recording");
        var songLength = _audioTimeSyncController.songLength;
        // preallocate size for 2 hr updates per second
        _data.Capacity = (int)songLength * 2;
        _hrDataManager.OnHRUpdate += OnHRUpdated;
        _replayRecorder.OnFinalizeReplay += OnFinalizeReplay;
        _logger.Info("Start recording heart rate for replay");
    }

    void IDisposable.Dispose()
    {
        _logger.Trace("ReplayHRRecorder Dispose");
        _replayRecorder.OnFinalizeReplay -= OnFinalizeReplay;
        if (_hrDataManager != null)
        {
            _hrDataManager.OnHRUpdate -= OnHRUpdated;
        }
    }

    private void OnHRUpdated(int hr)
    {
        var time = _audioTimeSyncController.songTime;
        // Enforce new song time is larger than previous so no need to check pause and
        // even if the player seeks the song in practice mode with PracticePlugin
        // we will only record the first time a part of the song is played.
        //
        // Using song time for rate limit will be affected by song speed,
        // but it's ok for reasonable song speed.
        if (hr == 0 || hr == _prev.HeartRate || time - _prev.SongTime < MIN_DATA_INTERVAL) return;
        var data = new ReplayHR { SongTime = time, HeartRate = hr };
        _data.Add(data);
        _prev = data;
    }

    private void OnFinalizeReplay()
    {
        _logger.Debug("Preparing heart rate custom data to save into replay");
        _replayRecorder.OnFinalizeReplay -= OnFinalizeReplay;

        if (_data.Count == 0)
        {
            _logger.Warn("No heart rate data is recorded, not saving");
            return;
        }

        _logger.Debug($"Data size: {_data.Count}");

        try
        {
            // TODO use data source as device name
            var hrAgent = $"{_pMetadata.Id}/{_pMetadata.HVersion}";
            var hrData = new ReplayHRData(_data.ToArray(), "HRCounter", hrAgent);
            var bytes = ReplayHRDataConverter.ToBytes(hrData);
            _replayRecorder.TryWriteCustomData(ReplayHRDataConverter.DataKey, bytes);
            _logger.Info("Heart rate custom data written");
        }
        catch (Exception e)
        {
            _logger.Critical("Failed to save heart rate custom data into replay");
            _logger.Critical(e);
            throw;
        }
    }
}
