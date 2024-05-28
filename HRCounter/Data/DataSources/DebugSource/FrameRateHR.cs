using System;
using System.Collections;
using UnityEngine;

namespace HRCounter.Data.DataSources.DebugSource
{
    public class FrameRateHR: MonoBehaviour, IHRDataSource
    {
        public event EventHandler<HRDataReceivedEventArgs>? OnHRDataReceived;
        
        private readonly float _sampleInterval = .5f;

        private void OnEnable()
        {
            StartCoroutine(FrameTimeUpdate());
        }
        
        private IEnumerator FrameTimeUpdate()
        {
            while (enabled)
            {
                var startFrameCount = Time.frameCount;
                yield return new WaitForSecondsRealtime(_sampleInterval);
                var endFrameCount = Time.frameCount;
                var frames = endFrameCount - startFrameCount;
                OnHRDataReceived?.Invoke(this, new HRDataReceivedEventArgs( (int)(frames / _sampleInterval)));
            }
        }
    }
}