using System;
using System.IO;
using System.Text;
using IPA.Logging;

namespace HRCounter.Data.Replay;

internal static class ReplayHRDataConverter
{
    public const string DataKey = "HeartBeatQuest";

    private static readonly Logger Logger = Plugin.GetChildLogger(nameof(ReplayHRDataConverter));

    public static ReplayHRData? Decode(byte[] data)
    {
#if DEBUG
        File.WriteAllBytes("debug_hrdata_parsed.bin", data);
#endif

        if (!BitConverter.IsLittleEndian)
        {
            // most systems are little endian nowadays
            Logger.Error("Big Endian systems are not supported");
            return null;
        }

        if (data.Length < 8)
        {
            Logger.Error("Data length is too short to contain valid HR data");
            return null;
        }

        var offset = 0;
        var version = DecodeInt32(data, ref offset);
        if (version != 1)
        {
            Logger.Error($"Unsupported HR data version: {version}");
            return null;
        }

        var entryCount = DecodeInt32(data, ref offset);
        if (data.Length < offset + entryCount * 8)
        {
            Logger.Error($"Data length is too short for {entryCount} entries");
            return null;
        }

        var entries = new ReplayHR[entryCount];
        for (var i = 0; i < entryCount; i++)
        {
            var songTime = DecodeFloat(data, ref offset);
            var heartRate = DecodeInt32(data, ref offset);
            entries[i] = new ReplayHR
            {
                SongTime = songTime,
                HeartRate = heartRate
            };
        }

        string? deviceName;
        if (data.Length < offset + 4)
        {
            Logger.Warn("Failed to read device name size, data might be corrupted");
            deviceName = "Unknown Device";
        }
        else if ((deviceName = DecodeString(data, ref offset)) == null)
        {
            deviceName = "Unknown Device";
            Logger.Warn("Failed to read device name, data might be corrupted");
        }

        string? hrAgent;
        if (data.Length < offset + 4)
        {
            // optional data
            hrAgent = "";
        }
        else if ((hrAgent = DecodeString(data, ref offset)) == null)
        {
            hrAgent = "Unknown";
            Logger.Warn("Failed to read hr agent, data might be corrupted");
        }

        return new ReplayHRData(entries, deviceName, hrAgent);
    }

    private static int DecodeInt32(byte[] data, ref int offset)
    {
        var result = BitConverter.ToInt32(data, offset);
        offset += 4;
        return result;
    }

    private static float DecodeFloat(byte[] data, ref int offset)
    {
        var result = BitConverter.ToSingle(data, offset);
        offset += 4;
        return result;
    }

    private static string? DecodeString(byte[] data, ref int offset)
    {
        var size = DecodeInt32(data, ref offset);
        if (size == 0) return "";
        if (data.Length < offset + size)
        {
            Logger.Warn("Data length is too short to decode string");
            return null;
        }

        string? result;
        try
        {
            result = Encoding.UTF8.GetString(data, offset, size);
            offset += size;
        }
        catch (Exception e)
        {
            result = null;
            Logger.Warn("Failed to decode string");
            Logger.Warn(e);
        }

        return result;
    }

    public static byte[] ToBytes(ReplayHRData data)
    {
        var deviceNameBytes = Encoding.UTF8.GetBytes(data.DeviceName);
        var hrAgentBytes = Encoding.UTF8.GetBytes(data.HRAgent);
        using var ms = new MemoryStream(4 + 4 + data.Count * 8 + 4 + deviceNameBytes.Length + 4 + hrAgentBytes.Length);
        // BinaryWriter is always little-endian
        using var writer = new BinaryWriter(ms, Encoding.UTF8, true);
        writer.Write(1); // version

        writer.Write(data.Count);
        foreach (var hrData in data)
        {
            writer.Write(hrData.SongTime);
            writer.Write(hrData.HeartRate);
        }

        writer.Write(deviceNameBytes.Length);
        writer.Write(deviceNameBytes);

        writer.Write(hrAgentBytes.Length);
        writer.Write(hrAgentBytes);

        writer.Flush();
        var result = ms.ToArray();
        Logger.Debug($"Replay HR data serialized, size: {result.Length}");

#if DEBUG
        File.WriteAllBytes("debug_hrdata_saved.bin", result);
#endif
        return result;
    }
}
