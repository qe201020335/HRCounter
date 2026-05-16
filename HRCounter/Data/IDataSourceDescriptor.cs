using System;
using System.Threading.Tasks;

namespace HRCounter.Data;

public interface IDataSourceDescriptor
{
    string Key { get; }

    Type DataSourceType { get; }

    bool StreamerMode { set; }

    event EventHandler? StatusChanged;

    Task<string> GetStatusText();

    bool PreconditionMet();
}
