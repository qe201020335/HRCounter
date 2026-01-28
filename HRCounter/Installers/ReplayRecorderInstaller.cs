using HRCounter.Configuration;
using HRCounter.Data.Replay;
using Zenject;

namespace HRCounter.Installers;

public class ReplayRecorderInstaller : Installer
{
    [Inject]
    private readonly PluginConfig _config = null!;

    public override void InstallBindings()
    {
        if (!Utils.Utils.IsInReplay() && _config.ReplayRecordHr)
        {
            Container.BindInterfacesTo<ReplayHRRecorder>().AsSingle();
        }
    }
}
