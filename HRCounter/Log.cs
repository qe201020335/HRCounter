using HRCounter.Configuration;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter
{
    internal static class Log
    {
        internal static IPALogger Logger { get; set; } = null!;

        internal static void DebugSpam(string s)
        {
#if DEBUG
            if (PluginConfig.Instance.DebugSpam)
            {
                Logger.Debug(s);
            }   
#endif
        }
    }
}