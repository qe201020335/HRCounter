using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage;
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

        public static void LoadAssetBundle()
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

        private static bool IsGameCoreLoaded =>
            SceneManager.GetAllScenes().Where(x => x.name == "GameCore").ToArray().Length > 0;

        private static bool IsCurrentMap360or90()
        {
            if (IsGameCoreLoaded)
            {
                // Get the StandardGameplay Scene
                Scene sgs = SceneManager.GetSceneByName("StandardGameplay")!;
                if (sgs != null)
                {
                    // Find the GameplayCore
                    foreach (GameObject rootGameObject in sgs.GetRootGameObjects())
                    {
                        if (rootGameObject.name == "Wrapper")
                        {
                            GameObject gc = rootGameObject.transform.Find("StandardGameplay").Find("GameplayCore").gameObject;
                            if (gc != null)
                            {
                                // Get the component and start the reflection...
                                GameplayCoreInstaller gci = gc.GetComponent<GameplayCoreInstaller>();
                                if (gci != null)
                                {
                                    GameplayCoreSceneSetupData ssd =
                                        gci.GetField<GameplayCoreSceneSetupData, GameplayCoreInstaller>(
                                            "_sceneSetupData");
                                    
                                    if (ssd != null)
                                    {
                                        IDifficultyBeatmap db = ssd.difficultyBeatmap;
                                        IDifficultyBeatmapSet dbs = db.parentDifficultyBeatmapSet;
                                        BeatmapCharacteristicSO bcso = dbs.beatmapCharacteristic;

                                        var sn = bcso.serializedName;
                                        
                                        bool contains360 = sn?.Contains("360") ?? false;
                                        bool contains90 = sn?.Contains("90") ?? false;
                                        return contains360 || contains90;
                                    }
                                    Logger.logger.Error("ssd null");
                                }
                                else
                                    Logger.logger.Error("gci null");
                            }
                            else
                                Logger.logger.Error("gc null");
                        }
                    }
                }
                else
                    Logger.logger.Error("sgs null");
            }
            Logger.logger.Error("false lol");
            // lol idk
            return false;
        }

        [CanBeNull]
        private static GameObject GetFlyingHUD()
        {
            bool foundOne = false;
            foreach (Scene allScene in SceneManager.GetAllScenes())
            {
                if (allScene.name.Contains("Environment") && allScene.isLoaded)
                {
                    // There should only be one
                    if (!foundOne)
                    {
                        foundOne = true;
                        // Find the GameObject
                        GameObject envGO = allScene.GetRootGameObjects().FirstOrDefault(x => x.name == "Environment")!;
                        if (envGO != null)
                        {
                            GameObject fghud = envGO.transform.Find("FlyingGameHUD").Find("Container").gameObject;
                            return fghud;
                        }
                    }
                }
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
                    .Where(x => x.name == "UINoGlow").FirstOrDefault();
                if (!IsCurrentMap360or90())
                {
                    // Place our Canvas in a Static Location
                    CurrentCanvas.transform.position = new Vector3(0, 1.2f, 7f);
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
            private GameObject fhud;
            private RectTransform icon_rt;

            private void Update()
            {
                if(fhud == null && IsCurrentMap360or90())
                    fhud = GetFlyingHUD();
                else if (CanFind && CurrentCanvas != null)
                {
                    CurrentCanvas.transform.position = fhud.transform.position;
                    if(icon_rt == null)
                        icon_rt = Icon.gameObject.GetComponent<RectTransform>();
                    icon_rt.localPosition = new Vector3(icon_rt.localPosition.x, -186, icon_rt.localPosition.z);
                    CurrentCanvas.transform.rotation = fhud.transform.rotation;
                }
            }
        }
    }
}