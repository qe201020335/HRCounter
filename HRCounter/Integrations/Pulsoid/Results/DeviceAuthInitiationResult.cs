using System;

namespace HRCounter.Integrations.Pulsoid.Results;

public class DeviceAuthInitiationResult
{
    public ResultType Result { get; set; }

    public string? VerificationUri { get; set; }

    public string? Error { get; set; }

    public Exception? Exception { get; set; }

    public enum ResultType
    {
        Success,
        Failure,
        Cancelled
    }
}
