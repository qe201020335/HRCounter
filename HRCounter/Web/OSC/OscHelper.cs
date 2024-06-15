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

        var end = offset;
        // I want to use strncpy so bad :(
        for (; end < data.Length && data[end] != '\0'; end++)
        {
        }

        var count = end - offset;
        var s = Encoding.ASCII.GetString(data, offset, count);
        offset = (end / 4 + 1) * 4; // align to the start of next 4 bytes
        return s;
    }
    
    public static bool TryReadInt32(byte[] data, ref int offset, out int result)
    {
        if (offset + 4 > data.Length)
        {
            result = 0;
            return false;
        }

        result = data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3];
        offset += 4;
        return true;
    }
}