using HRCounter.Web.HTTP.Handlers;
using Zenject;

namespace HRCounter.Data.DataSources;

internal class HttpServerDataSource : DataSource
{
    [Inject]
    private readonly HttpHRHandler _httpHRHandler = null!;

    private void HRDataPostedHandler(object sender, int num)
    {
        OnHeartRateDataReceived(num);
    }

    protected override void Start()
    {
        _httpHRHandler.HeartRatePosted += HRDataPostedHandler;
    }

    protected override void Stop()
    {
        _httpHRHandler.HeartRatePosted -= HRDataPostedHandler;
    }
}
