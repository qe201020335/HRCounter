using System;
using System.Collections;
using CountersPlus.Counters.Custom;
using HRCounter.Configuration;
using HRCounter.Data;
using JetBrains.Annotations;
using TMPro;
using IPALogger = IPA.Logging.Logger;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HRCounter
{
    public sealed class HRCounter: BasicCustomCounter
    {
        
        private readonly IPALogger _logger = Logger.logger;
        private TMP_Text _counter;
        private bool _updating;
        [CanBeNull] private BpmDownloader _bpmDownloader;

        private static bool Colorize => PluginConfig.Instance.Colorize;
        

        private GameObject _customCounter;
        private TMP_Text _customCounterText;

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
                
                case "Random":
                    _bpmDownloader = new RandomHR();
                    break;

                default:
                    _bpmDownloader = null;
                    _logger.Warn("Unknown Data Sources");
                    return false;
            }
            return true;
        }

        private void CreateCounter()
        {
            _logger.Info("Creating counter");
            _counter = CanvasUtility.CreateTextFromSettings(Settings);
            _counter.fontSize = 3;
            _counter.text = "";
            
            var canvas = CanvasUtility.GetCanvasFromID(Settings.CanvasID);
            if (canvas == null)
            {
                Logger.logger.Warn("Cannot find counters+ canvas");
                return;
            }

            var counter = AssetBundleManager.SetupCustomCounter();
            _customCounter = counter.Icon;
            _customCounterText = counter.Numbers;
            
            if (_customCounter == null || _customCounterText == null)
            {
                _logger.Error("Cannot create custom counter");
            }
            
            // position the counter as the counters+ one
            _customCounter.transform.localScale = Vector3.one / 30;
            _customCounter.transform.SetParent(canvas.transform, false);
            _customCounter.GetComponent<RectTransform>().anchoredPosition =
                _counter.rectTransform.anchoredPosition;
            _customCounter.transform.localPosition -= new Vector3(2, 0, 0); // recenter

            if (counter.CurrentCanvas != null)
            {
                // destroy the unused game obj
                Object.Destroy(counter.CurrentCanvas);
            }
            
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
                    _counter.text = Colorize ? $"<color=#FFFFFF>HR </color><color=#{Utils.Utils.DetermineColor(bpm)}>{bpm}</color>" : $"HR {bpm}";
                    _customCounter.SetActive(false);
                }
                else
                {
                    _counter.text = String.Empty;
                    _customCounter.SetActive(true);
                    _customCounterText.text = (Colorize ? $"<color=#{Utils.Utils.DetermineColor(bpm)}>{bpm}</color>" : $"{bpm}");
                }
                yield return new WaitForSecondsRealtime(0.25f);
            }
        }

        public override void CounterDestroy()
        {
            Stop();
            _bpmDownloader?.Stop();
            _counter = null;
            Utils.GamePause.GameEnd();
            // Currently reliant on Counters+, will be phased out later
            // AssetBundleManager.ForceRemoveCanvas();

            if (_customCounter != null)
            {
                Object.Destroy(_customCounter);
            }
            
            _logger.Info("Counter destroyed");
        }
    }
}
