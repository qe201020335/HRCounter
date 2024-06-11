using System;
using BeatSaberMarkupLanguage.Attributes;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.ViewControllers;
using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Web;
using IPA.Utilities.Async;
using SiraUtil.Logging;
using TMPro;
using UnityEngine;
using Zenject;

namespace HRCounter.UI
{
    // setting controller for menu button
    [HotReload(RelativePathToLayout = @"BSML\configMenu.bsml")]
    [ViewDefinition("HRCounter.UI.BSML.configMenu.bsml")]
    internal class SettingMenuController : BSMLAutomaticViewController
    {
        [Inject]
        private readonly PluginConfig _config = null!;
        
        [Inject]
        private readonly SiraLog _logger = null!;
        
        private string _previousDataSource = "";
        
        private void RefreshConfigUi()
        {
            _logger.Trace("SettingMenuController RefreshConfigUi");
            NotifyPropertyChanged(string.Empty);  // refresh all of them

            if (_previousDataSource != _config.DataSource)
            {
                _previousDataSource = _config.DataSource;
                UpdateDataSourceInfoText();
            }
        }
        
        private void UpdateDataSourceInfoText()
        {
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

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            _logger.Trace("SettingMenuController DidActivate");
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            
            if (firstActivation)
            {
                // no point to refresh the ui if it literally just activated and read the config
                UpdateDataSourceInfoText();
                _previousDataSource = _config.DataSource;
            }
            else
            {
                // config might have changed while we were inactive
                RefreshConfigUi();
            }
            
            _config.OnSettingsChanged += RefreshConfigUi;
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _logger.Trace("SettingMenuController DidDeactivate");
            _config.OnSettingsChanged -= RefreshConfigUi;
            _previousDataSource = "";  // force a refresh next time we activate
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        #region UIComponents
        
        [UIValue("ModEnable")]
        private bool ModEnable
        {
            get => _config.ModEnable;
            set => _config.ModEnable = value;
        }
        

        [UIValue("LogHR")]
        public bool LogHR
        {
            get => _config.LogHR;
            set => _config.LogHR = value;
        }
        

        [UIValue("Colorize")]
        public bool Colorize
        {
            get => _config.Colorize;
            set => _config.Colorize = value;
        }

        [UIValue("HRLow")]
        public int HRLow
        {
            get => _config.HRLow;
            set => _config.HRLow = value;
        }
        
        [UIValue("HRHigh")]
        public int HRHigh
        {
            get => _config.HRHigh;
            set => _config.HRHigh = value;
        }

        [UIValue("color-info-text")]
        public string ColorInfoText =>
            $"Current Colors: <color=#{ColorUtility.ToHtmlStringRGBA(_config.LowColor)}>Low</color> -> <color=#{ColorUtility.ToHtmlStringRGBA(_config.MidColor)}>Middle</color> -> <color=#{ColorUtility.ToHtmlStringRGBA(_config.HighColor)}>High</color>";
        
        [UIValue("HideDuringReplay")]
        public bool HideDuringReplay
        {
            get => _config.HideDuringReplay;
            set => _config.HideDuringReplay = value;
        }
        
        [UIValue("source-list-options")]
        public List<object> DataSourceOptions => new List<object>(DataSourceManager.DataSourceTypes.Keys);

        [UIValue("source-list-choice")]
        public string DataSourceChoice
        {
            get => _config.DataSource;
            set => _config.DataSource = value;
        }
        
        [UIValue("PauseHR")]
        public int PauseHR
        {
            get => _config.PauseHR;
            set => _config.PauseHR = value;
        }

        [UIValue("AutoPause")]
        public bool AutoPause
        {
            get => _config.AutoPause;
            set => _config.AutoPause = value;
        }

        [UIValue("IgnoreCounters+")]
        public bool UseCountersPlus
        {
            get => _config.IgnoreCountersPlus;
            set => _config.IgnoreCountersPlus = value;
        }
        
        [UIComponent("data-source-info-text")]
        private TextMeshProUGUI _dataSourceInfoText = null!;
        
        [UIValue("NoBloom")]
        private bool NoBloom
        {
            get => _config.NoBloom;
            set => _config.NoBloom = value;
        }
        #endregion

    }
}