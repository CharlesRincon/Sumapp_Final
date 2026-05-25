using UnityEngine;
using Fusion;
using Networking.Models;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Networking.Managers
{
    public class RainDrop : NetworkBehaviour, IPointerDownHandler
    {
        [SerializeField] private int _points = 1;
        [SerializeField] private float _destroyY = -1100f;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private bool _isClicked = false;
        private RainMinigameManager _manager;

        public override void Spawned()
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

        public override void FixedUpdateNetwork()
        {
            if (_isClicked) return;

            // Move down using RectTransform anchored position locally on all clients
            float speed = _manager != null ? _manager.CurrentFallSpeed : 600f;
            Vector2 pos = _rectTransform.anchoredPosition;
            pos.y -= speed * Runner.DeltaTime;
            _rectTransform.anchoredPosition = pos;

            // Only authority handles destruction
            if (Object.HasStateAuthority && _rectTransform.anchoredPosition.y < _destroyY)
            {
                Runner.Despawn(Object);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isClicked) return;
            _isClicked = true;

            var localPlayer = Runner.LocalPlayer;
            var playerObject = Runner.GetPlayerObject(localPlayer);
            if (playerObject != null)
            {
                var data = playerObject.GetComponent<PlayerSessionData>();
                if (data != null)
                {
                    data.RPC_AddMinigamePoints(_points);
                }
            }

            // Click animation - Scaled to 0.35f
            LeanTween.scale(gameObject, Vector3.one * 0.35f, 0.2f).setEaseOutQuad();
            if (_canvasGroup != null)
            {
                LeanTween.alphaCanvas(_canvasGroup, 0f, 0.2f).setOnComplete(() => {
                    RPC_RequestDespawn();
                });
            }
            else
            {
                RPC_RequestDespawn();
            }
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
        private void RPC_RequestDespawn()
        {
            if (Object != null && Object.IsValid)
            {
                Runner.Despawn(Object);
            }
        }
    }
}
