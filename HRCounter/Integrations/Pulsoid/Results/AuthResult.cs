using System;

namespace HRCounter.Integrations.Pulsoid.Results;

internal class AuthResult
{
    public ResultType Result { get; set; }

    public string? AccessToken { get; set; }

    public long ExpiresIn { get; set; }

    public string? Error { get; set; }

    public Exception? Exception { get; set; }

    public enum ResultType
    {
        Success,
        Denied,
        Timeout,
        Failure,
        Cancelled
    }
}
