using HRCounter.Configuration;
using HRCounter.Utils;
using JetBrains.Annotations;
using Zenject;

namespace HRCounter.Installers
{
    public class GameplayCoreInstaller : Installer<GameplayCoreInstaller>
    {
        [CanBeNull] private readonly GameplayCoreSceneSetupData _sceneSetupData;

        public GameplayCoreInstaller([InjectOptional] GameplayCoreSceneSetupData gameplayCoreSceneSetupData)
        {
            _sceneSetupData = gameplayCoreSceneSetupData;
        }

        public override void InstallBindings()
        {
            if (!PluginConfig.Instance.IgnoreCountersPlus && Utils.Utils.IsModEnabled("Counters+"))
            {
                Logger.logger.Info("Counters+ mod is enabled! Not binding!");
                return;
            }

            if (_sceneSetupData == null)
            {
                Logger.logger.Warn("GameplayCoreSceneSetupData is null");
            }
            else if (_sceneSetupData.playerSpecificSettings.noTextsAndHuds)
            {
                Logger.logger.Info("No Texts & HUDs");
            }
            else
            {
                Logger.logger.Debug("Binding HR Counter");
                Container.BindInterfacesTo<HRCounterController>().AsSingle().NonLazy();
                Logger.logger.Debug("HR Counter binded");

            }
        }
    }
}