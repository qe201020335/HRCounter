using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using HRCounter.Configuration;
using IPA.Logging;
using IPA.Utilities.Async;
using Zenject;

namespace HRCounter.Web.HTTP;

internal class SimpleHttpServer : IInitializable, IDisposable, IHttpServer
{
    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly Logger _logger = null!;

    /**
     * Path -> Method -> Handler
     */
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<HttpMethod, IHttpRouteHandler>> _handlers = [];

    private HttpListener? _listener;

    public event Action? StatusChanged;

    public bool IsLocalOnly { get; private set; }
    public int Port { get; private set; }
    public bool IsListening => _listener != null;
    public string? ErrorMessage { get; private set; }

    public void Initialize()
    {
        UpdateListener();
        _config.PropertyChanged += OnConfigChanged;
    }

    public void Dispose()
    {
        _config.PropertyChanged -= OnConfigChanged;
        StopListener();
    }

    private void OnConfigChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) ||
            e.PropertyName is nameof(PluginConfig.EnableHttpServer) or nameof(PluginConfig.HttpLocalOnly) or nameof(PluginConfig.HttpPort))
        {
            UpdateListener();
        }
    }

    private void UpdateListener()
    {
        if (IsListening && (!_config.EnableHttpServer || IsLocalOnly != _config.HttpLocalOnly || Port != _config.HttpPort))
        {
            StopListener();
        }

        if (_config.EnableHttpServer && !IsListening)
        {
            StartListener();
        }
    }

    private void StartListener()
    {
        IsLocalOnly = _config.HttpLocalOnly;
        Port = _config.HttpPort;
        _logger.Info($"Starting HTTP listener on port {Port}, local only: {IsLocalOnly}");
        try
        {
            var listener = new HttpListener();
            _listener = listener;
            listener.Prefixes.Add($"http://{(IsLocalOnly ? "localhost" : "+")}:{Port}/");
            listener.Start();
            ErrorMessage = null;

            Task.Run(() => GetAndProcessRequests(listener));
            InvokeStatusChanged();
        }
        catch (Exception e)
        {
            var reason = e is SocketException { SocketErrorCode: SocketError.AddressAlreadyInUse }
                ? "The HTTP port is already in use."
                : e.Message;
            _logger.Error($"Failed to start HTTP listener: {reason}");
            _logger.Error(e);
            StopListener(reason);
        }
    }

    private void StopListener(string? reason = null)
    {
        _logger.Info("Stopping HTTP listener.");
        ErrorMessage = reason;
        if (_listener != null)
        {
            if (_listener.IsListening) _listener.Stop();
            _listener.Close();
            _listener = null;
        }

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
                _logger.Error("Failed to invoke HTTP server status changed event.");
                _logger.Error(e);
            }
        });
    }

    private async Task GetAndProcessRequests(HttpListener listener)
    {
        try
        {
            while (true)
            {
                var context = await listener.GetContextAsync();
                _ = Task.Run(() => ProcessRequestAsync(context));
            }
        }
        catch (Exception e)
        {
            _ = UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                if (_listener != listener) return; // we stopped it

                _logger.Error("HTTP listener stopped unexpectedly.");
                _logger.Error(e);
                StopListener(e.Message);
            });
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        try
        {
            var path = NormalizedPath(context.Request.Url.AbsolutePath);
            var method = context.Request.HttpMethod;
            _logger.Trace($"{method}: {path}");

            if (!_handlers.TryGetValue(path, out var methods) || methods.IsEmpty)
            {
                _logger.Warn($"NotFound: {method} @ {path}");
                context.NotFound();
                return;
            }

            if (methods.TryGetValue(new HttpMethod(method), out var handler))
            {
                await handler.HandleRequestAsync(context);
            }
            else if (method == HttpMethod.Options.Method)
            {
                // allows custom handlers to handle OPTIONS requests
                // if they don't, we'll just use the default implementation
                HandleOptions(context, methods.Keys);
            }
            else
            {
                _logger.Warn($"MethodNotAllowed: {method} @ {path}");
                context.MethodNotAllowed();
            }
        }
        catch (Exception e)
        {
            _logger.Error("Exception while handling request");
            _logger.Error(e);
            try
            {
                await context.InternalServerErrorAsync(e.Message);
            }
            catch (Exception)
            {
                // The context might be already closed
                context.Response.Close();
            }
        }
    }

    private void HandleOptions(HttpListenerContext context, IEnumerable<HttpMethod> methods)
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
    }

    private static string NormalizedPath(string path)
    {
        return path.Length > 1 ? path.TrimEnd('/') : path;
    }

    public void RegisterHandler(IHttpRouteHandler handler)
    {
        foreach (var (path, method) in handler.Routes)
        {
            var methods = _handlers.GetOrAdd(NormalizedPath(path), _ => new ConcurrentDictionary<HttpMethod, IHttpRouteHandler>());
            if (!methods.TryAdd(method, handler))
            {
                _logger.Warn($"{method} @ {path} has already been registered");
            }
        }
    }

    public void UnregisterHandler(IHttpRouteHandler handler)
    {
        foreach (var methods in _handlers.Values)
        {
            foreach (var route in methods)
            {
                if (route.Value == handler)
                {
                    ((ICollection<KeyValuePair<HttpMethod, IHttpRouteHandler>>)methods).Remove(route);
                }
            }
        }
    }
}
