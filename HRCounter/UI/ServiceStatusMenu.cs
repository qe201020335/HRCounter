using BeatSaberMarkupLanguage.Attributes;
using HRCounter.Web.HTTP;
using HRCounter.Web.OSC;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace HRCounter.UI;

[HotReload(RelativePathToLayout = @"BSML\serviceStatus.bsml")]
[ViewDefinition("HRCounter.UI.BSML.serviceStatus.bsml")]
internal class ServiceStatusMenu : BaseConfigViewController
{
    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly SimpleOscServer _oscServer = null!;

    [Inject]
    private readonly SimpleHttpServer _httpServer = null!;

    [UIValue(nameof(Config.EnableHttpServer))]
    public bool EnableHttpServer
    {
        get => Config.EnableHttpServer;
        set
        {
            if (Config.EnableHttpServer != value) Config.EnableHttpServer = value;
        }
    }

    [UIValue(nameof(HttpStatusText))]
    public string HttpStatusText
    {
        get;
        private set
        {
            field = value;
            NotifyPropertyChanged();
        }
    } = "";

    [UIValue(nameof(Config.EnableOscServer))]
    public bool EnableOscServer
    {
        get => Config.EnableOscServer;
        set
        {
            if (Config.EnableOscServer != value) Config.EnableOscServer = value;
        }
    }

    [UIValue(nameof(OscStatusText))]
    public string OscStatusText
    {
        get;
        private set
        {
            field = value;
            NotifyPropertyChanged();
        }
    } = "";

    protected override void OnParsed()
    {
        if (!Parsed)
        {
            ((RectTransform)gameObject.transform).offsetMax = new Vector2(0, 22);
        }

        base.OnParsed();
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        _logger.Trace("ServiceStatusMenu DidActivate");
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

        _httpServer.StatusChanged += RefreshHttpStatus;
        _oscServer.StatusChanged += RefreshOscStatus;
    }

    protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
    {
        _logger.Trace("ServiceStatusMenu DidDeactivate");

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

    protected override void RefreshUI()
    {
        RefreshHttpStatus();
        RefreshOscStatus();
    }

    [UIAction(nameof(RefreshStatus))]
    [UsedImplicitly]
    private void RefreshStatus()
    {
        RefreshHttpStatus();
        RefreshOscStatus();
    }

    private void RefreshHttpStatus()
    {
        HttpStatusText = GetHttpStatusText();
    }

    private void RefreshOscStatus()
    {
        OscStatusText = GetOscStatusText();
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
