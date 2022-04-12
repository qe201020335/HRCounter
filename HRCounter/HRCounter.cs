using System;
using System.Collections;
using CountersPlus.Counters.Custom;
using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Events;
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
            
            if (!CreateCounter())
            {
                _logger.Warn("Cannot create counter");
                return;
            }
            
            Utils.GamePause.GameStart();

            if (!HRController.InitAndStartDownloader())
            {
                _logger.Warn("Can't start bpm downloader");
                _logger.Warn("Please check your settings about data source and the link or id.");
                return;
            }
            
            _logger.Info("Start updating counter text");
        }

        private bool CreateCounter()
        {
            _logger.Info("Creating counter");
            _counter = CanvasUtility.CreateTextFromSettings(Settings);
            _counter.fontSize = 3;
            _counter.text = "";
            
            var canvas = CanvasUtility.GetCanvasFromID(Settings.CanvasID);
            if (canvas == null)
            {
                Logger.logger.Warn("Cannot find counters+ canvas");
                return false;
            }

            var counter = AssetBundleManager.SetupCustomCounter();
            _customCounter = counter.Icon;
            _customCounterText = counter.Numbers;
            
            if (_customCounter == null || _customCounterText == null)
            {
                _logger.Error("Cannot create custom counter");
                return false;
            }
            
            // position the counter as the counters+ one
            _customCounter.transform.localScale = Vector3.one / 30;
            _customCounter.transform.SetParent(canvas.transform, false);
            _customCounter.GetComponent<RectTransform>().anchoredPosition =
                _counter.rectTransform.anchoredPosition;
            _customCounter.transform.localPosition -= new Vector3(2, 0, 0); // recenter

            OnHRUpdate(null, new HRUpdateEventArgs(BPM.Instance.Bpm));  // give it an initial value

            if (counter.CurrentCanvas != null)
            {
                // destroy the unused game obj
                Object.Destroy(counter.CurrentCanvas);
            }
            HRController.OnHRUpdate += OnHRUpdate;
            return true;
        }

        private void OnHRUpdate(object sender, HRUpdateEventArgs args)
        {
            var bpm = args.HeartRate;
            _customCounter.SetActive(true);
            _customCounterText.text = Colorize ? $"<color=#{Utils.Utils.DetermineColor(bpm)}>{bpm}</color>" : $"{bpm}";
        }

        public override void CounterDestroy()
        {
            _counter = null;
            Utils.GamePause.GameEnd();
            HRController.OnHRUpdate -= OnHRUpdate;
            if (_customCounter != null)
            {
                Object.Destroy(_customCounter);
            }
            
            _logger.Info("Counter destroyed");
        }
    }
}
