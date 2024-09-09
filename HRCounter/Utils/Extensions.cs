using System.IO;
using System.Threading.Tasks;

namespace HRCounter.Utils
{
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
    }
}