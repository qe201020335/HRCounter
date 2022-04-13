using BeatSaberMarkupLanguage.Attributes;
using System.Collections.Generic;
using TMPro;
using HRCounter.Configuration;

namespace HRCounter.UI.CountersPlus
{
    // setting controller for Counters+ counter configuration page
    internal class SettingController
    {
        /*
        [UIValue("LogHR")]
        public bool LogHR
        {
            get => PluginConfig.Instance.LogHR;
            set
            {
                PluginConfig.Instance.LogHR = value;
            }
        }
        

        [UIValue("Colorize")]
        public bool Colorize
        {
            get => PluginConfig.Instance.Colorize;
            set
            {
                PluginConfig.Instance.Colorize = value;
            }
        }

        [UIValue("HRLow")]
        public int HRLow
        {
            get => PluginConfig.Instance.HRLow;
            set
            {
                PluginConfig.Instance.HRLow = value;
            }
        }
        
        [UIValue("HRHigh")]
        public int HRHigh
        {
            get => PluginConfig.Instance.HRHigh;
            set
            {
                PluginConfig.Instance.HRHigh = value;
            }
        }
        
        [UIValue("HideDuringReplay")]
        public bool HideDuringReplay
        {
            get => PluginConfig.Instance.HideDuringReplay;
            set
            {
                PluginConfig.Instance.HideDuringReplay = value;
            }
        }
        
        [UIValue("source-list-options")]
        public List<object> options = Utils.Utils.DataSources;

        [UIValue("source-list-choice")]
        public string listChoice
        {
            get => PluginConfig.Instance.DataSource;
            set
            {
                PluginConfig.Instance.DataSource = value;
                UpdateText();
            }
        }
        
        [UIValue("PauseHR")]
        public int PauseHR
        {
            get => PluginConfig.Instance.PauseHR;
            set
            {
                PluginConfig.Instance.PauseHR = value;
            }
        }

        [UIValue("AutoPause")]
        public bool AutoPause
        {
            get => PluginConfig.Instance.AutoPause;
            set
            {
                PluginConfig.Instance.AutoPause = value;
            }
        }
        
        [UIComponent("modified-text")]
        private TextMeshProUGUI modifiedText;

        private void UpdateText()
        {
            modifiedText.text = Utils.Utils.GetCurrentSourceLinkText();
        }
        */
        
        [UIValue("data-source-info-text")]
        private string dataSourceInfo => Utils.Utils.GetCurrentSourceLinkText();

        [UIValue("data-source-text")]
        private string dataSource => $"Current DataSource: {PluginConfig.Instance.DataSource}";
    }
}