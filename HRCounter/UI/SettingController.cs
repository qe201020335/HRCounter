﻿using BeatSaberMarkupLanguage.Attributes;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.ViewControllers;
using HRCounter.Configuration;

namespace HRCounter.UI
{
    internal class SettingController : BSMLResourceViewController
    {
        public override string ResourceName => "HRCounter.UI.BSML.configMenu.bsml";

        [UIValue("LogHR")]
        public bool LogHR
        {
            get => PluginConfig.Instance.LogHR;
            set
            {
                PluginConfig.Instance.LogHR = value;
            }
        }
        [UIAction("SetLogHR")]
        public void SetLogHR(bool value)
        {
            LogHR = value;
        }
        
        //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////
        [UIValue("Colorize")]
        public bool Colorize
        {
            get => PluginConfig.Instance.Colorize;
            set
            {
                PluginConfig.Instance.Colorize = value;
            }
        }
        [UIAction("SetColorize")]
        public void SetColorize(bool value)
        {
            Colorize = value;
        }

        //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////
        [UIValue("HRLow")]
        public int HRLow
        {
            get => PluginConfig.Instance.HRLow;
            set
            {
                PluginConfig.Instance.HRLow = value;
            }
        }
        [UIAction("SetHRLow")]
        public void SetHRLow(int value)
        {
            HRLow = value;
        }

        //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////
        [UIValue("HRHigh")]
        public int HRHigh
        {
            get => PluginConfig.Instance.HRHigh;
            set
            {
                PluginConfig.Instance.HRHigh = value;
            }
        }
        [UIAction("SetHRHigh")]
        public void SetHRHigh(int value)
        {
            HRLow = value;
        }
        
        //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////
        [UIValue("HideDuringReplay")]
        public bool HideDuringReplay
        {
            get => PluginConfig.Instance.HideDuringReplay;
            set
            {
                PluginConfig.Instance.HideDuringReplay = value;
            }
        }
        
        //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////
        [UIValue("source-list-options")]
        public List<object> options = Utils.Utils.DataSources;

        [UIValue("source-list-choice")]
        public string listChoice
        {
            get => PluginConfig.Instance.DataSource;
            set
            {
                PluginConfig.Instance.DataSource = value;
            }
        }
        
        //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////
        [UIValue("PauseHR")]
        public int PauseHR
        {
            get => PluginConfig.Instance.PauseHR;
            set
            {
                PluginConfig.Instance.PauseHR = value;
            }
        }
        
        //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////
        [UIValue("AutoPause")]
        public bool AutoPause
        {
            get => PluginConfig.Instance.AutoPause;
            set
            {
                PluginConfig.Instance.AutoPause = value;
            }
        }
    }
}