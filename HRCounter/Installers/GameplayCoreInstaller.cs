using HRCounter.Configuration;
using HRCounter.Utils;
using JetBrains.Annotations;
using Zenject;

namespace HRCounter.Installers
{
    public class GameplayCoreInstaller : Installer<GameplayCoreInstaller>
    {
        private readonly GameplayCoreSceneSetupData? _sceneSetupData;
        private const string COUNTERS_PLUS_MOD_ID = "Counters+";
        public GameplayCoreInstaller([InjectOptional] GameplayCoreSceneSetupData gameplayCoreSceneSetupData)
        {
            _sceneSetupData = gameplayCoreSceneSetupData;
            Log.Logger.Debug("Installer ctor");
        }

        public override void InstallBindings()
        {
            if (!PluginConfig.Instance.ModEnable)
            {
                return;
            }
            if (!PluginConfig.Instance.IgnoreCountersPlus && Utils.Utils.IsModEnabled(COUNTERS_PLUS_MOD_ID))
            {
                Log.Logger.Info("Counters+ mod is enabled! Not binding!");
                return;
            }

            if (_sceneSetupData == null)
            {
                Log.Logger.Warn("GameplayCoreSceneSetupData is null");
            }
            else if (_sceneSetupData.playerSpecificSettings.noTextsAndHuds)
            {
                Log.Logger.Info("No Texts & HUDs");
            }
            else
            {
                Log.Logger.Debug("Binding HR Counter");
                Container.BindInterfacesTo<HRCounterController>().AsSingle().NonLazy();
                Log.Logger.Debug("HR Counter binded");
            }
        }
    }
}