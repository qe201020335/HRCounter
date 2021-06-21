using System.Collections;
using CountersPlus.Counters.Custom;
using HRCounter.Configuration;
using HRCounter.Data;
using TMPro;
using IPALogger = IPA.Logging.Logger;
using UnityEngine;


namespace HRCounter
{
    public sealed class HRCounter: BasicCustomCounter
    {
        
        private IPALogger _logger = Logger.logger;
        private TMP_Text counter;
        private bool _updating;
        private BpmDownloader _bpmDownloader;

        // color stuff
        private bool _colorize = PluginConfig.Instance.Colorize;
        private int _hrLow = PluginConfig.Instance.HRLow;
        private int _hrHigh = PluginConfig.Instance.HRHigh;
        private string _colorLow = PluginConfig.Instance.LowColor;
        private string _colorHigh = PluginConfig.Instance.HighColor;
        private string _colorMid = PluginConfig.Instance.MidColor;
        

        public override void CounterInit()
        {
            if (PluginConfig.Instance.DataSource == "WebRequest" && PluginConfig.Instance.FeedLink == "NotSet")
            {
                _logger.Warn("Feed link not set.");
                return;
            }
            /*
            if (PluginConfig.Instance.DataSource == "HypeRate" && PluginConfig.Instance.HypeRateSessionID == -1 )
            {
                _logger.Warn("Hype Rate Session ID not set.");
                return;
            }
            */

            if (PluginConfig.Instance.HideDuringReplay && Utils.Utils.IsInReplay())
            {
                _logger.Info("We are in a replay, Counter hides.");
                return;
            }

            if (!Refresh())
            {
                return;
            }
            
            CreateCounter();
            
            _bpmDownloader.Start();
            
            Start();
        }

        private bool Refresh()
        {
            switch (PluginConfig.Instance.DataSource)
            {
                case "WebRequest":
                    _bpmDownloader = new WebRequest();
                    break;
                
                default:
                    _bpmDownloader = null;
                    _logger.Warn("Unknown Data Sources");
                    return false;
            }
            _colorize = PluginConfig.Instance.Colorize;
            _hrLow = PluginConfig.Instance.HRLow;
            _hrHigh = PluginConfig.Instance.HRHigh;
            _colorLow = PluginConfig.Instance.LowColor;
            _colorHigh = PluginConfig.Instance.HighColor;
            _colorMid = PluginConfig.Instance.MidColor;
            return true;
        }

        private void CreateCounter()
        {
            _logger.Info("Creating counter");
            counter = CanvasUtility.CreateTextFromSettings(Settings);
            counter.fontSize = 3;
        }

        private void Start()
        {
            _updating = true;
            SharedCoroutineStarter.instance.StartCoroutine(Ticking());
        }

        private void Stop()
        {
            _updating = false;
        }

        private IEnumerator Ticking()
        {
            while(_updating)
            {
                int bpm = BPM.Instance.Bpm;
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
            _logger.Warn("Cannot determine color, please check hr boundaries and color codes.");
            _colorize = false;
            return ColorUtility.ToHtmlStringRGB(Color.white);
        }

        public override void CounterDestroy()
        {
            Stop();
            _bpmDownloader.Stop();
            _logger.Info("Counter destroyed");
        }
    }
}
