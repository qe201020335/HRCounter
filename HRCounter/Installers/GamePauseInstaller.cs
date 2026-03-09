using HRCounter.Configuration;
using IPA.Logging;
using Zenject;

namespace HRCounter.Installers;

public class GamePauseInstaller : Installer<GamePauseInstaller>
{
    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly Logger _logger = null!;

    public override void InstallBindings()
    {
        if (!_config.ModEnable)
        {
            return;
        }

        if (_config.AutoPause && !Utils.Utils.IsInReplay())
        {
            _logger.Debug("Binging game pause");
            Container
                .BindInterfacesTo<GamePauseController>()
                .AsSingle();
        }
    }
}
