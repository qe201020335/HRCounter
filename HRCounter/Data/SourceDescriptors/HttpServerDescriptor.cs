using System;
using System.Threading;
using System.Threading.Tasks;
using BGLib.Polyglot;
using HRCounter.Data.DataSources;
using HRCounter.Web.HTTP;
using JetBrains.Annotations;
using Zenject;

namespace HRCounter.Data.SourceDescriptors;

internal class HttpServerDescriptor(SimpleHttpServer httpServer) : IDisposableSourceDescriptor<HttpServerDataSource>
{
    private const string KEY = "HttpServer";

    public string Key => KEY;

    public string? Name => null;

    public string? NameKey => "HRCOUNTER_HTTP_SOURCE_DESCRIPTOR_NAME";

    public bool StreamerMode
    {
        set { }
    }

    public event Action? StatusChanged;

    [Inject]
    [UsedImplicitly]
    private void Init()
    {
        httpServer.StatusChanged += OnServerStatusChanged;
    }

    public void Dispose()
    {
        httpServer.StatusChanged -= OnServerStatusChanged;
    }

    private void OnServerStatusChanged()
    {
        StatusChanged?.Invoke();
    }

    public Task<string> GetStatusText(CancellationToken cancellationToken) =>
        Task.FromResult(httpServer.IsListening
            ? Localization.Get("HRCOUNTER_HTTP_SOURCE_DESCRIPTOR_LISTENING")
            : Localization.Get("HRCOUNTER_HTTP_SOURCE_DESCRIPTOR_NOT_LISTENING"));

    public bool PreconditionMet() => true;
}
