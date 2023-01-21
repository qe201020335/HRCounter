﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using HRCounter.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HRCounter.Data.DataSourcers
{
    internal class Pulsoid : DataSourcer
    {
        private static string Token => Config.PulsoidToken;
        private static string URL => DataSourceUtils.PULSOID_API;
        private bool _updating;

        private static readonly HttpClient HttpClient = new HttpClient();

        internal Pulsoid()
        {
            HttpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {Token}");
        }

        internal override void Start()
        {
            Logger.Info("Starts updating HR");
            _updating = true;
            Task.Factory.StartNew(async () =>
            {
                Logger.Debug("Requesting HR data");

                while (_updating)
                {
                    await UpdateHR();
                    await Task.Delay(750);
                }
            });
        }

        internal override void Stop()
        {
            _updating = false;
        }

        private void HandleSuccess(JObject json)
        {
            var hr = json["data"]?["heart_rate"]?.ToObject<int>();
            var measuredAt = json["measured_at"]?.ToObject<string>();

            if (hr == null)
            {
                Logger.Warn("No hr data");
            }
            else if (measuredAt == null)
            {
                OnHearRateDataReceived(hr.Value);
            }
            else
            {
                OnHearRateDataReceived(hr.Value, measuredAt);
            }
        }

        private async Task UpdateHR()
        {
            try
            {
                var res = await HttpClient.GetAsync(URL);

                // pulsoid: { "measured_at": 1650575246151, "data": { "heart_rate": 82 } }
                
                var json = JObject.Parse(await res.Content.ReadAsStringAsync());

                if (res.IsSuccessStatusCode)
                {
                    HandleSuccess(json);
                }
                else
                {
                    Logger.Error($"Failed to fetch HR: {Convert.ToInt32(res.StatusCode)} {res.StatusCode}, Error Code {json["error_code"]}, {json["error_message"]}");
                }

            }
            catch (HttpRequestException e)
            {
                Logger.Critical($"Failed to request HR: {e.Message}");
                Logger.Debug(e);
            }
            catch (JsonReaderException)
            {
                Logger.Critical($"Invalid json received");
            }
            catch (Exception e)
            {
                Logger.Warn($"Error Requesting HR data: {e.Message}");
                Logger.Warn(e);
            }
        }
    }
}