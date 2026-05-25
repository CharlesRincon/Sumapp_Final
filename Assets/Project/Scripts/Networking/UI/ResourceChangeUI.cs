using UnityEngine;
using TMPro;
using System.Collections;

namespace Networking.UI
{
    public class ResourceChangeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI _waterText;
        [SerializeField] private TextMeshProUGUI _moneyText;
        [SerializeField] private Transform _waterIconTransform;
        [SerializeField] private Transform _moneyIconTransform;

        [Header("Animation Settings")]
        [SerializeField] private float _tweenDuration = 0.8f;
        [SerializeField] private float _popupDuration = 1.5f;
        [SerializeField] private float _popupMoveDistance = 100f;
        [SerializeField] private float _popupScaleAmount = 1.2f;

        [Header("Colors")]
        [SerializeField] private Color _gainColor = Color.green;
        [SerializeField] private Color _lossColor = Color.red;

        private int _lastWater;
        private int _lastMoney;
        private bool _isInitialized;

        public void Initialize(int water, int money)
        {
            _lastWater = water;
            _lastMoney = money;
            UpdateText(_waterText, water);
            UpdateText(_moneyText, money);
            _isInitialized = true;
        }

        public void OnResourcesChanged(int currentWater, int currentMoney)
        {
            if (!_isInitialized)
            {
                Initialize(currentWater, currentMoney);
                return;
            }

            if (currentWater != _lastWater)
            {
                int delta = currentWater - _lastWater;
                AnimateResource(_waterText, _lastWater, currentWater, _waterIconTransform, delta);
                _lastWater = currentWater;
            }

            if (currentMoney != _lastMoney)
            {
                int delta = currentMoney - _lastMoney;
                AnimateResource(_moneyText, _lastMoney, currentMoney, _moneyIconTransform, delta);
                _lastMoney = currentMoney;
            }
        }

        private void AnimateResource(TextMeshProUGUI textComp, int start, int end, Transform iconTransform, int delta)
        {
            if (textComp == null) return;

            // 1. Animate the counter value
            LeanTween.cancel(textComp.gameObject);
            LeanTween.value(textComp.gameObject, (float val) => {
                textComp.text = Mathf.RoundToInt(val).ToString();
            }, start, end, _tweenDuration).setEase(LeanTweenType.easeOutQuad);

            // 2. Punch scale effect on the text for emphasis
            textComp.transform.localScale = Vector3.one;
            LeanTween.scale(textComp.gameObject, Vector3.one * 1.3f, 0.2f).setEasePunch();

            // 3. Spawn floating popup
            if (iconTransform != null)
            {
                SpawnFloatingText(iconTransform, delta);
            }
        }

        private void SpawnFloatingText(Transform parent, int delta)
        {
            GameObject go = new GameObject("ResourcePopup", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = delta > 0 ? $"+{delta}" : delta.ToString();
            text.color = delta > 0 ? _gainColor : _lossColor;
            text.fontSize = 36;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;

            // LeanTween Animations
            // Scale Up and Down
            go.transform.localScale = Vector3.zero;
            LeanTween.scale(go, Vector3.one * _popupScaleAmount, 0.3f).setEase(LeanTweenType.easeOutBack);
            
            // Move Up
            LeanTween.moveLocalY(go, _popupMoveDistance, _popupDuration).setEase(LeanTweenType.easeOutSine);

            // Fade Out
            LeanTween.value(go, 1f, 0f, _popupDuration).setEase(LeanTweenType.easeInSine).setOnUpdate((float alpha) => {
                if (text != null)
                {
                    Color c = text.color;
                    c.a = alpha;
                    text.color = c;
                }
            }).setOnComplete(() => {
                Destroy(go);
            });
        }

        private void UpdateText(TextMeshProUGUI textComp, int val)
        {
            if (textComp != null)
                textComp.text = val.ToString();
        }
    }
}
