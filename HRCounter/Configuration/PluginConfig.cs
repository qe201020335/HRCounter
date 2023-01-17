
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace HRCounter.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }
        // Must be 'virtual' if you want BSIPA to detect a value change and save the config automatically.

        public virtual bool ModEnable { get; set; } = true;
        public virtual bool LogHR { get; set; } = false;

        public virtual string DataSource { get; set; } = "YUR MOD";  // HypeRate, Pulsoid, Pulsoid Token, WebRequest, YUR APP, FitbitHRtoWS, HRProxy, YUR MOD, Also Random for testing 

        public virtual string PulsoidToken { get; set; } = "NotSet";
        
        public virtual string HypeRateSessionID { get; set; } = "-1";
        
        public virtual string PulsoidWidgetID { get; set; } = "NotSet";

        public virtual string FitbitWebSocket { get; set; } = "ws://localhost:8080/";

        public virtual string HRProxyID { get; set; } = "NotSet";
        
        public virtual string FeedLink { get; set; } = "NotSet";

        
        public virtual bool NoBloom { get; set; } = false;

        public virtual bool Colorize { get; set; } = true;
        
        public virtual bool HideDuringReplay { get; set; } = true;

        public virtual int HRLow { get; set; } = 120;
        
        public virtual int HRHigh { get; set; } = 180;

        public virtual string LowColor { get; set; } = "#00FF00"; // default to green

        public virtual string MidColor { get; set; } = "#FFFF00";  // default to yellow
        
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

        internal event EventHandler<EventArgs> OnSettingsChanged;
        
        public virtual void OnReload()
        {
            Log.Logger.Notice("HRCounter Settings Changed!");
            try
            {
                EventHandler<EventArgs> handler = OnSettingsChanged;
                Task.Factory.StartNew(() =>
                {
                    handler?.Invoke(this, EventArgs.Empty);
                });
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
