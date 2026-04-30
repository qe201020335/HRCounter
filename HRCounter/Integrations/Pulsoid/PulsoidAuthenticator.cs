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

    private readonly PulsoidOAuthClient _client = new("a81a9e16-2960-487d-a741-92e22b757c85");

    public enum State
    {
        New,
        Initiated,
        Polling,
        TimedOut,
        Cancelled,
        Failed,
        Success
    }

    public State CurrentState { get; private set; } = State.New;

    private DeviceAuthorizationInitiationResponse? _initiationResponse;

    public void Dispose()
    {
        _client.Dispose();
    }

    public void Reset()
    {
        CurrentState = State.New;
        _initiationResponse = null;
    }

    public async Task<DeviceAuthInitiationResult> InitiateDeviceAuthorizationAsync(CancellationToken ct)
    {
        if (CurrentState != State.New)
        {
            throw new InvalidOperationException("Device authorization already initiated.");
        }

        _logger.Info("Initiating Pulsoid device authorization");

        _initiationResponse = null;

        try
        {
            _initiationResponse = await _client.StartDeviceAuthorization(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.Debug("Pulsoid device authorization initiation canceled");
            CurrentState = State.Cancelled;
            return new DeviceAuthInitiationResult
            {
                Result = DeviceAuthInitiationResult.ResultType.Cancelled
            };
        }
        catch (Exception e)
        {
            _logger.Error("Failed to initiate device authorization");
            _logger.Error(e);
            CurrentState = State.Failed;
            return new DeviceAuthInitiationResult
            {
                Result = DeviceAuthInitiationResult.ResultType.Failure,
                Exception = e
            };
        }

        if (_initiationResponse is { IsValid: true })
        {
            _logger.Info("Pulsoid device authorization initiated");
            _logger.Debug($"UserCode: {_initiationResponse.UserCode?.Redact()}");
            _logger.Debug($"DeviceCode: {_initiationResponse.DeviceCode?.Redact()}");
            _logger.Debug($"Verification URI expires in {_initiationResponse.ExpiresIn} seconds");
            CurrentState = State.Initiated;
            return new DeviceAuthInitiationResult
            {
                Result = DeviceAuthInitiationResult.ResultType.Success,
                VerificationUri = _initiationResponse.VerificationUriComplete!
            };
        }

        _logger.Warn("Failed to initiate Pulsoid device authorization, response is null or not invalid");
        CurrentState = State.Failed;
        return new DeviceAuthInitiationResult
        {
            Result = DeviceAuthInitiationResult.ResultType.Failure,
            Error = "Response is null or invalid"
        };
    }

    public async Task<TokenPollResult> PollForTokenAsync(CancellationToken ct)
    {
        if (CurrentState != State.Initiated)
        {
            throw new InvalidOperationException("Device authorization not initiated.");
        }

        _logger.Info("Polling Pulsoid auth token");
        CurrentState = State.Polling;

        var interval = TimeSpan.FromSeconds(_initiationResponse!.Interval!.Value);
        var deviceCode = _initiationResponse.DeviceCode!;
        _logger.Debug($"Polling for access token every {interval.TotalSeconds} seconds with device code {deviceCode.Redact()}");
        while (true)
        {
            try
            {
                var (success, error) = await _client.TryObtainAccessToken(deviceCode, ct);
                if (success is { IsValid: true })
                {
                    _logger.Info("Successfully obtained Pulsoid access token");
                    CurrentState = State.Success;
                    return new TokenPollResult
                    {
                        Result = TokenPollResult.ResultType.Success,
                        AccessToken = success.AccessToken!
                    };
                }

                if (success is { IsValid: false })
                {
                    _logger.Warn("Invalid Pulsoid access token received");
                    CurrentState = State.Failed;
                    return new TokenPollResult
                    {
                        Result = TokenPollResult.ResultType.Failure,
                        Error = "Invalid Pulsoid access token received"
                    };
                }

                if (error is null)
                {
                    _logger.Warn("Unknown error while polling for Pulsoid access token, error is null");
                    CurrentState = State.Failed;
                    return new TokenPollResult
                    {
                        Result = TokenPollResult.ResultType.Failure,
                        Error = "Unknown error while polling for Pulsoid access token"
                    };
                }

                switch (error.Error)
                {
                    case TokenErrorResponse.ErrorType.AuthorizationPending:
                        _logger.Trace("Pulsoid authorization pending...");
                        break;
                    case TokenErrorResponse.ErrorType.AccessDenied:
                        _logger.Warn("Pulsoid authorization denied");
                        CurrentState = State.Failed;
                        return new TokenPollResult
                        {
                            Result = TokenPollResult.ResultType.Failure,
                            Error = "Authorization is denied"
                        };
                    case TokenErrorResponse.ErrorType.TokenAlreadyIssued:
                        _logger.Warn("Pulsoid access token already issued");
                        CurrentState = State.Failed;
                        return new TokenPollResult
                        {
                            Result = TokenPollResult.ResultType.Failure,
                            Error = "Access token already issued"
                        };
                    case TokenErrorResponse.ErrorType.ExpiredToken:
                        _logger.Warn("Device authorization polling time out");
                        CurrentState = State.TimedOut;
                        return new TokenPollResult
                        {
                            Result = TokenPollResult.ResultType.Timeout
                        };
                    default:
                        _logger.Warn("Unexpected error while polling for Pulsoid access token");
                        var message = $"{error.ErrorRaw ?? "Unknown Error"}: {error.ErrorDescription ?? "Unknown error description"}";
                        _logger.Warn(message);
                        CurrentState = State.Failed;
                        return new TokenPollResult
                        {
                            Result = TokenPollResult.ResultType.Failure,
                            Error = message
                        };
                }

                await Task.Delay(interval, ct);
            }
            catch (OperationCanceledException)
            {
                _logger.Debug("Pulsoid auth token polling canceled");
                CurrentState = State.Cancelled;
                return new TokenPollResult
                {
                    Result = TokenPollResult.ResultType.Cancelled
                };
            }
            catch (Exception e)
            {
                _logger.Error("Failed to poll for access token");
                _logger.Error(e);
                CurrentState = State.Failed;
                return new TokenPollResult
                {
                    Result = TokenPollResult.ResultType.Failure,
                    Exception = e
                };
            }
        }
    }
}
