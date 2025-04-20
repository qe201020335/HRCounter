﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace HRCounter.Web.HTTP.Handlers;

internal interface IHttpRouteHandler
{
    /*
     * (Path, Method)s
     */
    Tuple<string, HttpMethod>[] Routes { get; }

    Task HandleRequestAsync(HttpListenerContext context);
}
