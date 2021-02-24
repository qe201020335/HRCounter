using System;
using System.Collections;
using CountersPlus.Counters.Custom;
using HRCounter.Configuration;
using Newtonsoft.Json;
using System.Net;
using HRCounter.Data;
using TMPro;
using IPALogger = IPA.Logging.Logger;
using UnityEngine;

namespace HRCounter
{
    public class HRCounter: BasicCustomCounter
    {

        private readonly string URL = PluginConfig.Instance.Feed_link;
        private bool Log_HR = PluginConfig.Instance.Log_HR;
        private IPALogger log = Logger.logger;
        private string json_data = string.Empty;
        private TMP_Text counter;
        private WebClient w = new WebClient();
        private BPM bpm = new BPM();

        public override void CounterInit()
        {
            if (URL == "NotSet")
            {
                log.Debug("Feed link not set.");
                return;
            }

            try
            {   // attempt to download JSON data as a string
                json_data = w.DownloadString(URL);
                log.Debug(json_data);
            }
            catch (Exception e)
            {
                log.Error("Error requesting data");
                log.Error(e.ToString());
                return;
            }

            log.Debug("Creating counter");
            counter = CanvasUtility.CreateTextFromSettings(Settings);
            counter.fontSize = 3;
            log.Debug("Starts updating");
            SharedCoroutineStarter.instance.StartCoroutine(Ticking());
        }

        private IEnumerator Ticking()
        {
            while(true)
            {
                yield return new WaitForSecondsRealtime(1);
                Update();
                counter.text = $"HR {bpm.Bpm}";
            }
        }

        private void Update()
        {
            try
            {   // attempt to download JSON data as a string
                json_data = w.DownloadString(URL);
                // log.Debug(json_data);
            }
            catch (Exception e)
            {
                log.Error("Error requesting data");
                log.Error(e.ToString());
                return;
            }
            // if string with JSON data is not empty, deserialize it to class and return its instance
            if (!string.IsNullOrEmpty(json_data))
            {
                bpm = JsonConvert.DeserializeObject<BPM>(json_data);
                if (Log_HR)
                {
                    log.Debug(bpm.ToString());
                }
                
            }
        }

        public override void CounterDestroy()
        {
            log.Debug("Counter destroyed");
        }
    }
}
