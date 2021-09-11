using BeatSaberMarkupLanguage.Attributes;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.ViewControllers;
using HRCounter.Configuration;

namespace HRCounter.UI
{
    internal class SettingController : BSMLResourceViewController
    {
        public override string ResourceName => "HRCounter.Configuration.configMenu.bsml";

        [UIValue("LogHR")] public bool LogHR => PluginConfig.Instance.LogHR;

        [UIValue("Colorize")] public bool Colorize => PluginConfig.Instance.Colorize;

        [UIValue("HRLow")] public int HRLow => PluginConfig.Instance.HRLow;

        [UIValue("HRHigh")] public int HRHigh => PluginConfig.Instance.HRHigh;

        [UIValue("HideDuringReplay")] public bool HideDuringReplay => PluginConfig.Instance.HideDuringReplay;

        [UIValue("source-list-options")] public List<object> ListOptions => Utils.Utils.DataSources;

        [UIValue("source-list-choice")] public string ListChoice => PluginConfig.Instance.DataSource;

        [UIValue("PauseHR")] public int PauseHR => PluginConfig.Instance.PauseHR;

        [UIValue("AutoPause")] public bool AutoPause => PluginConfig.Instance.AutoPause;
        
        
    }
}