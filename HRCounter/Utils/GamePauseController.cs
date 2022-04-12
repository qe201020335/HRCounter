using System;
using HRCounter.Configuration;
using Zenject;

namespace HRCounter.Utils
{
    internal class GamePauseController : IDisposable
    {
        private static PauseController _pauseController;

        internal GamePauseController([InjectOptional] PauseController pauseController)
        {
            _pauseController = pauseController;
        }

        public void Dispose()
        {
            _pauseController = null;
        }
        
        internal static void PauseGame()
        {
            if (!PluginConfig.Instance.AutoPause)
            {
                return;
            }
            if (ReferenceEquals(_pauseController, null))
            {
                Logger.logger.Warn("Can't find game pause controller");
            } else if (_pauseController.isActiveAndEnabled)
            {
                Logger.logger.Info("Pausing Game");
                _pauseController.Pause();
            }
            else
            {
                Logger.logger.Warn("Pause controller is not active");
            }
        }
    }
}