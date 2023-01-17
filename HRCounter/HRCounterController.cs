using System;
using System.Linq;
using HRCounter.Configuration;
using HRCounter.Data;
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
        private readonly GameplayCoreSceneSetupData? _sceneSetupData;

        private bool _needs360Move;

        private GameObject _currentCanvas = null!;
        private TMP_Text _numbersText = null!;
        private static bool Colorize => PluginConfig.Instance.Colorize;
        private readonly IPALogger _logger = Log.Logger;

        internal HRCounterController([InjectOptional] GameplayCoreSceneSetupData gameplayCoreSceneSetupData)
        {
            _sceneSetupData = gameplayCoreSceneSetupData;
        }

        public void Initialize()
        {
            if (!PluginConfig.Instance.ModEnable)
            {
                return;
            }

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

            _needs360Move = _sceneSetupData.difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.requires360Movement;
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

            _currentCanvas = counter.CurrentCanvas;
            _numbersText = counter.Numbers;

            if (_currentCanvas == null)
            {
                _logger.Error("Cannot create custom counter");
                return false;
            }

            _currentCanvas.transform.localScale = Vector3.one / 150;

            OnHRUpdate(BPM.Instance.Bpm); // give it an initial value

            if (!_needs360Move)
            {
                // Place our Canvas in a Static Location
                var location = PluginConfig.Instance.StaticCounterPosition;
                _currentCanvas.transform.position = new Vector3(location.x, location.y, location.z);
                _currentCanvas.transform.rotation = Quaternion.identity;
            }
            else
            {
                // Attach it to the FlyingHUD
                _currentCanvas.AddComponent<MapMover>();
            }

            HRDataManager.OnHRUpdate += OnHRUpdate;
            PluginConfig.Instance.OnSettingsChanged += OnSettingChange;
            return true;
        }

        private void OnHRUpdate(int bpm)
        {
            _numbersText.text = Colorize ? $"<color=#{Utils.Utils.DetermineColor(bpm)}>{bpm}</color>" : $"{bpm}";
        }

        private void OnSettingChange(object? sender, EventArgs e)
        {
            Log.Logger.Info("Settings changed, updating counter location.");
            try
            {
                if (!_needs360Move)
                {
                    var location = PluginConfig.Instance.StaticCounterPosition;
                    _currentCanvas.transform.position = new Vector3(location.x, location.y, location.z);
                }
            }
            catch (Exception exception)
            {
                Log.Logger.Warn($"Exception Caught during counter location update");
                Log.Logger.Warn(exception);
            }
        }

        public void Dispose()
        {
            HRDataManager.OnHRUpdate -= OnHRUpdate;
            PluginConfig.Instance.OnSettingsChanged -= OnSettingChange;

            if (_currentCanvas != null)
            {
                Object.Destroy(_currentCanvas);
            }

            Plugin.Log.Info("HRCounter Disposed");
        }

        /// <summary>
        /// Will have the canvas follow the player when on a 90/360 degree map
        /// </summary>
        private class MapMover : MonoBehaviour
        {
            private GameObject? _flyingHUD;
            private RectTransform? _iconRtTransform;

            private GameObject _currentCanvas = null!;
            private GameObject _icon = null!;

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

                if (_flyingHUD == null || _iconRtTransform == null)
                {
                    enabled = false;
                    Destroy(this);
                }
            }

            private void Update()
            {
                _currentCanvas.transform.position = _flyingHUD!.transform.position;
                var position = _iconRtTransform!.localPosition;
                _iconRtTransform.localPosition = new Vector3(position.x, 150, position.z);
                _currentCanvas.transform.rotation = _flyingHUD.transform.rotation;
            }

            private GameObject? GetFlyingHUD()
            {
                // There should only be one
                Scene? scene = null;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var s = SceneManager.GetSceneAt(i);
                    if (s.name.Contains("Environment") && s.isLoaded)
                    {
                        scene = s;
                        break;
                    }
                }

                // Find the GameObject
                var environment = scene?.GetRootGameObjects().FirstOrDefault(x => x.name == "Environment");

                return environment == null ? null : environment.transform.Find("FlyingGameHUD/Container")?.gameObject;
            }
        }
    }
}