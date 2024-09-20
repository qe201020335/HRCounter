using HMUI;
using Zenject;

namespace HRCounter.UI;

public class ConfigViewFlowCoordinator : FlowCoordinator
{
    [Inject]
    private readonly MainFlowCoordinator _mainFlowCoordinator = null!;

    [Inject]
    private readonly SettingMenuController _mainPanel = null!;
    
    [Inject]
    private readonly ServiceStatusViewController _serviceStatusView = null!;

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        if (firstActivation)
        {
            SetTitle("HR Counter");
            showBackButton = true;
            ProvideInitialViewControllers(_mainPanel, _serviceStatusView);
        }
    }

    protected override void BackButtonWasPressed(ViewController topController)
    {
        base.BackButtonWasPressed(topController);
        _mainFlowCoordinator.DismissFlowCoordinator(this);
    }
}
