using Newtonsoft.Json;
using System.Collections;
using System.Net;
using HRCounter.Configuration;
using UnityEngine.Networking;
using IPALogger = IPA.Logging.Logger;
using UnityEngine;

namespace HRCounter.Data
{
    public class BpmDownloader
    {

        internal BPM bpm { get; } = new BPM();
        private readonly string URL = PluginConfig.Instance.FeedLink;
        private IPALogger log = Logger.logger;
        private bool Log_HR = PluginConfig.Instance.LogHR;
        public bool updating;

        public IEnumerator Updating()
        {   
            log.Debug("Requesting HR data");
            while (updating)
            {
                yield return Update();
                yield return new WaitForSecondsRealtime((float) 0.25);
            }
        }

        private IEnumerator Update()
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(URL))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();
                if (webRequest.isNetworkError)
                {
                    log.Error($"Error Requesting HR data: {webRequest.error}");
                    throw new WebException();
                }

                JsonConvert.PopulateObject(webRequest.downloadHandler.text, bpm);
                if (Log_HR)
                {
                    log.Info(bpm.ToString());
                }
            }
        }
        
        
        
    }
}