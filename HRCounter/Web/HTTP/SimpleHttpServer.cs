using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Web.HTTP.Handlers;
using SiraUtil.Logging;
using Zenject;

namespace HRCounter.Web.HTTP
{
    internal class SimpleHttpServer: IInitializable, IDisposable
    {
        
        private readonly PluginConfig _config;
        private readonly SiraLog _logger;
        private readonly HttpListener _listener = new HttpListener();
        /**
         * Path -> Method -> Handler
         */
        private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<HttpMethod, IHttpRouteHandler>> _handlers;

        public bool IsLocalOnly { get; }
        public int Port { get; }

        public bool IsListening { get; private set; } = false;
        
        internal SimpleHttpServer(PluginConfig config, SiraLog logger, IHttpRouteHandler[] handlers)
        {
            _config = config;
            _logger = logger;
            IsLocalOnly = config.HttpLocalOnly;
            Port = config.HttpPort;

            var domain = IsLocalOnly ? "localhost" : "+";
            var url =  $"http://{domain}:{Port}/";
            _listener.Prefixes.Add(url);
            
            var handlersDict = new Dictionary<string, Dictionary<HttpMethod, IHttpRouteHandler>>();
            foreach (var handler in handlers)
            {
                foreach (var (path, method) in handler.Routes)
                {
                    if (!handlersDict.TryGetValue(NormalizedPath(path), out var methods))
                    {
                        methods = new Dictionary<HttpMethod, IHttpRouteHandler>();
                        handlersDict[path] = methods;
                    }
                    
                    methods.Add(method, handler); // Will throw if duplicate routes with the same method
                }
            }
            
            _handlers = handlersDict
                .Select(pair => new KeyValuePair<string, IReadOnlyDictionary<HttpMethod, IHttpRouteHandler>>(pair.Key, pair.Value))
                .ToDictionary(pair => pair.Key, pair => pair.Value); // make it read-only
        }
        
        public void Initialize()
        {
            _config.OnSettingsChanged += UpdateListener;
            UpdateListener();
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
            
            IsListening = true;
            _listener.Start();
            Task.Run(GetAndProcessRequestsAsync);
        }
        
        private void StopListener()
        {
            _logger.Debug("Stopping listener");
            IsListening = false;
            _listener.Stop();
        }
        
        private async Task GetAndProcessRequestsAsync()
        {
            while (_listener.IsListening)
            {
                var context = await _listener.GetContextAsync();

                try
                {
                    _ = Task.Run(() => ProcessRequestAsync(context));
                }
                catch (Exception e)
                {
                    _logger.Critical("Exception while creating a new task for async request handling");
                    _logger.Critical(e);
                    await context.InternalServerErrorAsync(e);
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
                await context.InternalServerErrorAsync(e);
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
            response.StatusCode = (int) HttpStatusCode.NoContent;
            response.Close();
            return Task.CompletedTask;
        }
        
        private string NormalizedPath(string path)
        {
            return path.Length > 1 ? path.TrimEnd('/') : path;
        }
    }
}