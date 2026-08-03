using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using BGLib.Polyglot;
using HRCounter.Configuration;
using HRCounter.Utils;

namespace HRCounter.Data.SourceDescriptors;

internal class SimpleSourceDescriptor<T> : IDisposableSourceDescriptor<T> where T : class, IHRDataSource
{
    private readonly PluginConfig _config;

    private readonly string _configPropertyName;

    private readonly string _labelKey;

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

    public SimpleSourceDescriptor(string key, string labelKey, Func<string?> getConfigValue, PluginConfig config, string configPropertyName)
    {
        _config = config;
        _labelKey = labelKey;
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
            StatusChanged?.Invoke();
        }
    }

    private static bool PreconditionMet(string? value) => !string.IsNullOrWhiteSpace(value) && value != "NotSet" && value != "-1";

    public Task<string> GetStatusText(CancellationToken cancellationToken)
    {
        var value = _getConfigValue();
        var displayValue = PreconditionMet(value)
            ? StreamerMode ? value?.Redact() : value
            : Localization.Get("HRCOUNTER_SIMPLE_SOURCE_DESCRIPTOR_VALUE_NOT_SET");
        var result = $"{Localization.Get(_labelKey)}: {displayValue}";
        return Task.FromResult(result);
    }

    public bool PreconditionMet()
    {
        var s = _getConfigValue();
        return PreconditionMet(s);
    }
}
