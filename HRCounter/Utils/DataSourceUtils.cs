using System.Diagnostics;

namespace HRCounter.Utils;

public static class DataSourceUtils
{
    internal const string YUR_MOD_ID = "YUR Fit Calorie Tracker";

    internal static bool CheckYURProcess()
    {
        var processes = Process.GetProcessesByName("YUR.Fit.Windows.Service");
        return processes.Length > 0;
    }
}
