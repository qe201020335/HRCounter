using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using HRCounter.Configuration;
using HRCounter.Utils;
using IPA.Utilities;
using TMPro;
using UnityEngine;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace HRCounter.UI;

// setting controller for menu button
[HotReload(RelativePathToLayout = @"BSML\configMenu.bsml")]
[ViewDefinition("HRCounter.UI.BSML.configMenu.bsml")]
internal class SettingMenuController : BaseConfigViewController
{
    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly IconManager _iconManager = null!;

    private readonly IList<string> _iconNames = new List<string>();

    private void UpdateColorText()
    {
        if (_colorVisualizerCoroutine == null)
        {
            _colorInfoText.text = DefaultColorText;
        }
    }

    protected override void OnConfigChanged(string propertyName)
    {
        UpdateColorText();
        switch (propertyName)
        {
            case nameof(Config.CustomIcon):
                UpdateCustomIconSelection();
                break;
        }
    }

    protected override void OnParsed()
    {
        base.OnParsed();
        _ = LoadIconList(false);
    }

    protected override void RefreshUI()
    {
        UpdateCustomIconSelection();
        UpdateColorText();
    }

    protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
    {
        if (_colorVisualizerCoroutine != null)
        {
            VisualizeColorsBtnPressed(); // stop the visualization and reset the text
        }

        base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
    }

    private string DefaultColorText =>
        $"<color=#{ColorUtility.ToHtmlStringRGBA(Config.LowColor)}>Low</color> -> <color=#{ColorUtility.ToHtmlStringRGBA(Config.MidColor)}>Middle</color> -> <color=#{ColorUtility.ToHtmlStringRGBA(Config.HighColor)}>High</color>";

    private IEnumerator VisualizeColorsCoroutine()
    {
        var start = Config.HRLow;

        while (isActivated)
        {
            _colorInfoText.text = $"<size=+5><color=#{ColorUtility.ToHtmlStringRGB(RenderUtils.DetermineColor(start))}>{start}</color></size>";
            start++;
            if (start > Config.HRHigh)
            {
                start = Config.HRLow;
            }

            yield return new WaitForSeconds(0.05f);
        }
    }

    private async Task LoadIconList(bool refresh)
    {
        if (!UnityGame.OnMainThread)
        {
            throw new InvalidOperationException("This method can only be called from the main thread.");
        }

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

    #region UIComponents

    [UIComponent("color-info-text")]
    private TMP_Text _colorInfoText = null!;


    [UIComponent("icon-list")]
    private CustomListTableData _iconList = null!;

    #endregion

    #region UIValues

    [UIValue("ModEnable")]
    private bool ModEnable
    {
        get => Config.ModEnable;
        set => Config.ModEnable = value;
    }

    [UIValue("Colorize")]
    public bool Colorize
    {
        get => Config.Colorize;
        set => Config.Colorize = value;
    }

    [UIValue("HRLow")]
    public int HRLow
    {
        get => Config.HRLow;
        set => Config.HRLow = value;
    }

    [UIValue("HRHigh")]
    public int HRHigh
    {
        get => Config.HRHigh;
        set => Config.HRHigh = value;
    }

    [UIValue("PauseHR")]
    public int PauseHR
    {
        get => Config.PauseHR;
        set => Config.PauseHR = value;
    }

    [UIValue("AutoPause")]
    public bool AutoPause
    {
        get => Config.AutoPause;
        set => Config.AutoPause = value;
    }

    [UIValue("IgnoreCountersPlus")]
    public bool IgnoreCountersPlus
    {
        get => Config.IgnoreCountersPlus;
        set => Config.IgnoreCountersPlus = value;
    }

    [UIValue("NoBloom")]
    private bool NoBloom
    {
        get => Config.NoBloom;
        set => Config.NoBloom = value;
    }

    [UIValue("IgnoreZeroValues")]
    public bool IgnoreZeroValues
    {
        get => Config.IgnoreZeroValues;
        set => Config.IgnoreZeroValues = value;
    }

    [UIValue("LowColor")]
    private Color LowColor
    {
        get => Config.LowColor;
        set => Config.LowColor = value;
    }

    [UIValue("MidColor")]
    private Color MidColor
    {
        get => Config.MidColor;
        set => Config.MidColor = value;
    }

    [UIValue("HighColor")]
    private Color HighColor
    {
        get => Config.HighColor;
        set => Config.HighColor = value;
    }

    private string _visualizeColorsBtnText = "Visualize";

    [UIValue("visualize-colors-btn-text")]
    private string VisualizeColorsBtnText
    {
        get => _visualizeColorsBtnText;
        set
        {
            _visualizeColorsBtnText = value;
            NotifyPropertyChanged();
        }
    }

    private bool _isIconListLoaded = false;

    [UIValue("is-icon-loaded")]
    private bool IsIconListLoaded
    {
        get => _isIconListLoaded;
        set
        {
            _isIconListLoaded = value;
            NotifyPropertyChanged();
        }
    }

    [UIValue("ReplayRecordHr")]
    public bool ReplayRecordHr
    {
        get => Config.ReplayRecordHr;
        set => Config.ReplayRecordHr = value;
    }

    [UIValue("ReplayPlaybackSelfHr")]
    public bool ReplayPlaybackSelfHr
    {
        get => Config.ReplayPlaybackSelfHr;
        set => Config.ReplayPlaybackSelfHr = value;
    }

    [UIValue("ReplayPlaybackOthersHr")]
    public bool ReplayPlaybackOthersHr
    {
        get => Config.ReplayPlaybackOthersHr;
        set => Config.ReplayPlaybackOthersHr = value;
    }

    [UIValue("ReplayFallbackLiveHr")]
    public bool ReplayFallbackLiveHr
    {
        get => Config.ReplayFallbackLiveHr;
        set => Config.ReplayFallbackLiveHr = value;
    }

    #endregion

    #region UIActions

    [UIAction("reset-low-color")]
    private void ResetLowColor()
    {
        Config.LowColor = PluginConfig.DefaultValues.LowColor;
    }

    [UIAction("reset-mid-color")]
    private void ResetMidColor()
    {
        Config.MidColor = PluginConfig.DefaultValues.MidColor;
    }

    [UIAction("reset-high-color")]
    private void ResetHighColor()
    {
        Config.HighColor = PluginConfig.DefaultValues.HighColor;
    }

    private Coroutine? _colorVisualizerCoroutine;

    [UIAction("visualize-colors-btn-pressed")]
    private void VisualizeColorsBtnPressed()
    {
        if (_colorVisualizerCoroutine != null)
        {
            StopCoroutine(_colorVisualizerCoroutine);
            _colorVisualizerCoroutine = null;
            _colorInfoText.text = DefaultColorText;
            VisualizeColorsBtnText = "Visualize";
        }
        else
        {
            _colorVisualizerCoroutine = StartCoroutine(VisualizeColorsCoroutine());
            VisualizeColorsBtnText = "Stop";
        }
    }

    [UIAction("refresh-counter-icon")]
    private void RefreshIconOptions()
    {
        _ = LoadIconList(true);
    }

    [UIAction("icon-selected")]
    private void CounterIconSelected(TableView view, int index)
    {
        if (view != _iconList.TableView) return;
        var iconName = _iconNames[index];
        _logger.Debug($"Selected icon {index} ({iconName})");
        Config.CustomIcon = iconName;
    }

    [UIAction("open-icons-folder")]
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
}
