using HRCounter.UI;
using Zenject;

namespace HRCounter.Installers;

public class MenuInstaller : Installer<MenuInstaller>
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<SettingMenuController>().FromNewComponentAsViewController().AsSingle();
        Container.BindInterfacesAndSelfTo<ServiceStatusViewController>().FromNewComponentAsViewController().AsSingle();
        Container.BindInterfacesAndSelfTo<DataSourceMenu>().FromNewComponentAsViewController().AsSingle();
        Container.BindInterfacesAndSelfTo<ConfigViewFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
    }
}
