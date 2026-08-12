using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BGLib.Polyglot;
using IPA.Logging;

namespace HRCounter.Utils;

public static class Extensions
{
    // Can't name it Truncate, Beat Saber has the same extension in the global namespace
    internal static string TruncateW(this string s, int length = 30)
    {
        return s.Length <= length ? s : s.Substring(0, length) + "...";
    }

    internal static string Redact(this string s) => new('*', s.Length);

    internal static async Task<byte[]> ReadAllBytesAsync(this FileInfo file)
    {
        using var ms = new MemoryStream();
        using var stream = file.OpenRead();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }

    internal static string ToReadableString(this TimeSpan timeSpan)
    {
        var totalDays = (int)timeSpan.TotalDays;
        var years = totalDays / 365;
        var days = totalDays % 365;
        var hours = timeSpan.Hours;

        var yearsStr = years > 0 ? Localization.Instance.GetFormatOrKey("HRCOUNTER_COMMON_DURATION_YEARS", years) : "";
        var daysStr = days > 0 ? Localization.Instance.GetFormatOrKey("HRCOUNTER_COMMON_DURATION_DAYS", days) : "";
        var hoursStr = hours > 0 ? Localization.Instance.GetFormatOrKey("HRCOUNTER_COMMON_DURATION_HOURS", hours) : "";

        return $"{yearsStr}{daysStr}{hoursStr}".TrimEnd();
    }

    [Conditional("DEBUG")]
    internal static void Spam(this Logger logger, string s)
    {
        logger.Trace(s);
    }
}
