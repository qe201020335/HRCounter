using SiraUtil.Logging;
using Zenject;

namespace HRCounter.Installers
{
    public class MenuInstaller: Installer<MenuInstaller>
    {
        [Inject] private readonly SiraLog _logger = null!; 
        
        public override void InstallBindings()
        {
            Container.Install<BSMLInstaller>();
        }
    }
}