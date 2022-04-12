using System.Threading.Tasks;
using UnityEngine;

namespace HRCounter.Data.BpmDownloaders
{
    internal class RandomHR : BpmDownloader
    {
        private bool _updating = false;
        protected override void RefreshSettings()
        {
            
        }
        internal override void Start()
        {
            _updating = true;
            Task.Factory.StartNew(async () =>
            {
                while (_updating)
                {
                    OnHearRateDataReceived(Random.Range(60, 180));
                    await Task.Delay(Random.Range(250, 1000));
                }
            });
        }

        internal override void Stop()
        {
            _updating = false;
        }
    }
}