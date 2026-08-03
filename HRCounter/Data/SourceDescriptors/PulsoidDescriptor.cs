using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using BGLib.Polyglot;
using HRCounter.Configuration;
using HRCounter.Data.DataSources;
using HRCounter.Integrations.Pulsoid;
using HRCounter.Integrations.Pulsoid.Results;
using HRCounter.Utils;
using JetBrains.Annotations;
using Zenject;

namespace HRCounter.Data.SourceDescriptors;

internal class PulsoidDescriptor : IDisposableSourceDescriptor<Pulsoid2>
{
    internal const string KEY = "Pulsoid";

    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly PulsoidAuthenticator _authenticator = null!;

    public string Key => KEY;

    public bool StreamerMode
    {
        // do nothing
        set { }
    }

    public event Action? StatusChanged;

    private string? _token;

    [Inject]
    [UsedImplicitly]
    private void Init()
    {
        _config.PropertyChanged += OnConfigChanged;
    }

    public void Dispose()
    {
        _config.PropertyChanged -= OnConfigChanged;
    }

    private void OnConfigChanged(object? _, PropertyChangedEventArgs e)
    {
        if ((string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(_config.PulsoidToken)) && _config.PulsoidToken != _token)
        {
            _token = _config.PulsoidToken;
            StatusChanged?.Invoke();
        }
    }

    public async Task<string> GetStatusText(CancellationToken cancellationToken)
    {
        if (!PreconditionMet())
        {
            return Localization.Get("HRCOUNTER_PULSOID_SOURCE_DESCRIPTOR_TOKEN_NOT_SET");
        }

        var result = await _authenticator.ValidateTokenAsync(_config.PulsoidToken, cancellationToken).ConfigureAwait(true);
        switch (result.Result)
        {
            case TokenValidationResult.ResultType.Valid:
                return Localization.Instance.GetFormatOrKey("HRCOUNTER_PULSOID_SOURCE_DESCRIPTOR_TOKEN_VALID",
                    TimeSpan.FromSeconds(result.ExpiresIn).ToReadableString());
            case TokenValidationResult.ResultType.NotFound:
                return Localization.Get("HRCOUNTER_PULSOID_SOURCE_DESCRIPTOR_TOKEN_NOT_VALID");
            case TokenValidationResult.ResultType.Expired:
                return Localization.Get("HRCOUNTER_PULSOID_SOURCE_DESCRIPTOR_TOKEN_EXPIRED");
            case TokenValidationResult.ResultType.Cancelled:
                return Localization.Get("HRCOUNTER_PULSOID_SOURCE_DESCRIPTOR_VALIDATION_CANCELLED");
            case TokenValidationResult.ResultType.Failure:
                var text = Localization.Get("HRCOUNTER_PULSOID_SOURCE_DESCRIPTOR_VALIDATION_FAILED");
                text += '\n';
                text += result.Error;
                if (result.Exception != null)
                {
                    text += $"\n{result.Exception.Message}\n{Localization.Get("HRCOUNTER_COMMON_CHECK_LOGS")}";
                }

                return text;
            default:
                return Localization.Get("HRCOUNTER_PULSOID_SOURCE_DESCRIPTOR_UNKNOWN");
        }
    }

    public bool PreconditionMet()
    {
        var s = _config.PulsoidToken;
        return !string.IsNullOrWhiteSpace(s) && s != "NotSet" && s != "-1";
    }
}
