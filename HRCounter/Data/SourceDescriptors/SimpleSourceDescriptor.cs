using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Utils;
using IPA.Utilities.Async;

namespace HRCounter.Data.SourceDescriptors;

internal class SimpleSourceDescriptor<T> : IDataSourceDescriptor<T> where T : class, IHRDataSource
{
    private readonly PluginConfig _config;

    private readonly string _configPropertyName;

    private readonly string _label;

    private readonly Func<string?> _getConfigValue;

    public string Key { get; }

    public bool StreamerMode
    {
        private get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            StatusChanged?.Invoke();
        }
    }

    public event Action? StatusChanged;

    public SimpleSourceDescriptor(string key, string label, Func<string?> getConfigValue, PluginConfig config, string configPropertyName)
    {
        _config = config;
        _label = label;
        _configPropertyName = configPropertyName;
        _getConfigValue = getConfigValue;
        Key = key;
        _config.PropertyChanged += OnConfigChanged;
    }

    public void Dispose()
    {
        _config.PropertyChanged -= OnConfigChanged;
    }

    private void OnConfigChanged(object? _, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == _configPropertyName)
        {
            UnityMainThreadTaskScheduler.Factory.StartNew(() => { StatusChanged?.Invoke(); });
        }
    }

    private static bool PreconditionMet(string? value) => !string.IsNullOrWhiteSpace(value) && value != "NotSet" && value != "-1";

    public Task<string> GetStatusText(CancellationToken cancellationToken)
    {
        var value = _getConfigValue();
        var result =
            $"{_label}: {(PreconditionMet(value) ? StreamerMode ? value?.Redact() : value : "Not Set")}";
        return Task.FromResult(result);
    }

    public bool PreconditionMet()
    {
        var s = _getConfigValue();
        return PreconditionMet(s);
    }
}
