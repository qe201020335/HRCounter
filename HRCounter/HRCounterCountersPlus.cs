using CountersPlus.Counters.Custom;
using HRCounter.Configuration;
using HRCounter.Data;
using TMPro;
using IPALogger = IPA.Logging.Logger;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HRCounter
{
    public sealed class HRCounterCountersPlus: BasicCustomCounter
    {
        
        private readonly IPALogger _logger = Log.Logger;
        private TMP_Text? _counter;

        private static bool Colorize => PluginConfig.Instance.Colorize;

        private GameObject _customCounter = null!;
        private TMP_Text _customCounterText = null!;

        public override void CounterInit()
        {
            if (!PluginConfig.Instance.ModEnable)
            {
                return;
            }
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

            HRDataManager.OnHRUpdate += OnHRUpdate;
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
                Log.Logger.Warn("Cannot find counters+ canvas");
                return false;
            }

            var counter = AssetBundleManager.SetupCustomCounter();

            if (counter.Icon == null || counter.Numbers == null)
            {
                _logger.Error("Cannot create custom counter");
                return false;
            }

            _customCounter = counter.Icon;
            _customCounterText = counter.Numbers;
            
            // position the counter as the counters+ one
            _customCounter.transform.localScale = Vector3.one / 30;
            _customCounter.transform.SetParent(canvas.transform, false);
            _customCounter.GetComponent<RectTransform>().anchoredPosition = _counter.rectTransform.anchoredPosition;
            _customCounter.transform.localPosition -= new Vector3(2, 0, 0); // recenter
            OnHRUpdate(BPM.Bpm);  // give it an initial value
            _customCounter.SetActive(true);

            if (counter.CurrentCanvas != null)
            {
                // destroy the unused game obj
                Object.Destroy(counter.CurrentCanvas);
            }
            return true;
        }

        private void OnHRUpdate(int bpm)
        {
            _customCounterText.text = Colorize ? $"<color=#{Utils.Utils.DetermineColor(bpm)}>{bpm}</color>" : $"{bpm}";
        }

        public override void CounterDestroy()
        {
            _counter = null;
            HRDataManager.OnHRUpdate -= OnHRUpdate;
            if (_customCounter != null)
            {
                Object.Destroy(_customCounter);
            }
            
            _logger.Info("Counter destroyed");
        }
    }
}
