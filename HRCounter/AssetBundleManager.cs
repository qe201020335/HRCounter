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

        /*
        private static bool IsGameCoreLoaded => SceneManager.GetSceneByName("GameCore").IsValid();

        private static bool IsCurrentMap360or90()
        {
            if (!IsGameCoreLoaded)
            {
                Logger.logger.Warn("GameCore is not loaded");
                return false;
            }
            
            // Get the StandardGameplay Scene
            var standardGameplayScene = SceneManager.GetSceneByName("StandardGameplay");
            if (!standardGameplayScene.IsValid())
            {
                Logger.logger.Warn("Cannot find StandardGameplay scene");
                return false;
            }

            var rootGameObject = standardGameplayScene.GetRootGameObjects().FirstOrDefault(x => x.name == "Wrapper");
            if (rootGameObject == null)
            {
                Logger.logger.Warn("Cannot find Wrapper GameObject");
                return false;
            }
            
            // Find the GameplayCore
            var gamePlayCore = rootGameObject.transform.Find("StandardGameplay/GameplayCore")?.gameObject;
            if (gamePlayCore == null)
            {
                Logger.logger.Warn("Cannot find gamePlayCore");
                return false;
            }

            try
            {
                // Get the component and start the reflection...
                var gameplayCoreInstaller = gamePlayCore.GetComponent<GameplayCoreInstaller>();
                var sceneSetupData = gameplayCoreInstaller.GetField<GameplayCoreSceneSetupData, GameplayCoreInstaller>("_sceneSetupData");
                var beatmapCharacteristic = sceneSetupData.difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic;
                var sn = beatmapCharacteristic.serializedName;

                if (string.IsNullOrEmpty(sn))
                {
                    Logger.logger.Warn("Beatmap Characteristic name is null or empty");
                    return false;
                }
                return sn.Contains("360") || sn.Contains("90");
            }
            catch (Exception e)
            {
                Logger.logger.Critical("Exception occured while trying to determine 360/60");
                Logger.logger.Critical(e.Message);
                Logger.logger.Debug(e);
            }
            Logger.logger.Warn("Cannot determine if map is 360/90");
            return false;
        }*/

        

        // public static bool CanFind { get; private set; }
        // public static GameObject CurrentCanvas;
        // public static GameObject Icon;
        // public static TMP_Text Numbers;

        internal static CustomCounter SetupCustomCounter()
        {
            var currentCanvas = Object.Instantiate(CanvasOverlay);
            var icon = currentCanvas.transform.GetChild(0).gameObject;
            var numbers = icon.transform.GetChild(0).GetComponent<TMP_Text>();
            numbers.alignment = TextAlignmentOptions.MidlineLeft;
            icon.GetComponent<Image>().material = Resources.FindObjectsOfTypeAll<Material>()
                .FirstOrDefault(x => x.name == "UINoGlow");
            
            return new CustomCounter {
                CurrentCanvas = currentCanvas,
                Icon = icon,
                Numbers = numbers
            };
        }
        /*
        public static GameObject SetupCanvasInScene()
        {
            // Only add it if we haven't already
            if (!CanFind && IsGameCoreLoaded)
            {
                CurrentCanvas = Object.Instantiate(CanvasOverlay);
                Icon = CurrentCanvas.transform.GetChild(0).gameObject;
                Numbers = Icon.transform.GetChild(0).GetComponent<TMP_Text>();
                CanFind = true;
                CurrentCanvas.transform.localScale = Vector3.one / 150;
                Numbers.alignment = TextAlignmentOptions.MidlineLeft;
                Icon.GetComponent<Image>().material = Resources.FindObjectsOfTypeAll<Material>()
                    .FirstOrDefault(x => x.name == "UINoGlow");
                
            }

            return CurrentCanvas;
        }

        internal static GameObject SetupCanvasInScene(Canvas canvas, TMP_Text countersPlusText)
        {
            if (!CanFind)
            {
                CurrentCanvas = Object.Instantiate(CanvasOverlay);
                Icon = CurrentCanvas.transform.GetChild(0).gameObject;
                Numbers = Icon.transform.GetChild(0).GetComponent<TMP_Text>();
                CanFind = true;
                Numbers.alignment = TextAlignmentOptions.MidlineLeft;
                Icon.GetComponent<Image>().material = Resources.FindObjectsOfTypeAll<Material>()
                    .FirstOrDefault(x => x.name == "UINoGlow");
                Icon.transform.localScale = Vector3.one / 30;
                CurrentCanvas.SetActive(false);
                Icon.transform.SetParent(canvas.transform, false);
                Icon.GetComponent<RectTransform>().anchoredPosition =
                    countersPlusText.rectTransform.anchoredPosition;
                Icon.transform.localPosition -= new Vector3(2, 0, 0); // recenter

                Icon.SetActive(!PluginConfig.Instance.TextOnlyCounter);
            }
            else
            {
                Logger.logger.Warn("GameCore not loaded or we have a previous counter");
            }

            return Icon;
        }
        */

        // public static void ForceRemoveCanvas()
        // {
        //     if (CanFind)
        //     {
        //         if (CurrentCanvas != null)
        //         {
        //             Object.Destroy(Icon);
        //             Icon = null;
        //             Numbers = null;
        //             Object.Destroy(CurrentCanvas);
        //             CurrentCanvas = null;
        //         }
        //         CanFind = false;
        //     }
        // }

        internal struct CustomCounter
        {
            public GameObject CurrentCanvas;
            public GameObject Icon;
            public TMP_Text Numbers;
        }
    }
}