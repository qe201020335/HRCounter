using System;
using BeatSaberMarkupLanguage.MenuButtons;
using SiraUtil.Logging;
using Zenject;

namespace HRCounter.UI;

public class MenuButtonManager : IInitializable, IDisposable
{
    private readonly MenuButton _menuButton;
    private readonly MainFlowCoordinator _mainFlowCoordinator;
    private readonly ConfigViewFlowCoordinator _configViewFlowCoordinator;
    private readonly MenuButtons? _menuButtons;

    [Inject]
    private readonly SiraLog _logger = null!;

    public MenuButtonManager(MainFlowCoordinator mainFlowCoordinator, ConfigViewFlowCoordinator configViewFlowCoordinator,
        [InjectOptional] MenuButtons? menuButtons)
    {
        _mainFlowCoordinator = mainFlowCoordinator;
        _configViewFlowCoordinator = configViewFlowCoordinator;
        _menuButtons = menuButtons;

        _menuButton = new MenuButton("HRCounter", "Display your heart rate in game!", OnMenuButtonClick);
    }

    public void Initialize()
    {
        if (_menuButtons != null)
        {
            _menuButtons.RegisterButton(_menuButton);
        }
        else
        {
            _logger.Warn("BSML MenuButtons is null");
        }
    }

    public void Dispose()
    {
        if (_menuButtons != null)
        {
            _menuButtons.UnregisterButton(_menuButton);
        }
        else
        {
            _logger.Warn("BSML MenuButtons is null");
        }
    }

    private void OnMenuButtonClick()
    {
        _mainFlowCoordinator.PresentFlowCoordinator(_configViewFlowCoordinator);
    }
}
