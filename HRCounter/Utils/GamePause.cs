using System.Linq;
using UnityEngine;

namespace HRCounter.Utils
{
    internal static class GamePause
    {
        private static PauseController _pauseController;

        internal static void GameStart()
        {
            _pauseController = Resources.FindObjectsOfTypeAll<PauseController>().FirstOrDefault();
        }

        internal static void GameEnd()
        {
            _pauseController = null;
            // Currently reliant on Counters+, will be phased out later
            AssetBundleManager.ForceRemoveCanvas();
        }
        
        internal static void PauseGame()
        {
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