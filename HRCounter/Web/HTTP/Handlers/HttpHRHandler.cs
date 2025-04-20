using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HRCounter.Data;

namespace HRCounter.Web.HTTP.Handlers;

internal class HttpHRHandler : IHttpRouteHandler
{
    public Tuple<string, HttpMethod>[] Routes { get; } =
        [new("/hr", HttpMethod.Get), new("/hr", HttpMethod.Post)];

    internal event EventHandler<int>? HeartRatePosted;

    public async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        if (request.HttpMethod == HttpMethod.Get.Method)
        {
            var s = BPM.Bpm.ToString();
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
                _ = Task.Run(() =>
                {
                    var e = HeartRatePosted;
                    e?.Invoke(this, number);
                });

                await context.SendResponseAsync("OK");
            }
            else
            {
                context.BadRequest();
            }
        }
    }
}
