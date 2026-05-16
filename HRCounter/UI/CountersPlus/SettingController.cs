using System;
using System.ComponentModel;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using HRCounter.Configuration;
using HRCounter.Data;
using IPA.Utilities.Async;
using TMPro;
using UnityEngine;
using Zenject;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter.UI.CountersPlus;

// setting controller for Counters+ counter configuration page
internal class SettingController : MonoBehaviour
{
    private PluginConfig _config = null!;

    private IPALogger _logger = null!;

    private DataSourceManager _dataSourceManager = null!;

    [UIComponent("data-source-text")]
    private TMP_Text _dataSourceText = null!;

    [UIComponent("data-source-info-text")]
    private TextPageScrollView _dataSourceInfoText = null!;

    private bool _parsed = false;

    private string _previousDataSource = "";

    [Inject]
    private void Init(PluginConfig config, IPALogger logger, DataSourceManager dataSourceManager)
    {
        _config = config;
        _logger = logger;
        _dataSourceManager = dataSourceManager;
        _logger.Trace("SettingController injection init");
    }

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
            // the event is not broadcast on main thread
            UnityMainThreadTaskScheduler.Factory.StartNew(UpdateText);
        }
    }

    private void UpdateText()
    {
        _logger.Debug("Updating text");
        _previousDataSource = _config.DataSource;
        _dataSourceText.text = $"Current DataSource: {_config.DataSource}";
        var source = _dataSourceManager.GetFromKey(_config.DataSource);
        if (source is null)
        {
            _dataSourceInfoText.SetText("Unknown Data Source");
            return;
        }

        _dataSourceInfoText.SetText("Loading Data Source Info...");
        UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
        {
            try
            {
                _dataSourceInfoText.SetText(await source.GetStatusText());
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to update data source info text: {e}");
                throw;
            }
        });
    }
}
