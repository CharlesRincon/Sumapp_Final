using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.UI
{
    public class AnimationsLogic : MonoBehaviour
    {
        // Inspector-assigned animation-only references
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private RectTransform _loadingPanelImage;
        [Space]
        [SerializeField] private GameObject _openingPanel;
        [SerializeField] private RectTransform _waveImage1;
        [SerializeField] private RectTransform _waveImage2;
        [SerializeField] private RectTransform _waveImage3;
        [SerializeField] private RectTransform _openingWaterdrop;
        [SerializeField] private GameObject _openingTitleObject;
        [SerializeField] private GameObject _tapToContinueObject;
        [Space]
        [SerializeField] private RectTransform _turnNotificationPrimaryImage;
        [SerializeField] private RectTransform _turnNotificationSecondaryImage;
        [SerializeField] private RectTransform _turnNotificationFadeImage;
        [SerializeField] private RectTransform _turnNotificationSpinningFadeImage;
        [SerializeField] private float _turnNotificationEnterOffsetX = 420f;
        [SerializeField] private float _turnNotificationEntryDuration = 0.45f;
        [SerializeField] private float _turnNotificationDriftDistance = 36f;
        [SerializeField] private float _turnNotificationExitDistance = 460f;
        [SerializeField] private float _turnNotificationExitLeadTime = 0.35f;
        [SerializeField] private float _turnNotificationExitDuration = 0.2f;
        [SerializeField] private float _turnNotificationSecondaryImageDelay = 0.12f;
        [SerializeField] private float _turnNotificationFadeImageDelay = 0.08f;
        [SerializeField] private float _turnNotificationFadeDuration = 0.3f;
        [SerializeField] private float _turnNotificationSpinningFadeImageDelay = 0.1f;
        [SerializeField] private float _turnNotificationSpinDuration = 2.4f;
        [Space]
        [SerializeField] private float _turnNotificationDuration = 3f;

        // Read-only access for LobbyCanvas
        public GameObject OpeningPanel => _openingPanel;
        public GameObject LoadingPanel => _loadingPanel;

        // Runtime animation cache
        private Coroutine _wave1Coroutine;
        private Coroutine _wave2Coroutine;
        private Coroutine _wave3Coroutine;
        private Coroutine _tapPulseCoroutine;
        private Coroutine _hideNotificationCoroutine;

        private RectTransform _openingTitle;

        private Vector2 _openingWaterdropStartPos;
        private Vector3 _openingWaterdropStartScale;
        private float _openingWaterdropStartZ;
        private CanvasGroup _openingWaterdropCanvasGroup;

        private Vector2 _openingTitleStartPos;
        private Vector3 _openingTitleStartScale;
        private CanvasGroup _openingTitleCanvasGroup;

        private Vector2 _turnNotificationPrimaryImageStartPos;
        private bool _turnNotificationPrimaryImageCached;
        private Vector2 _turnNotificationSecondaryImageStartPos;
        private bool _turnNotificationSecondaryImageCached;
        private Image _turnNotificationPrimaryImageComponent;
        private Image _turnNotificationSecondaryImageComponent;
        private Image _turnNotificationFadeImageComponent;
        private Image _turnNotificationSpinningFadeImageComponent;
        private CanvasGroup _turnNotificationPrimaryImageCanvasGroup;
        private CanvasGroup _turnNotificationSecondaryImageCanvasGroup;
        private CanvasGroup _turnNotificationFadeImageCanvasGroup;
        private CanvasGroup _turnNotificationSpinningFadeImageCanvasGroup;
        private float _turnNotificationSpinningFadeImageStartZ;
        private bool _turnNotificationSpinningFadeImageCached;

        public void StartLoadingImageAnimation()
        {
            if (_loadingPanelImage == null) return;

            Vector3 startPos = _loadingPanelImage.anchoredPosition3D;
            float floatAmount = 18f;
            float duration = 1.6f;

            LeanTween.moveLocalY(_loadingPanelImage.gameObject, startPos.y + floatAmount, duration)
                .setEase(LeanTweenType.easeInOutSine)
                .setLoopPingPong();
        }

        public void CancelLoadingPanelAnimation()
        {
            if (_loadingPanel != null)
                LeanTween.cancel(_loadingPanel);

            if (_loadingPanelImage != null)
                LeanTween.cancel(_loadingPanelImage.gameObject);
        }

        public void StartOpeningPanelAnimations()
        {
            _openingTitle = _openingTitleObject != null ? _openingTitleObject.GetComponent<RectTransform>() : null;

            if (_waveImage1 != null)
                _wave1Coroutine = StartCoroutine(AnimateWave(_waveImage1, 14f, 1.1f, 0f));
            if (_waveImage2 != null)
                _wave2Coroutine = StartCoroutine(AnimateWave(_waveImage2, 20f, 0.85f, 1.2f));
            if (_waveImage3 != null)
                _wave3Coroutine = StartCoroutine(AnimateWave(_waveImage3, 10f, 1.4f, 2.5f));
            if (_tapToContinueObject != null)
                _tapPulseCoroutine = StartCoroutine(PulseTapToContinue(_tapToContinueObject));

            StartOpeningPanelMainTweens();
        }

        private void StartOpeningPanelMainTweens()
        {
            if (_openingWaterdrop != null)
            {
                _openingWaterdropStartPos = _openingWaterdrop.anchoredPosition;
                _openingWaterdropStartScale = _openingWaterdrop.localScale;
                _openingWaterdropStartZ = _openingWaterdrop.localEulerAngles.z;

                if (_openingWaterdropCanvasGroup == null)
                {
                    _openingWaterdropCanvasGroup = _openingWaterdrop.GetComponent<CanvasGroup>();
                    if (_openingWaterdropCanvasGroup == null)
                        _openingWaterdropCanvasGroup = _openingWaterdrop.gameObject.AddComponent<CanvasGroup>();
                }

                LeanTween.cancel(_openingWaterdrop.gameObject);
                LeanTween.cancel(_openingWaterdropCanvasGroup.gameObject);

                _openingWaterdropCanvasGroup.alpha = 0f;

                LeanTween.alphaCanvas(_openingWaterdropCanvasGroup, 1f, 0.9f)
                    .setEase(LeanTweenType.easeOutSine);

                LeanTween.moveLocalY(_openingWaterdrop.gameObject, _openingWaterdropStartPos.y + 14f, 1.8f)
                    .setEase(LeanTweenType.easeInOutSine)
                    .setLoopPingPong();

                LeanTween.scale(_openingWaterdrop.gameObject, _openingWaterdropStartScale + new Vector3(0.04f, -0.02f, 0f), 1.6f)
                    .setEase(LeanTweenType.easeInOutSine)
                    .setLoopPingPong();

                LeanTween.rotateZ(_openingWaterdrop.gameObject, _openingWaterdropStartZ + 3f, 2.2f)
                    .setEase(LeanTweenType.easeInOutSine)
                    .setLoopPingPong();
            }

            if (_openingTitle != null)
            {
                _openingTitleStartPos = _openingTitle.anchoredPosition;
                _openingTitleStartScale = _openingTitle.localScale;

                if (_openingTitleCanvasGroup == null)
                {
                    _openingTitleCanvasGroup = _openingTitle.GetComponent<CanvasGroup>();
                    if (_openingTitleCanvasGroup == null)
                        _openingTitleCanvasGroup = _openingTitle.gameObject.AddComponent<CanvasGroup>();
                }

                LeanTween.cancel(_openingTitle.gameObject);
                LeanTween.cancel(_openingTitleCanvasGroup.gameObject);

                _openingTitle.anchoredPosition = _openingTitleStartPos + new Vector2(0f, -20f);
                _openingTitle.localScale = _openingTitleStartScale;
                _openingTitleCanvasGroup.alpha = 0f;

                Vector2 introFrom = _openingTitle.anchoredPosition;
                LeanTween.value(_openingTitle.gameObject, 0f, 1f, 0.8f)
                    .setEase(LeanTweenType.easeOutCubic)
                    .setOnUpdate((float t) =>
                    {
                        if (_openingTitle != null)
                        {
                            _openingTitle.anchoredPosition = Vector2.LerpUnclamped(introFrom, _openingTitleStartPos, t);
                        }
                    });

                LeanTween.alphaCanvas(_openingTitleCanvasGroup, 1f, 0.8f)
                    .setEase(LeanTweenType.easeOutSine)
                    .setOnComplete(() =>
                    {
                        if (_openingTitle != null)
                        {
                            LeanTween.scale(_openingTitle.gameObject, _openingTitleStartScale * 1.1f, 2f)
                                .setEase(LeanTweenType.easeInOutSine)
                                .setLoopPingPong();
                        }
                    });
            }
        }

        public void StopOpeningPanelAnimations()
        {
            if (_wave1Coroutine != null) { StopCoroutine(_wave1Coroutine); _wave1Coroutine = null; }
            if (_wave2Coroutine != null) { StopCoroutine(_wave2Coroutine); _wave2Coroutine = null; }
            if (_wave3Coroutine != null) { StopCoroutine(_wave3Coroutine); _wave3Coroutine = null; }
            if (_tapPulseCoroutine != null) { StopCoroutine(_tapPulseCoroutine); _tapPulseCoroutine = null; }

            if (_openingWaterdrop != null)
            {
                LeanTween.cancel(_openingWaterdrop.gameObject);
                _openingWaterdrop.anchoredPosition = _openingWaterdropStartPos;
                _openingWaterdrop.localScale = _openingWaterdropStartScale;
                _openingWaterdrop.localRotation = Quaternion.Euler(0f, 0f, _openingWaterdropStartZ);
            }

            if (_openingWaterdropCanvasGroup != null)
            {
                LeanTween.cancel(_openingWaterdropCanvasGroup.gameObject);
                _openingWaterdropCanvasGroup.alpha = 1f;
            }

            if (_openingTitle != null)
            {
                LeanTween.cancel(_openingTitle.gameObject);
                _openingTitle.anchoredPosition = _openingTitleStartPos;
                _openingTitle.localScale = _openingTitleStartScale;
            }

            if (_openingTitleCanvasGroup != null)
            {
                LeanTween.cancel(_openingTitleCanvasGroup.gameObject);
                _openingTitleCanvasGroup.alpha = 1f;
            }
        }

        public void CloseOpeningPanel(GameObject initPanel)
        {
            StopOpeningPanelAnimations();

            if (_openingPanel != null)
                _openingPanel.SetActive(false);
            if (initPanel != null)
                initPanel.SetActive(true);
        }

        public void HideOpeningPanelImmediate()
        {
            if (_openingPanel == null) return;
            StopOpeningPanelAnimations();
            _openingPanel.SetActive(false);
        }

        public void SetTurnNotificationSecondaryImageSprite(Sprite sprite)
        {
            if (_turnNotificationSecondaryImage == null)
            {
                return;
            }

            if (_turnNotificationSecondaryImageComponent == null)
            {
                _turnNotificationSecondaryImageComponent = _turnNotificationSecondaryImage.GetComponent<Image>();
                if (_turnNotificationSecondaryImageComponent == null)
                {
                    Debug.LogWarning("[AnimationsLogic] Turn notification secondary image is missing an Image component.");
                    return;
                }
            }

            _turnNotificationSecondaryImageComponent.sprite = sprite;
            _turnNotificationSecondaryImageComponent.enabled = sprite != null;
        }

        public void SetTurnNotificationAccentColor(Color color)
        {
            ApplyImageColor(_turnNotificationPrimaryImage, ref _turnNotificationPrimaryImageComponent, color);
            ApplyImageColor(_turnNotificationFadeImage, ref _turnNotificationFadeImageComponent, color);
            ApplyImageColor(_turnNotificationSpinningFadeImage, ref _turnNotificationSpinningFadeImageComponent, color);
        }

        public void ShowTurnNotification(GameObject notificationPanel, TextMeshProUGUI notificationText, string message)
        {
            if (notificationPanel == null) return;

            if (notificationText != null)
                notificationText.text = message;

            notificationPanel.SetActive(true);
            AnimateTurnNotificationPrimaryImage();
            AnimateTurnNotificationSecondaryImage();
            AnimateTurnNotificationFadeImage();
            AnimateTurnNotificationSpinningFadeImage();

            if (_hideNotificationCoroutine != null)
                StopCoroutine(_hideNotificationCoroutine);
            _hideNotificationCoroutine = StartCoroutine(HideNotificationAfterDelay(notificationPanel, _turnNotificationDuration));
        }

        public void HideTurnNotification(GameObject notificationPanel)
        {
            if (_hideNotificationCoroutine != null)
            {
                StopCoroutine(_hideNotificationCoroutine);
                _hideNotificationCoroutine = null;
            }

            ResetTurnNotificationPrimaryImage();
            ResetTurnNotificationSecondaryImage();
            ResetTurnNotificationFadeImage();
            ResetTurnNotificationSpinningFadeImage();

            if (notificationPanel != null)
                notificationPanel.SetActive(false);
        }

        private IEnumerator HideNotificationAfterDelay(GameObject notificationPanel, float duration)
        {
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }

            ResetTurnNotificationPrimaryImage();
            ResetTurnNotificationSecondaryImage();
            ResetTurnNotificationFadeImage();
            ResetTurnNotificationSpinningFadeImage();
            if (notificationPanel != null)
                notificationPanel.SetActive(false);
            _hideNotificationCoroutine = null;
        }

        private void AnimateTurnNotificationPrimaryImage()
        {
            CacheTurnNotificationPrimaryImageStartPos();
            AnimateTurnNotificationImage(_turnNotificationPrimaryImage, _turnNotificationPrimaryImageStartPos, 0f, ref _turnNotificationPrimaryImageCanvasGroup);
        }

        private void AnimateTurnNotificationSecondaryImage()
        {
            CacheTurnNotificationSecondaryImageStartPos();
            AnimateTurnNotificationImage(_turnNotificationSecondaryImage, _turnNotificationSecondaryImageStartPos, _turnNotificationSecondaryImageDelay, ref _turnNotificationSecondaryImageCanvasGroup);
        }

        private void AnimateTurnNotificationFadeImage()
        {
            var canvasGroup = GetTurnNotificationFadeImageCanvasGroup();
            if (canvasGroup == null)
            {
                return;
            }

            LeanTween.cancel(canvasGroup.gameObject);
            canvasGroup.alpha = 0f;

            LeanTween.delayedCall(canvasGroup.gameObject, _turnNotificationFadeImageDelay, () =>
            {
                if (canvasGroup == null)
                {
                    return;
                }

                LeanTween.alphaCanvas(canvasGroup, 1f, _turnNotificationFadeDuration)
                    .setEase(LeanTweenType.easeOutSine);
            });

            float fadeOutDelay = Mathf.Max(
                _turnNotificationFadeImageDelay,
                _turnNotificationDuration - _turnNotificationFadeDuration);
            LeanTween.delayedCall(canvasGroup.gameObject, fadeOutDelay, () =>
            {
                if (canvasGroup == null)
                {
                    return;
                }

                LeanTween.cancel(canvasGroup.gameObject);
                LeanTween.alphaCanvas(canvasGroup, 0f, _turnNotificationFadeDuration)
                    .setEase(LeanTweenType.easeInSine);
            });
        }

        private void AnimateTurnNotificationSpinningFadeImage()
        {
            var canvasGroup = GetTurnNotificationSpinningFadeImageCanvasGroup();
            if (canvasGroup == null || _turnNotificationSpinningFadeImage == null)
            {
                return;
            }

            CacheTurnNotificationSpinningFadeImageState();

            LeanTween.cancel(canvasGroup.gameObject);
            LeanTween.cancel(_turnNotificationSpinningFadeImage.gameObject);
            canvasGroup.alpha = 0f;
            _turnNotificationSpinningFadeImage.localRotation = Quaternion.Euler(0f, 0f, _turnNotificationSpinningFadeImageStartZ);

            LeanTween.delayedCall(canvasGroup.gameObject, _turnNotificationSpinningFadeImageDelay, () =>
            {
                if (canvasGroup == null || _turnNotificationSpinningFadeImage == null)
                {
                    return;
                }

                LeanTween.alphaCanvas(canvasGroup, 1f, _turnNotificationFadeDuration)
                    .setEase(LeanTweenType.easeOutSine);

                LeanTween.value(
                    _turnNotificationSpinningFadeImage.gameObject,
                    0f,
                    -360f,
                    _turnNotificationSpinDuration)
                    .setEase(LeanTweenType.linear)
                    .setRepeat(-1)
                    .setOnUpdate((float angle) =>
                    {
                        if (_turnNotificationSpinningFadeImage != null)
                        {
                            _turnNotificationSpinningFadeImage.localRotation = Quaternion.Euler(
                                0f,
                                0f,
                                _turnNotificationSpinningFadeImageStartZ + angle);
                        }
                    });
            });

            float fadeOutDelay = Mathf.Max(
                _turnNotificationSpinningFadeImageDelay,
                _turnNotificationDuration - _turnNotificationFadeDuration);
            LeanTween.delayedCall(canvasGroup.gameObject, fadeOutDelay, () =>
            {
                if (canvasGroup == null || _turnNotificationSpinningFadeImage == null)
                {
                    return;
                }

                LeanTween.alphaCanvas(canvasGroup, 0f, _turnNotificationFadeDuration)
                    .setEase(LeanTweenType.easeInSine);
            });
        }

        private void ResetTurnNotificationPrimaryImage()
        {
            if (_turnNotificationPrimaryImage == null)
            {
                return;
            }

            CacheTurnNotificationPrimaryImageStartPos();
            var canvasGroup = GetOrAddCanvasGroup(_turnNotificationPrimaryImage, ref _turnNotificationPrimaryImageCanvasGroup);
            if (canvasGroup != null)
            {
                LeanTween.cancel(canvasGroup.gameObject);
                canvasGroup.alpha = 0f;
            }
            LeanTween.cancel(_turnNotificationPrimaryImage.gameObject);
            _turnNotificationPrimaryImage.anchoredPosition = _turnNotificationPrimaryImageStartPos;
        }

        private void ResetTurnNotificationSecondaryImage()
        {
            if (_turnNotificationSecondaryImage == null)
            {
                return;
            }

            CacheTurnNotificationSecondaryImageStartPos();
            var canvasGroup = GetOrAddCanvasGroup(_turnNotificationSecondaryImage, ref _turnNotificationSecondaryImageCanvasGroup);
            if (canvasGroup != null)
            {
                LeanTween.cancel(canvasGroup.gameObject);
                canvasGroup.alpha = 0f;
            }
            LeanTween.cancel(_turnNotificationSecondaryImage.gameObject);
            _turnNotificationSecondaryImage.anchoredPosition = _turnNotificationSecondaryImageStartPos;
        }

        private void ResetTurnNotificationFadeImage()
        {
            var canvasGroup = GetTurnNotificationFadeImageCanvasGroup();
            if (canvasGroup == null)
            {
                return;
            }

            LeanTween.cancel(canvasGroup.gameObject);
            canvasGroup.alpha = 0f;
        }

        private void ResetTurnNotificationSpinningFadeImage()
        {
            var canvasGroup = GetTurnNotificationSpinningFadeImageCanvasGroup();
            if (canvasGroup == null || _turnNotificationSpinningFadeImage == null)
            {
                return;
            }

            CacheTurnNotificationSpinningFadeImageState();
            LeanTween.cancel(canvasGroup.gameObject);
            LeanTween.cancel(_turnNotificationSpinningFadeImage.gameObject);
            canvasGroup.alpha = 0f;
            _turnNotificationSpinningFadeImage.localRotation = Quaternion.Euler(0f, 0f, _turnNotificationSpinningFadeImageStartZ);
        }

        private void CacheTurnNotificationPrimaryImageStartPos()
        {
            if (_turnNotificationPrimaryImageCached || _turnNotificationPrimaryImage == null)
            {
                return;
            }

            _turnNotificationPrimaryImageStartPos = _turnNotificationPrimaryImage.anchoredPosition;
            _turnNotificationPrimaryImageCached = true;
        }

        private void CacheTurnNotificationSecondaryImageStartPos()
        {
            if (_turnNotificationSecondaryImageCached || _turnNotificationSecondaryImage == null)
            {
                return;
            }

            _turnNotificationSecondaryImageStartPos = _turnNotificationSecondaryImage.anchoredPosition;
            _turnNotificationSecondaryImageCached = true;
        }

        private CanvasGroup GetTurnNotificationFadeImageCanvasGroup()
        {
            if (_turnNotificationFadeImage == null)
            {
                return null;
            }

            if (_turnNotificationFadeImageCanvasGroup == null)
            {
                _turnNotificationFadeImageCanvasGroup = _turnNotificationFadeImage.GetComponent<CanvasGroup>();
                if (_turnNotificationFadeImageCanvasGroup == null)
                {
                    _turnNotificationFadeImageCanvasGroup = _turnNotificationFadeImage.gameObject.AddComponent<CanvasGroup>();
                }
            }

            return _turnNotificationFadeImageCanvasGroup;
        }

        private CanvasGroup GetTurnNotificationSpinningFadeImageCanvasGroup()
        {
            if (_turnNotificationSpinningFadeImage == null)
            {
                return null;
            }

            if (_turnNotificationSpinningFadeImageCanvasGroup == null)
            {
                _turnNotificationSpinningFadeImageCanvasGroup = _turnNotificationSpinningFadeImage.GetComponent<CanvasGroup>();
                if (_turnNotificationSpinningFadeImageCanvasGroup == null)
                {
                    _turnNotificationSpinningFadeImageCanvasGroup = _turnNotificationSpinningFadeImage.gameObject.AddComponent<CanvasGroup>();
                }
            }

            return _turnNotificationSpinningFadeImageCanvasGroup;
        }

        private void CacheTurnNotificationSpinningFadeImageState()
        {
            if (_turnNotificationSpinningFadeImageCached || _turnNotificationSpinningFadeImage == null)
            {
                return;
            }

            _turnNotificationSpinningFadeImageStartZ = _turnNotificationSpinningFadeImage.localEulerAngles.z;
            _turnNotificationSpinningFadeImageCached = true;
        }

        private void AnimateTurnNotificationImage(RectTransform image, Vector2 restPos, float startDelay, ref CanvasGroup canvasGroupCache)
        {
            if (image == null)
            {
                return;
            }

            var canvasGroup = GetOrAddCanvasGroup(image, ref canvasGroupCache);
            LeanTween.cancel(image.gameObject);
            if (canvasGroup != null)
            {
                LeanTween.cancel(canvasGroup.gameObject);
                canvasGroup.alpha = 0f;
            }

            Vector2 enterFromPos = restPos + new Vector2(_turnNotificationEnterOffsetX, 0f);
            Vector2 driftPos = restPos + Vector2.left * _turnNotificationDriftDistance;
            Vector2 exitPos = driftPos + Vector2.left * _turnNotificationExitDistance;

            LeanTween.delayedCall(image.gameObject, startDelay, () =>
            {
                if (image == null)
                {
                    return;
                }

                image.anchoredPosition = enterFromPos;

                if (canvasGroup != null)
                {
                    LeanTween.alphaCanvas(canvasGroup, 1f, _turnNotificationFadeDuration)
                        .setEase(LeanTweenType.easeOutSine);
                }

                LeanTween.move(image, restPos, _turnNotificationEntryDuration)
                    .setEase(LeanTweenType.easeOutCubic)
                    .setOnComplete(() =>
                    {
                        if (image == null)
                        {
                            return;
                        }

                        float driftDuration = Mathf.Max(0f, _turnNotificationDuration - _turnNotificationExitLeadTime - _turnNotificationEntryDuration - startDelay);
                        if (driftDuration > 0f)
                        {
                            LeanTween.move(image, driftPos, driftDuration)
                                .setEase(LeanTweenType.linear);
                        }
                    });
            });

            float exitDelay = Mathf.Max(startDelay, _turnNotificationDuration - _turnNotificationExitLeadTime);
            LeanTween.delayedCall(image.gameObject, exitDelay, () =>
            {
                if (image == null)
                {
                    return;
                }

                LeanTween.cancel(image.gameObject);
                LeanTween.move(image, exitPos, _turnNotificationExitDuration)
                    .setEase(LeanTweenType.easeInCubic);

                if (canvasGroup != null)
                {
                    LeanTween.cancel(canvasGroup.gameObject);
                    LeanTween.alphaCanvas(canvasGroup, 0f, _turnNotificationFadeDuration)
                        .setEase(LeanTweenType.easeInSine);
                }
            });
        }

        private CanvasGroup GetOrAddCanvasGroup(RectTransform image, ref CanvasGroup canvasGroupCache)
        {
            if (image == null)
            {
                return null;
            }

            if (canvasGroupCache == null)
            {
                canvasGroupCache = image.GetComponent<CanvasGroup>();
                if (canvasGroupCache == null)
                {
                    canvasGroupCache = image.gameObject.AddComponent<CanvasGroup>();
                }
            }

            return canvasGroupCache;
        }

        private void ApplyImageColor(RectTransform imageRect, ref Image imageComponent, Color color)
        {
            if (imageRect == null)
            {
                return;
            }

            if (imageComponent == null)
            {
                imageComponent = imageRect.GetComponent<Image>();
                if (imageComponent == null)
                {
                    Debug.LogWarning($"[AnimationsLogic] {imageRect.name} is missing an Image component.");
                    return;
                }
            }

            float alpha = imageComponent.color.a;
            imageComponent.color = new Color(color.r, color.g, color.b, alpha);
        }

        private IEnumerator AnimateWave(RectTransform wave, float amplitude, float speed, float phaseOffset)
        {
            Vector2 startPos = wave.anchoredPosition;
            float time = phaseOffset;
            while (true)
            {
                time += Time.deltaTime * speed;
                wave.anchoredPosition = new Vector2(startPos.x, startPos.y + Mathf.Sin(time) * amplitude);
                yield return null;
            }
        }

        private IEnumerator PulseTapToContinue(GameObject obj)
        {
            CanvasGroup group = obj.GetComponent<CanvasGroup>();
            if (group == null) group = obj.AddComponent<CanvasGroup>();
            while (true)
            {
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / 2f;
                    group.alpha = Mathf.Lerp(0f, 1f, t);
                    yield return null;
                }
                yield return new WaitForSeconds(0.25f);
                t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / 0.75f;
                    group.alpha = Mathf.Lerp(1f, 0f, t);
                    yield return null;
                }
                yield return new WaitForSeconds(0.15f);
            }
        }
    }
}
