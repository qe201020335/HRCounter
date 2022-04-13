using HRCounter.Configuration;
using HRCounter.Utils;
using Zenject;

namespace HRCounter.Installers
{
    public class GamePauseInstaller : Installer<GamePauseInstaller>
    {
        public override void InstallBindings()
        {
            if (PluginConfig.Instance.AutoPause)
            {
                Logger.logger.Debug("Binging game pause");
                Container.BindInterfacesAndSelfTo<GamePauseController>().AsSingle().NonLazy();
                Logger.logger.Debug("Game pause bound");
            }
        }
    }
}