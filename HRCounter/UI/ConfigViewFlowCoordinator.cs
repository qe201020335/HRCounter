using BeatSaberMarkupLanguage.MenuButtons;
using HMUI;
using Zenject;

namespace HRCounter.UI;

public class ConfigViewFlowCoordinator : FlowCoordinator
{
    [Inject]
    private readonly MainFlowCoordinator _mainFlowCoordinator = null!;

    [Inject]
    private readonly MenuButtons _menuButtons = null!;

    [Inject]
    private readonly SettingMenuController _mainPanel = null!;
    
    [Inject]
    private readonly ServiceStatusViewController _serviceStatusView = null!;

    [Inject]
    private readonly DataSourceMenu _dataSourceMenu = null!;

    private readonly MenuButton _menuButton;

    public ConfigViewFlowCoordinator() => _menuButton = new MenuButton("HRCounter", "Display your heart rate in game!", OnMenuButtonClick);

    private void Start()
    {
        _menuButtons.RegisterButton(_menuButton);
    }

    private void OnMenuButtonClick()
    {
        _mainFlowCoordinator.PresentFlowCoordinator(this);
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        if (firstActivation)
        {
            SetTitle("HR Counter");
            showBackButton = true;
            ProvideInitialViewControllers(_mainPanel, _serviceStatusView, _dataSourceMenu);
        }
    }

    protected override void BackButtonWasPressed(ViewController topController)
    {
        base.BackButtonWasPressed(topController);
        _mainFlowCoordinator.DismissFlowCoordinator(this);
    }
}
