using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using HRCounter.Configuration;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace HRCounter.UI;

[HotReload(RelativePathToLayout = @"BSML\configMenu.bsml")]
[ViewDefinition("HRCounter.UI.BSML.configMenu.bsml")]
internal class MainConfigMenu : BaseConfigViewController
{
    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly IconManager _iconManager = null!;

    [Inject]
    [UsedImplicitly]
    private void Init(AssetBundleManager abm, DiContainer di)
    {
        _logger.Trace("MainConfigMenu Init");
        SetupPreviewCounter(abm, di);
    }

    protected override void OnParsed()
    {
        base.OnParsed();
        _ = LoadIconList(false);
    }

    protected override void RefreshUI()
    {
        UpdateCustomIconSelection();
    }

    protected override void OnConfigChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(Config.CustomIcon):
                UpdateCustomIconSelection();
                break;
        }
    }

    #region General Tab

    [UIValue(nameof(Config.ModEnable))]
    public bool ModEnable
    {
        get => Config.ModEnable;
        set
        {
            if (Config.ModEnable != value) Config.ModEnable = value;
        }
    }

    [UIValue(nameof(Config.IgnoreCountersPlus))]
    public bool IgnoreCountersPlus
    {
        get => Config.IgnoreCountersPlus;
        set
        {
            if (Config.IgnoreCountersPlus != value) Config.IgnoreCountersPlus = value;
        }
    }

    [UIValue(nameof(Config.NoBloom))]
    public bool NoBloom
    {
        get => Config.NoBloom;
        set
        {
            if (Config.NoBloom != value) Config.NoBloom = value;
        }
    }

    [UIValue(nameof(Config.IgnoreZeroValues))]
    public bool IgnoreZeroValues
    {
        get => Config.IgnoreZeroValues;
        set
        {
            if (Config.IgnoreZeroValues != value) Config.IgnoreZeroValues = value;
        }
    }

    #endregion

    #region Icon Tab

    private readonly IList<string> _iconNames = new List<string>();

    [UIComponent("icon-list")]
    private CustomListTableData _iconList = null!;

    [UIValue(nameof(IsIconListLoaded))]
    public bool IsIconListLoaded
    {
        get;
        private set
        {
            field = value;
            NotifyPropertyChanged();
        }
    }

    private async Task LoadIconList(bool refresh)
    {
        _logger.Info("Loading custom icons");

        IsIconListLoaded = false;
        _iconNames.Clear();
        var icons = await _iconManager.GetIconsWithSpriteAsync(refresh);
        var data = new List<CustomListTableData.CustomCellInfo>(icons.Count + 1)
        {
            new("Default", null, _iconManager.DefaultIcon)
        };
        _iconNames.Add("");
        foreach (var (filename, sprite) in icons)
        {
            data.Add(new CustomListTableData.CustomCellInfo(filename, null, sprite));
            _iconNames.Add(filename);
        }

        _iconList.Data = data;
        _iconList.TableView.ReloadData();

        _logger.Debug($"Loaded {data.Count} custom icon options");

        UpdateCustomIconSelection();

        IsIconListLoaded = true;
    }

    private void UpdateCustomIconSelection()
    {
        var selected = _iconNames.IndexOf(Config.CustomIcon);
        if (selected < 0)
        {
            _iconList.TableView.ClearSelection();
            _iconList.TableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
        }
        else
        {
            _iconList.TableView.SelectCellWithIdx(selected);
            _iconList.TableView.ScrollToCellWithIdx(selected, TableView.ScrollPositionType.Center, false);
        }
    }

    [UIAction(nameof(OnIconListCellSelect))]
    [UsedImplicitly]
    private void OnIconListCellSelect(TableView view, int index)
    {
        if (view != _iconList.TableView) return;
        var iconName = _iconNames[index];
        _logger.Debug($"Selected icon {index} ({iconName})");
        Config.CustomIcon = iconName;
    }

    [UIAction(nameof(RefreshIconOptions))]
    [UsedImplicitly]
    private void RefreshIconOptions()
    {
        _ = LoadIconList(true);
    }

    [UIAction(nameof(OpenIconsFolder))]
    [UsedImplicitly]
    private void OpenIconsFolder()
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = _iconManager.IconDirPath,
            UseShellExecute = true,
            Verb = "open"
        };

        System.Diagnostics.Process.Start(psi);
    }

    #endregion

    #region Colorize Tab

    [UIValue(nameof(Config.Colorize))]
    public bool Colorize
    {
        get => Config.Colorize;
        set
        {
            if (Config.Colorize != value) Config.Colorize = value;
        }
    }

    [UIValue(nameof(Config.HRLow))]
    public int HRLow
    {
        get => Config.HRLow;
        set
        {
            if (Config.HRLow != value) Config.HRLow = value;
        }
    }

    [UIValue(nameof(Config.HRHigh))]
    public int HRHigh
    {
        get => Config.HRHigh;
        set
        {
            if (Config.HRHigh != value) Config.HRHigh = value;
        }
    }

    [UIValue(nameof(Config.LowColor))]
    public Color LowColor
    {
        get => Config.LowColor;
        set
        {
            if (Config.LowColor != value) Config.LowColor = value;
        }
    }

    [UIValue(nameof(Config.MidColor))]
    public Color MidColor
    {
        get => Config.MidColor;
        set
        {
            if (Config.MidColor != value) Config.MidColor = value;
        }
    }

    [UIValue(nameof(Config.HighColor))]
    public Color HighColor
    {
        get => Config.HighColor;
        set
        {
            if (Config.HighColor != value) Config.HighColor = value;
        }
    }

    [UIValue(nameof(PreviewCounter))]
    public Transform PreviewCounter { get; private set; } = null!;

    [UIAction(nameof(ResetLowColor))]
    [UsedImplicitly]
    private void ResetLowColor()
    {
        Config.LowColor = PluginConfig.DefaultValues.LowColor;
    }

    [UIAction(nameof(ResetMidColor))]
    [UsedImplicitly]
    private void ResetMidColor()
    {
        Config.MidColor = PluginConfig.DefaultValues.MidColor;
    }

    [UIAction(nameof(ResetHighColor))]
    [UsedImplicitly]
    private void ResetHighColor()
    {
        Config.HighColor = PluginConfig.DefaultValues.HighColor;
    }

    private void SetupPreviewCounter(AssetBundleManager abm, DiContainer di)
    {
        _logger.Debug("Setting up preview counter");
        var counter = abm.SetupCustomCounter(false);
        if (!counter.HasValue)
        {
            _logger.Warn("Failed to setup preview counter");
            var text = BeatSaberUI.CreateCurvedUIText(transform as RectTransform, "Failed to load preview counter");
            text.color = Color.red;
            PreviewCounter = text.transform;
            return;
        }

        var container = counter.Value.Container;
        PreviewCounter = container;
        container.SetParent(transform, false);
        Destroy(counter.Value.Canvas);
        container.gameObject.name = "HRCounter Preview Counter";
        container.localScale = Vector3.one / 12;

        var go = container.gameObject;
        go.SetActive(false);
        di.InstantiateComponent<HRCounterPreview>(go);
        go.SetActive(true);
    }

    #endregion

    #region Safety Tab

    [UIValue(nameof(Config.AutoPause))]
    public bool AutoPause
    {
        get => Config.AutoPause;
        set
        {
            if (Config.AutoPause != value) Config.AutoPause = value;
        }
    }

    [UIValue(nameof(Config.PauseHR))]
    public int PauseHR
    {
        get => Config.PauseHR;
        set
        {
            if (Config.PauseHR != value) Config.PauseHR = value;
        }
    }

    #endregion

    #region Replay Tab

    [UIValue(nameof(Config.ReplayRecordHr))]
    public bool ReplayRecordHr
    {
        get => Config.ReplayRecordHr;
        set
        {
            if (Config.ReplayRecordHr != value) Config.ReplayRecordHr = value;
        }
    }

    [UIValue(nameof(Config.ReplayPlaybackSelfHr))]
    public bool ReplayPlaybackSelfHr
    {
        get => Config.ReplayPlaybackSelfHr;
        set
        {
            if (Config.ReplayPlaybackSelfHr != value) Config.ReplayPlaybackSelfHr = value;
        }
    }

    [UIValue(nameof(Config.ReplayPlaybackOthersHr))]
    public bool ReplayPlaybackOthersHr
    {
        get => Config.ReplayPlaybackOthersHr;
        set
        {
            if (Config.ReplayPlaybackOthersHr != value) Config.ReplayPlaybackOthersHr = value;
        }
    }

    [UIValue(nameof(Config.ReplayFallbackLiveHr))]
    public bool ReplayFallbackLiveHr
    {
        get => Config.ReplayFallbackLiveHr;
        set
        {
            if (Config.ReplayFallbackLiveHr != value) Config.ReplayFallbackLiveHr = value;
        }
    }

    #endregion
}
