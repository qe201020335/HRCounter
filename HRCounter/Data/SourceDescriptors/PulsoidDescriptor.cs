using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Data.DataSources;
using HRCounter.Integrations.Pulsoid;
using HRCounter.Integrations.Pulsoid.Results;
using HRCounter.Utils;
using IPA.Utilities.Async;
using JetBrains.Annotations;
using Zenject;

namespace HRCounter.Data.SourceDescriptors;

internal class PulsoidDescriptor : IDataSourceDescriptor<Pulsoid2>
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
            UnityMainThreadTaskScheduler.Factory.StartNew(() => { StatusChanged?.Invoke(); });
        }
    }

    public async Task<string> GetStatusText(CancellationToken cancellationToken)
    {
        if (!PreconditionMet())
        {
            return "Token Not Set";
        }

        var result = await _authenticator.ValidateTokenAsync(_config.PulsoidToken, cancellationToken).ConfigureAwait(true);
        switch (result.Result)
        {
            case TokenValidationResult.ResultType.Valid:
                return
                    $"<color=green>Token valid</color>\nExpires in {TimeSpan.FromSeconds(result.ExpiresIn).ToReadableString()}";
            case TokenValidationResult.ResultType.NotFound:
                return "<color=yellow>Token not found</color>";
            case TokenValidationResult.ResultType.Expired:
                return "<color=yellow>Token expired</color>";
            case TokenValidationResult.ResultType.Cancelled:
                return "Token validation cancelled";
            case TokenValidationResult.ResultType.Failure:
                var text = $"<color=red>Token validation failed</color>\n{result.Error}";
                if (result.Exception != null)
                {
                    text += $"\n{result.Exception.Message}\nCheck logs for details.";
                }

                return text;
            default:
                return "<color=yellow>Unknown token validation result</color>";
        }
    }

    public bool PreconditionMet()
    {
        var s = _config.PulsoidToken;
        return !string.IsNullOrWhiteSpace(s) && s != "NotSet" && s != "-1";
    }
}
