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

        private readonly string URL = PluginConfig.Instance.FeedLink;
        private IPALogger log = Logger.logger;
        private TMP_Text counter;
        private bool updating;
        private BpmDownloader _bpmDownloader = new BpmDownloader();

        // color stuff
        private bool _colorize = PluginConfig.Instance.Colorize;
        private int _hrLow = PluginConfig.Instance.HRLow;
        private int _hrHigh = PluginConfig.Instance.HRHigh;
        private string _colorLow = PluginConfig.Instance.LowColor;
        private string _colorHigh = PluginConfig.Instance.HighColor;
        private string _colorMid = PluginConfig.Instance.MidColor;
        

        public override void CounterInit()
        {
            if (URL == "NotSet")
            {
                log.Warn("Feed link not set.");
                return;
            }
            
            log.Info("Creating counter");
            counter = CanvasUtility.CreateTextFromSettings(Settings);
            counter.fontSize = 3;
            log.Debug("Starts updating HR");
            _bpmDownloader.updating = true;
            SharedCoroutineStarter.instance.StartCoroutine(_bpmDownloader.Updating());
            updating = true;
            SharedCoroutineStarter.instance.StartCoroutine(Ticking());
        }

        private IEnumerator Ticking()
        {
            while(updating)
            {
                int bpm = _bpmDownloader.bpm.Bpm;
                counter.text = _colorize ? $"<color=#FFFFFF>HR </color><color=#{DetermineColor(bpm)}>{bpm}</color>" : $"HR {bpm}";
                yield return new WaitForSecondsRealtime(1);
            }
        }

        private string DetermineColor(int hr)
        {
            if (_hrHigh >= _hrLow && _hrLow > 0)
            {
                if (ColorUtility.TryParseHtmlString(_colorHigh, out Color colorHigh) &&
                    ColorUtility.TryParseHtmlString(_colorLow, out Color colorLow) && 
                    ColorUtility.TryParseHtmlString(_colorMid, out Color colorMid))
                {
                    if (hr <= _hrLow)
                    {
                        return _colorLow.Substring(1); //the rgb color in setting are #RRGGBB, need to omit the #
                    }

                    if (hr >= _hrHigh)
                    {
                        return _colorHigh.Substring(1);
                    }

                    float ratio = (hr - _hrLow) / (float) (_hrHigh - _hrLow) * 2;
                    Color color = ratio < 1 ? Color.Lerp(colorLow, colorMid, ratio) : Color.Lerp(colorMid, colorHigh, ratio - 1);

                    return ColorUtility.ToHtmlStringRGB(color);
                }
            }
            log.Warn("Cannot determine color, please check hr boundaries and color codes.");
            return ColorUtility.ToHtmlStringRGB(Color.white);
        }

        public override void CounterDestroy()
        {
            updating = false;
            _bpmDownloader.updating = false;
            log.Debug("Counter destroyed");
        }
    }
}
