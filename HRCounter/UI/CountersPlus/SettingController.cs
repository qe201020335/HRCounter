using BeatSaberMarkupLanguage.Attributes;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Utils;

namespace HRCounter.UI.CountersPlus
{
    // setting controller for Counters+ counter configuration page
    internal class SettingController
    {
        [UIValue("data-source-info-text")]
        private string dataSourceInfo
        {
            get
            {
                Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(100);
                    var source = DataSourceType.GetFromStr(PluginConfig.Instance.DataSource);
                    _tmpText.text = source == null 
                        ? "Unknown Data Source"
                        : await source.GetSourceLinkText();
                });

                return "Loading Current Data Source Info...";
            }
        }

        [UIComponent("data-source-info-text")] private TextMeshProUGUI _tmpText;

        [UIValue("data-source-text")]
        private string dataSource => $"Current DataSource: {PluginConfig.Instance.DataSource}";
    }
}