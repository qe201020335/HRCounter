using HRCounter.Configuration;

namespace HRCounter.Data.DataSources
{
    internal abstract class DataSourceInternal : DataSource
    {
        protected static PluginConfig Config => PluginConfig.Instance;
    }
}