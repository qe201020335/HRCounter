
using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace HRCounter.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }
        // Must be 'virtual' if you want BSIPA to detect a value change and save the config automatically.
        public virtual bool LogHR { get; set; } = false;

        public virtual string DataSource { get; set; } = "WebRequest";  // HypeRate, WebRequest, or FitbitHRtoWS

        public virtual string HypeRateSessionID { get; set; } = "-1";

        public virtual string FitbitWebSocket { get; set; } = "ws://localhost:8080/";
        
        public virtual string FeedLink { get; set; } = "NotSet";

        public virtual bool Colorize { get; set; } = true;
        
        public virtual bool HideDuringReplay { get; set; } = true;

        public virtual int HRLow { get; set; } = 120;
        
        public virtual int HRHigh { get; set; } = 180;

        public virtual string LowColor { get; set; } = "#00FF00"; // default to green

        public virtual string MidColor { get; set; } = "#FFFF00";  // default to yellow
        
        public virtual string HighColor { get; set; } = "#FF0000"; // default to red

        public virtual int PauseHR { get; set; } = 200;
        
        public virtual bool AutoPause { get; set; } = false;

        public virtual bool YURModIntegration { get; set; } = true;

    }
}
