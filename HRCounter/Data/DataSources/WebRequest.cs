using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Data.DataSources.Base;
using IPA.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Zenject;

namespace HRCounter.Data.DataSources;

internal sealed class WebRequest : DataSource
{
    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly Logger _logger = null!;

    private string FeedLink => _config.FeedLink;
    private bool _updating;

    private readonly Regex _regex = new("^\\d+$");

    private static readonly HttpClient HttpClient = new();

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
                await Task.Delay(250);
            }
        });
    }

    protected override void Stop()
    {
        _updating = false;
    }

    private async Task UpdateHR()
    {
        try
        {
            var res = await HttpClient.GetStringAsync(FeedLink);
            if (_regex.IsMatch(res))
            {
                var hr = int.Parse(res);
                if (_updating)
                {
                    OnHeartRateDataReceived(hr);
                }
            }
            else
            {
                // pulsoid: {"bpm":0,"measured_at":"2021-06-21T01:34:39.320Z"}
                try
                {
                    var json = JObject.Parse(res);
                    if (json["bpm"] == null)
                    {
                        _logger.Warn("Json received does not contain necessary field");
                        _logger.Warn(res);
                    }
                    else
                    {
                        var hr = json["bpm"].ToObject<int>();
                        var timestamp = json["measured_at"]?.ToObject<string>();
                        if (_updating)
                        {
                            if (timestamp == null)
                            {
                                OnHeartRateDataReceived(hr);
                            }
                            else
                            {
                                OnHeartRateDataReceived(hr, timestamp);
                            }
                        }
                    }
                }
                catch (JsonReaderException)
                {
                    _logger.Critical($"Invalid json received: {res}");
                }
            }
        }
        catch (InvalidOperationException e)
        {
            _logger.Error($"Invalid request URI: {FeedLink}");
            _logger.Info("Stopping hr update");
            Stop();
        }
        catch (HttpRequestException e)
        {
            _logger.Critical($"Failed to request HR: {e.Message}");
            _logger.Debug(e);
        }
        catch (Exception e)
        {
            _logger.Warn($"Error Requesting HR data: {e.Message}");
            _logger.Warn(e);
        }
    }
}
