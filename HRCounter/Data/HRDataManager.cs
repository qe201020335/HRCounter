using System;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Data.DataSources;
using SiraUtil.Logging;
using Zenject;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter.Data
{
    public class HRDataManager : IInitializable, IDisposable
    {
        
        [Inject] private readonly SiraLog _logger = null!;

        private static DataSource? _dataSource;

        internal HRDataManager(DataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public event Action<int>? OnHRUpdate;

        public void Initialize()
        {
            _logger.Debug("HRDataManager Init");
            if (_dataSource == null)
            {
                _logger.Warn("BPM Downloader is null!");
                return;
            }

            _dataSource.OnHRUpdate += OnHRUpdateInternalHandler;

            try
            {
                _dataSource.Start();
                _logger.Info("Start updating heart rate");
            }
            catch (Exception e)
            {
                _logger.Error($"Could not start bpm downloader. {e.Message}");
                _logger.Debug(e);
                _dataSource.OnHRUpdate -= OnHRUpdateInternalHandler;
                _dataSource.Stop();
            }
        }

        public void Dispose()
        {
            _logger.Debug("HRDataManager Dispose");
            if (_dataSource != null)
            {
                _dataSource.OnHRUpdate -= OnHRUpdateInternalHandler;
            }

            _dataSource?.Stop();
            // _dataSource = null;
        }

        private void OnHRUpdateInternalHandler(int hr)
        {
            try
            {
                Task.Factory.StartNew(() => { OnHRUpdate?.Invoke(hr); });
            }
            catch (Exception e)
            {
                Log.Logger.Critical($"Exception Caught while broadcasting hr update event: {e.Message}");
                Log.Logger.Critical(e);
            }
        }
    }
}