using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Data.DataSources.DebugSource;
using UnityEngine;
using Zenject;

namespace HRCounter.Data.SourceDescriptors.DebugDescriptor;

public class FrameRateDescriptor : MonoBehaviour, IDataSourceDescriptor<FrameRateHR>
{
    private const string KEY = "FPS Debug";

    [Inject]
    private readonly PluginConfig _config = null!;

    public string Key => KEY;

    public bool StreamerMode
    {
        set { }
    }

    public event Action? StatusChanged;

    private float _fps;
    private float _time;

    private void Start()
    {
        enabled = _config.DataSource == KEY;
        _config.PropertyChanged += OnConfigChanged;
    }

    private void Update()
    {
        var fps = 1 / Time.smoothDeltaTime;

        if (Mathf.Abs(fps - _fps) > 1 && Time.time - _time > 1.5)
        {
            _fps = fps;
            _time = Time.time;
            StatusChanged?.Invoke();
        }
    }

    private void OnDestroy()
    {
        _config.PropertyChanged -= OnConfigChanged;
    }

    private void OnConfigChanged(object? _, PropertyChangedEventArgs e)
    {
        enabled = _config.DataSource == KEY;
    }

    public Task<string> GetStatusText(CancellationToken cancellationToken) => Task.FromResult($"FPS: {_fps:F1}");

    public bool PreconditionMet() => true;
}
