using System;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Integrations.Pulsoid.Models;
using HRCounter.Integrations.Pulsoid.Results;
using HRCounter.Utils;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace HRCounter.Integrations.Pulsoid;

internal class PulsoidAuthenticator : IDisposable
{
    [Inject]
    private readonly Logger _logger = null!;

    private readonly PulsoidOAuthClient _authClient = new("a81a9e16-2960-487d-a741-92e22b757c85");
    private readonly PulsoidApiClient _apiClient = new();

    public void Dispose()
    {
        _authClient.Dispose();
        _apiClient.Dispose();
    }

    public async Task<AuthResult> AuthenticateAsync(Action<string> onVerificationUriReceived, CancellationToken ct)
    {
        _logger.Info("Starting Pulsoid device authorization");
        StartDeviceAuthorizationResponse? initiationResponse;
        try
        {
            (initiationResponse, var errorResponse) = await _authClient.StartDeviceAuthorization(ct);
            if (errorResponse is not null)
            {
                var message = errorResponse.Error ?? "Unknown Error";
                if (!string.IsNullOrWhiteSpace(errorResponse.ErrorDescription))
                {
                    message += $": {errorResponse.ErrorDescription}";
                }

                _logger.Warn($"Failed to start Pulsoid device authorization: {message}");
                return new AuthResult
                {
                    Result = AuthResult.ResultType.Failure,
                    Error = message
                };
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Debug("Pulsoid device authorization initiation canceled");
            return new AuthResult { Result = AuthResult.ResultType.Cancelled };
        }
        catch (Exception e)
        {
            _logger.Error("Failed to start device authorization");
            _logger.Error(e);
            return new AuthResult
            {
                Result = AuthResult.ResultType.Failure,
                Error = "Unexpected error while initiating device authorization",
                Exception = e
            };
        }

        if (initiationResponse is not { IsValid: true })
        {
            _logger.Warn("Failed to start Pulsoid device authorization, response is null or invalid");
            return new AuthResult
            {
                Result = AuthResult.ResultType.Failure,
                Error = "Device authorization response is null or invalid"
            };
        }

        _logger.Info("Pulsoid device authorization initiated");
        _logger.Debug($"UserCode: {initiationResponse.UserCode?.Redact()}");
        _logger.Debug($"DeviceCode: {initiationResponse.DeviceCode?.Redact()}");
        _logger.Debug($"Verification URI expires in {initiationResponse.ExpiresIn} seconds");

        try
        {
            onVerificationUriReceived(initiationResponse.VerificationUriComplete!);
        }
        catch (Exception e)
        {
            _logger.Error("Verification URI callback threw");
            _logger.Error(e);
            return new AuthResult
            {
                Result = AuthResult.ResultType.Failure,
                Error = "Verification URI callback threw",
                Exception = e
            };
        }

        return await PollForTokenAsync(initiationResponse.DeviceCode!, TimeSpan.FromSeconds(initiationResponse.Interval!.Value), ct);
    }

    private async Task<AuthResult> PollForTokenAsync(string deviceCode, TimeSpan interval, CancellationToken ct)
    {
        _logger.Debug($"Polling for access token every {interval.TotalSeconds} seconds with device code {deviceCode.Redact()}");
        while (true)
        {
            try
            {
                var (success, error) = await _authClient.TryObtainAccessToken(deviceCode, ct);
                if (success is { IsValid: true })
                {
                    _logger.Info("Successfully obtained Pulsoid access token");
                    return new AuthResult
                    {
                        Result = AuthResult.ResultType.Success,
                        AccessToken = success.AccessToken!,
                        ExpiresIn = success.ExpiresIn
                    };
                }

                if (success is { IsValid: false })
                {
                    _logger.Warn("Invalid Pulsoid access token received");
                    return new AuthResult
                    {
                        Result = AuthResult.ResultType.Failure,
                        Error = "Invalid Pulsoid access token received"
                    };
                }

                if (error is null)
                {
                    _logger.Warn("Unknown error while polling for Pulsoid access token, error is null");
                    return new AuthResult
                    {
                        Result = AuthResult.ResultType.Failure,
                        Error = "Unknown error while polling for Pulsoid access token"
                    };
                }

                switch (error.Error)
                {
                    case ObtainTokenErrorResponse.ErrorType.AuthorizationPending:
                        _logger.Trace("Pulsoid authorization pending...");
                        break;
                    case ObtainTokenErrorResponse.ErrorType.AccessDenied:
                        _logger.Warn("Pulsoid authorization denied");
                        return new AuthResult
                        {
                            Result = AuthResult.ResultType.Denied,
                            Error = "Authorization is denied"
                        };
                    case ObtainTokenErrorResponse.ErrorType.TokenAlreadyIssued:
                        _logger.Warn("Pulsoid access token already issued");
                        return new AuthResult
                        {
                            Result = AuthResult.ResultType.Failure,
                            Error = "Access token already issued"
                        };
                    case ObtainTokenErrorResponse.ErrorType.ExpiredToken:
                        _logger.Warn("Device authorization polling time out");
                        return new AuthResult { Result = AuthResult.ResultType.Timeout };
                    default:
                        _logger.Warn("Unexpected error while polling for Pulsoid access token");
                        var message = $"{error.ErrorRaw ?? "Unknown Error"}: {error.ErrorDescription ?? "Unknown error description"}";
                        _logger.Warn(message);
                        return new AuthResult
                        {
                            Result = AuthResult.ResultType.Failure,
                            Error = message
                        };
                }

                await Task.Delay(interval, ct);
            }
            catch (OperationCanceledException)
            {
                _logger.Debug("Pulsoid auth token polling canceled");
                return new AuthResult { Result = AuthResult.ResultType.Cancelled };
            }
            catch (Exception e)
            {
                _logger.Error("Failed to poll for access token");
                _logger.Error(e);
                return new AuthResult
                {
                    Result = AuthResult.ResultType.Failure,
                    Error = "Unexpected error while polling for Pulsoid access token",
                    Exception = e
                };
            }
        }
    }

    public async Task<TokenValidationResult> ValidateTokenAsync(string token, CancellationToken ct)
    {
        _logger.Info("Validating Pulsoid token");
        ValidateTokenResponse? validation;
        TokenErrorResponse? error;
        try
        {
            (validation, error) = await _apiClient.ValidateTokenAsync(token, ct);
        }
        catch (OperationCanceledException)
        {
            _logger.Debug("Pulsoid token validation canceled");
            return new TokenValidationResult { Result = TokenValidationResult.ResultType.Cancelled };
        }
        catch (Exception e)
        {
            _logger.Error("Failed to validate Pulsoid token");
            _logger.Error(e);
            return new TokenValidationResult
            {
                Result = TokenValidationResult.ResultType.Failure,
                Error = "Unexpected error while validating token",
                Exception = e
            };
        }

        if (validation is not null)
        {
            _logger.Notice($"Pulsoid token is valid, expiring in {validation.ExpiresIn} seconds");
            return new TokenValidationResult
            {
                Result = TokenValidationResult.ResultType.Valid,
                ExpiresIn = validation.ExpiresIn
            };
        }

        if (error is null)
        {
            _logger.Warn("Unknown error while validating Pulsoid token, error is null");
            return new TokenValidationResult
            {
                Result = TokenValidationResult.ResultType.Failure,
                Error = "Unknown error while validating Pulsoid token"
            };
        }

        var message = $"{error.ErrorCode ?? "Unknown error code"}: {error.ErrorMessage ?? "Unknown error"}";
        _logger.Warn($"Pulsoid token validation failed: {message}");

        return error.Error switch
        {
            TokenErrorResponse.ErrorType.NotFound => new TokenValidationResult { Result = TokenValidationResult.ResultType.NotFound },
            TokenErrorResponse.ErrorType.Expired => new TokenValidationResult { Result = TokenValidationResult.ResultType.Expired },
            _ => new TokenValidationResult { Result = TokenValidationResult.ResultType.Failure, Error = message }
        };
    }

    public async Task<bool> RevokeTokenAsync(string token, CancellationToken ct)
    {
        _logger.Info($"Revoking Pulsoid token {token.Redact()}");
        try
        {
            await _authClient.RevokeAccessToken(token, ct);
            _logger.Info("Pulsoid token revoked");
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.Debug("Pulsoid token revocation canceled");
            return false;
        }
        catch (Exception e)
        {
            _logger.Error("Failed to revoke Pulsoid token");
            _logger.Error(e);
            return false;
        }
    }
}
