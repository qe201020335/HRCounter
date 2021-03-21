
using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace HRCounter.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }
        // Must be 'virtual' if you want BSIPA to detect a value change and save the config automatically.
        public virtual bool Log_HR { get; set; } = false;
        public virtual string Feed_link { get; set; } = "NotSet";

        public virtual bool Colorize { get; set; } = true;

        public virtual int HRLow { get; set; } = 120;
        
        public virtual int HRHigh { get; set; } = 180;

        public virtual string LowColor { get; set; } = "#00FF00"; // default to green
        
        public virtual string HighColor { get; set; } = "#FF0000"; // default to red

    }
}
