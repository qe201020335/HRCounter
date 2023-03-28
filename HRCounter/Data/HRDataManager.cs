using System;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Data.DataSources;
using Zenject;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter.Data
{
    public class HRDataManager : IInitializable, IDisposable
    {
        private static readonly IPALogger _logger = Log.Logger;
        private static DataSource? _dataSource;

        internal HRDataManager(DataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public static event Action<int>? OnHRUpdate;

        internal static void ClearThings()
        {
            Log.Logger.Info("Clearing bpm downloader and all related event subscribers");
            // _dataSource?.Stop();
            _dataSource = null;

            OnHRUpdate = null;
        }

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

        private static void OnHRUpdateInternalHandler(int hr)
        {
            if (PluginConfig.Instance.AutoPause && hr >= PluginConfig.Instance.PauseHR)
            {
                Log.Logger.Info("Heart Rate too high! Pausing!");
                GamePauseController.PauseGame();
            }

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