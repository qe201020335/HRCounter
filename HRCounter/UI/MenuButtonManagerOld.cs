using System;
using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaberMarkupLanguage;
using Zenject;

namespace HRCounter.UI
{
    public class MenuButtonManagerOld : IInitializable, IDisposable
    {
        private readonly MenuButton _menuButton;
        private readonly MainFlowCoordinator _mainFlowCoordinator;
        private readonly ConfigViewFlowCoordinator _configViewFlowCoordinator;

        public MenuButtonManagerOld(MainFlowCoordinator mainFlowCoordinator, ConfigViewFlowCoordinator configViewFlowCoordinator)
        {
            _mainFlowCoordinator = mainFlowCoordinator;
            _configViewFlowCoordinator = configViewFlowCoordinator;
            _menuButton = new MenuButton("HRCounter", "Display your heart rate in game!", OnMenuButtonClick);
        }

        public void Initialize()
        {
            MenuButtons.instance.RegisterButton(_menuButton);
        }

        public void Dispose()
        {
            if (MenuButtons.IsSingletonAvailable && BSMLParser.IsSingletonAvailable)
            {
                MenuButtons.instance.UnregisterButton(_menuButton);
            }
        }

        private void OnMenuButtonClick()
        {
            _mainFlowCoordinator.PresentFlowCoordinator(_configViewFlowCoordinator);
        }
    }
}