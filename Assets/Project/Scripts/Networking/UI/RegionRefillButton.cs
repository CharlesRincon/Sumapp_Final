using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Networking.Models;

namespace Networking.UI
{
    /// <summary>
    /// UI component for a regional refill button.
    /// Tracks water level from the RegionDroughtManager and displays it.
    /// </summary>
    public class RegionRefillButton : MonoBehaviour
    {
        [SerializeField] private int _regionIndex;
        [SerializeField] private TextMeshProUGUI _regionNameText;
        [SerializeField] private Slider _waterSlider;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Color _normalColor = Color.blue;
        [SerializeField] private Color _emergencyColor = Color.red;
        [SerializeField] private GameObject _emergencyIndicator;

        private Managers.RegionDroughtManager _manager;
        private NetworkRunner _runner;
        private Button _button;
        private bool _isDead = false;

        private void Start()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClick);
            }

            _runner = FindFirstObjectByType<NetworkRunner>();
            
            // Set region name based on index (mapping to ColombiaZone enum)
            if (_regionNameText != null)
            {
                // Mapping: 0:Caribbean, 1:Pacific, 2:Andean, 3:Orinoquia, 4:Amazon, 5:Insular
                _regionNameText.text = ((ColombiaZone)_regionIndex).ToString();
            }
        }

        private void Update()
        {
            if (_manager == null)
            {
                _manager = FindFirstObjectByType<Managers.RegionDroughtManager>();
                return;
            }

            if (!_manager.Object.IsValid) return;

            // Sync visual state from networked data
            float level = _manager.RegionWaterLevels[_regionIndex];
            bool isEmergency = _manager.EmergencyRegionIndex == _regionIndex;

            // Handle "Death"
            if (level <= 0 && !_isDead)
            {
                TriggerDeathSequence();
            }
            else if (level > 0 && _isDead)
            {
                // In case a region is revived (though not currently implemented in logic)
                _isDead = false;
                if (_button != null) _button.interactable = true;
                LeanTween.color(GetComponent<RectTransform>(), Color.white, 0.5f);
            }

            if (_waterSlider != null)
            {
                _waterSlider.value = level / 100f;
            }

            // Dynamic Color Logic: Reset to blue if healthy
            if (_fillImage != null && !_isDead)
            {
                Color targetColor = _normalColor;
                if (isEmergency) targetColor = _emergencyColor;
                else if (level < 30f) targetColor = Color.yellow;

                // Smoothly transition color if it changed
                if (_fillImage.color != targetColor)
                {
                    _fillImage.color = Color.Lerp(_fillImage.color, targetColor, Time.deltaTime * 5f);
                }
            }

            if (_emergencyIndicator != null)
            {
                // Only show indicator if not dead
                _emergencyIndicator.SetActive(isEmergency && !_isDead);
            }
        }

        private void TriggerDeathSequence()
        {
            _isDead = true;
            if (_button != null) _button.interactable = false;

            // Visual death: Turn grayscale or dark
            LeanTween.color(GetComponent<RectTransform>(), Color.gray, 1f).setEaseOutQuad();
            
            // Shake effect
            LeanTween.moveX(GetComponent<RectTransform>(), 5f, 0.1f).setLoopPingPong(3);
            
            // Fade out name text slightly
            if (_regionNameText != null)
            {
                LeanTween.value(_regionNameText.gameObject, _regionNameText.color.a, 0.3f, 1f).setOnUpdate((float a) => {
                    Color c = _regionNameText.color;
                    c.a = a;
                    _regionNameText.color = c;
                });
            }

            Debug.Log($"[RegionRefillButton] Region {_regionIndex} has died.");
        }

        private void OnButtonClick()
        {
            if (_manager == null || _runner == null || _isDead) return;

            if (!_manager.IsGameActive()) return;

            // Request refill via RPC
            _manager.RPC_RefillRegion(_regionIndex, _runner.LocalPlayer);
            
            // Local visual feedback: Scale punch and color flash
            LeanTween.cancel(gameObject);
            transform.localScale = Vector3.one;
            LeanTween.scale(gameObject, Vector3.one * 1.1f, 0.1f).setEaseOutQuad().setLoopPingPong(1);
            
            // Flash color briefly
            if (_fillImage != null)
            {
                Color original = _fillImage.color;
                LeanTween.value(gameObject, 0f, 1f, 0.1f).setOnUpdate((float val) => {
                    _fillImage.color = Color.Lerp(original, Color.white, val);
                }).setLoopPingPong(1);
            }
        }

        public void Setup(int index, string regionName)
        {
            _regionIndex = index;
            if (_regionNameText != null) _regionNameText.text = regionName;
        }
    }
}
