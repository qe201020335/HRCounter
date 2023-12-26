using Hive.Versioning;
using HRCounter.UI;
using SiraUtil.Logging;
using Zenject;

namespace HRCounter.Installers
{
    public class BSMLInstaller : Installer
    {
        
        private readonly Version _bsmlDiVersion = new Version(1, 7, 5);
        
        [Inject] 
        private readonly SiraLog _logger = null!; 
        
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<SettingMenuController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<ConfigViewFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
            
            if (Plugin.BSMLMeta!.HVersion < _bsmlDiVersion)
            {
                _logger.Debug("Older BSML encountered, using old MenuButtonManager");
                Container.BindInterfacesTo<MenuButtonManagerOld>().AsSingle();
            }
            else
            {
                Container.BindInterfacesTo<MenuButtonManager>().AsSingle();
            }
        }
    }
}