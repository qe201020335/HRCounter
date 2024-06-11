using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HRCounter.Configuration;
using SiraUtil.Logging;
using Zenject;

namespace HRCounter.Web
{
    internal class SimpleHttpServer: IInitializable, IDisposable
    {
        internal const string PREFIX = "http://localhost:65302/";  // TODO: make it configurable?
        
        private readonly PluginConfig _config;
        private readonly SiraLog _logger;
        private readonly HttpListener _listener = new HttpListener();
        /**
         * Path -> Method -> Handler
         */
        private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<HttpMethod, IHttpRouteHandler>> _handlers;
        
        private bool _isListening = false;
        
        internal SimpleHttpServer(PluginConfig config, SiraLog logger, IHttpRouteHandler[] handlers)
        {
            _config = config;
            _logger = logger;
            _listener.Prefixes.Add(PREFIX);
            
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
            StartListener();
        }

        public void Dispose()
        {
            StopListener();
            _listener.Close();
        }
        
        private void StartListener()
        {
            _logger.Debug("Starting listener");
            if (_isListening)
            {
                _logger.Warn("Listener already started");
                return;
            }
            
            _isListening = true;
            _listener.Start();
            Task.Run(GetAndProcessRequests);
        }
        
        private void StopListener()
        {
            _logger.Debug("Stopping listener");
            _isListening = false;
            _listener.Stop();
        }
        
        private void GetAndProcessRequests()
        {
            while (_listener.IsListening)  // TODO: make every request handling async
            {
                var context = _listener.GetContext();
                var path = context.Request.Url.AbsolutePath;
                var method = context.Request.HttpMethod;

                if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(method))
                {
                    _logger.Warn("Empty path or method");
                    context.BadRequest();
                    continue;
                }
                
                _logger.Trace($"{method}: {path}");

                path = NormalizedPath(path);
                
                try
                {
                    if (_handlers.TryGetValue(path, out var methods))
                    {
                        if (methods.TryGetValue(new HttpMethod(method), out var handler))
                        {
                            handler.HandleRequest(context);
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
                    context.InternalServerError();
                }
                
            }
        }
        
        private string NormalizedPath(string path)
        {
            return path.Length > 1 ? path.TrimEnd('/') : path;
        }
    }
}