using System;
using System.Linq;
using HRCounter.Configuration;
using UnityEngine;

namespace HRCounter.Utils
{
    public static class RenderUtils
    {
        private static Lazy<Material> _uiNoGlow =
            new Lazy<Material>(() => Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(x => x.name == "UINoGlow"));

        private static Lazy<Shader> _textNoGlow = new Lazy<Shader>(() => 
            Resources.FindObjectsOfTypeAll<Shader>().First(x => x.name.Contains("TextMeshPro/Mobile/Distance Field Zero Alpha Write")));

        private static Lazy<Shader> _textGlow = new Lazy<Shader>(() =>
            Resources.FindObjectsOfTypeAll<Shader>().First(x => x.name.Contains("TextMeshPro/Distance Field")));
        
        public static Material UINoGlow => _uiNoGlow.Value;
        public static Shader TextNoGlow => _textNoGlow.Value;
        public static Shader TextGlow => _textGlow.Value;
        
        private static  PluginConfig Config => PluginConfig.Instance;
        

        internal static Color DetermineColor(int hr)
        {
            var hrLow = Config.HRLow;
            var hrHigh = Config.HRHigh;
            var lowColor = Config.LowColor;
            var midColor = Config.MidColor;
            var highColor = Config.HighColor;
            
            if (hrHigh >= hrLow && hrLow > 0)
            {
                var ratio = (hr - hrLow) / (float)(hrHigh - hrLow) * 2;
                var color = ratio < 1
                    ? Color.Lerp(lowColor, midColor, ratio)
                    : Color.Lerp(midColor, highColor, ratio - 1);
                return color;
            }

            Plugin.Log.Warn("Cannot determine color, please check hr boundaries and color codes.");
            return Color.white;
        }
    }
}