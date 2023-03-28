using HRCounter.Configuration;
using HRCounter.Utils;
using Zenject;

namespace HRCounter.Installers
{
    public class GamePauseInstaller : Installer<GamePauseInstaller>
    {
        public override void InstallBindings()
        {
            if (!PluginConfig.Instance.ModEnable)
            {
                return;
            }
            if (PluginConfig.Instance.AutoPause)
            {
                Log.Logger.Debug("Binging game pause");
                Container.BindInterfacesAndSelfTo<GamePauseController>().AsSingle().NonLazy();
                Log.Logger.Debug("Game pause bound");
            }
        }
    }
}