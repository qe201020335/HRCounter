using System;
using System.Collections;
using HRCounter.Configuration;
using SiraUtil.Tools.SongControl;
using Zenject;

namespace HRCounter.Utils
{
    internal class GamePauseController : IDisposable
    {
        private static ISongControl _songControl;

        internal GamePauseController([InjectOptional] ISongControl songControl)
        {
            _songControl = songControl;
        }

        public void Dispose()
        {
            _songControl = null;
            Logger.logger.Debug("SongControl got yeeted.");
        }
        
        internal static void PauseGame()
        {
            if (!PluginConfig.Instance.ModEnable)
            {
                return;
            }
            if (!PluginConfig.Instance.AutoPause)
            {
                return;
            }

            if (_songControl != null && !_songControl.IsPaused)
            {
                // have to do this or some wierd stuff could be broken in some other pause menu related mods
                // for example "Fail Mod"
                SharedCoroutineStarter.instance.StartCoroutine(Pause());
            }
        }

        private static IEnumerator Pause()
        {
            yield return null;
            _songControl.Pause();
        }
    }
}