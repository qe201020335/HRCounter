using System;
using HRCounter.UI;
using Zenject;

namespace HRCounter.Installers
{
    public class MenuInstaller: Installer<MenuInstaller>
    {
        private static readonly Type? MenuControllerType = Type.GetType("HRCounter.UI.SettingMenuController");
        public override void InstallBindings()
        {
            if (Utils.Utils.IsModEnabled("BeatSaberMarkupLanguage"))
            {
                Container.BindInterfacesAndSelfTo(MenuControllerType!).FromNewComponentAsViewController().AsSingle();
                Container.BindInterfacesAndSelfTo<ConfigViewFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
                Container.BindInterfacesAndSelfTo<MenuButtonManager>().AsSingle();
            }
        }
    }
}