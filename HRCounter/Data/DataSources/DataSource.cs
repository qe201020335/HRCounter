using System;
using HRCounter.Configuration;
using IPA.Utilities.Async;
using SiraUtil.Logging;
using Zenject;

namespace HRCounter.Data.DataSources
{
    internal abstract class DataSource : IHRDataSource, IInitializable, IDisposable
    {
        [Inject]
        protected readonly PluginConfig Config = null!;
        
        [Inject]
        protected readonly SiraLog Logger = null!;

        public event EventHandler<HRDataReceivedEventArgs>? OnHRDataReceived;

        protected void OnHeartRateDataReceived(int hr, string? receivedAt = null)
        {
            UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                try
                {
                    var handler = OnHRDataReceived;
                    handler?.Invoke(this, new HRDataReceivedEventArgs(hr, receivedAt));
                }
                catch (Exception e)
                {
                    Log.Logger.Critical($"Exception Caught while broadcasting hr update event: {e.Message}");
                    Log.Logger.Critical(e);
                }
            });
        }

        public virtual void Initialize()
        {
            try
            {
                Start();
                Logger.Info("Start updating heart rate");
            }
            catch (Exception e)
            {
                Logger.Error($"Could not start bpm downloader. {e.Message}");
                Logger.Critical(e);
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
}