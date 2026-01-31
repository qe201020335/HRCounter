using System;
using HRCounter.Utils;
using IPA.Logging;
using JetBrains.Annotations;
using Zenject;

namespace HRCounter.Data.Replay;

internal class ReplayHRProvider : IInGameHRProvider, ITickable
{
    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly ReplayHRData _data = null!;

    [Inject]
    private readonly AudioTimeSyncController _syncController = null!;

    private int _currentHR;

    private int _currentIndex;

    private float _currentSongTime;

    public bool IsReplayData => true;

    public int CurrentHR => _currentHR;

    public event Action<int>? HRChanged;

    [Inject]
    [UsedImplicitly]
    private void Init()
    {
        _logger.Info("Providing HR data from replay");
        _logger.Debug($"Replay HR Agent: {_data.HRAgent ?? "null"}");
        _logger.Debug($"Replay HR data device: {_data.DeviceName}");
        _logger.Debug($"Replay HR data count: {_data.Count}");
        _logger.Spam(string.Join(',', _data));
        _currentHR = _data[0].HeartRate;
    }

    void ITickable.Tick()
    {
        var time = _syncController.songTime;
        int hr;
        if (time < _currentSongTime)
        {
            _logger.Debug("Song time went backwards!"); // seeking replay backwards
            hr = _data.FindHeartRateAt(time, out _currentIndex);
        }
        else
        {
            hr = _data.FindHeartRateAt(time, _currentIndex, out _currentIndex);
        }

        _currentSongTime = time;
        if (hr == _currentHR) return;
        _currentHR = hr;
        var handler = HRChanged;
        try
        {
            handler?.Invoke(hr);
        }
        catch (Exception e)
        {
            _logger.Error("Exception caught while providing HR");
            _logger.Error(e);
        }
    }
}
