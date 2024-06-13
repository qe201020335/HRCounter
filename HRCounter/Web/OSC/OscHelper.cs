using System.Text;

namespace HRCounter.Web.OSC;

public class OSCHelper
{
    public static string? ReadString(byte[] data, ref int offset)
    {
        if (offset >= data.Length)
        {
            return null;
        }

        if (offset == data.Length - 1)
        {
            offset++;
            return string.Empty;
        }

        var builder = new StringBuilder(32); // should be enough
        // I want to use strncpy so bad :(
        for (; offset < data.Length && data[offset] != '\0'; offset++)
        {
            builder.Append((char)data[offset]);
        }
        
        offset = (offset / 4 + 1) * 4; // align to the start of next 4 bytes
        return builder.ToString();
    }
}