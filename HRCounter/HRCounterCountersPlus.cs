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

    private Transform _counter = null!;

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

        _counter = counter.Container;
        _counter.gameObject.name = "HRCounter Counters+";
        _counter.localScale = Vector3.one / 30;
        _counter.SetParent(canvas.transform, false);
        _counter.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;

        // destroy the unused game obj
        Object.Destroy(counter.Canvas);
        return true;
    }

    void ICounter.CounterDestroy()
    {
        Teardown();
    }
}
