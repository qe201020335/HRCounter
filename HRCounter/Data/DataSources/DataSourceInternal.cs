using HRCounter.Configuration;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter.Data.DataSources
{
    internal abstract class DataSourceInternal : DataSource
    {
        protected static PluginConfig Config => PluginConfig.Instance;
        
        protected readonly IPALogger Logger = Log.Logger;

    }
}