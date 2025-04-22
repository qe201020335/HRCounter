using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Web.HTTP.Handlers;
using IPA.Logging;
using IPA.Utilities.Async;
using Zenject;

namespace HRCounter.Web.HTTP;

internal class SimpleHttpServer : IInitializable, IDisposable
{
    private readonly PluginConfig _config;
    private readonly Logger _logger;
    private readonly HttpListener _listener = new();

    /**
     * Path -> Method -> Handler
     */
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<HttpMethod, IHttpRouteHandler>> _handlers;

    public event Action? StatusChanged;

    public bool IsLocalOnly { get; }
    public int Port { get; }
    public bool IsListening { get; private set; } = false;
    public string? ErrorMessage { get; private set; }

    internal SimpleHttpServer(PluginConfig config, Logger logger, IHttpRouteHandler[] handlers)
    {
        _config = config;
        _logger = logger;
        IsLocalOnly = config.HttpLocalOnly;
        Port = config.HttpPort;

        var domain = IsLocalOnly ? "localhost" : "+";
        var url = $"http://{domain}:{Port}/";
        _listener.Prefixes.Add(url);

        var handlersDict = new Dictionary<string, Dictionary<HttpMethod, IHttpRouteHandler>>();
        foreach (var handler in handlers)
        foreach (var (path, method) in handler.Routes)
        {
            if (!handlersDict.TryGetValue(NormalizedPath(path), out var methods))
            {
                methods = new Dictionary<HttpMethod, IHttpRouteHandler>();
                handlersDict[path] = methods;
            }

            methods.Add(method, handler); // Will throw if duplicate routes with the same method
        }

        _handlers = handlersDict
            .Select(pair => new KeyValuePair<string, IReadOnlyDictionary<HttpMethod, IHttpRouteHandler>>(pair.Key, pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value); // make it read-only
    }

    public void Initialize()
    {
        UpdateListener();
        _config.OnSettingsChanged += UpdateListener;
    }

    public void Dispose()
    {
        _config.OnSettingsChanged -= UpdateListener;
        StopListener();
        _listener.Close();
    }

    private void UpdateListener()
    {
        if (_config.EnableHttpServer && !IsListening)
        {
            StartListener();
        }
        else if (!_config.EnableHttpServer && IsListening)
        {
            StopListener();
        }
    }

    private void StartListener()
    {
        _logger.Debug("Starting listener");
        if (IsListening)
        {
            _logger.Warn("Listener already started");
            return;
        }

        try
        {
            _listener.Start();
            IsListening = true;
            Task.Run(GetAndProcessRequests);
            InvokeStatusChanged();
        }
        catch (SocketException e)
        {
            if (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                _logger.Error("The HTTP port is already in use.");
                _logger.Debug(e);
                StopListener("The HTTP port is already in use.");
            }
            else
            {
                _logger.Critical($"SocketException while trying to start HTTP listener:  ({e.SocketErrorCode})");
                _logger.Critical(e);
                StopListener(e.SocketErrorCode + "\n" + e.Message);
            }
        }
        catch (Exception e)
        {
            _logger.Error("Failed to start HTTP listener: " + e.Message);
            _logger.Error(e);
            StopListener(e.Message);
        }
    }

    private void StopListener(string? reason = null)
    {
        _logger.Debug("Stopping listener");
        IsListening = false;
        if (_listener.IsListening) _listener.Stop();
        ErrorMessage = reason;
        InvokeStatusChanged();
    }

    private void InvokeStatusChanged()
    {
        UnityMainThreadTaskScheduler.Factory.StartNew(() =>
        {
            var action = StatusChanged;
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                _logger.Error("Failed to invoke OSC server status changed event.");
                _logger.Error(e);
            }
        });
    }

    private void GetAndProcessRequests()
    {
        while (_listener.IsListening)
        {
            try
            {
                var context = _listener.GetContext();
                _ = Task.Run(() => ProcessRequestAsync(context));
            }
            catch (ObjectDisposedException)
            {
                _logger.Trace("HTTP Listener was disposed.");
                return;
            }
            catch (HttpListenerException e) when (e.ErrorCode == 500)
            {
                // Mono's HttpListener created this exception,
                // the error code IS NOT a Win32 error code.
                _logger.Trace("HTTP Listener was stopped.");
                return;
            }
            catch (HttpListenerException e)
            {
                _logger.Error("HttpListenerException while trying to get context: " + e.Message);
                _logger.Error(e);
                StopListener(e.Message);
                return;
            }
            catch (Exception e)
            {
                _logger.Critical("Failed to receive and process a request. Stopping the http listener.");
                _logger.Critical(e);
                StopListener(e.Message);
                return;
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        var path = context.Request.Url.AbsolutePath;
        var method = context.Request.HttpMethod;

        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(method))
        {
            _logger.Warn("Empty path or method");
            context.BadRequest();
            return;
        }

        _logger.Trace($"{method}: {path}");

        path = NormalizedPath(path);

        try
        {
            if (_handlers.TryGetValue(path, out var methods))
            {
                if (methods.TryGetValue(new HttpMethod(method), out var handler))
                {
                    await handler.HandleRequestAsync(context);
                }
                else if (method == HttpMethod.Options.Method)
                {
                    // allows custom handlers to handle OPTIONS requests
                    // if they don't, we'll just use the default implementation
                    await HandleOptionsAsync(context, methods.Keys);
                }
                else
                {
                    _logger.Warn($"BadMethod: {method} @ {path}");
                    context.BadMethod();
                }
            }
            else
            {
                _logger.Warn($"NotFound: {method} @ {path}");
                context.NotFound();
            }
        }
        catch (Exception e)
        {
            _logger.Critical("Exception while handling request");
            _logger.Critical(e);
            try
            {
                await context.InternalServerErrorAsync(e);
            }
            catch (Exception)
            {
                // The context might be already closed
                context.Response.Close();
            }
        }
    }

    private Task HandleOptionsAsync(HttpListenerContext context, IEnumerable<HttpMethod> methods)
    {
        var response = context.Response;
        var methodsStr = string.Join(", ", methods.Append(HttpMethod.Options).Select(m => m.Method).Distinct());
        _logger.Trace($"OPTIONS: {methodsStr}");
        response.Headers.Add("Access-Control-Allow-Methods", methodsStr);
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        // response.Headers.Add("Access-Control-Expose-Headers", "*");
        response.Headers.Add("Access-Control-Max-Age", "7200");
        response.StatusCode = (int)HttpStatusCode.NoContent;
        response.Close();
        return Task.CompletedTask;
    }

    private string NormalizedPath(string path)
    {
        return path.Length > 1 ? path.TrimEnd('/') : path;
    }
}
