using BeatSaberMarkupLanguage;
using HMUI;
using HRCounter.Configuration;
using System;

namespace HRCounter.UI
{
    public class ConfigViewFlowCoordinator : FlowCoordinator
    {
        private SettingMenuController _mainPanel = null!;
        public void Awake()
        {
            if (_mainPanel == null)
            {
                _mainPanel = BeatSaberUI.CreateViewController<SettingMenuController>();
            }
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            try
            {
                if (firstActivation)
                {
                    SetTitle("HR Counter");
                    showBackButton = true;
                }

                if (addedToHierarchy)
                {
                    ProvideInitialViewControllers(_mainPanel);
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error(e);
                throw e;
            }
        }
        
        protected override void BackButtonWasPressed(ViewController topController)
        {
            base.BackButtonWasPressed(topController);
            BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this);
        }
        
    }
}