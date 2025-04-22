using System;
using System.ComponentModel;
using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Utils;
using IPA.Utilities.Async;
using TMPro;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter;

internal class HRCounterController : IInitializable, IDisposable
{
    [InjectOptional]
    private readonly GameplayCoreSceneSetupData? _sceneSetupData = null;

    [InjectOptional]
    private readonly FlyingGameHUDRotation? _flyingGameHUDRotation = null;

    [Inject]
    private readonly AssetBundleManager _assetBundleManager = null!;

    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly IPALogger _logger = null!;

    [InjectOptional]
    private readonly HRDataManager? _hrDataManager;

    private bool _needs360Move;

    private GameObject _currentCanvas = null!;
    private TMP_Text _numbersText = null!;

    public void Initialize()
    {
        if (!_config.ModEnable)
        {
            return;
        }

        if (_sceneSetupData == null)
        {
            _logger.Warn("GameplayCoreSceneSetupData is null");
            return;
        }

        if (_config.HideDuringReplay && Utils.Utils.IsInReplay())
        {
            _logger.Info("We are in a replay, HRCounter hides.");
            return;
        }

        if (_hrDataManager == null)
        {
            _logger.Warn("HRDataManager is null");
            return;
        }

        _needs360Move = _sceneSetupData.beatmapKey.beatmapCharacteristic.requires360Movement && _flyingGameHUDRotation != null;
        _logger.Info($"360/90?: {_needs360Move}");

        if (!CreateCounter())
        {
            _logger.Warn("Cannot create HRCounter");
            return;
        }

        _hrDataManager.OnHRUpdate -= OnHRUpdate;
        _hrDataManager.OnHRUpdate += OnHRUpdate;
        _config.PropertyChanged += OnSettingChange;

        _logger.Info("HRCounter Initialized");
    }

    private bool CreateCounter()
    {
        _logger.Info("Creating HRCounter");

        var counter = _assetBundleManager.SetupCustomCounter();
        if (!counter.IsNotNull())
        {
            _logger.Warn("No Counter asset is loaded!");
            return false;
        }

        _currentCanvas = counter.Counter!;
        _numbersText = counter.Numbers!;

        _currentCanvas.transform.localScale = Vector3.one / 150;

        OnHRUpdate(BPM.Bpm); // give it an initial value

        if (!_needs360Move)
        {
            // Place our Canvas in a Static Location
            var location = _config.StaticCounterPosition;
            _currentCanvas.transform.position = new Vector3(location.x, location.y, location.z);
            _currentCanvas.transform.rotation = Quaternion.identity;
        }
        else
        {
            _logger.Debug("Attaching HRCounter to FlyingGameHUDRotation");
            // Attach it to the FlyingHUD
            var hudContainer = _flyingGameHUDRotation!.transform.Find("Container");
            if (hudContainer == null)
            {
                _logger.Warn("Cannot find HUD container in FlyingGameHUDRotation");
                return false;
            }

            _currentCanvas.transform.SetParent(hudContainer);
            _currentCanvas.transform.localPosition = new Vector3(-2, -20, 0);
            _currentCanvas.transform.localRotation = Quaternion.identity;
        }

        return true;
    }

    private void OnHRUpdate(int bpm)
    {
        _numbersText.color = _config.Colorize ? RenderUtils.DetermineColor(bpm) : Color.white;
        _numbersText.text = bpm.ToString();
    }

    private void OnSettingChange(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(PluginConfig.StaticCounterPosition) || _needs360Move) return;
        _logger.Info("Settings changed, updating counter location.");

        UnityMainThreadTaskScheduler.Factory.StartNew(() => { _currentCanvas.transform.position = _config.StaticCounterPosition; });
    }

    public void Dispose()
    {
        if (_hrDataManager != null)
        {
            _hrDataManager.OnHRUpdate -= OnHRUpdate;
        }

        _config.PropertyChanged -= OnSettingChange;

        if (_currentCanvas != null)
        {
            Object.Destroy(_currentCanvas);
        }

        _logger.Info("HRCounter Disposed");
    }
}
