using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
        if (timeSpan.TotalDays < 1) return "1 day";

        var totalDays = (int)timeSpan.TotalDays;
        var years = totalDays / 365;
        totalDays -= years * 365;
        var months = totalDays / 30;
        var days = totalDays - months * 30;

        var yearsStr = years > 0 ? $"{years} years " : "";
        var monthsStr = months > 0 ? $"{months} months " : "";
        var daysStr = days > 0 ? $"{days} days" : "";

        return $"{yearsStr}{monthsStr}{daysStr}".TrimEnd();
    }

    [Conditional("DEBUG")]
    internal static void Spam(this Logger logger, string s)
    {
        logger.Trace(s);
    }
}
