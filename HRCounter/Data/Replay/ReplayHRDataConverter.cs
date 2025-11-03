using System;
using System.IO;
using System.Text;
using HRCounter.Utils;
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

        if (data.Length < 8)
        {
            Logger.Error("Data length is too short to contain valid HR data");
            return null;
        }

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var version = reader.ReadInt32();
        if (version != 1)
        {
            Logger.Error($"Unsupported HR data version: {version}");
            return null;
        }

        var entryCount = reader.ReadInt32();
        if (data.Length < 8 + entryCount * 8)
        {
            Logger.Error($"Data length is too short for {entryCount} entries");
            return null;
        }

        var entries = new ReplayHR[entryCount];
        for (var i = 0; i < entryCount; i++)
        {
            var songTime = reader.ReadSingle();
            var heartRate = reader.ReadInt32();
            entries[i] = new ReplayHR
            {
                SongTime = songTime,
                HeartRate = heartRate
            };
        }

        string deviceName;
        int deviceNameSize;
        if (data.Length < 8 + entryCount * 8 + 4)
        {
            Logger.Warn("Failed to read device name size, data might be corrupted");
            deviceName = "Unknown Device";
        }
        else if ((deviceNameSize = reader.ReadInt32()) == 0)
        {
            deviceName = "";
        }
        else
        {
            try
            {
                var deviceNameBytes = reader.ReadBytes(deviceNameSize);
                deviceName = Encoding.UTF8.GetString(deviceNameBytes);
            }
            catch (Exception e)
            {
                deviceName = "Unknown Device";
                Logger.Warn("Failed to read device name, data might be corrupted");
                Logger.Warn(e);
            }
        }

        var result = new ReplayHRData(entries, deviceName);
        Logger.Debug($"Replay HR data device: {result.DeviceName}");
        Logger.Debug($"Replay HR data count: {result.Count}");
        Logger.Spam(string.Join(',', result));
        return result;
    }
}
