using System;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using TMPro;
using HRCounter.Configuration;
using HRCounter.Data;
using IPA.Utilities.Async;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace HRCounter.UI.CountersPlus
{
    // setting controller for Counters+ counter configuration page
    internal class SettingController: MonoBehaviour
    {
        private PluginConfig _config = null!;

        private SiraLog _logger = null!;

        [UIComponent("data-source-text")]
        private TMP_Text _dataSourceText = null!;

        [UIComponent("data-source-info-text")]
        private TextPageScrollView _dataSourceInfoText = null!;

        private bool _parsed = false;

        private string _previousDataSource = "";

        [Inject]
        private void Init(PluginConfig config, SiraLog logger)
        {
            _config = config;
            _logger = logger;
            _logger.Trace("SettingController injection init");
        }
        
        private void OnEnable()
        {
            _config.OnSettingsChanged += SettingsChangedHandler;
            _logger.Trace("SettingController OnEnable");
            if (_parsed)
            {
                UpdateText();
            }
        }
        
        private void OnDisable()
        {
            _config.OnSettingsChanged -= SettingsChangedHandler;
            _logger.Trace("SettingController OnDisable");
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            _logger.Trace("PostParse");
            _parsed = true;
            UpdateText();
        }

        private void SettingsChangedHandler()
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
            var known = DataSourceManager.TryGetFromKey(_config.DataSource, out var source);
            if (!known)
            {
                _dataSourceInfoText.SetText("Unknown Data Source");
                return;
            }

            _dataSourceInfoText.SetText("Loading Data Source Info...");
            UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
            {
                try
                {
                    _dataSourceInfoText.SetText(await source.GetSourceLinkText());
                }
                catch (Exception e)
                {
                    _logger.Error($"Failed to update data source info text: {e}");
                    throw;
                }
            });
        }
    }
}