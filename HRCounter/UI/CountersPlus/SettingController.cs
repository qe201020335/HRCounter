using System.ComponentModel;
using BeatSaberMarkupLanguage.Attributes;
using HRCounter.Configuration;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using Zenject;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter.UI.CountersPlus;

/// <summary>
///     setting controller for Counters+ counter configuration page
/// </summary>
internal class SettingController : MonoBehaviour
{
    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly IPALogger _logger = null!;

    [UIComponent("data-source-text")]
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    private TMP_Text _dataSourceText = null!;

    private bool _parsed;

    private string _previousDataSource = "";

    private void OnEnable()
    {
        _config.PropertyChanged += SettingsChangedHandler;
        _logger.Trace("SettingController OnEnable");
        if (_parsed)
        {
            UpdateText();
        }
    }

    private void OnDisable()
    {
        _config.PropertyChanged -= SettingsChangedHandler;
        _logger.Trace("SettingController OnDisable");
    }

    [UIAction("#post-parse")]
    [UsedImplicitly]
    private void PostParse()
    {
        _logger.Trace("PostParse");
        _parsed = true;
        UpdateText();
    }

    private void SettingsChangedHandler(object? sender, PropertyChangedEventArgs args)
    {
        if (_parsed && _previousDataSource != _config.DataSource)
        {
            UpdateText();
        }
    }

    private void UpdateText()
    {
        _logger.Debug("Updating text");
        _previousDataSource = _config.DataSource;
        _dataSourceText.text = $"Current DataSource: {_config.DataSource}";
    }
}
