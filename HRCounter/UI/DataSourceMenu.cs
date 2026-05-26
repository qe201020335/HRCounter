using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using HRCounter.Data;
using HRCounter.Integrations.Pulsoid;
using HRCounter.Integrations.Pulsoid.Results;
using HRCounter.Utils;
using IPA.Utilities.Async;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace HRCounter.UI;

[HotReload(RelativePathToLayout = @"BSML\dataSource.bsml")]
[ViewDefinition("HRCounter.UI.BSML.dataSource.bsml")]
internal class DataSourceMenu : BaseConfigViewController
{
    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly DataSourceManager _dataSourceManager = null!;
    
    [Inject]
    private readonly PulsoidAuthenticator _pulsoidAuthenticator = null!;

    [UIParams]
    private BSMLParserParams _parserParams = null!;

    [UIValue(nameof(AllowEdit))]
    public bool AllowEdit => !StreamerMode;

    protected override void OnParsed()
    {
        if (!Parsed)
        {
            ((RectTransform)gameObject.transform).offsetMax = new Vector2(0, 22);
        }

        base.OnParsed();
    }

    protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
    {
        CloseAuthorizeModal();
        CancelTokenValidation();
        CancelDataSourceInfoUpdate();

        if (_sourceDescriptor != null)
        {
            _sourceDescriptor.StatusChanged -= UpdateDataSourceInfoText;
            _sourceDescriptor = null;
        }

        base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
    }

    protected override void OnConfigChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(Config.DataSource):
                UpdateDataSourceDescriptor();
                break;
            case nameof(Config.StreamerMode):
                NotifyPropertyChanged(nameof(AllowEdit));
                NotifyPropertyChanged(nameof(HypeRateSessionIDText));
                break;
            case nameof(Config.HypeRateSessionID):
                NotifyPropertyChanged(nameof(HypeRateSessionIDText));
                break;
            case nameof(Config.PulsoidToken):
                _ = ValidatePulsoidToken();
                break;
        }
    }

    protected override void RefreshUI()
    {
        UpdateDataSourceDescriptor();
        _ = ValidatePulsoidToken();
    }

    #region General Tab

    private IDataSourceDescriptor? _sourceDescriptor;

    private CancellationTokenSource? _sourceInfoCts;

    [UIValue(nameof(Config.StreamerMode))]
    public bool StreamerMode
    {
        get => Config.StreamerMode;
        set
        {
            if (Config.StreamerMode != value) Config.StreamerMode = value;
        }
    }
    
    [UIValue(nameof(DataSourceOptions))]
    [UsedImplicitly]
    public List<object> DataSourceOptions => [.._dataSourceManager.DataSources.Keys];

    [UIValue(nameof(Config.DataSource))]
    public string DataSource
    {
        get => Config.DataSource;
        set
        {
            if (Config.DataSource != value) Config.DataSource = value;
        }
    }

    [UIValue(nameof(DataSourceInfoRefreshBtnInteractable))]
    public bool DataSourceInfoRefreshBtnInteractable
    {
        get;
        private set
        {
            field = value;
            NotifyPropertyChanged();
        }
    } = true;

    [UIValue(nameof(DataSourceInfoText))]
    public string DataSourceInfoText
    {
        get;
        private set
        {
            field = value;
            NotifyPropertyChanged();
        }
    } = "";

    [UIAction(nameof(UpdateDataSourceInfoText))]
    private void UpdateDataSourceInfoText()
    {
        CancelDataSourceInfoUpdate();
        var source = _sourceDescriptor;
        if (source is null)
        {
            DataSourceInfoText = $"<color=yellow>Unknown Data Source</color>: {Config.DataSource}\nPlease select one above";
            return;
        }

        var cts = new CancellationTokenSource();
        _sourceInfoCts = cts;
        var ct = cts.Token;

        DataSourceInfoRefreshBtnInteractable = false;

        UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
        {
            _logger.Debug("Updating data source info text");
            try
            {
                var task = Task.Run(() => source.GetStatusText(ct), ct);
                if (await Task.WhenAny(task, Task.Delay(50, ct)).ConfigureAwait(true) != task)
                {
                    // it is taking some time to get the text
                    DataSourceInfoText = "Loading Data Source Info...";
                }

                DataSourceInfoText = await task.ConfigureAwait(true);
                await Task.Delay(400, ct).ConfigureAwait(true); // no spamming the button
            }
            catch (OperationCanceledException)
            {
                _logger.Trace("Data source info update cancelled");
                DataSourceInfoText = "Data source info update cancelled";
            }
            catch (Exception e)
            {
                _logger.Error("Failed to update data source info text");
                _logger.Error(e);
                DataSourceInfoText = $"<color=#FF0000>Failed to load info: {e.Message}\nCheck logs for details.</color>";
            }
            finally
            {
                DataSourceInfoRefreshBtnInteractable = true;
            }
        }, CancellationToken.None);
    }

    private void CancelDataSourceInfoUpdate()
    {
        _sourceInfoCts?.Cancel();
        _sourceInfoCts?.Dispose();
        _sourceInfoCts = null;
    }

    private void UpdateDataSourceDescriptor()
    {
        var descriptor = _dataSourceManager.GetFromKey(Config.DataSource);
        if (descriptor == _sourceDescriptor)
        {
            return;
        }

        if (_sourceDescriptor != null)
        {
            _sourceDescriptor.StatusChanged -= UpdateDataSourceInfoText;
        }

        _sourceDescriptor = descriptor;
        if (_sourceDescriptor != null)
        {
            _sourceDescriptor.StatusChanged += UpdateDataSourceInfoText;
        }

        UpdateDataSourceInfoText();
    }

    #endregion

    #region Pulsoid Tab

    [UIValue(nameof(PulsoidTokenValid))]
    public bool PulsoidTokenValid
    {
        get;
        private set
        {
            field = value;
            NotifyPropertyChanged();
        }
    }

    [UIValue(nameof(PulsoidTokenStatusText))]
    public string PulsoidTokenStatusText
    {
        get;
        private set
        {
            field = value;
            NotifyPropertyChanged();
        }
    } = "";

    [UIValue(nameof(AuthModalText))]
    public string AuthModalText
    {
        get;
        private set
        {
            field = value;
            NotifyPropertyChanged();
        }
    } = "";

    [UIValue(nameof(AuthModalCloseBtnText))]
    public string AuthModalCloseBtnText
    {
        get;
        private set
        {
            field = value;
            NotifyPropertyChanged();
        }
    } = "";

    [UIValue(nameof(AuthModalAuthBtnInteractable))]
    public bool AuthModalAuthBtnInteractable
    {
        get;
        private set
        {
            field = value;
            NotifyPropertyChanged();
        }
    } = true;

    [UIValue(nameof(DeauthModalDeauthBtnInteractable))]
    public bool DeauthModalDeauthBtnInteractable
    {
        get;
        private set
        {
            field = value;
            NotifyPropertyChanged();
        }
    } = true;

    private CancellationTokenSource? _pulsoidTokenCts;

    private void CancelTokenValidation()
    {
        if (_pulsoidTokenCts != null)
        {
            _logger.Debug("Cancelling Pulsoid token validation");
            _pulsoidTokenCts.Cancel();
            _pulsoidTokenCts.Dispose();
            _pulsoidTokenCts = null;
        }
    }

    private async Task ValidatePulsoidToken()
    {
        var token = Config.PulsoidToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            PulsoidTokenStatusText = "No Pulsoid token.\nClick <i><b><smallcaps>Authorize Pulsoid</smallcaps></b></i> to get one.";
            PulsoidTokenValid = false;
            return;
        }

        CancelTokenValidation();
        _pulsoidTokenCts = new CancellationTokenSource();
        var result = await _pulsoidAuthenticator.ValidateTokenAsync(token, _pulsoidTokenCts.Token).ConfigureAwait(true);
        if (result.Result == TokenValidationResult.ResultType.Cancelled) return;
        PulsoidTokenValid = result.Result == TokenValidationResult.ResultType.Valid;
        switch (result.Result)
        {
            case TokenValidationResult.ResultType.Valid:
                PulsoidTokenStatusText =
                    $"<color=green>Token valid</color>\nExpires in {TimeSpan.FromSeconds(result.ExpiresIn).ToReadableString()}";
                break;
            case TokenValidationResult.ResultType.NotFound:
                PulsoidTokenStatusText = "<color=yellow>Token not found</color>";
                break;
            case TokenValidationResult.ResultType.Expired:
                PulsoidTokenStatusText = "<color=yellow>Token expired</color>";
                break;
            case TokenValidationResult.ResultType.Failure:
                var text = $"<color=red>Token validation failed</color>\n{result.Error}";
                if (result.Exception != null)
                {
                    text += $"\n{result.Exception.Message}\nCheck logs for details.";
                }

                PulsoidTokenStatusText = text;
                break;
        }
    }

    private CancellationTokenSource? _pulsoidAuthCts;

    private void CancelAuthorization()
    {
        if (_pulsoidAuthCts != null)
        {
            _logger.Debug("Cancelling Pulsoid authorization");
            _pulsoidAuthCts.Cancel();
            _pulsoidAuthCts.Dispose();
            _pulsoidAuthCts = null;
        }
    }

    [UIAction(nameof(OpenAuthorizeModal))]
    [UsedImplicitly]
    private void OpenAuthorizeModal()
    {
        CancelAuthorization();
        AuthModalText = PulsoidTokenValid
            ? "Existing token is valid.\nAuthorize again will replace the current one.\nClick Authorize button to re-authorize with Pulsoid."
            : "Click Authorize button to begin authorizing with Pulsoid.";
        AuthModalCloseBtnText = "Cancel";
        AuthModalAuthBtnInteractable = true;
        _parserParams.EmitEvent("show-pulsoid-authorize-modal");
    }

    [UIAction(nameof(CloseAuthorizeModal))]
    [UsedImplicitly]
    private void CloseAuthorizeModal()
    {
        CancelAuthorization();
        _parserParams.EmitEvent("close-modal");
    }

    [UIAction(nameof(AuthorizePulsoid))]
    [UsedImplicitly]
    private async Task AuthorizePulsoid()
    {
        AuthModalAuthBtnInteractable = false;
        CancelAuthorization();
        _pulsoidAuthCts = new CancellationTokenSource();
        try
        {
            var authResult = await Task.Run(() => _pulsoidAuthenticator.AuthenticateAsync(url =>
            {
                _logger.Debug($"Pulsoid device authorization started, opening browser to {url.Redact()}");
                // launch browser
                Process.Start(new ProcessStartInfo { FileName = url, Verb = "open" });
                UnityMainThreadTaskScheduler.Factory.StartNew(() =>
                {
                    AuthModalText = "Browser has been opened for authorization.\n\nWaiting for authorization...";
                });
            }, _pulsoidAuthCts.Token)).ConfigureAwait(true);

            if (authResult.Result == AuthResult.ResultType.Cancelled)
            {
                return;
            }

            string text;

            if (authResult.Result == AuthResult.ResultType.Success && authResult.AccessToken != null)
            {
                _logger.Notice("Pulsoid authorization successful");
                _logger.Notice($"Pulsoid token: {authResult.AccessToken.Redact()}");
                CancelTokenValidation();
                Config.PulsoidToken = authResult.AccessToken!;
                text = "<color=green>Pulsoid authorization successful</color>";
                if (authResult.ExpiresIn > 0)
                {
                    var timeSpan = TimeSpan.FromSeconds(authResult.ExpiresIn);
                    text += $"\nToken expires in {timeSpan.ToReadableString()}";
                }
            }
            else
            {
                _logger.Warn(
                    $"Pulsoid authorization failed: {authResult.Result} {(string.IsNullOrWhiteSpace(authResult.Error) ? "" : $"({authResult.Error})")}");
                text = $"<color=yellow>Pulsoid authorization failed</color>\n{authResult.Error}";
                if (authResult.Exception != null)
                {
                    text += $"\n{authResult.Exception.Message}";
                }

                if (authResult.Result != AuthResult.ResultType.Denied)
                {
                    text += "\nCheck logs for details.";
                }
            }

            AuthModalText = text;
        }
        catch (Exception e)
        {
            _logger.Error("Failed to authorize Pulsoid");
            _logger.Error(e);
            var text = $"<color=red>Unexpected error during Pulsoid authorization</color>\n{e.Message}\nCheck logs for details.";
            AuthModalText = text;
        }
        finally
        {
            AuthModalCloseBtnText = "Close";
        }
    }

    [UIAction(nameof(DeauthorizePulsoid))]
    [UsedImplicitly]
    private async Task DeauthorizePulsoid()
    {
        var token = Config.PulsoidToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        DeauthModalDeauthBtnInteractable = false;
        CancelAuthorization();
        CancelTokenValidation();

        await _pulsoidAuthenticator.RevokeTokenAsync(token, CancellationToken.None).ConfigureAwait(true);
        Config.PulsoidToken = "";
        _parserParams.EmitEvent("close-modal");
        DeauthModalDeauthBtnInteractable = true;
    }

    #endregion

    #region HypeRate Tab

    [UIValue(nameof(Config.HypeRateSessionID))]
    public string HypeRateSessionID
    {
        get => Config.HypeRateSessionID;
        set
        {
            if (Config.HypeRateSessionID != value) Config.HypeRateSessionID = value;
        }
    }

    [UIValue(nameof(HypeRateSessionIDText))]
    public string HypeRateSessionIDText => StreamerMode ? "********" : Config.HypeRateSessionID;

    #endregion
}
