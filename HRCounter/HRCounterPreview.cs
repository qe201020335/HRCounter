using System.Collections;
using System.ComponentModel;
using HRCounter.Configuration;
using HRCounter.Utils;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter;

public class HRCounterPreview : MonoBehaviour
{
    [Inject]
    private readonly IPALogger _logger = null!;

    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly IconManager _iconManager = null!;

    private readonly WaitForSeconds _delay = new(0.05f);

    private TMP_Text _text = null!;

    private Image? _icon;

    private Coroutine? _coroutine;

    private int _value;

    [Inject]
    [UsedImplicitly]
    private void Init()
    {
        _logger.Trace("Init");
        _text = GetComponentInChildren<TMP_Text>();
        if (_text == null)
        {
            _logger.Warn("Failed to find TMP_Text component in preview counter!");
            enabled = false;
            return;
        }

        _value = Mathf.Max(_config.HRLow - 10, 0);
        _icon = transform.GetChild(0).GetComponent<Image>();
    }

    private void OnEnable()
    {
        _logger.Trace("OnEnable");
        _coroutine = StartCoroutine(UpdateText());
        OnConfigChanged(_config, new PropertyChangedEventArgs(""));
        _config.PropertyChanged += OnConfigChanged;
        if (_icon != null)
        {
            _iconManager.IconsLoaded += UpdateIcon;
        }
    }

    private void OnDisable()
    {
        _logger.Trace("OnDisable");
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }

        _config.PropertyChanged -= OnConfigChanged;
        _iconManager.IconsLoaded -= UpdateIcon;
    }

    private IEnumerator UpdateText()
    {
        while (enabled)
        {
            _text.text = _value.ToString();
            _text.color = RenderUtils.DetermineColor(_value);
            _value++;
            if (_value > _config.HRHigh + 10)
            {
                _value = Mathf.Max(_config.HRLow - 10, 0);
            }

            yield return _delay;
        }
    }

    private void OnConfigChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(_config.NoBloom))
        {
            _text.fontMaterial.shader = _config.NoBloom ? RenderUtils.TextNoGlow : RenderUtils.TextGlow;
        }

        if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(_config.CustomIcon))
        {
            UpdateIcon();
        }
    }

    private void UpdateIcon()
    {
        if (_icon == null) return;
        var sprite = string.IsNullOrWhiteSpace(_config.CustomIcon)
            ? _iconManager.DefaultIcon
            : _iconManager.TryGetIconSprite(_config.CustomIcon, out var sprite1)
                ? sprite1
                : _iconManager.DefaultIcon;
        _icon.sprite = sprite;
    }
}
