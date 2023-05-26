using HRCounter.Configuration;
using HRCounter.Utils;
using SiraUtil.Logging;
using Zenject;

namespace HRCounter.Installers
{
    public class GamePauseInstaller : Installer<GamePauseInstaller>
    {
        [Inject] private readonly PluginConfig _config = null!;
        [Inject] private readonly SiraLog _logger = null!;

        public override void InstallBindings()
        {
            if (!_config.ModEnable)
            {
                return;
            }

            if (_config.AutoPause)
            {
                _logger.Debug("Binging game pause");
                Container.BindInterfacesAndSelfTo<GamePauseController>().AsSingle().NonLazy();
            }
        }
    }
}