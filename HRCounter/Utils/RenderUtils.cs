using System;
using System.Linq;
using HRCounter.Configuration;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace HRCounter.Utils
{
    public class RenderUtils : IInitializable, IDisposable
    {
        private static Lazy<Material> _uiNoGlow =
            new Lazy<Material>(() => Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(x => x.name == "UINoGlow"));

        private static Lazy<Shader> _textNoGlow = new Lazy<Shader>(() => Shader.Find("TextMeshPro/Mobile/Distance Field Zero Alpha Write"));
        public static Material UINoGlow => _uiNoGlow.Value;
        public static Shader TextNoGlow => _textNoGlow.Value;

        private readonly PluginConfig _config;
        private readonly SiraLog _logger;

        private Color? _lowColor;
        private Color? _highColor;
        private Color? _midColor;
        private int _hrLow;
        private int _hrHigh;

        internal RenderUtils([Inject] PluginConfig pluginConfig, [Inject] SiraLog logger)
        {
            _config = pluginConfig;
            _logger = logger;
            OnConfigChanged();
        }

        public void Initialize()
        {
            _config.OnSettingsChanged -= OnConfigChanged;
            _config.OnSettingsChanged += OnConfigChanged;
        }

        public void Dispose()
        {
            _config.OnSettingsChanged -= OnConfigChanged;
        }

        private void OnConfigChanged()
        {
            if (ColorUtility.TryParseHtmlString(_config.HighColor, out var colorHigh)) _highColor = colorHigh;
            if (ColorUtility.TryParseHtmlString(_config.LowColor, out var colorLow)) _lowColor = colorLow;
            if (ColorUtility.TryParseHtmlString(_config.HighColor, out var colorMid))  _midColor = colorMid;
            
            _hrLow = _config.HRLow;
            _hrHigh = _config.HRHigh;
        }

        internal string DetermineColor(int hr)
        {
            var result = Color.white;
            if (_hrHigh >= _hrLow && _hrLow > 0 && _lowColor != null && _highColor != null && _midColor != null)
            {
                if (hr <= _hrLow)
                {
                    result = _lowColor.Value; //the rgb color in setting are #RRGGBB, need to omit the #
                }
                else if (hr >= _hrHigh)
                {
                    result = _highColor.Value;
                }
                else
                {
                    var ratio = (hr - _hrLow) / (float)(_hrHigh - _hrLow) * 2;
                    var color = ratio < 1
                        ? Color.Lerp(_lowColor.Value, _midColor.Value, ratio)
                        : Color.Lerp(_midColor.Value, _highColor.Value, ratio - 1);
                    result = color;
                }
            }
            else
            {
                _logger.Warn("Cannot determine color, please check hr boundaries and color codes.");
            }

            return "#" + ColorUtility.ToHtmlStringRGB(result);
        }
    }
}