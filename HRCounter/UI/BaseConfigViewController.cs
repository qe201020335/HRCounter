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
    [Inject(Id = typeof(BaseConfigViewController))]
    private readonly Logger _logger = null!;

    [Inject]
    protected readonly PluginConfig Config = null!;

    protected bool Parsed { get; private set; }

    [UIAction("#post-parse")]
    protected virtual void OnParsed()
    {
        Parsed = true;
        RefreshUI(false);
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

    private void RefreshUI(bool notifyAll)
    {
        if (!Parsed) return;
        if (notifyAll) NotifyPropertyChanged(null);
        RefreshNoBindUI();
    }

    /**
     * Refreshes UI elements that doesn't have value binding.
     */
    protected abstract void RefreshNoBindUI();

    private void OnConfigChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (!Parsed) return;
        UnityMainThreadTaskScheduler.Factory.StartNew(() =>
        {
            NotifyPropertyChanged(args.PropertyName);
            if (string.IsNullOrEmpty(args.PropertyName))
            {
                RefreshUI(false);
            }
            else
            {
                OnConfigChanged(args.PropertyName);
            }
        });
    }

    /**
     * Called when a config property changes. Only called for the changed property, not for all properties.
     * NotifyPropertyChanged is already called for the changed property at this point.
     */
    protected abstract void OnConfigChanged(string propertyName);
}
