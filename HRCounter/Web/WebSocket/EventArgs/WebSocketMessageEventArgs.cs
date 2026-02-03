namespace HRCounter.Web.WebSocket.EventArgs;

public class WebSocketMessageEventArgs : System.EventArgs
{
    public string Message { get; }

    public WebSocketMessageEventArgs(string message) => Message = message;
}
