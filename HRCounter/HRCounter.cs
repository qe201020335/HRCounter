using System.ComponentModel;
using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Utils;
using IPA.Utilities.Async;
using TMPro;
using UnityEngine;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace HRCounter;

internal abstract class HRCounter
{
    [Inject(Id = typeof(HRCounter))]
    private readonly Logger _logger = null!;

    [Inject]
    protected readonly PluginConfig Config = null!;

    [Inject]
    private readonly AssetBundleManager _assetBundleManager = null!;

    [InjectOptional]
    private readonly IInGameHRProvider? _hrProvider = null;

    [InjectOptional]
    private readonly CoreGameHUDController.InitData? _hudInitData;

    private TMP_Text _counterText = null!;

    protected bool Setup()
    {
        if (_hrProvider == null)
        {
            return false;
        }

        if (_hudInitData is { hide: true })
        {
            _logger.Info("CoreGameHUDController.InitData says to hide the HUD, HRCounter hides.");
            return false;
        }

        if (Utils.Utils.IsInReplay() && !_hrProvider.IsReplayData && !Config.ReplayFallbackLiveHr)
        {
            _logger.Info("We are in a replay without hr data, HRCounter hides.");
            return false;
        }

        var counter = _assetBundleManager.SetupCustomCounter();
        if (counter == null)
        {
            _logger.Warn("No Counter asset is loaded!");
            return false;
        }

        counter.Value.ReplayIcon.SetActive(_hrProvider.IsReplayData);
        _counterText = counter.Value.Numbers;

        if (!SetupCounter(counter.Value))
        {
            _logger.Warn("Failed to set up the counter");
        }

        OnHRUpdate(_hrProvider.CurrentHR); // give it an initial value

        _hrProvider.HRChanged += OnHRUpdate;
        Config.PropertyChanged += OnSettingChange;
        _logger.Info("HRCounter created");
        return true;
    }

    protected abstract bool SetupCounter(AssetBundleManager.CustomCounter counter);

    private void OnHRUpdate(int bpm)
    {
        _counterText.color = Config.Colorize ? RenderUtils.DetermineColor(bpm) : Color.white;
        _counterText.text = bpm.ToString();
    }

    private void OnSettingChange(object? sender, PropertyChangedEventArgs args)
    {
        UnityMainThreadTaskScheduler.Factory.StartNew(() => { OnSettingChange(args.PropertyName); });
    }

    protected virtual void OnSettingChange(string propertyName)
    {
    }

    protected void Teardown()
    {
        if (_hrProvider != null)
        {
            _hrProvider.HRChanged -= OnHRUpdate;
        }

        Config.PropertyChanged -= OnSettingChange;
    }
}
