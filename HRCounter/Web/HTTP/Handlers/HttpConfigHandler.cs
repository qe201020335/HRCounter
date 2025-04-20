﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HRCounter.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SiraUtil.Logging;
using Zenject;

namespace HRCounter.Web.HTTP.Handlers;

internal class HttpConfigHandler : IHttpRouteHandler
{
    [Inject]
    private readonly PluginConfig _config;

    [Inject]
    private readonly SiraLog _logger;

    private readonly JsonSerializer _serializer = new();

    public Tuple<string, HttpMethod>[] Routes { get; } =
        [new("/config", HttpMethod.Get), new("/config", HttpMethod.Post)];

    public async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;

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
                _logger.Critical("Failed to parse request body");
                _logger.Critical(e);
                context.BadRequest();
                return;
            }
            catch (Exception e)
            {
                _logger.Critical("Failed to update config");
                _logger.Critical(e);
                await context.InternalServerErrorAsync(e);
                return;
            }

            var responseString = JObject.FromObject(_config.GetColdCopy()).ToString();
            await context.SendJsonResponseAsync(responseString);
        }
        else
        {
            context.BadMethod();
        }
    }
}
