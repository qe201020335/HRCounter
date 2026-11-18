using System;
using System.Net;

namespace HRCounter.Web.OSC;

/// <summary>
///     The OSC (UDP) server. Handlers stay registered when the server is stopped or restarted.
/// </summary>
public interface IOscServer
{
    /// <summary>
    ///     Raised on the main thread when the server starts, stops, or fails.
    /// </summary>
    event Action? StatusChanged;

    bool IsListening { get; }

    /// <summary>
    ///     The bound local endpoint for listening, or null when not listening.
    /// </summary>
    IPEndPoint? EndPoint { get; }

    /// <summary>
    ///     Why the server last failed or stopped, or null if there was no error.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    ///     Registers the handler for its <see cref="IOscMessageHandler.Addresses" />. Must be called on the main thread.
    ///     Addresses that are invalid or already registered are ignored.
    /// </summary>
    void RegisterHandler(IOscMessageHandler handler);

    /// <summary>
    ///     Unregisters the handler and all its addresses. Must be called on the main thread.
    /// </summary>
    void UnregisterHandler(IOscMessageHandler handler);

    /// <summary>
    ///     Re-reads the addresses of a registered handler. Must be called on the main thread.
    /// </summary>
    void UpdateHandler(IOscMessageHandler handler);
}
