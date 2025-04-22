using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using HRCounter.Configuration;
using IPA.Logging;

namespace HRCounter.Utils;

public static class Extensions
{
    // Can't name it Truncate, Beat Saber has the same extension in the global namespace
    internal static string TruncateW(this string s, int length = 30)
    {
        return s.Length <= length ? s : s.Substring(0, length) + "...";
    }

    internal static async Task<byte[]> ReadAllBytesAsync(this FileInfo file)
    {
        using var ms = new MemoryStream();
        using var stream = file.OpenRead();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }

    [Conditional("DEBUG")]
    internal static void Spam(this Logger logger, string s)
    {
        if (PluginConfig.Instance.DebugSpam)
        {
            logger.Trace(s);
        }
    }
}
