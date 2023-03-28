namespace HRCounter.Utils
{
    public static class Extensions
    {
                
        // Can't name it Truncate, Beat Saber has the same extension in the global namespace
        internal static string TruncateW(this string s, int length = 30)
        {
            return s.Length <= length ? s : s.Substring(0, length) + "...";
        }
    }
}