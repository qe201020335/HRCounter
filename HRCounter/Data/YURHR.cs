using System;
using System.Collections;
using HRCounter.Configuration;
using UnityEngine;
using YUR.Fit.Core.Models;

namespace HRCounter.Data
{
    public static class YURData
    {
        public static int HR;

        public static void SetHR(float _hr)
        {
            HR = (int)_hr;
        }
    }
    
    public class ActivityViewControllerPatch
    {
        public static void OverlayUpdateAction(OverlayStatusUpdate status)
        {
            try
            {
                YURData.SetHR(status.HeartRate ?? 0);
            }
            catch(Exception e){}
        }
    }
    
    internal sealed class YURHR : BpmDownloader
    {
        private bool _logHr = PluginConfig.Instance.LogHR;
        
        internal YURHR() => RefreshSettings();

        private bool _updating = false;
        private Coroutine _coroutine = null;
        
        protected override void RefreshSettings()
        {
            _logHr = PluginConfig.Instance.LogHR;
        }

        internal override void Start()
        {
            _updating = true;
            _coroutine = SharedCoroutineStarter.instance.StartCoroutine(updater());
        }

        private IEnumerator updater()
        {
            while (_updating)
            {
                Bpm.Bpm = YURData.HR;
                yield return new WaitForSeconds(0.1f);
            }
        }

        internal override void Stop()
        {
            _updating = false;
            if(_coroutine != null)
                SharedCoroutineStarter.instance.StopCoroutine(_coroutine);
        }
    }
}