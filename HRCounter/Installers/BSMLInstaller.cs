using HRCounter.UI;
using Zenject;

namespace HRCounter.Installers;

public class BSMLInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<MainConfigMenu>().FromNewComponentAsViewController().AsSingle();
        Container.BindInterfacesAndSelfTo<ServiceStatusMenu>().FromNewComponentAsViewController().AsSingle();
        Container.BindInterfacesAndSelfTo<DataSourceMenu>().FromNewComponentAsViewController().AsSingle();
        Container.BindInterfacesAndSelfTo<ConfigViewFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
    }
}
