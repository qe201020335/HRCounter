using BeatSaberMarkupLanguage.Attributes;
using BGLib.Polyglot;
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
        if (!Config.EnableHttpServer) return Localization.Get("HRCOUNTER_SERVICE_STATUS_MENU_HTTP_STATUS_DISABLED");
        if (_httpServer.IsListening)
        {
            if (_httpServer.IsLocalOnly)
            {
                return Localization.Instance.GetFormatOrKey("HRCOUNTER_SERVICE_STATUS_MENU_HTTP_STATUS_LOCALHOST", _httpServer.Port);
            }

            return Localization.Instance.GetFormatOrKey("HRCOUNTER_SERVICE_STATUS_MENU_HTTP_STATUS_ALL_INTERFACES", _httpServer.Port);
        }

        return $"{Localization.Get("HRCOUNTER_SERVICE_STATUS_MENU_HTTP_STATUS_ERROR")}\n" +
               $"{Localization.Get("HRCOUNTER_COMMON_CHECK_LOGS")}\n\n{_httpServer.ErrorMessage}";
    }

    private string GetOscStatusText()
    {
        if (!Config.EnableOscServer) return Localization.Get("HRCOUNTER_SERVICE_STATUS_MENU_OSC_STATUS_DISABLED");
        if (_oscServer.IsListening)
        {
            return Localization.Instance.GetFormatOrKey("HRCOUNTER_SERVICE_STATUS_MENU_OSC_STATUS_LISTENING",
                _oscServer.EndPoint?.ToString() ?? "null");
        }

        return $"{Localization.Get("HRCOUNTER_SERVICE_STATUS_MENU_OSC_STATUS_ERROR")}\n" +
               $"{Localization.Get("HRCOUNTER_COMMON_CHECK_LOGS")}\n\n{_oscServer.ErrorMessage}";
    }
}
