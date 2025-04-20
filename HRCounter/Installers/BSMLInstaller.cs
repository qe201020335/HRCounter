using HRCounter.UI;
using SiraUtil.Logging;
using Zenject;

namespace HRCounter.Installers;

public class BSMLInstaller : Installer
{
    [Inject]
    private readonly SiraLog _logger = null!;

    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<SettingMenuController>().FromNewComponentAsViewController().AsSingle();
        Container.BindInterfacesAndSelfTo<ConfigViewFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
        Container.BindInterfacesTo<MenuButtonManager>().AsSingle();
    }
}
