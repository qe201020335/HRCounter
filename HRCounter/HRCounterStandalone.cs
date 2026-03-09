using System;
using HRCounter.Configuration;
using UnityEngine;
using Zenject;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter;

internal class HRCounterStandalone : HRCounter, IInitializable, IDisposable
{
    private readonly IPALogger _logger;

    private readonly Transform? _rotationContainer;

    private GameObject _counter = null!;

    public HRCounterStandalone(IPALogger logger, [InjectOptional] FlyingGameHUDRotation? flyingGameHUDRotation)
    {
        _logger = logger;
        if (flyingGameHUDRotation != null)
        {
            _rotationContainer = flyingGameHUDRotation.transform.Find("Container");
            if (_rotationContainer == null)
            {
                logger.Warn("FlyingGameHUDRotation exists but cannot find rotation container");
            }
        }
    }

    public void Initialize()
    {
        if (!Config.ModEnable)
        {
            return;
        }

        _logger.Info("Initializing standalone counter");
        Setup();
    }

    protected override bool SetupCounter(AssetBundleManager.CustomCounter counter)
    {
        _counter = counter.Canvas;
        _counter.name = "HRCounter Standalone";
        _counter.transform.localScale = Vector3.one / 150;

        if (_rotationContainer == null)
        {
            // Place our Canvas in a Static Location
            _counter.transform.position = Config.StaticCounterPosition + AssetBundleManager.StaticPositionOffset;
            _counter.transform.rotation = Quaternion.identity;
        }
        else
        {
            _logger.Debug("Attaching HRCounter to flying hud container");
            // Attach it to the FlyingHUD
            _counter.transform.SetParent(_rotationContainer);
            _counter.transform.localPosition = new Vector3(0, 55, 0);
            _counter.transform.localRotation = Quaternion.identity;
        }

        return true;
    }

    protected override void OnSettingChange(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(PluginConfig.StaticCounterPosition))
        {
            if (_rotationContainer == null)
            {
                _logger.Info("Settings changed, updating counter location.");
                _counter.transform.position = Config.StaticCounterPosition;
            }
        }
    }

    public void Dispose()
    {
        Teardown();
    }
}
