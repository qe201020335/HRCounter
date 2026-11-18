using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HRCounter.Configuration;
using IPA.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Zenject;

namespace HRCounter.Web.HTTP.Handlers;

internal class HttpConfigHandler : IHttpRouteHandler, IInitializable, IDisposable
{
    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly IHttpServer _server = null!;

    private readonly JsonSerializer _serializer = new() { ObjectCreationHandling = ObjectCreationHandling.Replace };

    public Tuple<string, HttpMethod>[] Routes { get; } = [new("/config", HttpMethod.Get), new("/config", HttpMethod.Post)];

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

        if (!_config.HttpAllowConfig)
        {
            context.Forbidden();
            return;
        }

        if (request.HttpMethod == HttpMethod.Get.Method)
        {
            var responseString = JObject.FromObject(_config.GetColdCopy()).ToString();
            await context.SendJsonResponseAsync(responseString);
        }
        else if (request.HttpMethod == HttpMethod.Post.Method)
        {
            try
            {
                var coldCopy = _config.GetColdCopy();
                using TextReader reader = new StreamReader(request.InputStream);
                _serializer.Populate(reader, coldCopy);
                _logger.Debug("Copying config values");
                _config.CopyFrom(coldCopy);
            }
            catch (JsonException e)
            {
                _logger.Warn("Failed to parse request body");
                _logger.Warn(e);
                context.BadRequest();
                return;
            }
            catch (Exception e)
            {
                _logger.Error("Failed to update config");
                _logger.Error(e);
                await context.InternalServerErrorAsync(e.Message);
                return;
            }

            var responseString = JObject.FromObject(_config.GetColdCopy()).ToString();
            await context.SendJsonResponseAsync(responseString);
        }
        else
        {
            context.MethodNotAllowed();
        }
    }
}
