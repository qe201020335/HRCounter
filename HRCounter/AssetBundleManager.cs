﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HRCounter.Configuration;
using HRCounter.Utils;
using SiraUtil.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace HRCounter
{
    internal class AssetBundleManager: IInitializable, IDisposable
    {
        private GameObject? CounterPrefab { get; set; }

        [Inject] private readonly PluginConfig _config = null!;

        [Inject] private readonly SiraLog _logger = null!;
        
        public void Initialize()
        {
            using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HRCounter.Resources.hrcounter");
            if (resourceStream == null)
            {
                _logger.Error("Failed to find hrcounter AssetBundle from ManifestResourceStream!");
                return;
            }

            try
            {
                // Load the AssetBundle
                var bundle = AssetBundle.LoadFromStream(resourceStream);
                CounterPrefab = bundle.LoadAsset<GameObject>("Assets/HRCounter.prefab");
                bundle.Unload(false);
                if (CounterPrefab == null)
                {
                    throw new NullReferenceException("The counter prefab is null!");
                }
                
                _logger.Info("AssetBundle Loaded!");
            }
            catch (Exception e)
            {
                _logger.Error($"Cannot load the counter prefab from the asset bundle: {e.Message}");
                _logger.Error(e);
            }
        }

        public void Dispose()
        {
            if (CounterPrefab != null)
            {
                Object.Destroy(CounterPrefab);
                CounterPrefab = null;
            }
        }

        internal CustomCounter SetupCustomCounter()
        {
            if (CounterPrefab == null)
            {
                return new CustomCounter
                {
                    Counter = null,
                    Icon = null,
                    Numbers = null
                };
            }
            
            var currentCanvas = Object.Instantiate(CounterPrefab);
            var icon = currentCanvas.transform.GetChild(0).gameObject;
            var numbers = icon.transform.GetChild(0).GetComponent<TMP_Text>();
            numbers.alignment = TextAlignmentOptions.MidlineLeft;

            icon.GetComponent<Image>().material = RenderUtils.UINoGlow;
            if (_config.NoBloom)
            {
                numbers.fontMaterial.shader = RenderUtils.TextNoGlow;
            }
            else
            {
                numbers.fontMaterial.shader = RenderUtils.TextGlow;
            }

            return new CustomCounter
            {
                Counter = currentCanvas,
                Icon = icon,
                Numbers = numbers
            };
        }

        internal struct CustomCounter
        {
            public GameObject? Counter;
            public GameObject? Icon;
            public TMP_Text? Numbers;

            internal bool IsNotNull()
            {
                return Counter != null && Icon != null && Numbers != null;
            }
        }
    }
}