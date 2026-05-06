using Newtonsoft.Json;

namespace HRCounter.Integrations.Pulsoid.Models;

public class StartDeviceAuthorizationResponse
{
    [JsonProperty("device_code")]
    public string? DeviceCode { get; private set; }

    [JsonProperty("user_code")]
    public string? UserCode { get; private set; }

    [JsonProperty("verification_uri")]
    public string? VerificationUri { get; private set; }

    [JsonProperty("verification_uri_complete")]
    public string? VerificationUriComplete { get; private set; }

    [JsonProperty("expires_in")]
    public int? ExpiresIn { get; private set; }

    [JsonProperty("interval")]
    public int? Interval { get; private set; }

    public bool IsValid => !string.IsNullOrWhiteSpace(DeviceCode) && !string.IsNullOrEmpty(VerificationUriComplete) &&
                           Interval.HasValue && Interval.Value > 0;
}
