using System;
using System.Linq;
using HRCounter.Configuration;
using HRCounter.Data;
using HRCounter.Utils;
using JetBrains.Annotations;
using SiraUtil.Logging;
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
        [InjectOptional] private readonly GameplayCoreSceneSetupData? _sceneSetupData = null;
        [Inject] private readonly AssetBundleManager _assetBundleManager = null!;
        [Inject] private readonly PluginConfig _config = null!;
        [Inject] private readonly SiraLog _logger = null!;
        [Inject] private readonly RenderUtils _renderUtils = null!;
        [InjectOptional] private readonly HRDataManager? _hrDataManager;
        [InjectOptional] private CoreGameHUDController.InitData? _hudInitData = null;
        private bool _needs360Move;

        private GameObject _currentCanvas = null!;
        private TMP_Text _numbersText = null!;

        public void Initialize()
        {
            if (!_config.ModEnable)
            {
                return;
            }
            
            if (_hudInitData == null)
            {
                _logger.Warn("CoreGameHUDController.InitData is null, not creating HRCounter");
                return;
            }
            
            if (_hudInitData.hide)
            {
                _logger.Info("CoreGameHUDController.InitData says to hide the HUD, not creating HRCounter");
                return;
            }

            if (_config.HideDuringReplay && Utils.Utils.IsInReplay())
            {
                _logger.Info("We are in a replay, HRCounter hides.");
                return;
            }

            if (_hrDataManager == null)
            {
                Plugin.Log.Warn("HRDataManager is null");
                return;
            }

            _needs360Move = _sceneSetupData.difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.requires360Movement;
            Plugin.Log.Info($"360/90?: {_needs360Move}");

            if (!CreateCounter())
            {
                _logger.Warn("Cannot create HRCounter");
                return;
            }

            _hrDataManager.OnHRUpdate -= OnHRUpdate;
            _hrDataManager.OnHRUpdate += OnHRUpdate;
            _config.OnSettingsChanged += OnSettingChange;
            
            _logger.Info("HRCounter Initialized");
        }

        private bool CreateCounter()
        {
            _logger.Info("Creating HRCounter");
            var counter = _assetBundleManager.SetupCustomCounter();
            if (!counter.IsNotNull())
            {
                _logger.Warn("No Counter asset is loaded!");
                return false;
            }

            _currentCanvas = counter.Counter!;
            _numbersText = counter.Numbers!;

            _currentCanvas.transform.localScale = Vector3.one / 150;

            OnHRUpdate(BPM.Bpm); // give it an initial value

            if (!_needs360Move)
            {
                // Place our Canvas in a Static Location
                var location = _config.StaticCounterPosition;
                _currentCanvas.transform.position = new Vector3(location.x, location.y, location.z);
                _currentCanvas.transform.rotation = Quaternion.identity;
            }
            else
            {
                // Attach it to the FlyingHUD
                _currentCanvas.AddComponent<MapMover>();
            }
            return true;
        }

        private void OnHRUpdate(int bpm)
        {
            _numbersText.color = _config.Colorize ? _renderUtils.DetermineColor(bpm) : Color.white;
            _numbersText.text = bpm.ToString();
        }

        private void OnSettingChange()
        {
            _logger.Info("Settings changed, updating counter location.");
            try
            {
                if (!_needs360Move)
                {
                    var location = _config.StaticCounterPosition;
                    _currentCanvas.transform.position = new Vector3(location.x, location.y, location.z);
                }
            }
            catch (Exception exception)
            {
                _logger.Warn($"Exception Caught during counter location update");
                _logger.Warn(exception);
            }
        }

        public void Dispose()
        {
            if (_hrDataManager != null)
            {
                _hrDataManager.OnHRUpdate -= OnHRUpdate;
            }
            
            _config.OnSettingsChanged -= OnSettingChange;

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