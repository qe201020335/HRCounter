﻿using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HRCounter.Data;
using IPA.Config.Stores;
using Newtonsoft.Json;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace HRCounter.Configuration
{
    internal class PluginConfig
    {
        [Obsolete]
        public static PluginConfig Instance { get; set; }
        // Must be 'virtual' if you want BSIPA to detect a value change and save the config automatically.

        public virtual bool ModEnable { get; set; } = true;
        public virtual bool LogHR { get; set; } = false;

        public virtual string DataSource
        {
            get => _dataSourceStr;
            set => _dataSourceStr = DataSourceType.MigrateStr(value);
        }

        [JsonIgnore] private string _dataSourceStr = DataSourceType.YURMod.Str;
        public virtual string PulsoidToken { get; set; } = "";

        public virtual string HypeRateSessionID { get; set; } = "";

        public virtual string PulsoidWidgetID { get; set; } = "";

        public virtual string FitbitWebSocket { get; set; } = "";

        public virtual string HRProxyID { get; set; } = "";

        public virtual string FeedLink { get; set; } = "";


        public virtual bool NoBloom { get; set; } = false;

        public virtual bool Colorize { get; set; } = true;

        public virtual bool HideDuringReplay { get; set; } = true;

        public virtual int HRLow { get; set; } = 120;

        public virtual int HRHigh { get; set; } = 180;

        public virtual string LowColor { get; set; } = "#00FF00"; // default to green

        public virtual string MidColor { get; set; } = "#FFFF00"; // default to yellow

        public virtual string HighColor { get; set; } = "#FF0000"; // default to red

        public virtual int PauseHR { get; set; } = 200;

        public virtual bool AutoPause { get; set; } = false;

        public virtual bool IgnoreCountersPlus { get; set; } = false;

        public virtual bool DebugSpam { get; set; } = false;

        public virtual V3 StaticCounterPosition { get; set; } = new V3
        {
            x = 0f,
            y = 1.2f,
            z = 7f
        };

        internal event Action? OnSettingsChanged;

        public virtual void OnReload()
        {
            Log.Logger.Notice("HRCounter Settings Changed!");
            try
            {
                var e = OnSettingsChanged;
                Task.Factory.StartNew(() => { e?.Invoke(); });
            }
            catch (Exception e)
            {
                Log.Logger.Critical($"Exception Caught while broadcasting settings changed event: {e.Message}");
                Log.Logger.Critical(e);
            }
        }

        public struct V3
        {
            public float x;
            public float y;
            public float z;
        }
    }
}