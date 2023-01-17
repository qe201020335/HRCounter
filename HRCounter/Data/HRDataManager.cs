using System;
using System.Threading.Tasks;
using HRCounter.Data.DataSourcers;
using Zenject;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter.Data
{
    public class HRDataManager : IInitializable, IDisposable
    {
        private static readonly IPALogger _logger = Log.Logger;
        private static DataSourcer? _dataSourcer;

        internal HRDataManager(DataSourcer dataSourcer)
        {
            _dataSourcer = dataSourcer;
        }
        
        public static event Action<int>? OnHRUpdate;
        
        internal static void ClearThings()
        {
            Log.Logger.Info("Clearing bpm downloader and all related event subscribers");
            // _dataSourcer?.Stop();
            _dataSourcer = null;

            OnHRUpdate = null;
        }

        public void Initialize()
        {
            _logger.Debug("HRDataManager Init");
            if (_dataSourcer == null)
            {
                _logger.Warn("BPM Downloader is null!");
                return;
            }
            _dataSourcer.OnHRUpdate += OnHRUpdateInternalHandler;
            
            try
            {
                _dataSourcer.Start();
                _logger.Info("Start updating heart rate");
            }
            catch (Exception e)
            {
                _logger.Error($"Could not start bpm downloader. {e.Message}");
                _logger.Debug(e);
                _dataSourcer.OnHRUpdate -= OnHRUpdateInternalHandler;
                _dataSourcer.Stop();
            }
        }

        public void Dispose()
        {
            _logger.Debug("HRDataManager Dispose");
            if (_dataSourcer != null)
            {
                _dataSourcer.OnHRUpdate -= OnHRUpdateInternalHandler;
            }
            _dataSourcer?.Stop();
            // _dataSourcer = null;
        }

        private static void OnHRUpdateInternalHandler(int hr)
        {
            try
            {
                Task.Factory.StartNew(() =>
                {
                    OnHRUpdate?.Invoke(hr);
                });
            }
            catch (Exception e)
            {
                Log.Logger.Critical($"Exception Caught while broadcasting hr update event: {e.Message}");
                Log.Logger.Critical(e);
            }
        }
    }
}