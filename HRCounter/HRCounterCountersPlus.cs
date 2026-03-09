using CountersPlus.Counters.Interfaces;
using CountersPlus.Custom;
using CountersPlus.Utils;
using JetBrains.Annotations;
using TMPro;
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
    private readonly CanvasUtility CanvasUtility = null!;

    [Inject]
    private readonly CustomConfigModel Settings = null!;

    private TMP_Text? _counter;

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
        _counter = CanvasUtility.CreateTextFromSettings(Settings);
        _counter.fontSize = 3;
        _counter.text = "";

        var canvas = CanvasUtility.GetCanvasFromID(Settings.CanvasID);
        if (canvas == null)
        {
            _logger.Warn("Cannot find counters+ canvas");
            return false;
        }

        _customCounter = counter.Icon;

        // position the counter as the counters+ one
        _customCounter.transform.localScale = Vector3.one / 30;
        _customCounter.transform.SetParent(canvas.transform, false);
        _customCounter.GetComponent<RectTransform>().anchoredPosition = _counter.rectTransform.anchoredPosition;
        _customCounter.transform.localPosition -= new Vector3(2, 0, 0); // recenter

        // destroy the unused game obj
        Object.Destroy(counter.Counter);

        return true;
    }

    void ICounter.CounterDestroy()
    {
        Teardown();
    }
}
