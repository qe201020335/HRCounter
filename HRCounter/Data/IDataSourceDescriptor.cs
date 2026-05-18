using System;
using System.Threading;
using System.Threading.Tasks;

namespace HRCounter.Data;

/// <summary>
///     Don't implement this directly. Implement <see cref="IDataSourceDescriptor{T}" /> instead.
/// </summary>
public interface IDataSourceDescriptor : IDisposable
{
    string Key { get; }

    // mark internal so external data source descriptors cannot implement this interface directly
    internal Type DataSourceType { get; }

    /// <summary>
    ///     Setter will be called on the main thread
    /// </summary>
    bool StreamerMode { set; }

    event Action? StatusChanged;

    Task<string> GetStatusText(CancellationToken cancellationToken);

    bool PreconditionMet();
}

public interface IDataSourceDescriptor<T> : IDataSourceDescriptor where T : class, IHRDataSource
{
    Type IDataSourceDescriptor.DataSourceType => typeof(T);
}
