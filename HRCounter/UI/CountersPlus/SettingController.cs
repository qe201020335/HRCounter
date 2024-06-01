using System;
using BeatSaberMarkupLanguage.Attributes;
using TMPro;
using HRCounter.Configuration;
using HRCounter.Data;
using IPA.Utilities.Async;
using SiraUtil.Logging;

namespace HRCounter.UI.CountersPlus
{
    // setting controller for Counters+ counter configuration page
    internal class SettingController
    {
        private PluginConfig _config = null!;

        private SiraLog _logger = null!;

        [UIComponent("data-source-text")]
        private TMP_Text _dataSourceText = null!;

        [UIComponent("data-source-info-text")]
        private TMP_Text _dataSourceInfoText = null!;

        private bool _parsed = false;

        private string _previousDataSource = "";

        private SettingController(PluginConfig config, SiraLog logger)
        {
            _config = config;
            _logger = logger;
            _config.OnSettingsChanged += SettingsChangedHandler;
            _logger.Trace("SettingController constructed");
        }

        // Due to an really old issue in Counters+ https://github.com/NuggoDEV/CountersPlus/pull/141
        // MonoBehaviour settings menu will not work, though we should use MonoBehaviour to sync up 
        // our menu lifecycle with the UI components'
        // A workaround here using a de-constructor.
        ~SettingController()
        {
            _config.OnSettingsChanged -= SettingsChangedHandler;
            _logger.Trace("SettingController destructed");
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
            if (!_parsed || _dataSourceText == null || _dataSourceInfoText == null)
            {
                // the game soft restarted so the components were destroyed
                _parsed = false;
                _dataSourceText = null!;
                _dataSourceInfoText = null!;
                return;
            }

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
                _dataSourceInfoText.text = "Unknown Data Source";
                return;
            }

            _dataSourceInfoText.text = "Loading Data Source Info...";
            UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
            {
                try
                {
                    _dataSourceInfoText.text = await source.GetSourceLinkText();
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