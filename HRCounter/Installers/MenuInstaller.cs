using SiraUtil.Logging;
using Zenject;

namespace HRCounter.Installers
{
    public class MenuInstaller: Installer<MenuInstaller>
    {
        [Inject] private readonly SiraLog _logger = null!; 
        
        public override void InstallBindings()
        {
            if (Plugin.BSMLMeta != null)
            {
                _logger.Debug("BSML is installed, installing the menus");
                Container.Install<BSMLInstaller>();
            }
            else
            {
                _logger.Warn("BSML is not installed, not installing the menus");
            }
        }
    }
}