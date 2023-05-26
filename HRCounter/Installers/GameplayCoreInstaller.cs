using HRCounter.Configuration;
using SiraUtil.Logging;
using Zenject;

namespace HRCounter.Installers
{
    public class GameplayCoreInstaller : Installer<GameplayCoreInstaller>
    {
        [Inject] private readonly PluginConfig _config = null!;
        [Inject] private readonly SiraLog _logger = null!;
        [InjectOptional] private readonly GameplayCoreSceneSetupData? _sceneSetupData = null;
        private const string COUNTERS_PLUS_MOD_ID = "Counters+";

        public override void InstallBindings()
        {
            if (!_config.ModEnable)
            {
                return;
            }
            
            if (!_config.IgnoreCountersPlus && Utils.Utils.IsModEnabled(COUNTERS_PLUS_MOD_ID))
            {
                _logger.Info("Counters+ mod is enabled! Not binding!");
                return;
            }

            if (_sceneSetupData == null)
            {
                _logger.Warn("GameplayCoreSceneSetupData is null");
            }
            else if (_sceneSetupData.playerSpecificSettings.noTextsAndHuds)
            {
                _logger.Info("No Texts & HUDs");
            }
            else
            {
                _logger.Debug("Binding HR Counter");
                Container.BindInterfacesTo<HRCounterController>().AsSingle().NonLazy();
                _logger.Debug("HR Counter binded");
            }
        }
    }
}