using System.Threading.Tasks;

namespace HRCounter.Data.DataSources
{
    internal class SweepHR: DataSourceInternal
    {
        private bool _updating = false;
        private const int Low = 60;
        private const int High = 240;
            
        
        protected internal override void Start()
        {
            _updating = true;
            Task.Factory.StartNew(async () =>
            {
                var hr = 0;
                while (_updating)
                {
                    OnHearRateDataReceived(hr + Low);
                    await Task.Delay(100);
                    hr = (hr + 1) % (High - Low);
                }
            });
        }

        protected internal override void Stop()
        {
            _updating = false;
        }
    }
}