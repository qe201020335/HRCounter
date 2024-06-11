using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HRCounter.Data;

namespace HRCounter.Web;

internal class HttpHRHandler : IHttpRouteHandler
{
    public Tuple<string, HttpMethod>[] Routes { get; } =
        [new Tuple<string, HttpMethod>("/hr", HttpMethod.Get), new Tuple<string, HttpMethod>("/hr", HttpMethod.Post)];
    
    internal event EventHandler<int>? HeartRatePosted;

    public void HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        if (request.HttpMethod == HttpMethod.Get.Method)
        {
            var s = BPM.Bpm.ToString();
            context.SendResponse(s);
        }
        else if (request.HttpMethod == HttpMethod.Post.Method)
        {
            using var reader = new StreamReader(request.InputStream);
            var body = reader.ReadToEnd();
            if (int.TryParse(body, out var number))
            {
                Task.Run(() =>
                {
                    var e = HeartRatePosted;
                    e?.Invoke(this, number);
                });
                context.SendResponse("OK");
            }
            else
            {
                context.BadRequest();
            }
        }
    }
}