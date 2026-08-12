using System;
using System.Threading;
using System.Threading.Tasks;

namespace HRCounter.Data;

/// <summary>
///     Don't implement this directly. Implement <see cref="IDataSourceDescriptor{T}" /> or <see cref="IDisposableSourceDescriptor{T}"/> instead.
/// </summary>
public interface IDataSourceDescriptor
{
    string Key { get; }

    /// <summary>
    ///     The Display name of the data source, will use <see cref="Key" /> if null or empty string
    /// </summary>
    string? Name { get; }

    /// <summary>
    ///     Polyglot key for the display name of the data source.
    ///     When provided, <see cref="Name" /> will be ignored.
    ///     Return null or empty string if localization is not available
    /// </summary>
    string? NameKey { get; }

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

/// <summary>
///     A data source descriptor interface.
///     An implementation will be instantiated by Zenject so dependencies can be injected.
///     If it needs disposing resources, implement <see cref="IDisposableSourceDescriptor{T}" /> instead.
/// </summary>
/// <typeparam name="T">Type of the HR data source</typeparam>
public interface IDataSourceDescriptor<T> : IDataSourceDescriptor where T : class, IHRDataSource
{
    /// <summary>
    ///     Default implementation returning the generic type parameter <typeparamref name="T"/>.
    ///     Do NOT re-implement. This guarantees <see cref="IDataSourceDescriptor.DataSourceType"/>
    ///     is always an <see cref="IHRDataSource"/>.
    /// </summary>
    Type IDataSourceDescriptor.DataSourceType => typeof(T);
}

/// <summary>
///     A disposable data source descriptor.
///     The same as <see cref="IDataSourceDescriptor{T}" /> but also implements <see cref="IDisposable" />.
/// </summary>
/// <typeparam name="T">Type of the HR data source</typeparam>
public interface IDisposableSourceDescriptor<T> : IDataSourceDescriptor<T>, IDisposable where T : class, IHRDataSource;
