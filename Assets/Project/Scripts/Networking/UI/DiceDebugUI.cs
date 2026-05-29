using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Networking.Services;
using Networking.Models;

namespace Networking.UI
{
/// <summary>
    /// UI-based Debug tool for forcing dice results. 
    /// Responds to the New Input System and uGUI interactions.
    /// </summary>
    public class DiceDebugUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Toggle _enableToggle;
        [SerializeField] private TextMeshProUGUI _valueDisplay;
        [SerializeField] private Button _btnMinus;
        [SerializeField] private Button _btnPlus;
        [SerializeField] private Button[] _quickButtons;
        
        [Header("Weather Debug")]
        [SerializeField] private Button _btnSunDebug;
        [SerializeField] private Button _btnRainDebug;
        [SerializeField] private Button _btnFreezeDebug;
        [SerializeField] private Button _btnClearWeather;

        [Header("Minigame Debug")]
        [SerializeField] private Button _btnMinigameClick;
        [SerializeField] private Button _btnMinigamePipe;
        [SerializeField] private Button _btnMinigameWeather;
        [SerializeField] private Button _btnMinigameRain;
        [SerializeField] private Button _btnMinigameRandom;

        private void Start()
        {
            if (_enableToggle != null)
            {
                _enableToggle.isOn = DiceDebugService.IsEnabled;
                _enableToggle.onValueChanged.AddListener(OnToggleChanged);
            }

            if (_btnMinus != null) _btnMinus.onClick.AddListener(() => ChangeValue(-1));
            if (_btnPlus != null) _btnPlus.onClick.AddListener(() => ChangeValue(1));

            if (_quickButtons != null)
            {
                for (int i = 0; i < _quickButtons.Length; i++)
                {
                    int val = i + 1;
                    _quickButtons[i].onClick.AddListener(() => SetValue(val));
                }
            }

            if (_btnSunDebug != null) _btnSunDebug.onClick.AddListener(() => SetWeatherDebug(WeatherTag.Drought));
            if (_btnRainDebug != null) _btnRainDebug.onClick.AddListener(() => SetWeatherDebug(WeatherTag.Rain));
            if (_btnFreezeDebug != null) _btnFreezeDebug.onClick.AddListener(() => SetWeatherDebug(WeatherTag.Freeze));
            if (_btnClearWeather != null) _btnClearWeather.onClick.AddListener(() => SetWeatherDebug(WeatherTag.None));

            if (_btnMinigameClick != null) _btnMinigameClick.onClick.AddListener(() => SetMinigameDebug("Minigame"));
            if (_btnMinigamePipe != null) _btnMinigamePipe.onClick.AddListener(() => SetMinigameDebug("PipeMinigame"));
            if (_btnMinigameWeather != null) _btnMinigameWeather.onClick.AddListener(() => SetMinigameDebug("WeatherMinigame"));
            if (_btnMinigameRain != null) _btnMinigameRain.onClick.AddListener(() => SetMinigameDebug("RainMinigame"));
            if (_btnMinigameRandom != null) _btnMinigameRandom.onClick.AddListener(() => SetMinigameDebug(""));

            UpdateDisplay();
            
            // Start hidden
            if (_panel != null) _panel.SetActive(false);
        }

        private void SetMinigameDebug(string sceneName)
        {
            MinigameDebugService.IsEnabled = !string.IsNullOrEmpty(sceneName);
            MinigameDebugService.ForcedMinigameScene = sceneName;
            Debug.Log($"[DiceDebugUI] Minigame Debug Override: {(string.IsNullOrEmpty(sceneName) ? "Random" : sceneName)}");
            UpdateDisplay();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
            {
                if (_panel != null) _panel.SetActive(!_panel.activeSelf);
            }
        }

        public void Setup(GameObject panel, Toggle toggle, TextMeshProUGUI valueText, Button minus, Button plus, Button[] quicks, Button sun, Button rain, Button clear)
        {
            _panel = panel;
            _enableToggle = toggle;
            _valueDisplay = valueText;
            _btnMinus = minus;
            _btnPlus = plus;
            _quickButtons = quicks;
            _btnSunDebug = sun;
            _btnRainDebug = rain;
            _btnClearWeather = clear;
        }

        private void SetWeatherDebug(WeatherTag tag)
        {
            if (WeatherVisualsManager.Instance != null)
            {
                WeatherVisualsManager.Instance.SetDebugOverride(tag, tag != WeatherTag.None);
                Debug.Log($"[DiceDebugUI] Weather Debug Override: {tag}");
            }
        }

        private void OnToggleChanged(bool isOn)
        {
            DiceDebugService.IsEnabled = isOn;
        }

        private void ChangeValue(int delta)
        {
            DiceDebugService.ForcedValue = Mathf.Clamp(DiceDebugService.ForcedValue + delta, 1, 10);
            UpdateDisplay();
        }

        private void SetValue(int val)
        {
            DiceDebugService.ForcedValue = val;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_valueDisplay != null)
            {
                string minigameInfo = MinigameDebugService.IsEnabled ? $"\nMinigame: {MinigameDebugService.ForcedMinigameScene}" : "\nMinigame: Random";
                _valueDisplay.text = $"Value: {DiceDebugService.ForcedValue}{minigameInfo}";
            }
        }
}
}
