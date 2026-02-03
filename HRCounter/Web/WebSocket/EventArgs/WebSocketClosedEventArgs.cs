using System;
using System.Net.WebSockets;

namespace HRCounter.Web.WebSocket.EventArgs;

public class WebSocketClosedEventArgs : System.EventArgs
{
    public WebSocketCloseStatus CloseStatus { get; }
    public string CloseStatusDescription { get; }

    /// <summary>
    ///     Is the closed message a result of a WebSocket message
    /// </summary>
    public bool IsWebSocketMessage { get; }

    /// <summary>
    ///     The exception that caused the WebSocket to close, if any
    /// </summary>
    public Exception? Exception { get; }

    public WebSocketClosedEventArgs(WebSocketCloseStatus closeStatus, string closeStatusDescription, bool isWebSocketMessage, Exception? exception)
    {
        CloseStatus = closeStatus;
        CloseStatusDescription = closeStatusDescription;
        IsWebSocketMessage = isWebSocketMessage;
        Exception = exception;
    }
}
