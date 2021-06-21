using System.Collections;
using System.Net;
using HRCounter.Configuration;
using UnityEngine.Networking;
using IPALogger = IPA.Logging.Logger;
using UnityEngine;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HRCounter.Data
{
    internal sealed class WebRequest : BpmDownloader
    {
        private string _url = PluginConfig.Instance.FeedLink;
        private bool _logHr = PluginConfig.Instance.LogHR;
        private bool _updating;

        private Regex _regex = new Regex("^\\d+$");


        internal WebRequest()
        {
            RefreshSettings();
        }
        
        protected override void RefreshSettings()
        {
            _url = PluginConfig.Instance.FeedLink;
            _logHr = PluginConfig.Instance.LogHR;
        }

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
            using (UnityWebRequest webRequest = UnityWebRequest.Get(_url))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();
                if (webRequest.isNetworkError)
                {
                    logger.Error($"Error Requesting HR data: {webRequest.error}");
                    throw new WebException();
                }

                if (_regex.IsMatch(webRequest.downloadHandler.text))
                {
                    Bpm.Bpm = int.Parse(webRequest.downloadHandler.text);
                    Bpm.ReceivedAt = System.DateTime.Now.ToString("HH:mm:ss");
                }
                else
                {
                    // pulsoid: {"bpm":0,"measured_at":"2021-06-21T01:34:39.320Z"}
                    try
                    {
                        JObject json = JObject.Parse(webRequest.downloadHandler.text);
                        if (json["bpm"] != null)
                        {
                            Bpm.Bpm = json["bpm"].ToObject<int>();
                        }
                        else
                        {
                            logger.Warn("Json received does not contain necessary field");
                            logger.Warn(webRequest.downloadHandler.text);
                        }
                        
                        Bpm.ReceivedAt = json["measured_at"] != null ? json["measured_at"].ToObject<string>() : Bpm.ReceivedAt = System.DateTime.Now.ToString("HH:mm:ss");
                        
                    }
                    catch (JsonReaderException)
                    {
                        logger.Critical("Invalid json received.");
                        logger.Critical(webRequest.downloadHandler.text);
                    }
                }

                if (_logHr)
                {
                    logger.Info(Bpm.ToString());
                }
            }
        }
    }
}