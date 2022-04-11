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
        }

        [CanBeNull]
        private static GameObject GetFlyingHUD()
        {
            // There should only be one
            Scene scene = SceneManager.GetAllScenes().FirstOrDefault(x => x.name.Contains("Environment") && x.isLoaded);

            // Find the GameObject
            GameObject environment = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "Environment");

            if (environment != null)
            {
                return environment.transform.Find("FlyingGameHUD/Container")?.gameObject;
            }

            return null;
        }

        public static bool CanFind { get; private set; }
        public static GameObject CurrentCanvas;
        public static GameObject Icon;
        public static TMP_Text Numbers;
        
        public static void SetupCanvasInScene()
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
                if (!IsCurrentMap360or90())
                {
                    // Place our Canvas in a Static Location
                    var location = PluginConfig.Instance.StaticCounterPosition;
                    CurrentCanvas.transform.position = new Vector3(location.x, location.y, location.z);
                    CurrentCanvas.transform.rotation = Quaternion.identity;
                }
                else
                {
                    // Attach it to the FlyingHUD
                    CurrentCanvas.AddComponent<MapMover>();
                }
            }
        }
        
        public static void ForceRemoveCanvas()
        {
            if (CanFind)
            {
                if (CurrentCanvas != null)
                {
                    Object.Destroy(CurrentCanvas);
                    CurrentCanvas = null;
                    Icon = null;
                    Numbers = null;
                }
                CanFind = false;
            }
        }

        internal static void OnSettingChange(object sender, EventArgs e)
        {
            Logger.logger.Info("Settings changed, updating counter location.");
            try
            {
                if (CanFind && IsGameCoreLoaded && !IsCurrentMap360or90())
                {
                    var location = PluginConfig.Instance.StaticCounterPosition;
                    CurrentCanvas.transform.position = new Vector3(location.x, location.y, location.z);
                }
            }
            catch (Exception exception)
            {
                Logger.logger.Warn($"Exception Caught during counter location update");
                Logger.logger.Warn(exception);
            }
        }
        
        public static void SetHR(int HR)
        {
            if (CanFind)
                Numbers.text = HR.ToString();
        }
        public static void SetHR(string HR)
        {
            if(CanFind)
                Numbers.text = HR;
        }
        
        /// <summary>
        /// Will have the canvas follow the player when on a 90/360 degree map
        /// </summary>
        private class MapMover : MonoBehaviour
        {
            private GameObject _flyingHUD;
            private RectTransform _iconRtTransform;

            private bool _check = false;

            private void Awake()
            {
                if (_flyingHUD == null && IsCurrentMap360or90())
                {
                    _flyingHUD = GetFlyingHUD();
                }
                if (_iconRtTransform == null)
                {
                    _iconRtTransform = Icon.gameObject.GetComponent<RectTransform>();
                }

                _check = CanFind && CurrentCanvas != null;
            }

            private void Update()
            {
                if (_check)
                {
                    CurrentCanvas.transform.position = _flyingHUD.transform.position;
                    Vector3 position = _iconRtTransform.localPosition;
                    _iconRtTransform.localPosition = new Vector3(position.x, -186, position.z);
                    CurrentCanvas.transform.rotation = _flyingHUD.transform.rotation;
                }
            }
        }
    }
}