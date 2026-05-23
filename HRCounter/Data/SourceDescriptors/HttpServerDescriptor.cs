using System;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Data.DataSources;
using HRCounter.Web.HTTP;
using JetBrains.Annotations;
using Zenject;

namespace HRCounter.Data.SourceDescriptors;

internal class HttpServerDescriptor(SimpleHttpServer httpServer) : IDisposableSourceDescriptor<HttpServerDataSource>
{
    private const string KEY = "HttpServer";

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
            ? "POST to the <color=#00FF00>/hr</color> endpoint"
            : "<color=#FF0000>HTTP Server is NOT listening!</color>");

    public bool PreconditionMet() => true;
}
