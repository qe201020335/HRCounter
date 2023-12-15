using HRCounter.Configuration;
using HRCounter.Utils;
using Zenject;

namespace HRCounter.Installers
{
    public class AppInstaller: Installer<AppInstaller>
    {
        private readonly PluginConfig _config;

        private AppInstaller(PluginConfig config)
        {
            _config = config;
        }
        
        public override void InstallBindings()
        {
            Container.BindInstance(_config).AsSingle();
            Container.BindInterfacesAndSelfTo<AssetBundleManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<RenderUtils>().AsSingle().Lazy();
        }
    }
}