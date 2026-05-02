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
        ModalText = "Click Authorize button to begin authorizing with Pulsoid.";
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
                _pulsoidAuthenticator.Reset();
                var initiationResult = await _pulsoidAuthenticator.InitiateDeviceAuthorizationAsync(_pulsoidAuthCts.Token);
                if (initiationResult.Result == DeviceAuthInitiationResult.ResultType.Cancelled)
                {
                    return;
                }
                if (initiationResult.Result != DeviceAuthInitiationResult.ResultType.Success)
                {
                    var text = $"<color=yellow>Failed to start Pulsoid authorization</color>\n{initiationResult.Error}";
                    if (initiationResult.Exception != null)
                    {
                        text += $"\n{initiationResult.Exception.Message}";
                    }

                    text += "\nCheck logs for details.";
                    ShowModalText(text);
                    return;
                }

                var url = initiationResult.VerificationUri!;
                _logger.Debug($"Pulsoid device authorization initiated, opening browser to {url.Redact()}");
                // launch browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    Verb = "open"
                });

                ShowModalText("Browser has been opened for authorization.\n\nWaiting for authorization...");

                var authResult = await _pulsoidAuthenticator.PollForTokenAsync(_pulsoidAuthCts.Token);
                if (authResult.Result == TokenPollResult.ResultType.Cancelled)
                {
                    return;
                }
                if (authResult.Result == TokenPollResult.ResultType.Success && authResult.AccessToken != null)
                {
                    _logger.Notice("Pulsoid authorization successful");
                    _logger.Notice($"Pulsoid token: {authResult.AccessToken.Redact()}");
                    Config.PulsoidToken = authResult.AccessToken!;
                    var text = "<color=green>Pulsoid authorization successful</color>";
                    if (authResult.ExpiresIn > 0)
                    {
                        var timeSpan = TimeSpan.FromSeconds(authResult.ExpiresIn);
                        text += $"\nToken expires in {timeSpan.TotalDays} days";
                    }

                    ShowModalText(text);
                }
                else
                {
                    _logger.Warn(
                        $"Pulsoid authorization failed: {authResult.Result} {(string.IsNullOrWhiteSpace(authResult.Error) ? "" : $"({authResult.Error})")}");
                    var text = $"<color=yellow>Pulsoid authorization failed</color>\n{authResult.Error}";
                    if (authResult.Exception != null)
                    {
                        text += $"\n{authResult.Exception.Message}";
                    }

                    if (authResult.Result != TokenPollResult.ResultType.Denied)
                    {
                        text += "\nCheck logs for details.";
                    }

                    ShowModalText(text);
                }
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

    #endregion
}
