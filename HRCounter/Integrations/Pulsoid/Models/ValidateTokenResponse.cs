using System.Collections.Generic;
using Newtonsoft.Json;

namespace HRCounter.Integrations.Pulsoid.Models;

public class ValidateTokenResponse
{
    [JsonProperty("token")]
    public string? Token { get; private set; }

    [JsonProperty("client_id")]
    public string? ClientId { get; private set; }

    [JsonProperty("expires_in")]
    public long ExpiresIn { get; private set; }

    [JsonProperty("profile_id")]
    public string? ProfileId { get; private set; }

    [JsonProperty("scopes")]
    public IReadOnlyList<string>? Scopes { get; private set; }
}
