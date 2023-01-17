using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HRCounter.Data.DataSourcers
{
    internal sealed class WebRequest : DataSourcer
    {
        private static string FeedLink => Config.FeedLink;
        private bool _updating;

        private readonly Regex _regex = new Regex("^\\d+$");
        
        private static readonly HttpClient HttpClient = new HttpClient();

        internal override void Start()
        {
            logger.Info("Starts updating HR");
            _updating = true;
            Task.Factory.StartNew(async () =>
            {
                logger.Debug("Requesting HR data");

                while (_updating)
                {
                    await UpdateHR();
                    await Task.Delay(250);
                }
            });
        }

        internal override void Stop()
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
                        OnHearRateDataReceived(hr);
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
                            logger.Warn("Json received does not contain necessary field");
                            logger.Warn(res);
                        }
                        else
                        {
                            var hr = json["bpm"].ToObject<int>();
                            var timestamp = json["measured_at"]?.ToObject<string>();
                            if (_updating)
                            {
                                if (timestamp == null)
                                {
                                    OnHearRateDataReceived(hr);
                                }
                                else
                                {
                                    OnHearRateDataReceived(hr, timestamp);
                                }
                            }
                        }
                    }
                    catch (JsonReaderException)
                    {
                        logger.Critical($"Invalid json received: {res}");
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                logger.Error($"Invalid request URI: {FeedLink}");
                logger.Info("Stopping hr update");
                Stop();
            }
            catch (HttpRequestException e)
            {
                logger.Critical($"Failed to request HR: {e.Message}");
                logger.Debug(e);
            }
            catch (Exception e)
            {
                logger.Warn($"Error Requesting HR data: {e.Message}");
                logger.Warn(e);
            }
        }
    }
}