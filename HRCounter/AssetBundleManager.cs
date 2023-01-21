using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage;
using HRCounter.Configuration;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using IPA.Utilities;

namespace HRCounter
{
    public class AssetBundleManager
    {
        private static AssetBundle loadedAssetBundle;
        private static GameObject CanvasOverlay;

        private static Material? _uiNoGlow;
        private static Shader? _textNoGlow;

        internal static Material UINoGlow
        {
            get
            {
                _uiNoGlow ??= Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(x => x.name == "UINoGlow");
                return _uiNoGlow;
            }
        }

        internal static Shader TextNoGlow
        {
            get
            {
                _textNoGlow ??= Shader.Find("TextMeshPro/Mobile/Distance Field Zero Alpha Write");
                return _textNoGlow;
            }
        }

        internal static void LoadAssetBundle()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HRCounter.hrcounter"))
            {
                if (stream != null)
                {
                    MemoryStream ms = new MemoryStream();
                    stream.CopyTo(ms);
                    byte[] assetBundleRaw = ms.ToArray();
                    // Load the AssetBundle
                    loadedAssetBundle = AssetBundle.LoadFromMemory(assetBundleRaw);
                    CanvasOverlay = loadedAssetBundle.LoadAsset<GameObject>("Assets/HRCounter.prefab");
                    loadedAssetBundle.Unload(false);
                    Log.Logger.Info("Loaded AssetBundle!");
                }
                else
                    Log.Logger.Error("Failed to find hrcounter AssetBundle from ManifestResourceStream!");
            }
            
        }

        internal static CustomCounter SetupCustomCounter()
        {
            var currentCanvas = Object.Instantiate(CanvasOverlay);
            var icon = currentCanvas.transform.GetChild(0).gameObject;
            var numbers = icon.transform.GetChild(0).GetComponent<TMP_Text>();
            numbers.alignment = TextAlignmentOptions.MidlineLeft;
            
            icon.GetComponent<Image>().material = UINoGlow;
            if (PluginConfig.Instance.NoBloom)
            {
                numbers.fontMaterial.shader = TextNoGlow;
            }

            return new CustomCounter {
                CurrentCanvas = currentCanvas,
                Icon = icon,
                Numbers = numbers
            };
        }

        internal struct CustomCounter
        {
            public GameObject CurrentCanvas;
            public GameObject Icon;
            public TMP_Text Numbers;
        }
    }
}