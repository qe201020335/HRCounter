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

    /// <summary>
    ///     This is not a "real-time" system, do not raise this with high frequency
    /// </summary>
    event Action? StatusChanged;

    Task<string> GetStatusText(CancellationToken cancellationToken);

    bool PreconditionMet();
}

public interface IDataSourceDescriptor<T> : IDataSourceDescriptor where T : class, IHRDataSource
{
    /// <summary>
    ///     Default implementation returning the generic type parameter <typeparamref name="T"/>.
    ///     Do NOT re-implement. This guarantees <see cref="IDataSourceDescriptor.DataSourceType"/>
    ///     is always an <see cref="IHRDataSource"/>.
    /// </summary>
    Type IDataSourceDescriptor.DataSourceType => typeof(T);
}
