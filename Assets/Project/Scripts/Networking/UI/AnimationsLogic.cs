using System.Collections;
using TMPro;
using UnityEngine;

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

        public void ShowTurnNotification(GameObject notificationPanel, TextMeshProUGUI notificationText, string message)
        {
            if (notificationPanel == null) return;

            if (notificationText != null)
                notificationText.text = message;

            notificationPanel.SetActive(true);

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

            if (notificationPanel != null)
                notificationPanel.SetActive(false);
        }

        private IEnumerator HideNotificationAfterDelay(GameObject notificationPanel, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (notificationPanel != null)
                notificationPanel.SetActive(false);
            _hideNotificationCoroutine = null;
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
