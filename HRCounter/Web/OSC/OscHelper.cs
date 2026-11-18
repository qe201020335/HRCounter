using System;
using System.Buffers.Binary;
using JetBrains.Annotations;

namespace HRCounter.Web.OSC;

/// <summary>
///     Helpers for reading OSC data. Each one advances <c>data</c> past the value it reads.
/// </summary>
public static class OscHelper
{
    /// <summary>
    ///     Reads a null-terminated, 4-byte padded OSC string.
    ///     The length of <c>data</c> must be a multiple of 4, or the padding skip can go out of range.
    /// </summary>
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

    /// <summary>
    ///     Reads a big-endian OSC int32.
    /// </summary>
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
