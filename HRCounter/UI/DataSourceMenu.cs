using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
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
internal class DataSourceMenu : BaseConfigViewController
{
    [Inject]
    private readonly Logger _logger = null!;

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
        if (!Parse)
        {
            ((RectTransform)gameObject.transform).offsetMax = new Vector2(0, 22);
        }

        base.OnParsed();
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
}
