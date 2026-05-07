using Newtonsoft.Json;

namespace HRCounter.Integrations.Pulsoid.Models;

[method: JsonConstructor]
public class ObtainTokenErrorResponse(string? errorRaw, string? errorDescription)
{
    [JsonProperty("error")]
    public string? ErrorRaw { get; } = errorRaw;

    [JsonProperty("error_description")]
    public string? ErrorDescription { get; } = errorDescription;

    public ErrorType Error { get; } = (errorRaw, errorDescription) switch
    {
        ("authorization_pending", _) => ErrorType.AuthorizationPending,
        ("invalid_grant", "user didn't grant access") => ErrorType.AccessDenied,
        ("invalid_grant", "access token already issued") => ErrorType.TokenAlreadyIssued,
        ("expired_token", _) => ErrorType.ExpiredToken,
        _ => ErrorType.Unknown
    };

    public enum ErrorType
    {
        Unknown,
        AuthorizationPending,
        AccessDenied,
        TokenAlreadyIssued,
        ExpiredToken
    }
}
