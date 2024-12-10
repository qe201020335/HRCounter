using System;
using System.Threading.Tasks;
using IPA.Utilities.Async;
using SiraUtil.Logging;
using Zenject;

namespace HRCounter.Data
{
    public class HRDataManager : IInitializable, IDisposable
    {
        [Inject] 
        private readonly SiraLog _logger = null!;

        [InjectOptional]
        private IHRDataSource? _dataSource;

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
}