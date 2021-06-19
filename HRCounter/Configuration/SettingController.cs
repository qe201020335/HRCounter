using BeatSaberMarkupLanguage.Attributes;

namespace HRCounter.Configuration
{
    internal  class SettingController
    {
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
    }
}