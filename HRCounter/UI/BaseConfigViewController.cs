using System.ComponentModel;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using HRCounter.Configuration;
using IPA.Logging;
using IPA.Utilities.Async;
using Zenject;

namespace HRCounter.UI;

internal abstract class BaseConfigViewController : BSMLAutomaticViewController
{
    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    protected readonly PluginConfig Config = null!;

    protected bool Parse { get; private set; }

    [UIAction("#post-parse")]
    protected virtual void OnParsed()
    {
        Parse = true;
        RefreshUI();
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        _logger.Trace("DidActivate");
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

        if (!firstActivation)
        {
            // config might have changed while we were inactive
            RefreshUI(true);
        }

        Config.PropertyChanged += OnConfigChanged;
    }

    protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
    {
        _logger.Trace("DidDeactivate");
        Config.PropertyChanged -= OnConfigChanged;

        base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
    }

    private void RefreshUI(bool fullRefresh = false)
    {
        if (!Parse) return;
        if (fullRefresh) NotifyPropertyChanged(null);
        RefreshNoBindUI();
    }

    protected abstract void RefreshNoBindUI();

    private void OnConfigChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (!Parse) return;
        UnityMainThreadTaskScheduler.Factory.StartNew(() =>
        {
            NotifyPropertyChanged(args.PropertyName);
            if (string.IsNullOrEmpty(args.PropertyName))
            {
                RefreshUI();
            }
            else
            {
                OnConfigChanged(args.PropertyName);
            }
        });
    }

    protected abstract void OnConfigChanged(string propertyName);
}
