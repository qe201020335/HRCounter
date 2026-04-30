using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using HRCounter.Data;
using HRCounter.Integrations.Pulsoid;
using HRCounter.Integrations.Pulsoid.Results;
using HRCounter.Utils;
using IPA.Utilities.Async;
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

    [UIComponent("authorize-pulsoid-btn")]
    private Button _authorizePulsoidBtn = null!;

    private CancellationTokenSource? _pulsoidAuthCts;

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
        if (_pulsoidAuthCts != null)
        {
            _pulsoidAuthCts.Cancel();
            _pulsoidAuthCts.Dispose();
            _pulsoidAuthCts = null;
        }

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

    [UIAction("AuthorizePulsoid")]
    private void AuthorizePulsoid()
    {
        if (_pulsoidAuthCts != null)
        {
            _pulsoidAuthCts.Cancel();
            _pulsoidAuthCts.Dispose();
            _pulsoidAuthCts = null;
        }

        _pulsoidAuthCts = new CancellationTokenSource();

        _authorizePulsoidBtn.interactable = false;
        Task.Run(async () =>
        {
            try
            {
                _pulsoidAuthenticator.Reset();
                var initiationResult = await _pulsoidAuthenticator.InitiateDeviceAuthorizationAsync(_pulsoidAuthCts.Token);
                if (initiationResult.Result != DeviceAuthInitiationResult.ResultType.Success)
                {
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

                var authResult = await _pulsoidAuthenticator.PollForTokenAsync(_pulsoidAuthCts.Token);
                if (authResult.Result == TokenPollResult.ResultType.Success && authResult.AccessToken != null)
                {
                    _logger.Notice("Pulsoid authorization successful");
                    _logger.Notice($"Pulsoid token: {authResult.AccessToken.Redact()}");
                    Config.PulsoidToken = authResult.AccessToken!;
                }
                else
                {
                    _logger.Warn(
                        $"Pulsoid authorization failed: {authResult.Result} {(string.IsNullOrWhiteSpace(authResult.Error) ? "" : $"({authResult.Error})")}");
                    if (authResult.Exception != null)
                    {
                        _logger.Warn(authResult.Exception);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error("Failed to authorize Pulsoid");
                _logger.Error(e);
            }
            finally
            {
                _ = UnityMainThreadTaskScheduler.Factory.StartNew(() => _authorizePulsoidBtn.interactable = true);
            }
        });
    }
}
