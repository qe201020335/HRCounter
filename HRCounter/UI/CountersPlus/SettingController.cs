using System.ComponentModel;
using BeatSaberMarkupLanguage.Attributes;
using HRCounter.Configuration;
using HRCounter.Data;
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

    [Inject]
    private readonly DataSourceManager _dataSourceManager = null!;

    [UIComponent("data-source-text")]
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    private TMP_Text _dataSourceText = null!;

    private bool _parsed;

    private void OnEnable()
    {
        _config.PropertyChanged += SettingsChangedHandler;
        _logger.Trace("SettingController OnEnable");
        UpdateText();
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
        if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(_config.DataSource))
        {
            UpdateText();
        }
    }

    private void UpdateText()
    {
        if (!_parsed) return;
        _logger.Debug("Updating text");
        _dataSourceText.text = _dataSourceManager.GetFromKey(_config.DataSource) is null
            ? $"Current DataSource: <color=yellow>Unknown</color> ({_config.DataSource})"
            : $"Current DataSource: <color=lightblue>{_config.DataSource}</color>";
    }
}
