using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Data.DataSources.Base;
using IPA.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Zenject;

namespace HRCounter.Data.DataSources;

/// <summary>
///     Legacy Pulsoid data source using HTTP polling
/// </summary>
[Obsolete("Use Pulsoid2 instead.", true)]
internal class Pulsoid : DataSource
{
    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly Logger _logger = null!;

    private bool _updating;

    private const string PULSOID_API = "https://dev.pulsoid.net/api/v1/data/heart_rate/latest";

    private static readonly HttpClient HttpClient = new();

    public override void Initialize()
    {
        HttpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {_config.PulsoidToken}");
        base.Initialize();
    }

    protected override void Start()
    {
        _logger.Info("Starts updating HR");
        _updating = true;
        Task.Factory.StartNew(async () =>
        {
            _logger.Debug("Requesting HR data");

            while (_updating)
            {
                await UpdateHR();
                await Task.Delay(750);
            }
        });
    }

    protected override void Stop()
    {
        _updating = false;
    }

    private void HandleSuccess(JObject json)
    {
        var hr = json["data"]?["heart_rate"]?.ToObject<int>();
        var measuredAt = json["measured_at"]?.ToObject<string>();

        if (hr == null)
        {
            _logger.Warn("No hr data");
        }
        else if (measuredAt == null)
        {
            OnHeartRateDataReceived(hr.Value);
        }
        else
        {
            OnHeartRateDataReceived(hr.Value, measuredAt);
        }
    }

    private async Task UpdateHR()
    {
        try
        {
            var res = await HttpClient.GetAsync(PULSOID_API);

            // pulsoid: { "measured_at": 1650575246151, "data": { "heart_rate": 82 } }

            var json = JObject.Parse(await res.Content.ReadAsStringAsync());

            if (res.IsSuccessStatusCode)
            {
                HandleSuccess(json);
            }
            else
            {
                _logger.Error(
                    $"Failed to fetch HR: {Convert.ToInt32(res.StatusCode)} {res.StatusCode}, Error Code {json["error_code"]}, {json["error_message"]}");
            }
        }
        catch (HttpRequestException e)
        {
            _logger.Critical($"Failed to request HR: {e.Message}");
            _logger.Debug(e);
        }
        catch (JsonReaderException)
        {
            _logger.Critical($"Invalid json received");
        }
        catch (Exception e)
        {
            _logger.Warn($"Error Requesting HR data: {e.Message}");
            _logger.Warn(e);
        }
    }
}
