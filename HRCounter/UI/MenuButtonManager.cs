using BeatSaberMarkupLanguage.MenuButtons;
using Zenject;

namespace HRCounter.UI;

internal class MenuButtonManager : IInitializable
{
    [Inject]
    private readonly MainFlowCoordinator _mainFlowCoordinator = null!;

    [Inject]
    private readonly ConfigViewFlowCoordinator _configViewFlowCoordinator = null!;

    [Inject]
    private readonly MenuButtons _menuButtons = null!;

    private readonly MenuButton _menuButton;

    public MenuButtonManager()
    {
        _menuButton = new MenuButton("HRCounter", "Display your heart rate in game!", OnMenuButtonClick);
    }

    public void Initialize()
    {
        _menuButtons.RegisterButton(_menuButton);
    }

    private void OnMenuButtonClick()
    {
        _mainFlowCoordinator.PresentFlowCoordinator(_configViewFlowCoordinator);
    }
}
