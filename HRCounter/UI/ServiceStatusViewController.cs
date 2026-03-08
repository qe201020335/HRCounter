using BeatSaberMarkupLanguage.Attributes;
using HRCounter.Web.HTTP;
using HRCounter.Web.OSC;
using TMPro;
using UnityEngine;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace HRCounter.UI;

[HotReload(RelativePathToLayout = @"BSML\serviceStatus.bsml")]
[ViewDefinition("HRCounter.UI.BSML.serviceStatus.bsml")]
internal class ServiceStatusViewController : BaseConfigViewController
{
    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly SimpleOscServer _oscServer = null!;

    [Inject]
    private readonly SimpleHttpServer _httpServer = null!;

    [UIComponent("http_status_text")]
    private TMP_Text _httpStatusText = null!;

    [UIComponent("osc_status_text")]
    private TMP_Text _oscStatusText = null!;

    [UIValue("EnableHttpServer")]
    private bool EnableHttpServer
    {
        get => Config.EnableHttpServer;
        set => Config.EnableHttpServer = value;
    }

    [UIValue("EnableOscServer")]
    private bool EnableOscServer
    {
        get => Config.EnableOscServer;
        set => Config.EnableOscServer = value;
    }

    protected override void OnParsed()
    {
        if (!Parse)
        {
            ((RectTransform)gameObject.transform).offsetMax = new Vector2(0, 22);
        }

        base.OnParsed();
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        _logger.Trace("ServiceStatusViewController DidActivate");
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

        _httpServer.StatusChanged += RefreshHttpStatus;
        _oscServer.StatusChanged += RefreshOscStatus;
    }

    protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
    {
        _logger.Trace("ServiceStatusViewController DidDeactivate");

        _httpServer.StatusChanged -= RefreshHttpStatus;
        _oscServer.StatusChanged -= RefreshOscStatus;
        base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
    }

    protected override void OnConfigChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(Config.EnableHttpServer):
                RefreshHttpStatus();
                break;
            case nameof(Config.EnableOscServer):
                RefreshOscStatus();
                break;
        }
    }

    protected override void RefreshNoBindUI()
    {
        RefreshHttpStatus();
        RefreshOscStatus();
    }

    [UIAction("RefreshStatus")]
    private void RefreshStatus()
    {
        RefreshHttpStatus();
        RefreshOscStatus();
    }

    private void RefreshHttpStatus()
    {
        if (!Parse) return;
        _httpStatusText.text = GetHttpStatusText();
    }

    private void RefreshOscStatus()
    {
        if (!Parse) return;
        _oscStatusText.text = GetOscStatusText();
    }

    private string GetHttpStatusText()
    {
        if (!Config.EnableHttpServer) return "HTTP server is disabled.";
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

        return $"HTTP server is enabled but <color=red>NOT listening</color>. Check logs for details.\n\n{_httpServer.ErrorMessage}";
    }

    private string GetOscStatusText()
    {
        if (!Config.EnableOscServer) return "OSC server is disabled.";
        if (_oscServer.IsListening)
        {
            return $"OSC server is <color=green>listening</color> on {_oscServer.EndPoint}";
        }

        return $"OSC server is enabled but <color=red>NOT listening</color>. Check logs for details.\n\n{_oscServer.ErrorMessage}";
    }
}
