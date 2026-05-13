using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Networking.UI
{
    /// <summary>
    /// Displays a 3D model inside a UI panel using a dedicated Camera → RenderTexture → RawImage pipeline.
    ///
    /// Setup in Unity Editor:
    ///   1. Create a new Layer (e.g. "ModelViewer").
    ///   2. Place your 3D model (and any lights for it) on that layer.
    ///   3. Create a Camera, set its Culling Mask to only "ModelViewer", and assign it to _viewerCamera.
    ///   4. Create an empty pivot GameObject (parent of the 3D model root) and assign it to _modelPivot.
    ///   5. Add a RawImage to your panel and assign it to _displayImage.
    ///   6. Attach this component to the panel root (it must also have a RectTransform and CanvasGroup or Graphic for raycasting).
    ///   7. Make sure an EventSystem exists in the scene and that the panel has a Graphic Raycaster on the Canvas.
    ///   8. To appear in front of all other panels: use a separate Canvas component on this panel's root
    ///      with Sort Order set higher than the main canvas (e.g. 10), Render Mode = Screen Space – Overlay.
    /// </summary>
    public class ModelViewerController : MonoBehaviour, IDragHandler, IScrollHandler, IPointerDownHandler
    {
        [Header("References")]
        [SerializeField] private Camera _viewerCamera;
        /// <summary>
        /// The scene's main camera (renders to Display 1). Leave blank to auto-find by tag.
        /// Must always stay enabled so Unity does not warn "Display 1 No cameras rendering"
        /// while the viewer camera targets a RenderTexture instead of the screen.
        /// </summary>
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private Transform _modelPivot;
        [SerializeField] private RawImage _displayImage;

        [Header("Rotation")]
        [SerializeField] private float _rotationSpeedX = 0.4f;
        [SerializeField] private float _rotationSpeedY = 0.4f;

        [Header("Zoom")]
        [SerializeField] private float _zoomScrollSpeed = 0.3f;
        [SerializeField] private float _zoomPinchSpeed = 0.005f;
        [SerializeField] private float _minScale = 0.1f;
        [SerializeField] private float _maxScale = 4f;

        [Header("Panel")]
        [SerializeField] private GameObject _panel;

        // ── Internal State ──────────────────────────────────────────────
        private RenderTexture _rt;
        private float _currentScale = 1f;

        // Touch pinch tracking
        private bool _isPinching;

        // Active show coroutine — cancelled if Hide() is called before it completes.
        private Coroutine _showCoroutine;

        // ── Lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            // Auto-find the main camera if not set in the Inspector.
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_mainCamera == null)
                Debug.LogWarning("[ModelViewerController] No camera tagged 'MainCamera' found. " +
                    "Add one so Display 1 always has a camera rendering.");

            if (_viewerCamera != null)
            {
                // Keep the camera disabled until the panel is explicitly opened.
                _viewerCamera.enabled = false;
            }

            // Capture the model's initial scale so zoom starts from whatever was set in the scene.
            if (_modelPivot != null)
                _currentScale = _modelPivot.localScale.x;
        }

        private void Start()
        {
            // Nothing — the RT is built on Show() after layout settles.
            // The viewer camera starts disabled (set in Awake).
        }

        private void OnDestroy()
        {
            ReleaseRenderTexture();
        }

        private void OnEnable()
        {
            // Enable EnhancedTouch so Touch.activeTouches is populated (new Input System).
            EnhancedTouchSupport.Enable();
            // Camera and RT state are managed entirely by Show() / Hide().
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        // ── Public API ──────────────────────────────────────────────────
        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);

            // Wait one frame so Unity's layout system has time to calculate the
            // RectTransform rect after the panel becomes active. On mobile this
            // rect is still zero on the same frame SetActive is called.
            if (_showCoroutine != null) StopCoroutine(_showCoroutine);
            _showCoroutine = StartCoroutine(ShowNextFrame());
        }

        private IEnumerator ShowNextFrame()
        {
            yield return null; // one layout pass
            ConfigureViewerCamera();
            ReleaseRenderTexture();
            BuildRenderTexture();
            _showCoroutine = null;
        }

        public void Hide()
        {
            // Cancel any in-flight show coroutine.
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }

            if (_panel != null) _panel.SetActive(false);
            // Disable only the viewer camera (renders to RT, not Display 1).
            if (_viewerCamera != null)
                _viewerCamera.enabled = false;
            // Always keep the main camera enabled so Display 1 keeps rendering.
            EnsureMainCameraActive();
        }

        // ── RenderTexture ───────────────────────────────────────────────
        private void BuildRenderTexture()
        {
            // Camera must be configured (enabled) before we build the RT.
            if (_viewerCamera == null || _displayImage == null) return;

            // Read the actual panel rect — by the time this runs (after ShowNextFrame yield),
            // layout has settled and we get the real pixel dimensions.
            Rect rect = _displayImage.rectTransform.rect;
            int w = Mathf.RoundToInt(rect.width);
            int h = Mathf.RoundToInt(rect.height);
            if (w <= 1 || h <= 1)
            {
                // Fallback: panel still has no size — use screen dimensions.
                w = Screen.width;
                h = Screen.height;
                Debug.LogWarning("[ModelViewerController] RawImage rect is zero; " +
                    "using screen size for RenderTexture. Check panel layout.");
            }

            // Reuse existing RT if dimensions haven't changed.
            if (_rt != null && _rt.width == w && _rt.height == h)
            {
                _displayImage.texture = _rt;
                _viewerCamera.targetTexture = _rt;
                return;
            }

            ReleaseRenderTexture();

            // RenderTextureFormat.Default is the safest choice across Android GPUs.
            // antiAliasing 1 = off; higher values are not reliably supported on mobile RT.
            _rt = new RenderTexture(w, h, 24, RenderTextureFormat.Default)
            {
                antiAliasing = 1
            };
            _rt.Create();

            _viewerCamera.targetTexture = _rt;
            _displayImage.texture = _rt;
        }

        private void ConfigureViewerCamera()
        {
            if (_viewerCamera == null) return;

            // Ensure the viewer camera's GameObject and component are active.
            if (!_viewerCamera.gameObject.activeSelf)
                _viewerCamera.gameObject.SetActive(true);
            _viewerCamera.enabled = true;

            // Mobile GPUs leave previous color data in the RT unless we clear explicitly.
            _viewerCamera.clearFlags = CameraClearFlags.SolidColor;
            _viewerCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);

            // Main camera must stay on — it owns Display 1.
            EnsureMainCameraActive();
        }

        private void EnsureMainCameraActive()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;
            if (_mainCamera != null && !_mainCamera.enabled)
            {
                _mainCamera.enabled = true;
                Debug.LogWarning("[ModelViewerController] Main camera was disabled — re-enabled it.");
            }
        }

        private void ReleaseRenderTexture()
        {
            if (_rt == null) return;

            if (_viewerCamera != null && _viewerCamera.targetTexture == _rt)
                _viewerCamera.targetTexture = null;

            if (_displayImage != null && _displayImage.texture == _rt)
                _displayImage.texture = null;

            _rt.Release();
            Destroy(_rt);
            _rt = null;
        }

        // ── Input ───────────────────────────────────────────────────────
        public void OnPointerDown(PointerEventData eventData)
        {
            // Consume pointer-down so drag detection starts correctly.
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_modelPivot == null) return;

            // Block single-finger rotation while a pinch is in progress.
            if (_isPinching) return;

            float dx = eventData.delta.x * _rotationSpeedX;
            float dy = eventData.delta.y * _rotationSpeedY;

            _modelPivot.Rotate(Vector3.up, -dx, Space.World);
            _modelPivot.Rotate(_modelPivot.right, -dy, Space.World);
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!IsPanelVisible()) return;
            // Scroll up (positive y) = zoom in = model bigger.
            ApplyZoomDelta(eventData.scrollDelta.y * _zoomScrollSpeed);
        }

        private void Update()
        {
            // Only process input while the panel is open and the camera is rendering.
            if (!IsPanelVisible()) return;
            HandlePinchZoom();
        }

        private bool IsPanelVisible()
        {
            return _panel != null && _panel.activeSelf && _viewerCamera != null && _viewerCamera.enabled;
        }

        private void HandlePinchZoom()
        {
            // Use EnhancedTouch — old Input.GetTouch() returns nothing with the new Input System.
            var activeTouches = EnhancedTouch.activeTouches;

            if (activeTouches.Count < 2)
            {
                _isPinching = false;
                return;
            }

            _isPinching = true;

            var t0 = activeTouches[0];
            var t1 = activeTouches[1];

            // Skip the very first frame of the gesture — no previous position yet.
            if (t0.phase == UnityEngine.InputSystem.TouchPhase.Began ||
                t1.phase == UnityEngine.InputSystem.TouchPhase.Began)
                return;

            // Reconstruct previous positions using each touch's own per-frame delta.
            // This avoids any reliance on _prevPinchDistance tracking and is always accurate.
            Vector2 t0Prev = t0.screenPosition - t0.delta;
            Vector2 t1Prev = t1.screenPosition - t1.delta;

            float prevDist = Vector2.Distance(t0Prev, t1Prev);
            float currDist = Vector2.Distance(t0.screenPosition, t1.screenPosition);

            // Spread fingers → distDelta > 0 → zoom in → model bigger.
            // Pinch fingers  → distDelta < 0 → zoom out → model smaller.
            float distDelta = currDist - prevDist;
            ApplyZoomDelta(distDelta * _zoomPinchSpeed);
        }

        private void ApplyZoomDelta(float delta)
        {
            if (_modelPivot == null) return;

            // Scale the model pivot instead of moving the camera.
            // This avoids all camera clipping and near-plane issues entirely.
            _currentScale = Mathf.Clamp(_currentScale + delta, _minScale, _maxScale);
            _modelPivot.localScale = Vector3.one * _currentScale;
        }
    }
}
