using System.Threading.Tasks;
using UnityEngine;

namespace HRCounter.Data.DataSources.DebugSource;

internal class RandomHR : DataSource
{
    private bool _updating = false;

    protected override void Start()
    {
        _updating = true;
        Task.Factory.StartNew(async () =>
        {
            while (_updating)
            {
                OnHeartRateDataReceived(Random.Range(60, 180));
                await Task.Delay(Random.Range(250, 1000));
            }
        });
    }

    protected override void Stop()
    {
        _updating = false;
    }
}
