using System;
using UnityEngine;

namespace HRCounter.Data.DataSources.DebugSource;

public class FrameRateHR : MonoBehaviour, IHRDataSource
{
    public event EventHandler<int>? HRDataReceived;

    public bool AllowReplayRecording => false;

    private void Update()
    {
        var fps = 1 / Time.deltaTime;
        HRDataReceived?.Invoke(this, (int)fps);
    }
}
