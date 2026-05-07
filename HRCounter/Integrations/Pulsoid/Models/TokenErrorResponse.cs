using Newtonsoft.Json;

namespace HRCounter.Integrations.Pulsoid.Models;

public class TokenErrorResponse(string? errorCode, string? errorMessage)
{
    [JsonProperty("error_code")]
    public string? ErrorCode { get; } = errorCode;

    [JsonProperty("error_message")]
    public string? ErrorMessage { get; } = errorMessage;

    public ErrorType Error { get; } = errorCode switch
    {
        "7005" => ErrorType.NotFound,
        "7006" => ErrorType.Expired,
        _ => ErrorType.Other
    };

    public enum ErrorType
    {
        NotFound,
        Expired,
        Other
    }
}
