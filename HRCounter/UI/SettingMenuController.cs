using BeatSaberMarkupLanguage.Attributes;
using System.Collections.Generic;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.ViewControllers;
using HRCounter.Configuration;
using HRCounter.Data;
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
            $"Current Colors: <color={ColorUtility.ToHtmlStringRGBA(_config.LowColor)}>Low</color> -> <color={ColorUtility.ToHtmlStringRGBA(_config.MidColor)}>Middle</color> -> <color={ColorUtility.ToHtmlStringRGBA(_config.HighColor)}>High</color>";
        
        [UIValue("HideDuringReplay")]
        public bool HideDuringReplay
        {
            get => _config.HideDuringReplay;
            set => _config.HideDuringReplay = value;
        }
        
        [UIValue("source-list-options")]
        public List<object> options = new List<object>(DataSourceManager.DataSourceTypes.Keys);

        [UIValue("source-list-choice")]
        public string listChoice
        {
            get => _config.DataSource;
            set
            {
                _config.DataSource = value;
                UpdateText();
            }
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
        private TextMeshProUGUI modifiedText;

        [UIValue("data-source-info-text")]
        private string dataSourceInfo
        {
            get
            {
                Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(100);
                    UpdateText();
                });
                return "Loading Data Source Info...";
            }
        }

        private async void UpdateText()
        {
            modifiedText.text = "Loading Data Source Info...";
            await Task.Delay(100);
            modifiedText.text = DataSourceManager.TryGetFromStr(_config.DataSource, out var source) 
                ? await source.GetSourceLinkText()
                : "Unknown Data Source";
        }
        
        [UIValue("NoBloom")]
        private bool NoBloom
        {
            get => _config.NoBloom;
            set => _config.NoBloom = value;
        }
        
    }
}