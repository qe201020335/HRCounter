using System;
using System.Linq;
using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Events;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Object = UnityEngine.Object;
using IPALogger = IPA.Logging.Logger;

namespace HRCounter
{
    internal class HRCounterController : IInitializable, IDisposable
    {

        [CanBeNull] private readonly GameplayCoreSceneSetupData _sceneSetupData;

        private bool _needs360Move = false;
        
        private GameObject CurrentCanvas;
        private TMP_Text Numbers;
        private static bool Colorize => PluginConfig.Instance.Colorize;
        private readonly IPALogger _logger = Logger.logger;

        
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
            
            if (PluginConfig.Instance.HideDuringReplay && Utils.Utils.IsInReplay())
            {
                _logger.Info("We are in a replay, HRCounter hides.");
                return;
            }
            
            _needs360Move = _sceneSetupData.difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic
                .requires360Movement;
            Plugin.Log.Info($"360/90?: {_needs360Move}");

            if (!CreateCounter())
            {
                _logger.Warn("Cannot create HRCounter");
                return;
            }
            
            _logger.Info("HRCounter Initialized");
        }

        private bool CreateCounter()
        {
            _logger.Info("Creating HRCounter");
            var counter = AssetBundleManager.SetupCustomCounter();

            CurrentCanvas = counter.CurrentCanvas;
            Numbers = counter.Numbers;
            
            if (CurrentCanvas == null)
            {
                _logger.Error("Cannot create custom counter");
                return false;
            }
            
            CurrentCanvas.transform.localScale = Vector3.one / 150;
            
            OnHRUpdate(null, new HRUpdateEventArgs(BPM.Instance.Bpm));  // give it an initial value

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
            HRController.OnHRUpdate += OnHRUpdate;
            PluginConfig.Instance.OnSettingsChanged += OnSettingChange;
            return true;
        }

        private void OnHRUpdate(object sender, HRUpdateEventArgs args)
        {
            var bpm = args.HeartRate;
            Numbers.text = Colorize ? $"<color=#{Utils.Utils.DetermineColor(bpm)}>{bpm}</color>" : $"{bpm}";
        }

        private void OnSettingChange(object sender, EventArgs e)
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
        
        public void Dispose()
        {
            HRController.OnHRUpdate -= OnHRUpdate;
            PluginConfig.Instance.OnSettingsChanged -= OnSettingChange;

            if (CurrentCanvas != null)
            {
                Object.Destroy(CurrentCanvas);
            }
            Plugin.Log.Info("HRCounter Disposed");
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
                _iconRtTransform.localPosition = new Vector3(position.x, 150, position.z);
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