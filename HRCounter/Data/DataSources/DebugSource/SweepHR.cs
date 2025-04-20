using System.Threading.Tasks;

namespace HRCounter.Data.DataSources.DebugSource;

internal class SweepHR : DataSource
{
    private bool _updating = false;
    private const int Low = 60;
    private const int High = 240;


    protected override void Start()
    {
        _updating = true;
        Task.Factory.StartNew(async () =>
        {
            var hr = 0;
            while (_updating)
            {
                OnHeartRateDataReceived(hr + Low);
                await Task.Delay(100);
                hr = (hr + 1) % (High - Low);
            }
        });
    }

    protected override void Stop()
    {
        _updating = false;
    }
}
