using System;
using System.Collections;
using HRCounter.Configuration;
using HRCounter.Data;
using SiraUtil.Logging;
using SiraUtil.Tools.SongControl;
using Zenject;

namespace HRCounter
{
    internal class GamePauseController : IInitializable, IDisposable
    {
        [Inject] private readonly PluginConfig _config = null!;
        [Inject] private readonly SiraLog _logger = null!;
        [InjectOptional] private readonly ISongControl? _songControl;
        [InjectOptional] private readonly HRDataManager? _hrDataManager;

        public void Initialize()
        {
            if (_hrDataManager != null)
            {
                _hrDataManager.OnHRUpdate -= OnHRUpdate;
                _hrDataManager.OnHRUpdate += OnHRUpdate;
            }
        }

        public void Dispose()
        {
            if (_hrDataManager != null)
            {
                _hrDataManager.OnHRUpdate -= OnHRUpdate;
            }
            _logger.Debug("SongControl got yeeted.");
        }

        private void OnHRUpdate(int hr)
        {
            if (hr >= _config.PauseHR)
            {
                _logger.Info("Heart Rate too high! Pausing!");
                PauseGame();
            }
        }
        
        internal void PauseGame()
        {
            if (_songControl != null && !_songControl.IsPaused)
            {
                // have to do this or some wierd stuff could be broken in some other pause menu related mods
                // for example "Fail Mod"
                SharedCoroutineStarter.instance.StartCoroutine(Pause());
            }
        }

        private IEnumerator Pause()
        {
            yield return null;
            _songControl?.Pause();
        }
    }
}