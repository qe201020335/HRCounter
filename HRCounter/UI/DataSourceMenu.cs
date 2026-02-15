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

    [UIComponent("data-source-info-text")]
    private TextPageScrollView _dataSourceInfoText = null!;

    [UIComponent("data-source-info-refresh-btn")]
    private Button _dataSourceInfoRefreshBtn = null!;

    private string _previousDataSource = "";

    [UIAction("#post-parse")]
    private void OnParsed()
    {
        if (!_parsed)
        {
            ((RectTransform)gameObject.transform).offsetMax = new Vector2(0, 22);
        }

        _parsed = true;
        UpdateDataSourceInfoText();
        _previousDataSource = _config.DataSource;
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        _logger.Trace("DataSourceMenu DidActivate");
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

        if (!firstActivation)
        {
            // config might have changed while we were inactive
            RefreshConfigUi();
        }

        _config.PropertyChanged += OnConfigChanged;
    }

    protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
    {
        _logger.Trace("DataSourceMenu DidDeactivate");
        _config.PropertyChanged -= OnConfigChanged;
        _previousDataSource = ""; // force a refresh next time we activate

        base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
    }

    private void OnConfigChanged(object? sender, PropertyChangedEventArgs args)
    {
        // TODO change only needed values
        UnityMainThreadTaskScheduler.Factory.StartNew(RefreshConfigUi);
    }

    private void RefreshConfigUi()
    {
        _logger.Trace("DataSourceMenu RefreshConfigUi");
        NotifyPropertyChanged(string.Empty); // refresh all of them

        if (_previousDataSource != _config.DataSource)
        {
            _previousDataSource = _config.DataSource;
            UpdateDataSourceInfoText();
        }
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
        UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
        {
            _dataSourceInfoRefreshBtn.interactable = false;
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
}
