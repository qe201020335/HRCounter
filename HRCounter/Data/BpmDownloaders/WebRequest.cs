using System.Collections;
using System.Text.RegularExpressions;
using HRCounter.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace HRCounter.Data.BpmDownloaders
{
    internal sealed class WebRequest : BpmDownloader
    {
        private static string FeelLink => PluginConfig.Instance.FeedLink;
        private bool _updating;

        private readonly Regex _regex = new Regex("^\\d+$");

        protected override void RefreshSettings() {}

        internal override void Start()
        {
            logger.Info("Starts updating HR");
            _updating = true;
            SharedCoroutineStarter.instance.StartCoroutine(Updating());
        }

        internal override void Stop()
        {
            _updating = false;
        }

        private IEnumerator Updating()
        {   
            logger.Debug("Requesting HR data");
            while (_updating)
            {
                yield return UpdateHR();
                yield return new WaitForSecondsRealtime( 0.25f);
            }
        }

        private IEnumerator UpdateHR()
        {
            using (var webRequest = UnityWebRequest.Get(FeelLink))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();
                if (webRequest.isNetworkError)
                {
                    logger.Warn($"Error Requesting HR data: {webRequest.error}");
                }

                if (_regex.IsMatch(webRequest.downloadHandler.text))
                {
                    var hr = int.Parse(webRequest.downloadHandler.text);
                    OnHearRateDataReceived(hr);
                }
                else
                {
                    // pulsoid: {"bpm":0,"measured_at":"2021-06-21T01:34:39.320Z"}
                    try
                    {
                        var json = JObject.Parse(webRequest.downloadHandler.text);
                        if (json["bpm"] == null)
                        {
                            logger.Warn("Json received does not contain necessary field");
                            logger.Warn(webRequest.downloadHandler.text);
                        }
                        else
                        {
                            var hr = json["bpm"].ToObject<int>();
                            var timestamp = json["measured_at"]?.ToObject<string>();
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
                    catch (JsonReaderException)
                    {
                        logger.Critical("Invalid json received.");
                        logger.Critical(webRequest.downloadHandler.text);
                    }
                }
            }
        }
    }
}