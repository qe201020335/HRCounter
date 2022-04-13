using BeatSaberMarkupLanguage.Attributes;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.ViewControllers;
using HRCounter.Configuration;
using TMPro;

namespace HRCounter.UI
{
    // setting controller for menu button
    [HotReload(RelativePathToLayout = @"BSML\configMenu.bsml")]
    [ViewDefinition("HRCounter.UI.BSML.configMenu.bsml")]
    internal class SettingMenuController : BSMLAutomaticViewController
    {

        private static PluginConfig Config => PluginConfig.Instance;

        [UIValue("LogHR")]
        public bool LogHR
        {
            get => Config.LogHR;
            set => Config.LogHR = value;
        }
        

        [UIValue("Colorize")]
        public bool Colorize
        {
            get => Config.Colorize;
            set => Config.Colorize = value;
        }

        [UIValue("HRLow")]
        public int HRLow
        {
            get => Config.HRLow;
            set => Config.HRLow = value;
        }
        
        [UIValue("HRHigh")]
        public int HRHigh
        {
            get => Config.HRHigh;
            set => Config.HRHigh = value;
        }

        [UIValue("color-info-text")]
        public string ColorInfoText =>
            $"Current Colors: <color={Config.LowColor}>Low</color> -> <color={Config.MidColor}>Middle</color> -> <color={Config.HighColor}>High</color>";
        
        [UIValue("HideDuringReplay")]
        public bool HideDuringReplay
        {
            get => Config.HideDuringReplay;
            set => Config.HideDuringReplay = value;
        }
        
        [UIValue("source-list-options")]
        public List<object> options = Utils.Utils.DataSources;

        [UIValue("source-list-choice")]
        public string listChoice
        {
            get => Config.DataSource;
            set
            {
                Config.DataSource = value;
                UpdateText();
            }
        }
        
        [UIValue("PauseHR")]
        public int PauseHR
        {
            get => Config.PauseHR;
            set => Config.PauseHR = value;
        }

        [UIValue("AutoPause")]
        public bool AutoPause
        {
            get => Config.AutoPause;
            set => Config.AutoPause = value;
        }

        [UIValue("IgnoreCounters+")]
        public bool UseCountersPlus
        {
            get => Config.IgnoreCountersPlus;
            set => Config.IgnoreCountersPlus = value;
        }
        
        [UIComponent("data-source-info-text")]
        private TextMeshProUGUI modifiedText;

        [UIValue("data-source-info-text")]
        private string dataSourceInfo = Utils.Utils.GetCurrentSourceLinkText();

        private void UpdateText()
        {
            modifiedText.text = Utils.Utils.GetCurrentSourceLinkText();
            dataSourceInfo = Utils.Utils.GetCurrentSourceLinkText();
        }
        
    }
}