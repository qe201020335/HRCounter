using HRCounter.Configuration;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter
{
    internal static class Logger
    {
        internal static IPALogger logger { get; set; }

        internal static void DebugSpam(string s)
        {
#if DEBUG
            if (PluginConfig.Instance.DebugSpam)
            {
                logger.Debug(s);
            }   
#endif
        }
    }
}