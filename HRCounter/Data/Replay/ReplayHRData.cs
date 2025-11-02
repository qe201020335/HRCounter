using System;
using System.Collections;
using System.Collections.Generic;

namespace HRCounter.Data.Replay;

public class ReplayHRData : IEnumerable<ReplayHR>
{
    private readonly ReplayHR[] _data;
    public string DeviceName { get; }

    public int Count => _data.Length;

    internal ReplayHRData(ReplayHR[] hrData, string deviceName)
    {
        // Should I clone the arrays here to prevent external modification?
        Array.Sort(hrData);
        _data = hrData;
        DeviceName = deviceName;
    }

    public ReplayHR this[int index] => _data[index];

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<ReplayHR> GetEnumerator() => ((IEnumerable<ReplayHR>)_data).GetEnumerator();

    public int FindHeartRateAt(float time, out int foundIndex)
    {
        if (time <= _data[0].SongTime)
        {
            foundIndex = 0;
            return _data[0].HeartRate;
        }

        if (time >= _data[^1].SongTime)
        {
            foundIndex = _data.Length - 1;
            return _data[^1].HeartRate;
        }

        var index = Array.BinarySearch(_data, new ReplayHR { SongTime = time });
        if (index >= 0)
        {
            foundIndex = index;
            return _data[index].HeartRate;
        }

        // ~index is the first entry with SongTime > time
        // it will never be 0 or _data.Length here due to the earlier checks
        foundIndex = ~index - 1;
        return _data[foundIndex].HeartRate;
    }

    public int FindHeartRateAt(float time, int startIndex, out int foundIndex)
    {
        if (time > _data[^1].SongTime)
        {
            foundIndex = _data.Length - 1;
            return _data[^1].HeartRate;
        }

        var index = startIndex;
        // Find first entry with SongTime > time
        for (; index < _data.Length - 1 && _data[index].SongTime <= time; index++) ;
        foundIndex = index;
        return _data[foundIndex].HeartRate;
    }
}

public struct ReplayHR : IComparable<ReplayHR>
{
    public float SongTime;
    public int HeartRate;

    public override string ToString() => $"{HeartRate}@{SongTime:F1}";
    public int CompareTo(ReplayHR other) => SongTime.CompareTo(other.SongTime);
}
