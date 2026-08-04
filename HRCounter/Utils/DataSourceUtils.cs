using System.Diagnostics;
using BGLib.Polyglot;
using HRCounter.Data;

namespace HRCounter.Utils;

public static class DataSourceUtils
{
    internal const string YUR_MOD_ID = "YUR Fit Calorie Tracker";

    internal static bool CheckYURProcess()
    {
        var processes = Process.GetProcessesByName("YUR.Fit.Windows.Service");
        return processes.Length > 0;
    }

    /// <summary>
    ///     Resolves the display name of a data source: localized <see cref="IDataSourceDescriptor.NameKey" />
    ///     if set, else <see cref="IDataSourceDescriptor.Name" />, else <see cref="IDataSourceDescriptor.Key" />.
    /// </summary>
    internal static string GetDisplayName(this IDataSourceDescriptor descriptor)
    {
        if (!string.IsNullOrEmpty(descriptor.NameKey))
        {
            return Localization.Get(descriptor.NameKey!);
        }

        if (!string.IsNullOrEmpty(descriptor.Name))
        {
            return descriptor.Name!;
        }

        return descriptor.Key;
    }
}
