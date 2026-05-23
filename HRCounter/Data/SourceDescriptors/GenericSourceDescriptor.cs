using System;
using System.Threading;
using System.Threading.Tasks;

namespace HRCounter.Data.SourceDescriptors;

internal sealed class GenericSourceDescriptor<T>(string key, Func<CancellationToken, Task<string>> getStatusText, Func<bool> precondition)
    : IDataSourceDescriptor<T> where T : class, IHRDataSource
{
    public string Key { get; } = key;

    public bool StreamerMode
    {
        // do nothing
        set { }
    }

    // unused
    public event Action? StatusChanged
    {
        add { }
        remove { }
    }

    public async Task<string> GetStatusText(CancellationToken cancellationToken) => await getStatusText(cancellationToken);

    public bool PreconditionMet() => precondition();
}
