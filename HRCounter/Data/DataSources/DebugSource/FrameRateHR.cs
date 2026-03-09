using System;
using UnityEngine;

namespace HRCounter.Data.DataSources.DebugSource;

public class FrameRateHR : MonoBehaviour, IHRDataSource
{
    public event EventHandler<HRDataReceivedEventArgs>? OnHRDataReceived;

    public bool AllowReplayRecording => false;

    private void Update()
    {
        var fps = 1 / Time.deltaTime;
        OnHRDataReceived?.Invoke(this, new HRDataReceivedEventArgs((int)fps));
    }
}
