using System;
using System.Buffers.Binary;

namespace HRCounter.Web.OSC;

internal static class OSCHelper
{
    public static bool TryReadString(ref ReadOnlySpan<byte> data, out ReadOnlySpan<byte> value)
    {
        // null-terminated string
        var end = data.IndexOf((byte)0);
        if (end < 0)
        {
            value = default;
            return false;
        }

        value = data[..end];
        // pad to 4 bytes
        data = data[((end & ~3) + 4)..];
        return true;
    }

    public static bool TryReadInt32(ref ReadOnlySpan<byte> data, out int result)
    {
        if (data.Length < 4)
        {
            result = 0;
            return false;
        }

        result = BinaryPrimitives.ReadInt32BigEndian(data);
        data = data[4..];
        return true;
    }
}
