using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using BGLib.Polyglot;
using HRCounter.Configuration;
using HRCounter.Data.DataSources;
using HRCounter.Web.OSC;
using JetBrains.Annotations;
using Zenject;

namespace HRCounter.Data.SourceDescriptors;

internal class OscDescriptor(PluginConfig config, IOscServer oscServer) : IDisposableSourceDescriptor<OscHR>
{
    private const string KEY = "OSC Protocol";

    public string Key => KEY;

    public string? Name => null;

    public string? NameKey => "HRCOUNTER_OSC_SOURCE_DESCRIPTOR_NAME";

    public bool StreamerMode
    {
        set { }
    }

    public event Action? StatusChanged;

    [Inject]
    [UsedImplicitly]
    private void Init()
    {
        oscServer.StatusChanged += OnServerStatusChanged;
        config.PropertyChanged += OnConfigChanged;
    }

    public void Dispose()
    {
        oscServer.StatusChanged -= OnServerStatusChanged;
        config.PropertyChanged -= OnConfigChanged;
    }

    private void OnConfigChanged(object? _, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(PluginConfig.OscAddress))
        {
            StatusChanged?.Invoke();
        }
    }

    private void OnServerStatusChanged()
    {
        StatusChanged?.Invoke();
    }

    public Task<string> GetStatusText(CancellationToken cancellationToken) =>
        Task.FromResult(oscServer.IsListening
            ? Localization.Get("HRCOUNTER_OSC_SOURCE_DESCRIPTOR_ADDRESSES") + "\n  " + string.Join("\n  ", config.OscAddress)
            : Localization.Get("HRCOUNTER_OSC_SOURCE_DESCRIPTOR_NOT_LISTENING"));

    public bool PreconditionMet() => true;
}
