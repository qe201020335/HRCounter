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

    internal CustomCounter? SetupCustomCounter()
    {
        if (CounterPrefab == null)
        {
            return null;
        }

        var currentCanvas = Object.Instantiate(CounterPrefab);
        var icon = currentCanvas.transform.GetChild(0).gameObject;
        var numbers = icon.transform.GetChild(0).GetComponent<TMP_Text>();
        var replayIcon = icon.transform.GetChild(1).gameObject;
        numbers.alignment = TextAlignmentOptions.MidlineLeft;

        var iconImage = icon.GetComponent<Image>();
        iconImage.material = RenderUtils.UINoGlow;
        // var replayIconImage = replayIcon.GetComponent<Image>();
        // replayIconImage.material = RenderUtils.UINoGlow;
        if (!string.IsNullOrWhiteSpace(_config.CustomIcon) && _iconManager.TryGetIconSprite(_config.CustomIcon, out var sprite))
        {
            iconImage.sprite = sprite;
        }

        numbers.font.material = numbers.fontMaterial;
        numbers.fontMaterial.shader = _config.NoBloom ? RenderUtils.TextNoGlow : RenderUtils.TextGlow;
        _logger.Debug($"Using font shader: {numbers.fontMaterial.shader.name}");

        return new CustomCounter
        {
            Counter = currentCanvas,
            Icon = icon,
            ReplayIcon = replayIcon,
            Numbers = numbers
        };
    }

    internal struct CustomCounter
    {
        public GameObject Counter;
        public GameObject Icon;
        public GameObject ReplayIcon;
        public TMP_Text Numbers;
    }
}
