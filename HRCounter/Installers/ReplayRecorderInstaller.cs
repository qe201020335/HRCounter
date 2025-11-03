using HRCounter.Data.Replay;
using Zenject;

namespace HRCounter.Installers;

public class ReplayRecorderInstaller : Installer
{
    public override void InstallBindings()
    {
        if (!Utils.Utils.IsInReplay())
        {
            Container.BindInterfacesTo<ReplayHRRecorder>().AsSingle();
        }
    }
}
