using System;
using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaberMarkupLanguage;
using Zenject;

namespace HRCounter.UI
{
    public class MenuButtonManager : IInitializable, IDisposable
    {
        private readonly MenuButton _menuButton;
        private readonly MainFlowCoordinator _mainFlowCoordinator;
        private readonly ConfigViewFlowCoordinator _configViewFlowCoordinator;
        private readonly MenuButtons _menuButtons;

        public MenuButtonManager(MainFlowCoordinator mainFlowCoordinator, ConfigViewFlowCoordinator configViewFlowCoordinator, MenuButtons menuButtons)
        {
            _mainFlowCoordinator = mainFlowCoordinator;
            _configViewFlowCoordinator = configViewFlowCoordinator;
            _menuButtons = menuButtons;
            _menuButton = new MenuButton("HRCounter", "Display your heart rate in game!", OnMenuButtonClick);
        }

        public void Initialize()
        {
            _menuButtons.RegisterButton(_menuButton);
        }

        public void Dispose()
        {
            _menuButtons.UnregisterButton(_menuButton);
        }

        private void OnMenuButtonClick()
        {
            _mainFlowCoordinator.PresentFlowCoordinator(_configViewFlowCoordinator);
        }
    }
}