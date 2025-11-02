using System;
using HRCounter.Configuration;
using IPA.Logging;
using IPA.Utilities.Async;
using Zenject;

namespace HRCounter.Data;

public class HRDataManager : IInitializable, IDisposable
{
    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly PluginConfig _config = null!;

    [InjectOptional]
    private IHRDataSource? _dataSource;

    /// <summary>
    ///     Always invoked on the main thread when new HR data is received.
    /// </summary>
    public event Action<int>? OnHRUpdate;

    public int CurrentBpm => BPM.Bpm;

    public void Initialize()
    {
        _logger.Debug("HRDataManager Init");
        if (_dataSource == null)
        {
            _logger.Critical("BPM Downloader is null!");
            return;
        }

        _dataSource.OnHRDataReceived += OnHrDataReceivedInternalHandler;
    }

    public void Dispose()
    {
        _logger.Debug("HRDataManager Dispose");
        if (_dataSource != null)
        {
            _dataSource.OnHRDataReceived -= OnHrDataReceivedInternalHandler;
        }
    }

    private void OnHrDataReceivedInternalHandler(object sender, HRDataReceivedEventArgs args)
    {
        BPM.Set(args.HR, args.ReceivedAt);

        if (_config.LogHR) _logger.Info($"Received HR: {args.HR} at {args.ReceivedAt}");

        UnityMainThreadTaskScheduler.Factory.StartNew(() =>
        {
            try
            {
                var handler = OnHRUpdate;
                handler?.Invoke(args.HR);
            }
            catch (Exception e)
            {
                _logger.Critical($"Exception Caught while broadcasting hr update event: {e.Message}");
                _logger.Critical(e);
            }
        });
    }
}
