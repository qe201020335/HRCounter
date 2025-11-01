using System;
using System.IO;
using System.Text;
using IPA.Logging;

namespace HRCounter.Data.Replay;

internal static class ReplayHRDataConverter
{
    public const string DataKey = "HeartBeatQuest";

    private static Logger _logger = Plugin.GetChildLogger(nameof(ReplayHRDataConverter));

    public static ReplayHRData? Decode(byte[] data)
    {
#if DEBUG
        File.WriteAllBytes("debug_hrdata.bin", data);
#endif

        if (!BitConverter.IsLittleEndian)
        {
            // most systems are little endian nowadays
            _logger.Error("Big Endian systems are not supported");
            return null;
        }

        if (data.Length < 8)
        {
            _logger.Error("Data length is too short to contain valid HR data");
            return null;
        }

        var version = BitConverter.ToInt32(data, 0);
        if (version != 1)
        {
            _logger.Error($"Unsupported HR data version: {version}");
            return null;
        }

        var entryCount = BitConverter.ToInt32(data, 4);
        if (data.Length < 8 + entryCount * 8)
        {
            _logger.Error($"Data length is too short for {entryCount} entries");
            return null;
        }

        var entries = new ReplayHR[entryCount];
        for (var i = 0; i < entryCount; i++)
        {
            var offset = 8 + i * 8;
            var songTime = BitConverter.ToSingle(data, offset);
            var heartRate = BitConverter.ToInt32(data, offset + 4);
            entries[i] = new ReplayHR
            {
                SongTime = songTime,
                HeartRate = heartRate
            };
        }

        string deviceName;

        var deviceNameSizeOffset = 8 + entryCount * 8;
        if (data.Length < deviceNameSizeOffset + 4)
        {
            _logger.Warn("Failed to read device name size, data might be corrupted");
            deviceName = "Unknown Device";
        }
        else
        {
            var deviceNameSize = BitConverter.ToInt32(data, deviceNameSizeOffset);
            var deviceNameOffset = deviceNameSizeOffset + 4;
            try
            {
                deviceName = Encoding.UTF8.GetString(data, deviceNameOffset, deviceNameSize);
            }
            catch (Exception e)
            {
                deviceName = "Unknown Device";
                _logger.Warn("Failed to read device name, data might be corrupted");
                _logger.Warn(e);
            }
        }

        return new ReplayHRData(version, entries, deviceName);
    }
}
