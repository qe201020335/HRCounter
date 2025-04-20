using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HRCounter.Configuration;
using UnityEngine;
using Color = UnityEngine.Color;

namespace HRCounter.Utils;

public static class RenderUtils
{
    private static Lazy<Material> _uiNoGlow = new(() => Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(x => x.name == "UINoGlow"));

    private static Lazy<Shader> _textNoGlow = new(() =>
        Resources.FindObjectsOfTypeAll<Shader>().First(x => x.name.Contains("TextMeshPro/Mobile/Distance Field Zero Alpha Write")));

    private static Lazy<Shader> _textGlow = new(() =>
        Resources.FindObjectsOfTypeAll<Shader>().First(x => x.name.Contains("TextMeshPro/Distance Field")));

    public static Material UINoGlow => _uiNoGlow.Value;
    public static Shader TextNoGlow => _textNoGlow.Value;
    public static Shader TextGlow => _textGlow.Value;

    private static PluginConfig Config => PluginConfig.Instance;


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

        using var stream = file.OpenRead();

#if DEBUG
        var stopwatch = new Stopwatch();
        stopwatch.Start();
#endif
        var texture = await LoadImageAsync(stream);

#if DEBUG
        stopwatch.Stop();
        Plugin.Logger.Trace($"LoadImageAsync tool {stopwatch.ElapsedMilliseconds}ms to load {file.Name}");
#endif
        texture.name = file.Name;
        return texture;
    }

    /**
     * Literally copy-pasted from BeatSaberMarkupLanguage.Utilities
     * MIT License: https://github.com/monkeymanboy/BeatSaberMarkupLanguage/raw/master/LICENSE
     */
    internal static async Task<Texture2D> LoadImageAsync(Stream stream, bool updateMipmaps = true, bool makeNoLongerReadable = true)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var (width, height, data) = await Task.Factory.StartNew(
            () =>
            {
                using Bitmap bitmap = new(stream);

                // flip it over since Unity uses OpenGL coordinates - (0, 0) is the bottom left corner instead of the top left
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb);
                var data = new byte[bitmapData.Stride * bitmapData.Height];

                Marshal.Copy(bitmapData.Scan0, data, 0, bitmapData.Stride * bitmapData.Height);

                bitmap.UnlockBits(bitmapData);

                return (bitmap.Width, bitmap.Height, data);
            },
            TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);

        // basically all processors are little endian these days so pixel format order is reversed
        Texture2D texture = new(width, height, TextureFormat.BGRA32, false);
        texture.LoadRawTextureData(data);
        texture.Apply(updateMipmaps, makeNoLongerReadable);
        return texture;
    }

    internal static Sprite CreateSprite(Texture2D texture)
    {
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        sprite.name = texture.name;
        return sprite;
    }
}
