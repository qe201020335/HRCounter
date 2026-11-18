using System;

namespace HRCounter.Web.HTTP;

/// <summary>
///     The local HTTP server. Handlers stay registered when the listener is stopped or restarted.
/// </summary>
public interface IHttpServer
{
    /// <summary>
    ///     Raised on the main thread when the listener starts, stops, or fails.
    /// </summary>
    event Action? StatusChanged;

    /// <summary>
    ///     Whether the current or last listener only accepts localhost.
    /// </summary>
    bool IsLocalOnly { get; }

    /// <summary>
    ///     The port of the current or last listener.
    /// </summary>
    int Port { get; }

    bool IsListening { get; }

    /// <summary>
    ///     Why the listener last failed or stopped, or null if there was no error.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    ///     Registers the handler for its <see cref="IHttpRouteHandler.Routes" />. Can be called from any thread.
    ///     A trailing slash in a path is ignored. Routes that are already registered are ignored.
    /// </summary>
    void RegisterHandler(IHttpRouteHandler handler);

    /// <summary>
    ///     Unregisters all routes of the handler. Can be called from any thread.
    /// </summary>
    void UnregisterHandler(IHttpRouteHandler handler);
}
