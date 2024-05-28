using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HRCounter.Data;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using JetBrains.Annotations;
using UnityEngine;

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
            set => _dataSourceStr = DataSourceManager.MigrateKey(value);
        }

        [Ignore]
        private string _dataSourceStr = DataSourceManager.YURMod.Key;

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

        [UseConverter(typeof(HexColorConverter))]
        public virtual Color LowColor { get; set; } = new Color(0, 1, 0); // default to green

        [UseConverter(typeof(HexColorConverter))]
        public virtual Color MidColor { get; set; } = new Color(1, 1, 0); // default to yellow

        [UseConverter(typeof(HexColorConverter))]
        public virtual Color HighColor { get; set; } = new Color(1, 0, 0); // default to red

        public virtual int PauseHR { get; set; } = 200;

        public virtual bool AutoPause { get; set; } = false;

        public virtual bool IgnoreCountersPlus { get; set; } = false;

        public virtual bool DebugSpam { get; set; } = false;

        public virtual Vector3 StaticCounterPosition { get; set; } = new Vector3(0f, 1.2f, 7f);

        internal event Action? OnSettingsChanged;
        
        private void RaiseSettingsChanged()
        {
            Log.Logger.Trace("HRCounter Settings Changed!");
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var e = OnSettingsChanged;
                    e?.Invoke();
                }
                catch (Exception e)
                {
                    Log.Logger.Critical($"Exception Caught while broadcasting settings changed event: {e.Message}");
                    Log.Logger.Critical(e);
                }
            });
        }
        
        [UsedImplicitly]
        public virtual void Changed()
        {
            RaiseSettingsChanged();
        }

        [UsedImplicitly]
        public virtual void OnReload()
        {
            RaiseSettingsChanged();
        }
    }
}