using CountersPlus.Counters.Interfaces;
using CountersPlus.Custom;
using CountersPlus.Utils;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter;

[UsedImplicitly]
internal sealed class HRCounterCountersPlus : HRCounter, ICounter
{
    [Inject]
    private readonly IPALogger _logger = null!;

    [Inject]
    private readonly CanvasUtility _canvasUtility = null!;

    [Inject]
    private readonly CustomConfigModel _settings = null!;

    private GameObject _customCounter = null!;

    void ICounter.CounterInit()
    {
        if (!Config.ModEnable)
        {
            return;
        }

        _logger.Info("Initializing Counters+ counter");
        Setup();
    }

    protected override bool SetupCounter(AssetBundleManager.CustomCounter counter)
    {
        var canvas = _canvasUtility.GetCanvasFromID(_settings.CanvasID);
        var canvasSettings = _canvasUtility.GetCanvasSettingsFromID(_settings.CanvasID);
        if (canvas == null || canvasSettings == null)
        {
            _logger.Warn("Cannot find counters+ canvas");
            return false;
        }

        var anchoredPosition = _canvasUtility.GetAnchoredPositionFromConfig(_settings) * canvasSettings.PositionScale;

        _customCounter = counter.Icon;
        _customCounter.name = "HRCounter Counters+";
        _customCounter.transform.localScale = Vector3.one / 30;
        _customCounter.transform.SetParent(canvas.transform, false);
        _customCounter.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
        _customCounter.transform.localPosition -= new Vector3(2, 0, 0); // recenter

        // destroy the unused game obj
        Object.Destroy(counter.Canvas);
        return true;
    }

    void ICounter.CounterDestroy()
    {
        Teardown();
    }
}
