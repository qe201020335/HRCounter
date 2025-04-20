using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HRCounter.Data;
using HRCounter.Utils.Converters;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace HRCounter.Configuration;

internal class PluginConfig
{
    [Obsolete("Try using DI instead.")]
    public static PluginConfig Instance { get; set; }

    public static PluginConfig DefaultValues { get; } = new();

    #region private backing fields

    private bool _modEnable = true;
    private bool _logHR = false;
    private string _dataSource = DataSourceManager.YURMod.Key;
    private string _pulsoidToken = "";
    private string _hypeRateSessionID = "";
    private string _pulsoidWidgetID = "";
    private string _fitbitWebSocket = "";
    private string _hrProxyID = "";
    private string _feedLink = "";
    private bool _noBloom = false;
    private bool _colorize = true;
    private bool _hideDuringReplay = true;
    private int _hrLow = 120;
    private int _hrHigh = 180;
    private Color _lowColor = new(0, 1, 0); // default to green
    private Color _midColor = new(1, 1, 0); // default to yellow
    private Color _highColor = new(1, 0, 0); // default to red
    private int _pauseHR = 200;
    private bool _autoPause = false;
    private bool _ignoreCountersPlus = false;
    private bool _debugSpam = false;
    private Vector3 _staticCounterPosition = new(0f, 1.2f, 7f);
    private bool _enableHttpServer = true;
    private bool _enableOscServer = true;
    private IPAddress _oscBindIp = IPAddress.Any;
    private int _oscPort = 9000;

    private List<string> _oscAddress =
        ["/hr", "/avatar/parameters/HR", "/avatar/parameters/HeartRateInt", "/avatar/parameters/Heartrate3", "/avatar/parameters/HeartRateBPM"];

    private bool _httpLocalOnly = true;
    private int _httpPort = 65302;

    private string _customIcon = "";

    #endregion

    // Must be 'virtual' if you want BSIPA to detect a value change and save the config automatically.

    public virtual bool ModEnable
    {
        get => _modEnable;
        set => _modEnable = value;
    }

    public virtual bool LogHR
    {
        get => _logHR;
        set => _logHR = value;
    }

    public virtual string DataSource
    {
        get => _dataSource;
        set => _dataSource = DataSourceManager.MigrateKey(value);
    }

    public virtual string PulsoidToken
    {
        get => _pulsoidToken;
        set => _pulsoidToken = value;
    }

    public virtual string HypeRateSessionID
    {
        get => _hypeRateSessionID;
        set => _hypeRateSessionID = value;
    }

    public virtual string PulsoidWidgetID
    {
        get => _pulsoidWidgetID;
        set => _pulsoidWidgetID = value;
    }

    public virtual string FitbitWebSocket
    {
        get => _fitbitWebSocket;
        set => _fitbitWebSocket = value;
    }

    public virtual string HRProxyID
    {
        get => _hrProxyID;
        set => _hrProxyID = value;
    }

    public virtual string FeedLink
    {
        get => _feedLink;
        set => _feedLink = value;
    }

    public virtual bool NoBloom
    {
        get => _noBloom;
        set => _noBloom = value;
    }

    public virtual bool Colorize
    {
        get => _colorize;
        set => _colorize = value;
    }

    public virtual bool HideDuringReplay
    {
        get => _hideDuringReplay;
        set => _hideDuringReplay = value;
    }

    public virtual int HRLow
    {
        get => _hrLow;
        set => _hrLow = value;
    }

    public virtual int HRHigh
    {
        get => _hrHigh;
        set => _hrHigh = value;
    }

    [UseConverter(typeof(HexColorConverter))]
    [JsonConverter(typeof(WrappedTextValueJsonConverter<Color, HexColorConverter>))]
    public virtual Color LowColor
    {
        get => _lowColor;
        set => _lowColor = value;
    }

    [UseConverter(typeof(HexColorConverter))]
    [JsonConverter(typeof(WrappedTextValueJsonConverter<Color, HexColorConverter>))]
    public virtual Color MidColor
    {
        get => _midColor;
        set => _midColor = value;
    }

    [UseConverter(typeof(HexColorConverter))]
    [JsonConverter(typeof(WrappedTextValueJsonConverter<Color, HexColorConverter>))]
    public virtual Color HighColor
    {
        get => _highColor;
        set => _highColor = value;
    }

    public virtual int PauseHR
    {
        get => _pauseHR;
        set => _pauseHR = value;
    }

    public virtual bool AutoPause
    {
        get => _autoPause;
        set => _autoPause = value;
    }

    public virtual bool IgnoreCountersPlus
    {
        get => _ignoreCountersPlus;
        set => _ignoreCountersPlus = value;
    }

    public virtual bool DebugSpam
    {
        get => _debugSpam;
        set => _debugSpam = value;
    }

    [JsonConverter(typeof(Vector3JsonConverter))]
    public virtual Vector3 StaticCounterPosition
    {
        get => _staticCounterPosition;
        set => _staticCounterPosition = value;
    }

    public virtual bool EnableHttpServer
    {
        get => _enableHttpServer;
        set => _enableHttpServer = value;
    }

    public virtual bool EnableOscServer
    {
        get => _enableOscServer;
        set => _enableOscServer = value;
    }

    [JsonConverter(typeof(WrappedTextValueJsonConverter<IPAddress, IPAddressValueConverter>))]
    [UseConverter(typeof(IPAddressValueConverter))]
    public virtual IPAddress OscBindIP
    {
        get => _oscBindIp;
        set => _oscBindIp = value;
    }

    public virtual int OscPort
    {
        get => _oscPort;
        set => _oscPort = value;
    }

    [UseConverter(typeof(ListConverter<string>))]
    public virtual List<string> OscAddress
    {
        get => _oscAddress;
        set
        {
            if (value == null) // the json value can be null
            {
                value = [];
            }

            _oscAddress = value;
        }
    }

    public virtual bool HttpLocalOnly
    {
        get => _httpLocalOnly;
        set => _httpLocalOnly = value;
    }

    public virtual int HttpPort
    {
        get => _httpPort;
        set => _httpPort = value;
    }

    public virtual string CustomIcon
    {
        get => _customIcon;
        set => _customIcon = value;
    }

    internal event Action? OnSettingsChanged;

    private void RaiseSettingsChanged()
    {
        Plugin.Logger.Trace("HRCounter Settings Changed!");
        Task.Factory.StartNew(() =>
        {
            try
            {
                var e = OnSettingsChanged;
                e?.Invoke();
            }
            catch (Exception e)
            {
                Plugin.Logger.Critical($"Exception Caught while broadcasting settings changed event: {e.Message}");
                Plugin.Logger.Critical(e);
            }
        });
    }

    [UsedImplicitly]
    public virtual void Changed()
    {
        RaiseSettingsChanged();
    }

    [UsedImplicitly]
    public virtual void OnReload()
    {
        RaiseSettingsChanged();
    }

    internal PluginConfig GetColdCopy()
    {
        return new PluginConfig
        {
            _modEnable = _modEnable,
            _logHR = _logHR,
            _dataSource = _dataSource,
            _pulsoidToken = _pulsoidToken,
            _hypeRateSessionID = _hypeRateSessionID,
            _pulsoidWidgetID = _pulsoidWidgetID,
            _fitbitWebSocket = _fitbitWebSocket,
            _hrProxyID = _hrProxyID,
            _feedLink = _feedLink,
            _noBloom = _noBloom,
            _colorize = _colorize,
            _hideDuringReplay = _hideDuringReplay,
            _hrLow = _hrLow,
            _hrHigh = _hrHigh,
            _lowColor = _lowColor,
            _midColor = _midColor,
            _highColor = _highColor,
            _pauseHR = _pauseHR,
            _autoPause = _autoPause,
            _ignoreCountersPlus = _ignoreCountersPlus,
            _debugSpam = _debugSpam,
            _staticCounterPosition = _staticCounterPosition,
            _enableHttpServer = _enableHttpServer,
            _enableOscServer = _enableOscServer,
            _oscBindIp = _oscBindIp,
            _oscPort = _oscPort,
            _oscAddress = _oscAddress,
            _httpLocalOnly = _httpLocalOnly,
            _httpPort = _httpPort,
            _customIcon = _customIcon
        };
    }

    internal void CopyFrom(PluginConfig other)
    {
        _modEnable = other._modEnable;
        _logHR = other._logHR;
        _dataSource = other._dataSource;
        _pulsoidToken = other._pulsoidToken;
        _hypeRateSessionID = other._hypeRateSessionID;
        _pulsoidWidgetID = other._pulsoidWidgetID;
        _fitbitWebSocket = other._fitbitWebSocket;
        _hrProxyID = other._hrProxyID;
        _feedLink = other._feedLink;
        _noBloom = other._noBloom;
        _colorize = other._colorize;
        _hideDuringReplay = other._hideDuringReplay;
        _hrLow = other._hrLow;
        _hrHigh = other._hrHigh;
        _lowColor = other._lowColor;
        _midColor = other._midColor;
        _highColor = other._highColor;
        _pauseHR = other._pauseHR;
        _autoPause = other._autoPause;
        _ignoreCountersPlus = other._ignoreCountersPlus;
        _debugSpam = other._debugSpam;
        _staticCounterPosition = other._staticCounterPosition;
        _enableHttpServer = other._enableHttpServer;
        _enableOscServer = other._enableOscServer;
        _oscBindIp = other._oscBindIp;
        _oscPort = other._oscPort;
        _oscAddress = other._oscAddress;
        _httpLocalOnly = other._httpLocalOnly;
        _httpPort = other._httpPort;
        _customIcon = other._customIcon;
        Changed();
    }
}
