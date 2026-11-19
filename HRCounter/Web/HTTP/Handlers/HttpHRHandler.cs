using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HRCounter.Data;
using Zenject;

namespace HRCounter.Web.HTTP.Handlers;

internal class HttpHRHandler : IHttpRouteHandler, IInitializable, IDisposable
{
    [Inject]
    private readonly IHttpServer _server = null!;

    public Tuple<string, HttpMethod>[] Routes { get; } = [new("/hr", HttpMethod.Get), new("/hr", HttpMethod.Post)];

    internal event Action<int>? HeartRatePosted;

    void IInitializable.Initialize()
    {
        _server.RegisterHandler(this);
    }

    void IDisposable.Dispose()
    {
        _server.UnregisterHandler(this);
    }

    public async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        if (request.HttpMethod == HttpMethod.Get.Method)
        {
            var s = HRDataManager.CurrentBpm.ToString();
            await context.SendResponseAsync(s);
        }
        else if (request.HttpMethod == HttpMethod.Post.Method)
        {
            using var reader = new StreamReader(request.InputStream);
            var buffer = new char[16];
            var numChars = await reader.ReadAsync(buffer, 0, 16);
            var truncated = new string(buffer, 0, numChars);
            if (int.TryParse(truncated, out var number))
            {
                var e = HeartRatePosted;
                e?.Invoke(number);

                await context.SendResponseAsync("OK");
            }
            else
            {
                context.BadRequest();
            }
        }
    }
}
