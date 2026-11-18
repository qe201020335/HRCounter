using System;
using System.Collections.Generic;
using System.ComponentModel;
using HRCounter.Configuration;
using HRCounter.Data.DataSources.Base;
using HRCounter.Web.OSC;
using IPA.Logging;
using Zenject;

namespace HRCounter.Data.DataSources;

internal class OscHR : DataSource, IOscMessageHandler
{
    [Inject]
    private readonly Logger _logger = null!;
    
    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly IOscServer _oscServer = null!;

    public IReadOnlyList<string> Addresses => _config.OscAddress;

    protected override void Start()
    {
        _oscServer.RegisterHandler(this);
        _config.PropertyChanged += OnConfigChanged;
    }

    protected override void Stop()
    {
        _config.PropertyChanged -= OnConfigChanged;
        _oscServer.UnregisterHandler(this);
    }

    private void OnConfigChanged(object _, PropertyChangedEventArgs args)
    {
        if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(_config.OscAddress))
        {
            _oscServer.UpdateHandler(this);
        }
    }

    public void HandleMessage(ReadOnlySpan<byte> arguments, ReadOnlySpan<byte> data)
    {
        if (arguments.Length != 1 || arguments[0] != 'i')
        {
            return;
        }

        if (OscHelper.TryReadInt32(ref data, out var number))
        {
            OnHeartRateDataReceived(number);
        }
        else
        {
            _logger.Warn("Failed to parse heart rate");
        }
    }
}
