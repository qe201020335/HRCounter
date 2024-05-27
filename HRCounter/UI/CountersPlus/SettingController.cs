using BeatSaberMarkupLanguage.Attributes;
using System.Threading.Tasks;
using TMPro;
using HRCounter.Configuration;
using HRCounter.Data;
using Zenject;

namespace HRCounter.UI.CountersPlus
{
    // setting controller for Counters+ counter configuration page
    internal class SettingController
    {
        [Inject]
        private readonly PluginConfig _config = null!;
        
        [UIValue("data-source-info-text")]
        private string dataSourceInfo
        {
            get
            {
                Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(100);
                    _tmpText.text = DataSourceManager.TryGetFromKey(_config.DataSource, out var source) 
                        ? await source.GetSourceLinkText()
                        : "Unknown Data Source";
                });

                return "Loading Current Data Source Info...";
            }
        }

        [UIComponent("data-source-info-text")] private TextMeshProUGUI _tmpText;

        [UIValue("data-source-text")]
        private string dataSource => $"Current DataSource: {_config.DataSource}";
    }
}