using System;

namespace HRCounter.Integrations.Pulsoid.Results;

internal class TokenPollResult
{
    public ResultType Result { get; set; }

    public string? AccessToken { get; set; }

    public string? Error { get; set; }

    public Exception? Exception { get; set; }

    public enum ResultType
    {
        Success,
        Failure,
        Timeout,
        Cancelled
    }
}
