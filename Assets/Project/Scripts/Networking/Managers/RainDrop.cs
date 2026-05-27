using UnityEngine;
using Fusion;
using Networking.Models;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Networking.Managers
{
    public class RainDrop : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private int _points = 1;
        [SerializeField] private float _destroyY = -1100f;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private bool _isClicked = false;
        private RainMinigameManager _manager;

        private void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _manager = FindFirstObjectByType<RainMinigameManager>();

            // Find the drops container in the canvas
            var container = GameObject.Find("DropsContainer");
            if (container != null)
            {
                transform.SetParent(container.transform, false);
            }

            // Pop-in animation - Adjusted scale to 0.25f for 720 width
            transform.localScale = Vector3.zero;
            LeanTween.scale(gameObject, Vector3.one * 0.25f, 0.3f).setEaseOutBack();
        }

        private void Update()
        {
            if (_isClicked) return;

            // Move down using RectTransform anchored position locally
            float speed = _manager != null ? _manager.CurrentFallSpeed : 600f;
            Vector2 pos = _rectTransform.anchoredPosition;
            pos.y -= speed * Time.deltaTime;
            _rectTransform.anchoredPosition = pos;

            // Handle destruction locally
            if (_rectTransform.anchoredPosition.y < _destroyY)
            {
                Destroy(gameObject);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isClicked) return;
            _isClicked = true;

            var runner = Services.FusionNetworkService.LocalRunner;
            if (runner != null)
            {
                var localPlayer = runner.LocalPlayer;
                var gameManager = GameManager.Instance;
                if (gameManager != null)
                {
                    var data = gameManager.GetPlayerData(localPlayer, runner);
                    if (data != null)
                    {
                        data.RPC_AddMinigamePoints(_points);
                    }
                }
            }

            // Click animation - Scaled to 0.35f
            LeanTween.scale(gameObject, Vector3.one * 0.35f, 0.2f).setEaseOutQuad();
            if (_canvasGroup != null)
            {
                LeanTween.alphaCanvas(_canvasGroup, 0f, 0.2f).setOnComplete(() => {
                    Destroy(gameObject);
                });
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
