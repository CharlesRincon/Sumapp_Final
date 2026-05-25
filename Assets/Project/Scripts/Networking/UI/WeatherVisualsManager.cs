using UnityEngine;
using Networking.Managers;
using Networking.Models;
using System.Collections.Generic;

namespace Networking.UI
{
    /// <summary>
    /// Controls the visual weather effects in the UI, supporting both Shaders and Particles.
    /// Local to the LobbyScene; naturally disappears during minigames.
    /// </summary>
    public class WeatherVisualsManager : MonoBehaviour
    {
        public static WeatherVisualsManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private float _fadeDuration = 1.5f;
        
        [Header("Shader Overlays (CanvasGroups)")]
        [SerializeField] private CanvasGroup _sunnyShaderOverlay; // For El Niño (Drought)

        [Header("Particle Systems")]
        [SerializeField] private UIParticleSystem _rainParticles;  // For La Niña (Rain)
        [SerializeField] private UIParticleSystem _floodParticles;
        [SerializeField] private UIParticleSystem _freezeParticles;

        private WeatherTag _lastWeatherTag = WeatherTag.None;
        private int _lastWeatherVersion = -1;

        private WeatherTag _debugOverrideTag = WeatherTag.None;
        private bool _isDebugOverrideActive = false;

        public void SetDebugOverride(WeatherTag tag, bool active)
        {
            _isDebugOverrideActive = active;
            _debugOverrideTag = tag;
            
            TransitionWeather(_lastWeatherTag, active ? tag : (_gameManager != null ? _gameManager.ActiveWeatherTag : WeatherTag.None));
            _lastWeatherTag = active ? tag : (_gameManager != null ? _gameManager.ActiveWeatherTag : WeatherTag.None);
        }

        private void Awake()
        {
            Instance = this;

            // Initial state: hide everything
            HideAllVisualsImmediately();
        }

        private void HideAllVisualsImmediately()
        {
            if (_sunnyShaderOverlay != null)
            {
                _sunnyShaderOverlay.alpha = 0f;
                _sunnyShaderOverlay.gameObject.SetActive(false);
            }

            if (_rainParticles != null) { _rainParticles.Stop(); _rainParticles.Clear(); _rainParticles.gameObject.SetActive(false); }
            if (_floodParticles != null) { _floodParticles.Stop(); _floodParticles.Clear(); _floodParticles.gameObject.SetActive(false); }
            if (_freezeParticles != null) { _freezeParticles.Stop(); _freezeParticles.Clear(); _freezeParticles.gameObject.SetActive(false); }
        }

        private void Start()
        {
            if (_gameManager == null)
                _gameManager = GameManager.Instance;
            
            // Sync initial state if game is already running
            if (_gameManager != null)
            {
                WeatherTag currentTag = _gameManager.ActiveWeatherTag;
                int currentVersion = _gameManager.ActiveWeatherVersion;
                if (currentTag != WeatherTag.None)
                {
                    StartWeatherEffect(currentTag);
                    _lastWeatherTag = currentTag;
                    _lastWeatherVersion = currentVersion;
                }
            }
        }

        private void Update()
        {
            if (_isDebugOverrideActive) return;

            if (_gameManager == null)
            {
                _gameManager = GameManager.Instance;
                return;
            }

            WeatherTag currentTag = _gameManager.ActiveWeatherTag;
            int currentVersion = _gameManager.ActiveWeatherVersion;

            if (currentTag != _lastWeatherTag || currentVersion != _lastWeatherVersion)
            {
                TransitionWeather(_lastWeatherTag, currentTag);
                _lastWeatherTag = currentTag;
                _lastWeatherVersion = currentVersion;
            }
        }

        private void TransitionWeather(WeatherTag oldTag, WeatherTag newTag)
        {
            StopWeatherEffect(oldTag);
            StartWeatherEffect(newTag);
            Debug.Log($"[WeatherVisualsManager] Transitioning: {oldTag} -> {newTag}");
        }

        private void StartWeatherEffect(WeatherTag tag)
        {
            switch (tag)
            {
                case WeatherTag.Drought:
                    if (_sunnyShaderOverlay != null)
                    {
                        _sunnyShaderOverlay.gameObject.SetActive(true);
                        LeanTween.alphaCanvas(_sunnyShaderOverlay, 1f, _fadeDuration).setEase(LeanTweenType.easeInOutQuad);
                    }
                    break;

                case WeatherTag.Rain:
                    if (_rainParticles != null) 
                    { 
                        _rainParticles.gameObject.SetActive(true);
                        _rainParticles.Clear(); 
                        _rainParticles.Play(); 
                    }
                    break;

                case WeatherTag.Flood:
                    if (_floodParticles != null) 
                    { 
                        _floodParticles.gameObject.SetActive(true);
                        _floodParticles.Clear(); 
                        _floodParticles.Play(); 
                    }
                    break;

                case WeatherTag.Freeze:
                    if (_freezeParticles != null) 
                    { 
                        _freezeParticles.gameObject.SetActive(true);
                        _freezeParticles.Clear(); 
                        _freezeParticles.Play(); 
                    }
                    break;
            }
        }

        private void StopWeatherEffect(WeatherTag tag)
        {
            switch (tag)
            {
                case WeatherTag.Drought:
                    if (_sunnyShaderOverlay != null)
                    {
                        LeanTween.alphaCanvas(_sunnyShaderOverlay, 0f, _fadeDuration)
                            .setEase(LeanTweenType.easeInOutQuad)
                            .setOnComplete(() => _sunnyShaderOverlay.gameObject.SetActive(false));
                    }
                    break;

                case WeatherTag.Rain:
                    if (_rainParticles != null) _rainParticles.Stop();
                    break;

                case WeatherTag.Flood:
                    if (_floodParticles != null) _floodParticles.Stop();
                    break;

                case WeatherTag.Freeze:
                    if (_freezeParticles != null) _freezeParticles.Stop();
                    break;
            }
        }
    }
}
