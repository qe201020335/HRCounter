using System;

namespace HRCounter.Integrations.Pulsoid.Results;

public class TokenValidationResult
{
    public ResultType Result { get; set; }

    public long ExpiresIn { get; set; }

    public string? Error { get; set; }

    public Exception? Exception { get; set; }

    public enum ResultType
    {
        Valid,
        NotFound,
        Expired,
        Failure,
        Cancelled
    }
}
