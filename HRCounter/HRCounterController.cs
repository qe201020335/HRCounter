using System;
using System.Linq;
using HRCounter.Configuration;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace HRCounter
{
    internal class HRCounterController : IInitializable, IDisposable
    {

        [CanBeNull] private readonly GameplayCoreSceneSetupData _sceneSetupData;

        private bool _needs360Move = false;
        
        private GameObject CurrentCanvas;
        private TMP_Text Numbers;
            
        internal HRCounterController([InjectOptional] GameplayCoreSceneSetupData gameplayCoreSceneSetupData)
        {
            _sceneSetupData = gameplayCoreSceneSetupData;
        }

        public void Initialize()
        {
            if (_sceneSetupData == null)
            {
                Plugin.Log.Warn("GameplayCoreSceneSetupData is null");
                return;
            }

            _needs360Move = _sceneSetupData.difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic
                .requires360Movement;
            Plugin.Log.Info($"360/90?: {_needs360Move}");

            var counter = AssetBundleManager.SetupCustomCounter();

            CurrentCanvas = counter.CurrentCanvas;
            Numbers = counter.Numbers;

            CurrentCanvas.transform.localScale = Vector3.one / 150;
            
            if (!_needs360Move)
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

        public void Dispose()
        {
            Plugin.Log.Info("Disposed lol");
        }

        internal void OnSettingChange(object sender, EventArgs e)
        {
            Logger.logger.Info("Settings changed, updating counter location.");
            try
            {
                if (!_needs360Move)
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

        /// <summary>
        /// Will have the canvas follow the player when on a 90/360 degree map
        /// </summary>
        private class MapMover : MonoBehaviour
        {
            private GameObject _flyingHUD;
            private RectTransform _iconRtTransform;
            
            private GameObject _currentCanvas;
            private GameObject _icon;

            private void Awake()
            {
                _currentCanvas = gameObject;
                _icon = _currentCanvas.transform.GetChild(0).gameObject;
                
                if (_flyingHUD == null)
                {
                    _flyingHUD = GetFlyingHUD();
                }
                if (_iconRtTransform == null)
                {
                    _iconRtTransform = _icon.gameObject.GetComponent<RectTransform>();
                }
            }

            private void Update()
            {
                _currentCanvas.transform.position = _flyingHUD.transform.position;
                Vector3 position = _iconRtTransform.localPosition;
                _iconRtTransform.localPosition = new Vector3(position.x, -186, position.z);
                _currentCanvas.transform.rotation = _flyingHUD.transform.rotation;
            }
            
            [CanBeNull]
            private GameObject GetFlyingHUD()
            {
                // There should only be one
                Scene scene = SceneManager.GetAllScenes()
                    .FirstOrDefault(x => x.name.Contains("Environment") && x.isLoaded);

                // Find the GameObject
                GameObject environment = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "Environment");

                if (environment != null)
                {
                    return environment.transform.Find("FlyingGameHUD/Container")?.gameObject;
                }

                return null;
            }
        }
        
    }
}