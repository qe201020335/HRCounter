using System;
using System.Reflection;
using HRCounter.Configuration;
using HRCounter.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Logger = IPA.Logging.Logger;
using Object = UnityEngine.Object;

namespace HRCounter;

internal class AssetBundleManager : IInitializable, IDisposable
{
    // Prefab layout cleanup changed the center position,
    // this is the offset move it back to the previous position 
    internal static readonly Vector3 StaticPositionOffset = new(0, 1.7f, 0);
    
    private GameObject? CounterPrefab { get; set; }

    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly IconManager _iconManager = null!;

    internal Sprite? DefaultIconSprite { get; private set; }

    public void Initialize()
    {
        using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HRCounter.Resources.hrcounter");
        if (resourceStream == null)
        {
            _logger.Error("Failed to find hrcounter AssetBundle from ManifestResourceStream!");
            return;
        }

        try
        {
            // Load the AssetBundle
            var bundle = AssetBundle.LoadFromStream(resourceStream);
            CounterPrefab = bundle.LoadAsset<GameObject>("Assets/HRCounter.prefab");
            bundle.Unload(false);
            if (CounterPrefab == null)
            {
                throw new NullReferenceException("The counter prefab is null!");
            }

            DefaultIconSprite = CounterPrefab.GetComponentInChildren<Image>().sprite;
            _logger.Info("AssetBundle Loaded!");
        }
        catch (Exception e)
        {
            _logger.Error($"Cannot load the counter prefab from the asset bundle: {e.Message}");
            _logger.Error(e);
        }
    }

    public void Dispose()
    {
        if (CounterPrefab != null)
        {
            Object.Destroy(CounterPrefab);
            CounterPrefab = null;
        }
    }

    internal CustomCounter? SetupCustomCounter(bool isReplay)
    {
        _logger.Info("Creating HRCounter object from prefab");
        if (CounterPrefab == null)
        {
            _logger.Warn("Counter prefab is not loaded!");
            return null;
        }

        var canvas = Object.Instantiate(CounterPrefab);
        if (canvas == null)
        {
            _logger.Warn("Failed to instantiate counter prefab");
            return null;
        }

        var container = canvas.transform.GetChild(0)!;
        var icon = container.GetChild(0).gameObject;
        var replayIcon = icon.transform.GetChild(0).gameObject;
        var numbers = container.GetChild(1).GetComponent<TMP_Text>();
        
        var iconImage = icon.GetComponent<Image>();
        iconImage.material = RenderUtils.UINoGlow;
        if (!string.IsNullOrWhiteSpace(_config.CustomIcon) && _iconManager.TryGetIconSprite(_config.CustomIcon, out var sprite))
        {
            iconImage.sprite = sprite;
        }

        replayIcon.SetActive(isReplay);

        numbers.font.material = numbers.fontMaterial;
        numbers.fontMaterial.shader = _config.NoBloom ? RenderUtils.TextNoGlow : RenderUtils.TextGlow;

        return new CustomCounter
        {
            Canvas = canvas,
            Container = container,
            // Icon = icon,
            // ReplayIcon = replayIcon,
            Numbers = numbers
        };
    }

    internal struct CustomCounter
    {
        public GameObject Canvas;

        public Transform Container;

        // public GameObject Icon;
        // public GameObject ReplayIcon;

        public TMP_Text Numbers;
    }
}
