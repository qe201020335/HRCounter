using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HRCounter.Configuration;
using IPA.Utilities;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace HRCounter;

internal class IconManager: IInitializable
{
    [Inject] private readonly PluginConfig _config = null!;

    [Inject] private readonly SiraLog _logger = null!;
    
    private readonly string _iconPathBase = Path.Combine(UnityGame.UserDataPath, "HRCounter", "Icons");
    
    private readonly IList<Tuple<string, Sprite>> _loadedIcons = new List<Tuple<string, Sprite>>();
    
    void IInitializable.Initialize()
    {
        if (!Directory.Exists(_iconPathBase))
        {
            Directory.CreateDirectory(_iconPathBase);
        }
    }
    
    internal IList<string> GetIcons()
    {
        return Directory.GetFiles(_iconPathBase).Select(Path.GetFileName).ToList();
    }
    
    internal async Task<IList<Tuple<string, Sprite>>> GetIconsWithSprite(bool refresh = false)
    {
        var files = Directory.GetFiles(_iconPathBase);
        _logger.Debug($"Attempting to load {files.Length} icons");
        var result = new List<Tuple<string, Sprite>>(files.Length);
        foreach (var file in files)
        {
            try
            {
                var name = Path.GetFileName(file);
                var data = File.ReadAllBytes(file);
                var s = await BeatSaberMarkupLanguage.Utilities.LoadSpriteAsync(data);
                if (name == null || s == null) continue;

                result.Add(new Tuple<string, Sprite>(name, s));
            }
            catch (Exception e)
            {
                _logger.Warn($"Failed to load icon {file}");
                _logger.Warn(e);
            }
        }
        
        _logger.Debug($"Loaded {result.Count} icons");
        return result;
    }
}