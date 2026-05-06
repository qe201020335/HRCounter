using Newtonsoft.Json;

namespace HRCounter.Integrations.Pulsoid.Models;

public class StartDeviceAuthorizationErrorResponse
{
    [JsonProperty("error")]
    public string? Error { get; private set; }

    [JsonProperty("error_description")]
    public string? ErrorDescription { get; private set; }
}
