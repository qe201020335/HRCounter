using CountersPlus.Counters.Custom;
using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Utils;
using TMPro;
using UnityEngine;
using Zenject;
using IPALogger = IPA.Logging.Logger;
using Object = UnityEngine.Object;

namespace HRCounter;

public sealed class HRCounterCountersPlus : BasicCustomCounter
{
    [Inject]
    private readonly AssetBundleManager _assetBundleManager = null!;

    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly IPALogger _logger = null!;

    [InjectOptional]
    private readonly IInGameHRProvider? _hrProvider = null;

    private TMP_Text? _counter;

    private GameObject _customCounter = null!;
    private TMP_Text _customCounterText = null!;

    public override void CounterInit()
    {
        if (!_config.ModEnable || _hrProvider == null)
        {
            return;
        }

        if (Utils.Utils.IsInReplay() && !_hrProvider.IsReplayData && !_config.ReplayFallbackLiveHr)
        {
            _logger.Info("We are in a replay without hr data, HRCounter hides.");
            return;
        }

        if (!CreateCounter())
        {
            _logger.Warn("Cannot create counter");
            return;
        }

        _hrProvider.HRChanged += OnHRUpdate;
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

        if (counter == null)
        {
            _logger.Error("Cannot create custom counter");
            return false;
        }

        counter.Value.ReplayIcon.SetActive(_hrProvider!.IsReplayData);
        _customCounter = counter.Value.Icon;
        _customCounterText = counter.Value.Numbers;

        // position the counter as the counters+ one
        _customCounter.transform.localScale = Vector3.one / 30;
        _customCounter.transform.SetParent(canvas.transform, false);
        _customCounter.GetComponent<RectTransform>().anchoredPosition = _counter.rectTransform.anchoredPosition;
        _customCounter.transform.localPosition -= new Vector3(2, 0, 0); // recenter
        OnHRUpdate(_hrProvider.CurrentHR); // give it an initial value
        _customCounter.SetActive(true);

        // destroy the unused game obj
        Object.Destroy(counter.Value.Counter);

        return true;
    }

    private void OnHRUpdate(int bpm)
    {
        _customCounterText.color = _config.Colorize ? RenderUtils.DetermineColor(bpm) : Color.white;
        _customCounterText.text = bpm.ToString();
    }

    public override void CounterDestroy()
    {
        if (_hrProvider != null)
        {
            _hrProvider.HRChanged -= OnHRUpdate;
        }

        _counter = null;
        if (_customCounter != null)
        {
            Object.Destroy(_customCounter);
        }

        _logger.Info("Counter destroyed");
    }
}
