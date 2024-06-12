using System;
using System.Collections;
using UnityEngine;

namespace HRCounter.Data.DataSources.DebugSource
{
    public class FrameRateHR: MonoBehaviour, IHRDataSource
    {
        public event EventHandler<HRDataReceivedEventArgs>? OnHRDataReceived;
        
        private readonly float _sampleInterval = .5f;

        private void Update()
        {
            var fps = 1 / Time.deltaTime;
            OnHRDataReceived?.Invoke(this, new HRDataReceivedEventArgs((int) fps));
        }
    }
}