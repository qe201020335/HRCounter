using System;
using System.Threading;
using HRCounter.Configuration;
using HRCounter.Utils;
using IPA.Logging;
using Zenject;

namespace HRCounter.Data;

public class HRDataManager : IInitializable, IDisposable, ITickable
{
    private const int NO_PENDING_HR = -1;

    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly PluginConfig _config = null!;

    [InjectOptional]
    private readonly IHRDataSource? _dataSource = null;

    private readonly SafeEvent<int> _hrUpdated = new();

    /// <summary>
    ///     Always invoked on the main thread, at most once per frame, for every new hr sample, changed or not.
    /// </summary>
    public event Action<int> HRUpdated
    {
        add => _hrUpdated.Add(value);
        remove => _hrUpdated.Remove(value);
    }

    public static int CurrentBpm { get; private set; } //TODO make it instance when HRDataManager is a global singleton

    internal bool AllowReplayRecording => _dataSource?.AllowReplayRecording ?? false;

    private int _pendingHr = NO_PENDING_HR;

    void IInitializable.Initialize()
    {
        _logger.Debug("HRDataManager Init");
        if (_dataSource == null)
        {
            _logger.Warn("BPM Downloader is null!");
            return;
        }

        _dataSource.HRDataReceived += OnHRDataReceived;
    }

    void ITickable.Tick()
    {
        var hr = Interlocked.Exchange(ref _pendingHr, NO_PENDING_HR);
        if (hr < 0) return;
        CurrentBpm = hr;

        _hrUpdated.Invoke(hr);
    }

    void IDisposable.Dispose()
    {
        _logger.Debug("HRDataManager Dispose");
        if (_dataSource != null)
        {
            _dataSource.HRDataReceived -= OnHRDataReceived;
        }
    }

    private void OnHRDataReceived(object sender, int hr)
    {
        if (sender != _dataSource || (_config.IgnoreZeroValues && hr == 0)) return;

        Volatile.Write(ref _pendingHr, hr);
    }
}
