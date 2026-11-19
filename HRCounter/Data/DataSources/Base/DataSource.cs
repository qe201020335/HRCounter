using System;
using IPA.Logging;
using Zenject;

namespace HRCounter.Data.DataSources.Base;

public abstract class DataSource : IHRDataSource, IInitializable, IDisposable
{
    [Inject(Id = typeof(DataSource))]
    private readonly Logger _logger = null!;

    public virtual bool AllowReplayRecording => true;

    public event EventHandler<int>? HRDataReceived;

    protected void OnHeartRateDataReceived(int hr)
    {
        var handler = HRDataReceived;
        handler?.Invoke(this, hr);
    }

    public virtual void Initialize()
    {
        try
        {
            Start();
            _logger.Info("Start updating heart rate");
        }
        catch (Exception e)
        {
            _logger.Error($"Could not start bpm downloader. {e.Message}");
            _logger.Critical(e);
            Stop();
        }
    }

    public virtual void Dispose()
    {
        Stop();
    }

    protected abstract void Start();

    protected abstract void Stop();
}
