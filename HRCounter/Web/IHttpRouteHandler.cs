using System;
using System.Net;
using System.Net.Http;

namespace HRCounter.Web
{
    internal interface IHttpRouteHandler
    {
        /*
         * (Path, Method)s
         */
        Tuple<string, HttpMethod>[] Routes { get; }
        
        void HandleRequest(HttpListenerContext context);
    }
}