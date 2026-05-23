using System;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Data.DataSources;
using HRCounter.Web.OSC;
using JetBrains.Annotations;
using Zenject;

namespace HRCounter.Data.SourceDescriptors;

internal class OscDescriptor(PluginConfig config, SimpleOscServer oscServer) : IDisposableSourceDescriptor<OscHR>
{
    private const string KEY = "OSC Protocol";

    // changing addresses require soft restart to be in-effect, soft restart will also recreate the data source manager which will recreate this
    private readonly string _text = $"Use addresses below (one Int32 value only)\n  {string.Join("\n  ", config.OscAddress)}";

    public string Key => KEY;

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
    }

    public void Dispose()
    {
        oscServer.StatusChanged -= OnServerStatusChanged;
    }

    private void OnServerStatusChanged()
    {
        StatusChanged?.Invoke();
    }

    public Task<string> GetStatusText(CancellationToken cancellationToken) =>
        Task.FromResult(oscServer.IsListening ? _text : "<color=#FF0000>OSC Server is NOT listening!</color>");

    public bool PreconditionMet() => true;
}
