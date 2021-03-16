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

        

        public override void CounterDestroy()
        {
            updating = false;
            _bpmDownloader.updating = false;
            log.Debug("Counter destroyed");
        }
    }
}
