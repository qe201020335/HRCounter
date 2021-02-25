using System.Collections;
using CountersPlus.Counters.Custom;
using HRCounter.Configuration;
using HRCounter.Data;
using TMPro;
using IPALogger = IPA.Logging.Logger;
using UnityEngine;



namespace HRCounter
{
    public class HRCounter: BasicCustomCounter
    {

        private readonly string URL = PluginConfig.Instance.Feed_link;
        private IPALogger log = Logger.logger;
        private TMP_Text counter;
        private bool updating;
        private BpmDownloader _bpmDownloader = new BpmDownloader();

        public override void CounterInit()
        {
            if (URL == "NotSet")
            {
                log.Debug("Feed link not set.");
                return;
            }
            /*
            try
            {   
                _bpmDownloader.Update();
            }
            catch (Exception e)
            {
                log.Error("Error requesting data");
                log.Error(e.ToString());
                return;
            }
            */
            log.Debug("Creating counter");
            counter = CanvasUtility.CreateTextFromSettings(Settings);
            counter.fontSize = 3;
            log.Debug("Starts updating");
            _bpmDownloader.updating = true;
            SharedCoroutineStarter.instance.StartCoroutine(_bpmDownloader.Updating());
            updating = true;
            SharedCoroutineStarter.instance.StartCoroutine(Ticking());
        }

        private IEnumerator Ticking()
        {
            while(updating)
            {
                yield return new WaitForSecondsRealtime(1);
                counter.text = $"HR {_bpmDownloader.bpm.Bpm}";
            }
        }

        /*
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
                JsonConvert.PopulateObject(json_data, bpm);
                if (Log_HR)
                {
                    log.Debug(bpm.ToString());
                }
                
            }
        }
        */

        public override void CounterDestroy()
        {
            updating = false;
            _bpmDownloader.updating = false;
            log.Debug("Counter destroyed");
        }
    }
}
