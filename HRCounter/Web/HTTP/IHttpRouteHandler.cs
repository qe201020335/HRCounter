using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace HRCounter.Web.HTTP;

/// <summary>
///     Handles HTTP requests for its routes. Register it with <see cref="IHttpServer" />.
/// </summary>
public interface IHttpRouteHandler
{
    /// <summary>
    ///     The (path, method) pairs to handle, e.g. <c>("/hr", HttpMethod.Post)</c>. Only read on register.
    ///     OPTIONS gets a default CORS response unless it is included here.
    /// </summary>
    Tuple<string, HttpMethod>[] Routes { get; }

    /// <summary>
    ///     Called on a thread pool thread. Must send a response or close it.
    ///     An exception is answered with a 500.
    /// </summary>
    Task HandleRequestAsync(HttpListenerContext context);
}
