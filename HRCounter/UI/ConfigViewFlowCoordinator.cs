using HMUI;
using Zenject;

namespace HRCounter.UI;

public class ConfigViewFlowCoordinator : FlowCoordinator
{
    [Inject]
    private readonly MainFlowCoordinator _mainFlowCoordinator = null!;

    [Inject]
    private readonly SettingMenuController _mainPanel = null!;

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        if (firstActivation)
        {
            SetTitle("HR Counter");
            showBackButton = true;
            ProvideInitialViewControllers(_mainPanel);
        }
    }

    protected override void BackButtonWasPressed(ViewController topController)
    {
        base.BackButtonWasPressed(topController);
        _mainFlowCoordinator.DismissFlowCoordinator(this);
    }
}
