using System;
using IPA.Logging;
using Zenject;

namespace HRCounter.Data.Replay;

internal class ReplayHRProvider : IInGameHRProvider
{
    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly ReplayHRData _data = null!;

    public bool IsReplayData => true;

    [Inject]
    private void Init()
    {
        _logger.Info("Providing HR data from replay");
        _logger.Debug($"Replay HR data count: {_data.HRData.Length}");
        _logger.Debug($"Replay HR data device: {_data.DeviceName}");
#if DEBUG
        _logger.Debug(string.Join(',', _data.HRData));
#endif
    }

    public event Action<int> HRChanged;

    public int GetCurrentHR() => 0;
}
