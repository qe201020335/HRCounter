using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using HRCounter.Configuration;
using HRCounter.Data;
using IPA.Utilities.Async;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace HRCounter.UI;

[HotReload(RelativePathToLayout = @"BSML\dataSource.bsml")]
[ViewDefinition("HRCounter.UI.BSML.dataSource.bsml")]
internal class DataSourceMenu : BSMLAutomaticViewController
{
    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly Logger _logger = null!;

    private bool _parsed = false;

    [UIValue("DataSourceOptions")]
    public List<object> DataSourceOptions => [..DataSourceManager.DataSourceTypes.Keys];

    [UIValue("DataSource")]
    public string DataSource
    {
        get => _config.DataSource;
        set => _config.DataSource = value;
    }

    [UIValue("StreamerMode")]
    public bool StreamerMode
    {
        get => _config.StreamerMode;
        set => _config.StreamerMode = value;
    }

    [UIValue("AllowEdit")]
    public bool AllowEdit => !StreamerMode;

    [UIValue("HypeRateSessionID")]
    public string HypeRateSessionID
    {
        get => _config.HypeRateSessionID;
        set => _config.HypeRateSessionID = value;
    }

    [UIComponent("data-source-info-text")]
    private TextPageScrollView _dataSourceInfoText = null!;

    [UIComponent("data-source-info-refresh-btn")]
    private Button _dataSourceInfoRefreshBtn = null!;

    [UIComponent("hyperate-session-id-text")]
    private TMP_Text _HypeRateSessionIDText = null!;

    [UIAction("#post-parse")]
    private void OnParsed()
    {
        if (!_parsed)
        {
            ((RectTransform)gameObject.transform).offsetMax = new Vector2(0, 22);
        }

        _parsed = true;
        RefreshUI();
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        _logger.Trace("DataSourceMenu DidActivate");
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

        if (!firstActivation)
        {
            // config might have changed while we were inactive
            RefreshUI(true);
        }

        _config.PropertyChanged += OnConfigChanged;
    }

    protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
    {
        _logger.Trace("DataSourceMenu DidDeactivate");
        _config.PropertyChanged -= OnConfigChanged;

        base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
    }

    private void OnConfigChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (!_parsed) return;
        UnityMainThreadTaskScheduler.Factory.StartNew(() =>
        {
            NotifyPropertyChanged(args.PropertyName);
            switch (args.PropertyName)
            {
                case nameof(_config.DataSource):
                    UpdateDataSourceInfoText();
                    break;
                case nameof(_config.StreamerMode):
                    NotifyPropertyChanged(nameof(AllowEdit));
                    UpdateDataSourceInfoText();
                    UpdateHypeRateSessionIDText();
                    break;
                case nameof(_config.HypeRateSessionID):
                    UpdateHypeRateSessionIDText();
                    break;
                case "" or null:
                    RefreshUI();
                    break;
            }
        });
    }

    private void RefreshUI(bool fullRefresh = false)
    {
        if (!_parsed) return;
        if (fullRefresh) NotifyPropertyChanged(null);
        UpdateDataSourceInfoText();
        UpdateHypeRateSessionIDText();
    }

    [UIAction("UpdateDataSourceInfoText")]
    private void UpdateDataSourceInfoText()
    {
        var known = DataSourceManager.TryGetFromKey(_config.DataSource, out var source);
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
        _HypeRateSessionIDText.text = StreamerMode ? "********" : _config.HypeRateSessionID;
    }
}
