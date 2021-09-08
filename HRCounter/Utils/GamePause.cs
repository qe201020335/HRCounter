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
        }
        
        internal static void PauseGame()
        {
            if (!ReferenceEquals(_pauseController, null) && _pauseController.isActiveAndEnabled)
            {
                Logger.logger.Info("Pausing Game");
                _pauseController.Pause();
            }
        }
    }
}