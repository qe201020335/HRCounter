using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Utils;
using IPA.Utilities;
using IPA.Utilities.Async;
using UnityEngine;
using Zenject;
using Logger = IPA.Logging.Logger;
using UObject = UnityEngine.Object;

namespace HRCounter;

internal class IconManager : IInitializable, IDisposable
{
    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly Logger _logger = null!;
    
    [Inject]
    private readonly AssetBundleManager _assetBundleManager = null!;

    private readonly DirectoryInfo _iconDir = new(Path.Combine(UnityGame.UserDataPath, "HRCounter", "Icons"));

    private readonly Dictionary<string, Sprite> _loadedIcons = new();

    private readonly HashSet<string> _acceptableExtensions = [".png", ".jpg", ".jpeg", ".gif"];

    internal string IconDirPath => _iconDir.FullName;
    
    internal Sprite? DefaultIcon { get; private set; }

    internal IconManager()
    {
        _iconDir.Create();
    }

    void IInitializable.Initialize()
    {
        DefaultIcon = _assetBundleManager.DefaultIconSprite;
        try
        {
            var hrcIconPath = Path.Combine(IconDirPath, "hrc_white.png");
            if (!File.Exists(hrcIconPath))
            {
                using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HRCounter.Resources.hrcounter.png");
                if (resourceStream != null)
                {
                    var iconPath = Path.Combine(IconDirPath, "hrc_white.png");
                    using var fileStream = new FileStream(iconPath, FileMode.Create, FileAccess.Write);
                    resourceStream.CopyTo(fileStream);
                }
            }
        }
        catch (Exception e)
        {
            _logger.Debug("Failed to copy hrc_white.png");
            _logger.Debug(e);
        }

        UnityMainThreadTaskScheduler.Factory.StartNew(LoadAllIconsAsync);
    }

    void IDisposable.Dispose()
    {
        ClearIconCache();
    }

    internal bool TryGetIconSprite(string name, out Sprite sprite)
    {
        return _loadedIcons.TryGetValue(name, out sprite);
    }

    private void ClearIconCache()
    {
        foreach (var icon in _loadedIcons)
        {
            var texture = icon.Value.texture;
            UObject.Destroy(icon.Value);
            UObject.Destroy(texture);
        }

        _loadedIcons.Clear();
    }

    internal async Task<IList<Tuple<string, Sprite>>> GetIconsWithSpriteAsync(bool refresh)
    {
        if (!UnityGame.OnMainThread)
        {
            throw new InvalidOperationException("This method can only be called from the main thread.");
        }

        if (refresh)
        {
            await LoadAllIconsAsync();
        }

        return _loadedIcons.Select(icon => new Tuple<string, Sprite>(icon.Key, icon.Value)).ToList();
    }

    private async Task LoadAllIconsAsync()
    {
        _logger.Debug("Loading all icons");
        ClearIconCache();
        foreach (var file in _iconDir.EnumerateFiles())
            if (file.Exists && _acceptableExtensions.Contains(file.Extension))
            {
                var name = file.Name;
                var sprite = await LoadIconSpriteAsync(file);
                if (sprite != null && !string.IsNullOrWhiteSpace(name))
                {
                    _loadedIcons[name] = sprite;
                }
            }

        _logger.Debug($"Loaded {_loadedIcons.Count} icons");
    }

    private async Task<Sprite?> LoadIconSpriteAsync(FileInfo file)
    {
        var fileName = file.Name;
        _logger.Trace($"Loading icon {fileName}");

        try
        {
            _logger.Trace($"Creating texture from {fileName}");
            var texture = await RenderUtils.LoadImageAsync(file);
            if (texture == null)
            {
                _logger.Warn($"Failed to load image from {fileName}");
                return null;
            }

            var sprite = RenderUtils.CreateSprite(texture);

            _logger.Trace($"Loaded sprite from {fileName}");
            return sprite;
        }
        catch (Exception e)
        {
            _logger.Critical($"Failed to load icon sprite from {fileName}");
            _logger.Critical(e);
            return null;
        }
    }
}
