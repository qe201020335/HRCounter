namespace HRCounter.Utils
{
    public static class Extensions
    {
        internal static string CondTrunc(this string s, int len)
        {
            return s.Length <= len ? s : s.Substring(0, len) + "...";
        }
    }
}