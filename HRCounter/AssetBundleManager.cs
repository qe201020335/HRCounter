﻿using System;
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

        private static Material _noGlow = null;

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
                    Logger.logger.Info("Loaded AssetBundle!");
                }
                else
                    Logger.logger.Error("Failed to find hrcounter AssetBundle from ManifestResourceStream!");
            }
            
        }

        internal static CustomCounter SetupCustomCounter()
        {
            var currentCanvas = Object.Instantiate(CanvasOverlay);
            var icon = currentCanvas.transform.GetChild(0).gameObject;
            var numbers = icon.transform.GetChild(0).GetComponent<TMP_Text>();
            numbers.alignment = TextAlignmentOptions.MidlineLeft;
            
            _noGlow ??= Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(x => x.name == "UINoGlow");
            
            if (ReferenceEquals(_noGlow, null))
            {
                Logger.logger.Warn("Cannot find material UINoGlow!");
            }
            else
            {
                icon.GetComponent<Image>().material = _noGlow;
                if (PluginConfig.Instance.NoBloom)
                {
                    numbers.text = "";
                    var newNumbers = BeatSaberUI.CreateText(icon.transform as RectTransform, "",
                        numbers.rectTransform.anchoredPosition);
                    newNumbers.name = "Numbers no glow";
                    newNumbers.gameObject.layer = numbers.gameObject.layer;
                    newNumbers.alignment = numbers.alignment;
                    newNumbers.fontSize = numbers.fontSize;
                    newNumbers.enableWordWrapping = numbers.enableWordWrapping;
                    newNumbers.rectTransform.offsetMin = numbers.rectTransform.offsetMin;
                    newNumbers.rectTransform.offsetMax = numbers.rectTransform.offsetMax;
                    numbers = newNumbers;
                }
                
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