using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using HRCounter.Configuration;
using SiraUtil.Logging;
using TMPro;
using Zenject;

namespace HRCounter.UI;

[HotReload(RelativePathToLayout = @"BSML\serviceStatus.bsml")]
[ViewDefinition("HRCounter.UI.BSML.serviceStatus.bsml")]
internal class ServiceStatusViewController : BSMLAutomaticViewController
{
    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly SiraLog _logger = null!;

    [UIComponent("http_status_text")]
    private TMP_Text _httpStatusText = null!;

    [UIComponent("osc_status_text")]
    private TMP_Text _oscStatusText = null!;

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

    [UIAction("#post-parse")]
    private void OnParsed()
    {
        _httpStatusText.text = "some text";
        _oscStatusText.text = "some other text";
    }
}
