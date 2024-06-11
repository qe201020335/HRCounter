using HRCounter.Web.OSC.Handlers;
using Zenject;

namespace HRCounter.Data.DataSources
{
    internal class OscHR : DataSource
    {
        [Inject]
        private readonly OscHRHandler _oscHRHandler = null!;
    
        private void HRDataPostedHandler(object sender, int num)
        {
            OnHeartRateDataReceived(num);
        }
    
        protected override void Start()
        {
            _oscHRHandler.HeartRatePosted += HRDataPostedHandler;
        }

        protected override void Stop()
        {
            _oscHRHandler.HeartRatePosted -= HRDataPostedHandler;
        }
    }
}
