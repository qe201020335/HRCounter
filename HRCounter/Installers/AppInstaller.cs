using HRCounter.Configuration;
using HRCounter.Web;
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
            
            // Web stuff
            Container.BindInterfacesTo<SimpleHttpServer>().AsSingle();
            Container.BindInterfacesTo<HttpConfigHandler>().AsSingle();
        }
    }
}