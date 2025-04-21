using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using HRCounter.Configuration;
using HRCounter.Web.HTTP;
using HRCounter.Web.OSC;
using IPA.Logging;
using IPA.Utilities.Async;
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
    private readonly Logger _logger = null!;

    [Inject]
    private readonly SimpleOscServer _oscServer = null!;

    [Inject]
    private readonly SimpleHttpServer _httpServer = null!;

    private bool _parsed = false;

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
        _parsed = true;
        RefreshStatus();
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        _logger.Trace("ServiceStatusViewController DidActivate");
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

        if (_parsed)
        {
            RefreshStatus();
        }

        _config.OnSettingsChanged += RequestSettingRefresh;
        _httpServer.StatusChanged += RefreshHttpStatus;
        _oscServer.StatusChanged += RefreshOscStatus;
    }

    protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
    {
        _logger.Trace("ServiceStatusViewController DidDeactivate");
        base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

        _config.OnSettingsChanged -= RequestSettingRefresh;
        _httpServer.StatusChanged -= RefreshHttpStatus;
        _oscServer.StatusChanged -= RefreshOscStatus;
    }

    private void RequestSettingRefresh()
    {
        UnityMainThreadTaskScheduler.Factory.StartNew(() => { NotifyPropertyChanged(null); });
    }

    [UIAction("RefreshStatus")]
    private void RefreshStatus()
    {
        if (!_parsed) return;
        RefreshHttpStatus();
        RefreshOscStatus();
    }

    private void RefreshHttpStatus()
    {
        if (!_parsed) return;
        _httpStatusText.text = GetHttpStatusText();
    }

    private void RefreshOscStatus()
    {
        if (!_parsed) return;
        _oscStatusText.text = GetOscStatusText();
    }

    private string GetHttpStatusText()
    {
        if (!_config.EnableHttpServer) return "HTTP server is disabled.";
        if (_httpServer.IsListening)
        {
            if (_httpServer.IsLocalOnly)
            {
                return $"HTTP server is <color=green>listening</color> on localhost:{_httpServer.Port}\n" +
                       $"Requests must be sent to http://localhost:{_httpServer.Port}";
            }

            return $"HTTP Server is <color=green>listening</color> on all interfaces\n" +
                   $"Requests must be sent to http://<ip>:{_httpServer.Port}";
        }

        return "HTTP server is enabled but <color=red>NOT listening</color>\nCheck logs for details.";
    }

    private string GetOscStatusText()
    {
        if (!_config.EnableOscServer) return "OSC server is disabled.";
        if (_oscServer.IsListening)
        {
            return $"OSC server is <color=green>listening</color> on {_oscServer.EndPoint}";
        }

        return $"OSC server is enabled but <color=red>NOT listening</color>\n{_oscServer.ErrorMessage}\nCheck logs for details.";
    }
}
