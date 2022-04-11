using System.Threading.Tasks;
using UnityEngine;

namespace HRCounter.Data
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
                    Bpm.Bpm = Random.Range(60, 180);
                    await Task.Delay(250);
                }
            });
        }

        internal override void Stop()
        {
            _updating = false;
        }
    }
}