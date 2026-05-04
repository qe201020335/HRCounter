using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using HRCounter.Configuration;
using UnityEngine;
using Color = UnityEngine.Color;

namespace HRCounter.Utils;

public static class RenderUtils
{
    private static readonly Lazy<Material> _uiNoGlow = new(() => Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "UINoGlow"));

    private static readonly Lazy<Shader> _textNoGlow = new(() =>
        Resources.FindObjectsOfTypeAll<Shader>().First(x => x.name.Contains("TextMeshPro/Mobile/Distance Field Zero Alpha Write")));

    private static readonly Lazy<Shader> _textGlow = new(() =>
        Resources.FindObjectsOfTypeAll<Shader>().First(x => x.name.Contains("TextMeshPro/Distance Field")));

    public static Material UINoGlow => _uiNoGlow.Value;
    public static Shader TextNoGlow => _textNoGlow.Value;
    public static Shader TextGlow => _textGlow.Value;

    private static PluginConfig Config => Plugin.Config;


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

        Plugin.Logger.Warn("Cannot determine color, please check hr boundaries and color codes.");
        return Color.white;
    }

    internal static async Task<Texture2D?> LoadImageAsync(FileInfo file)
    {
        if (!file.Exists || file.Length == 0)
        {
            return null;
        }

        await using var stream = file.OpenRead();

#if DEBUG
        var stopwatch = new Stopwatch();
        stopwatch.Start();
#endif
        var texture = await Utilities.LoadImageAsync(stream);

#if DEBUG
        stopwatch.Stop();
        Plugin.Logger.Trace($"LoadImageAsync took {stopwatch.ElapsedMilliseconds}ms to load {file.Name}");
#endif
        texture.name = file.Name;
        return texture;
    }

    internal static Sprite CreateSprite(Texture2D texture)
    {
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        sprite.name = texture.name;
        return sprite;
    }
}
