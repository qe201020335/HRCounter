using System;
using HRCounter.Configuration;
using HRCounter.Data;
using SiraUtil.Tools.SongControl;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace HRCounter;

internal class GamePauseController : IInitializable, IDisposable
{
    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly Logger _logger = null!;

    [InjectOptional]
    private readonly ISongControl? _songControl = null;

    [InjectOptional]
    private readonly HRDataManager? _hrDataManager = null;

    public void Initialize()
    {
        if (_hrDataManager != null && _songControl != null)
        {
            _hrDataManager.OnHRUpdate += OnHRUpdate;
        }
    }

    public void Dispose()
    {
        if (_hrDataManager != null)
        {
            _hrDataManager.OnHRUpdate -= OnHRUpdate;
        }

        _logger.Debug("GamePauseController got yeeted.");
    }

    private void OnHRUpdate(int hr)
    {
        if (hr >= _config.PauseHR && _songControl?.IsPaused == false)
        {
            _logger.Info("Heart Rate too high! Pausing!");
            _songControl?.Pause();
        }
    }
}
