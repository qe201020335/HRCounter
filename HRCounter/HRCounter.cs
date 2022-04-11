using System;
using System.Collections;
using CountersPlus.Counters.Custom;
using HRCounter.Configuration;
using HRCounter.Data;
using JetBrains.Annotations;
using TMPro;
using IPALogger = IPA.Logging.Logger;
using UnityEngine;

namespace HRCounter
{
    public sealed class HRCounter: BasicCustomCounter
    {
        
        private readonly IPALogger _logger = Logger.logger;
        private TMP_Text _counter;
        private bool _updating;
        [CanBeNull] private BpmDownloader _bpmDownloader;

        // color stuff
        private bool _colorize = PluginConfig.Instance.Colorize;
        private int _hrLow = PluginConfig.Instance.HRLow;
        private int _hrHigh = PluginConfig.Instance.HRHigh;
        private string _colorLow = PluginConfig.Instance.LowColor;
        private string _colorHigh = PluginConfig.Instance.HighColor;
        private string _colorMid = PluginConfig.Instance.MidColor;
        

        public override void CounterInit()
        {
            if (PluginConfig.Instance.HideDuringReplay && Utils.Utils.IsInReplay())
            {
                _logger.Info("We are in a replay, Counter hides.");
                return;
            }
            
            if (!Refresh())
            {
                _logger.Info("Can't Refresh");
                _logger.Info("Please check your settings about data source and the link or id.");
                return;
            }
            
            
            CreateCounter();
            Utils.GamePause.GameStart();

            try
            {
                _bpmDownloader.Start();
                _logger.Info("Start updating heart rate");
            }
            catch (Exception e)
            {
                _logger.Critical("Could not start bpm downloader.");
                _logger.Error(e.Message);
                _logger.Debug(e);
                _bpmDownloader.Stop();
                return;
            }
            Start();
            _logger.Info("Start updating counter text");
        }

        private bool Refresh()
        {
            _logger.Info("Refreshing Settings");
            switch (PluginConfig.Instance.DataSource)
            {
                case "WebRequest":
                    if (PluginConfig.Instance.FeedLink == "NotSet")
                    {
                        _logger.Warn("Feed link not set.");
                        return false;
                    }
                    _bpmDownloader = new WebRequest();
                    break;
                
                case "HypeRate":
                    if (PluginConfig.Instance.HypeRateSessionID == "-1")
                    {
                        _logger.Warn("Hype Rate Session ID not set.");
                        return false;
                    }

                    _bpmDownloader = new HRProxy();
                    break;
                    
                case "Pulsoid":
                    if (PluginConfig.Instance.PulsoidWidgetID == "NotSet")
                    {
                        _logger.Warn("Pulsoid Widget ID not set.");
                        return false;
                    }

                    _bpmDownloader = new HRProxy();
                    break;

                case "FitbitHRtoWS":
                    if(PluginConfig.Instance.FitbitWebSocket == string.Empty)
                    {
                        _logger.Warn("FitbitWebSocket is empty.");
                        return false;
                    }
                    _bpmDownloader = new FitbitHRtoWS();
                    break;
                
                case "YUR APP":
                    _bpmDownloader = new YURApp();
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
            _counter = CanvasUtility.CreateTextFromSettings(Settings);
            _counter.fontSize = 3;

            var canvas = CanvasUtility.GetCanvasFromID(Settings.CanvasID);
            if (canvas == null)
            {
                Logger.logger.Warn("Cannot find counters+ canvas");
                return;
            }
            
            AssetBundleManager.SetupCanvasInScene(canvas, _counter);
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
                var bpm = BPM.Instance.Bpm;
                if (PluginConfig.Instance.TextOnlyCounter)
                {
                    _counter.text = _colorize ? $"<color=#FFFFFF>HR </color><color=#{DetermineColor(bpm)}>{bpm}</color>" : $"HR {bpm}";
                    AssetBundleManager.CurrentCanvas.SetActive(false);
                }
                else
                {
                    _counter.text = String.Empty;
                    AssetBundleManager.CurrentCanvas.SetActive(true);
                    AssetBundleManager.SetHR(_colorize ? $"<color=#{DetermineColor(bpm)}>{bpm}</color>" : $"{bpm}");
                }
                yield return new WaitForSecondsRealtime(0.25f);
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

                    var ratio = (hr - _hrLow) / (float) (_hrHigh - _hrLow) * 2;
                    var color = ratio < 1 ? Color.Lerp(colorLow, colorMid, ratio) : Color.Lerp(colorMid, colorHigh, ratio - 1);

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
            _bpmDownloader?.Stop();
            _counter = null;
            Utils.GamePause.GameEnd();
            // Currently reliant on Counters+, will be phased out later
            AssetBundleManager.ForceRemoveCanvas();
            _logger.Info("Counter destroyed");
        }
    }
}
