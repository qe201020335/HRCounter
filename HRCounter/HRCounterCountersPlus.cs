using CountersPlus.Counters.Custom;
using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Utils;
using SiraUtil.Logging;
using TMPro;
using IPALogger = IPA.Logging.Logger;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace HRCounter
{
    public sealed class HRCounterCountersPlus : BasicCustomCounter
    {
        [Inject] private readonly AssetBundleManager _assetBundleManager = null!;

        [Inject] private readonly PluginConfig _config = null!;

        [Inject] private readonly SiraLog _logger = null!;

        [InjectOptional] private readonly HRDataManager? _hrDataManager;

        [Inject] private readonly RenderUtils _renderUtils = null!;

        private TMP_Text? _counter;

        private GameObject _customCounter = null!;
        private TMP_Text _customCounterText = null!;

        public override void CounterInit()
        {
            if (!_config.ModEnable)
            {
                return;
            }

            if (_config.HideDuringReplay && Utils.Utils.IsInReplay())
            {
                _logger.Info("We are in a replay, Counter hides.");
                return;
            }

            if (_hrDataManager == null)
            {
                Plugin.Log.Warn("HRDataManager is null");
                return;
            }
            
            if (!CreateCounter())
            {
                _logger.Warn("Cannot create counter");
                return;
            }

            _hrDataManager.OnHRUpdate += OnHRUpdate;
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
                _logger.Warn("Cannot find counters+ canvas");
                return false;
            }

            var counter = _assetBundleManager.SetupCustomCounter();

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
            OnHRUpdate(BPM.Bpm); // give it an initial value
            _customCounter.SetActive(true);

            if (counter.Counter != null)
            {
                // destroy the unused game obj
                Object.Destroy(counter.Counter);
            }

            return true;
        }

        private void OnHRUpdate(int bpm)
        {
            _customCounterText.color = _config.Colorize ? _renderUtils.DetermineColor(bpm) : Color.white;
            _customCounterText.text = bpm.ToString();
        }

        public override void CounterDestroy()
        {
            if (_hrDataManager != null)
            {
                _hrDataManager.OnHRUpdate -= OnHRUpdate;
            }

            _counter = null;
            if (_customCounter != null)
            {
                Object.Destroy(_customCounter);
            }

            _logger.Info("Counter destroyed");
        }
    }
}