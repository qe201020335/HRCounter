using System;
using System.Collections;
using BeatSaberMarkupLanguage.Attributes;
using System.Collections.Generic;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.ViewControllers;
using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Utils;
using IPA.Utilities.Async;
using SiraUtil.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

            UpdateColorText();
        }
        
        private void UpdateColorText()
        {
            if (_colorVisualizerCoroutine == null)
            {
                ColorInfoText.text = DefaultColorText;
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
                _dataSourceInfoRefreshBtn.interactable = false;
                string newText;
                try
                {
                    newText = await source.GetSourceLinkText();
                }
                catch (Exception e)
                {
                    _logger.Error($"Failed to update data source info text: {e}");
                    newText = "<color=#FF0000>Failed to load info, check logs for details.</color>";
                }

                _dataSourceInfoText.text = newText;
                await Task.Delay(500);  // no spamming the button
                _dataSourceInfoRefreshBtn.interactable = true;
            });
        }
        
        /**
         * Request a UI refresh from other threads
         */
        private void RequestUiRefresh()
        {
            UnityMainThreadTaskScheduler.Factory.StartNew(RefreshConfigUi);
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
                UpdateColorText();
            }
            else
            {
                // config might have changed while we were inactive
                RefreshConfigUi();
            }
            
            _config.OnSettingsChanged += RequestUiRefresh;
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _logger.Trace("SettingMenuController DidDeactivate");
            _config.OnSettingsChanged -= RequestUiRefresh;
            _previousDataSource = "";  // force a refresh next time we activate
            if (_colorVisualizerCoroutine != null)
            {
                VisualizeColorsBtnPressed();  // stop the visualization and reset the text
            }
            
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

        [UIComponent("color-info-text")]
        private TMP_Text ColorInfoText = null!;
        
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
        
        [UIComponent("data-source-info-refresh-btn")]
        private Button _dataSourceInfoRefreshBtn = null!;

        [UIValue("EnableHttpServer")]
        private bool EnableHttpServer
        {
            get => _config.EnableHttpServer;
            set => _config.EnableHttpServer = value;
        }
        
        [UIValue("EnableOscServer")]
        private bool EnableOscServer
        {
            get => _config.EnableOscServer;
            set => _config.EnableOscServer = value;
        }
        
        [UIValue("LowColor")]
        private Color LowColor
        {
            get => _config.LowColor;
            set => _config.LowColor = value;
        }
        
        [UIValue("MidColor")]
        private Color MidColor
        {
            get => _config.MidColor;
            set => _config.MidColor = value;
        }
        
        [UIValue("HighColor")]
        private Color HighColor
        {
            get => _config.HighColor;
            set => _config.HighColor = value;
        }
        #endregion
        
        [UIAction("data-source-info-refresh-btn-action")]
        private void OnDataSourceInfoRefreshBtnPressed()
        {
            UpdateDataSourceInfoText();
        }
        
        [UIAction("reset-low-color")]
        private void ResetLowColor()
        {
            _config.LowColor = PluginConfig.DefaultValues.LowColor;
        }
        
        [UIAction("reset-mid-color")]
        private void ResetMidColor()
        {
            _config.MidColor = PluginConfig.DefaultValues.MidColor;
        }
        
        [UIAction("reset-high-color")]
        private void ResetHighColor()
        {
            _config.HighColor = PluginConfig.DefaultValues.HighColor;
        }
        
        private Coroutine? _colorVisualizerCoroutine;
        
        private string _visualizeColorsBtnText = "Visualize";
        
        [UIValue("visualize-colors-btn-text")]
        private string VisualizeColorsBtnText { 
            get => _visualizeColorsBtnText;
            set
            {
                _visualizeColorsBtnText = value;
                NotifyPropertyChanged();
            } 
        }
        
        [UIAction("visualize-colors-btn-pressed")]
        private void VisualizeColorsBtnPressed()
        {
            if (_colorVisualizerCoroutine != null)
            {
                StopCoroutine(_colorVisualizerCoroutine);
                _colorVisualizerCoroutine = null;
                ColorInfoText.text = DefaultColorText;
                VisualizeColorsBtnText = "Visualize";
            }
            else
            {
                _colorVisualizerCoroutine = StartCoroutine(VisualizeColorsCoroutine());
                VisualizeColorsBtnText = "Stop";
            }
        }

        private string DefaultColorText =>
            $"<color=#{ColorUtility.ToHtmlStringRGBA(_config.LowColor)}>Low</color> -> <color=#{ColorUtility.ToHtmlStringRGBA(_config.MidColor)}>Middle</color> -> <color=#{ColorUtility.ToHtmlStringRGBA(_config.HighColor)}>High</color>";

        private IEnumerator VisualizeColorsCoroutine()
        {
            var start = _config.HRLow;

            while (isActivated)
            {
                ColorInfoText.text = $"<size=+5><color=#{ColorUtility.ToHtmlStringRGB(RenderUtils.DetermineColor(start))}>{start}</color></size>";
                start++;
                if (start > _config.HRHigh)
                {
                    start = _config.HRLow;
                }
                yield return new WaitForSeconds(0.05f);
            }
        }
    }
}