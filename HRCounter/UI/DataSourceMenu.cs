using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using HMUI;
using HRCounter.Data;
using HRCounter.Integrations.Pulsoid;
using HRCounter.Integrations.Pulsoid.Results;
using HRCounter.Utils;
using IPA.Utilities.Async;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
    private readonly PulsoidAuthenticator _pulsoidAuthenticator = null!;

    [UIParams]
    private BSMLParserParams _parserParams = null!;

    [UIValue("DataSourceOptions")]
    public List<object> DataSourceOptions => [..DataSourceManager.DataSourceTypes.Keys];

    [UIValue("DataSource")]
    public string DataSource
    {
        get => Config.DataSource;
        set => Config.DataSource = value;
    }

    [UIValue("StreamerMode")]
    public bool StreamerMode
    {
        get => Config.StreamerMode;
        set => Config.StreamerMode = value;
    }

    [UIValue("AllowEdit")]
    public bool AllowEdit => !StreamerMode;

    [UIValue("HypeRateSessionID")]
    public string HypeRateSessionID
    {
        get => Config.HypeRateSessionID;
        set => Config.HypeRateSessionID = value;
    }

    [UIComponent("data-source-info-text")]
    private TextPageScrollView _dataSourceInfoText = null!;

    [UIComponent("data-source-info-refresh-btn")]
    private Button _dataSourceInfoRefreshBtn = null!;

    [UIComponent("hyperate-session-id-text")]
    private TMP_Text _HypeRateSessionIDText = null!;

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

        base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
    }

    protected override void OnConfigChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(Config.DataSource):
                UpdateDataSourceInfoText();
                break;
            case nameof(Config.StreamerMode):
                NotifyPropertyChanged(nameof(AllowEdit));
                UpdateDataSourceInfoText();
                UpdateHypeRateSessionIDText();
                break;
            case nameof(Config.HypeRateSessionID):
                UpdateHypeRateSessionIDText();
                break;
            case nameof(Config.PulsoidToken):
                _ = ValidatePulsoidToken();
                // TODO proper data source info update event
                if (DataSource == DataSourceManager.Pulsoid.Key)
                {
                    UpdateDataSourceInfoText();
                }

                break;
        }
    }

    protected override void RefreshNoBindUI()
    {
        UpdateDataSourceInfoText();
        UpdateHypeRateSessionIDText();
        _ = ValidatePulsoidToken();
    }

    [UIAction("UpdateDataSourceInfoText")]
    private void UpdateDataSourceInfoText()
    {
        var known = DataSourceManager.TryGetFromKey(Config.DataSource, out var source);
        if (!known)
        {
            _dataSourceInfoText.SetText("Unknown Data Source");
            return;
        }

        _dataSourceInfoText.SetText("Loading Data Source Info...");
        _dataSourceInfoRefreshBtn.interactable = false;

        UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
        {
            string newText;
            try
            {
                newText = await source.GetSourceLinkText();
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to update data source info text: {e}");
                newText = "<color=#FF0000>Failed to load info, check logs for details.</color>";
            }

            _dataSourceInfoText.SetText(newText);
            await Task.Delay(500); // no spamming the button
            _dataSourceInfoRefreshBtn.interactable = true;
        });
    }

    private void UpdateHypeRateSessionIDText()
    {
        _HypeRateSessionIDText.text = StreamerMode ? "********" : Config.HypeRateSessionID;
    }

    #region PulsoidAuth

    private bool PulsoidTokenValid
    {
        get;
        set
        {
            field = value;
            if (Parsed)
            {
                _deauthorizePulsoidButton.interactable = value;
            }
        }
    }

    [UIValue("PulsoidTokenStatusText")]
    private string PulsoidTokenStatusText
    {
        get;
        set
        {
            field = value;
            NotifyPropertyChanged();
        }
    } = "";

    private CancellationTokenSource? _pulsoidTokenCts;

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

    [UIComponent("modal-authorize-button")]
    private Button _modalAuthorizeBtn = null!;

    private CancellationTokenSource? _pulsoidAuthCts;

    [UIValue("ModalText")]
    private string ModalText
    {
        get;
        set
        {
            field = value;
            NotifyPropertyChanged();
        }
    } = "";

    [UIValue("ModalCloseButtonText")]
    private string ModalCloseButtonText
    {
        get;
        set
        {
            field = value;
            NotifyPropertyChanged();
        }
    } = "";

    [UIAction("OpenAuthorizeModal")]
    private void OpenAuthorizeModal()
    {
        CancelAuthorization();
        ModalText = PulsoidTokenValid
            ? "Existing token is valid.\nAuthorize again will replace the current one.\nClick Authorize button to re-authorize with Pulsoid."
            : "Click Authorize button to begin authorizing with Pulsoid.";
        ModalCloseButtonText = "Cancel";
        _modalAuthorizeBtn.interactable = true;
        _parserParams.EmitEvent("show-pulsoid-authorize-modal");
    }

    [UIAction("CloseAuthorizeModal")]
    private void CloseAuthorizeModal()
    {
        CancelAuthorization();
        _parserParams.EmitEvent("close-modal");
    }

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

    private void ShowModalText(string text)
    {
        UnityMainThreadTaskScheduler.Factory.StartNew(() => { ModalText = text; });
    }

    [UIAction("AuthorizePulsoid")]
    [UsedImplicitly]
    private void AuthorizePulsoid()
    {
        CancelAuthorization();
        _pulsoidAuthCts = new CancellationTokenSource();
        _modalAuthorizeBtn.interactable = false;
        Task.Run(async () =>
        {
            try
            {
                var authResult = await _pulsoidAuthenticator.AuthenticateAsync(url =>
                {
                    _logger.Debug($"Pulsoid device authorization started, opening browser to {url.Redact()}");
                    // launch browser
                    Process.Start(new ProcessStartInfo { FileName = url, Verb = "open" });
                    ShowModalText("Browser has been opened for authorization.\n\nWaiting for authorization...");
                }, _pulsoidAuthCts.Token);

                if (authResult.Result == AuthResult.ResultType.Cancelled)
                {
                    return;
                }

                string text;

                if (authResult.Result == AuthResult.ResultType.Success && authResult.AccessToken != null)
                {
                    _logger.Notice("Pulsoid authorization successful");
                    _logger.Notice($"Pulsoid token: {authResult.AccessToken.Redact()}");
                    await UnityMainThreadTaskScheduler.Factory.StartNew(CancelTokenValidation);
                    Config.PulsoidToken = authResult.AccessToken!;
                    text = "<color=green>Pulsoid authorization successful</color>";
                    if (authResult.ExpiresIn > 0)
                    {
                        var timeSpan = TimeSpan.FromSeconds(authResult.ExpiresIn);
                        text += $"\nToken expires in {timeSpan.ToReadableString()} days";
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

                ShowModalText(text);
            }
            catch (Exception e)
            {
                _logger.Error("Failed to authorize Pulsoid");
                _logger.Error(e);
                var text = $"<color=red>Unexpected error during Pulsoid authorization</color>\n{e.Message}\nCheck logs for details.";
                ShowModalText(text);
            }
            finally
            {
                _ = UnityMainThreadTaskScheduler.Factory.StartNew(() => { ModalCloseButtonText = "Close"; });
            }
        });
    }

    [UIComponent("deauthorize-pulsoid-btn")]
    private Button _deauthorizePulsoidButton = null!;

    [UIAction("DeauthorizePulsoid")]
    private async Task DeauthorizePulsoid()
    {
        var token = Config.PulsoidToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        _deauthorizePulsoidButton.interactable = false;
        CancelAuthorization();
        CancelTokenValidation();

        var success = await _pulsoidAuthenticator.RevokeTokenAsync(token, CancellationToken.None).ConfigureAwait(true);
        Config.PulsoidToken = success ? "" : token; // force a refresh
        _parserParams.EmitEvent("close-modal");
        _deauthorizePulsoidButton.interactable = true;
    }

    #endregion
}
