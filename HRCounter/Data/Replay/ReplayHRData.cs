namespace HRCounter.Data.Replay;

public class ReplayHRData
{
    public int Version { get; }
    public ReplayHR[] HRData { get; }
    public string DeviceName { get; }

    public ReplayHRData(int version, ReplayHR[] hrData, string deviceName)
    {
        Version = version;
        HRData = hrData;
        DeviceName = deviceName;
    }
}

public struct ReplayHR
{
    public float SongTime;
    public int HeartRate;

    public override string ToString() => $"{HeartRate}@{SongTime:F1}";
}
