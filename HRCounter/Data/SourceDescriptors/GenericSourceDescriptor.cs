using System;
using System.Threading.Tasks;

namespace HRCounter.Data;

public sealed class GenericSourceDescriptor<T>(string key, Func<Task<string>> getStatusText, Func<bool> precondition)
    : IDataSourceDescriptor where T : class, IHRDataSource
{
    public string Key { get; } = key;

    public Type DataSourceType { get; } = typeof(T);

    public bool StreamerMode { get; set; }

    public event EventHandler? StatusChanged;

    public override string ToString() => $"{Key} ({DataSourceType.Name})";

    public async Task<string> GetStatusText() => await getStatusText();

    public bool PreconditionMet() => precondition();
}
