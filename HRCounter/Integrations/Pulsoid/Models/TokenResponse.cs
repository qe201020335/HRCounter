using Newtonsoft.Json;

namespace HRCounter.Integrations.Pulsoid.Models;

public class TokenResponse
{
    [JsonProperty("access_token")]
    public string? AccessToken { get; private set; }

    [JsonProperty("expires_in")]
    public long ExpiresIn { get; private set; }

    [JsonProperty("token_type")]
    public string? TokenType { get; private set; }

    public bool IsValid => !string.IsNullOrWhiteSpace(AccessToken);
}