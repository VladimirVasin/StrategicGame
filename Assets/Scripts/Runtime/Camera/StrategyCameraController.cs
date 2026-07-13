using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class StrategyCameraController : MonoBehaviour
    {
        [Header("Pan")]
        [SerializeField] private float keyboardPanSpeed = 18f;
        [SerializeField] private float edgePanSpeed = 15f;
        [SerializeField] private float edgePanPixels = 24f;
        [SerializeField] private bool edgePanEnabled = true;
        [SerializeField] private float dragPanMultiplier = 1f;

        [Header("Zoom")]
        [SerializeField] private float minZoom = 4f;
        [SerializeField] private float maxZoom = 54f;
        [SerializeField] private float wheelZoomStep = 2.5f;
        [SerializeField] private float keyZoomSpeed = 16f;

        private const int FocusHoldFrameCount = 4;
        private const float FocusInputSuppressSeconds = 0.35f;

        private CityMapController map;
        private StrategyPopulationController population;
        private StrategyInputRouter inputRouter;
        private Camera strategyCamera;
        private Bounds? movementBounds;
        private Vector3 heldFocusCenter;
        private float heldFocusSize;
        private int focusHoldFramesRemaining;
        private float inputSuppressedUntilUnscaledTime;

        private void Awake()
        {
            strategyCamera = GetComponent<Camera>();
            strategyCamera.orthographic = true;
            strategyCamera.orthographicSize = Mathf.Clamp(strategyCamera.orthographicSize, minZoom, maxZoom);
        }

        private void Update()
        {
            if (strategyCamera == null)
            {
                return;
            }

            if (Time.unscaledTime < inputSuppressedUntilUnscaledTime)
            {
                ClampToBounds();
                return;
            }

            if (HandleCampFocusShortcut())
            {
                ClampToBounds();
                return;
            }

            HandleZoom();
            HandlePan();
            ClampToBounds();
        }

        private void LateUpdate()
        {
            if (focusHoldFramesRemaining <= 0 || strategyCamera == null)
            {
                return;
            }

            ApplyFocus(heldFocusCenter, heldFocusSize);
            focusHoldFramesRemaining--;
        }

        public void SetBounds(Bounds bounds)
        {
            EnsureCameraReference();
            movementBounds = bounds;
            ClampToBounds();
        }

        public void SetInputRouter(StrategyInputRouter router)
        {
            inputRouter = router;
        }

        public void SetCampFocusSource(
            CityMapController mapController,
            StrategyPopulationController populationController)
        {
            map = mapController;
            population = populationController;
        }

        public void FocusOn(Vector3 worldCenter, float orthographicSize)
        {
            EnsureCameraReference();
            heldFocusCenter = worldCenter;
            heldFocusSize = orthographicSize;
            focusHoldFramesRemaining = FocusHoldFrameCount;
            inputSuppressedUntilUnscaledTime = Mathf.Max(
                inputSuppressedUntilUnscaledTime,
                Time.unscaledTime + FocusInputSuppressSeconds);

            ApplyFocus(worldCenter, orthographicSize);
            StrategyDebugLogger.Info(
                "Camera",
                "FocusApplied",
                StrategyDebugLogger.F("target", worldCenter),
                StrategyDebugLogger.F("size", strategyCamera != null ? strategyCamera.orthographicSize : -1f),
                StrategyDebugLogger.F("position", transform.position));
        }

        private void ApplyFocus(Vector3 worldCenter, float orthographicSize)
        {
            if (strategyCamera == null)
            {
                return;
            }

            Vector3 position = transform.position;
            position.x = worldCenter.x;
            position.y = worldCenter.y;
            transform.position = position;

            strategyCamera.orthographicSize = Mathf.Clamp(orthographicSize, minZoom, maxZoom);
            ClampToBounds();
        }

        private void EnsureCameraReference()
        {
            if (strategyCamera == null)
            {
                strategyCamera = GetComponent<Camera>();
            }

            if (strategyCamera != null)
            {
                strategyCamera.orthographic = true;
            }
        }

        private void HandlePan()
        {
            Vector2 pan = ReadKeyboardPan();
            pan += ReadEdgePan();

            if (pan.sqrMagnitude > 1f)
            {
                pan.Normalize();
            }

            float zoomScale = GetZoomScale();
            Vector3 movement = new Vector3(pan.x, pan.y, 0f) * keyboardPanSpeed * zoomScale * Time.unscaledDeltaTime;
            movement += ReadDragPan();

            if (movement.sqrMagnitude > 0f)
            {
                transform.position += movement;
            }
        }

        private bool HandleCampFocusShortcut()
        {
            if (inputRouter == null || !inputRouter.CameraFocusCampPressed)
            {
                return false;
            }

            if (map == null
                || population == null
                || !population.TryGetCampCell(out Vector2Int campCell))
            {
                StrategyDebugLogger.Warn("Camera", "CampFocusRejected");
                return false;
            }

            Vector3 campWorld = map.GetCellCenterWorld(campCell.x, campCell.y);
            FocusOn(campWorld, strategyCamera.orthographicSize);
            StrategyDebugLogger.Info(
                "Camera",
                "CampFocusShortcut",
                StrategyDebugLogger.F("campCell", campCell),
                StrategyDebugLogger.F("world", campWorld));
            return true;
        }

        private Vector2 ReadKeyboardPan()
        {
            return inputRouter != null ? inputRouter.CameraPan : Vector2.zero;
        }

        private Vector2 ReadEdgePan()
        {
            if (!edgePanEnabled
                || inputRouter == null
                || !inputRouter.IsChannelEnabled(StrategyInputChannel.Camera)
                || IsPointerOverUi()
                || Screen.width <= 0
                || Screen.height <= 0)
            {
                return Vector2.zero;
            }

            Vector2 position = inputRouter.CameraPointerPosition;
            if (position.x < 0f || position.y < 0f || position.x > Screen.width || position.y > Screen.height)
            {
                return Vector2.zero;
            }

            Vector2 pan = Vector2.zero;
            float zoomScale = GetZoomScale();
            float scaledKeyboardPanSpeed = Mathf.Max(0.01f, keyboardPanSpeed * zoomScale);

            if (position.x <= edgePanPixels)
            {
                pan.x -= edgePanSpeed * zoomScale / scaledKeyboardPanSpeed;
            }
            else if (position.x >= Screen.width - edgePanPixels)
            {
                pan.x += edgePanSpeed * zoomScale / scaledKeyboardPanSpeed;
            }

            if (position.y <= edgePanPixels)
            {
                pan.y -= edgePanSpeed * zoomScale / scaledKeyboardPanSpeed;
            }
            else if (position.y >= Screen.height - edgePanPixels)
            {
                pan.y += edgePanSpeed * zoomScale / scaledKeyboardPanSpeed;
            }

            return pan;
        }

        private Vector3 ReadDragPan()
        {
            if (inputRouter == null
                || IsPointerOverUi()
                || (!inputRouter.CameraMiddleDragHeld && !inputRouter.CameraRightDragHeld))
            {
                return Vector3.zero;
            }

            Vector2 delta = inputRouter.CameraPointerDelta;
            if (delta.sqrMagnitude <= 0f)
            {
                return Vector3.zero;
            }

            float worldUnitsPerPixel = strategyCamera.orthographicSize * 2f / Mathf.Max(1, Screen.height);
            return new Vector3(-delta.x, -delta.y, 0f) * worldUnitsPerPixel * dragPanMultiplier;
        }

        private void HandleZoom()
        {
            float zoomDelta = ReadZoomDelta();
            if (Mathf.Approximately(zoomDelta, 0f))
            {
                return;
            }

            Vector3? mouseWorldBefore = TryReadMouseWorldPosition();
            strategyCamera.orthographicSize = Mathf.Clamp(
                strategyCamera.orthographicSize - zoomDelta,
                minZoom,
                maxZoom);

            Vector3? mouseWorldAfter = TryReadMouseWorldPosition();
            if (mouseWorldBefore.HasValue && mouseWorldAfter.HasValue)
            {
                transform.position += mouseWorldBefore.Value - mouseWorldAfter.Value;
            }
        }

        private float ReadZoomDelta()
        {
            float zoomDelta = 0f;
            float zoomScale = GetZoomScale();

            if (inputRouter != null && !IsPointerOverUi())
            {
                float wheel = inputRouter.CameraScroll.y;
                if (!Mathf.Approximately(wheel, 0f))
                {
                    zoomDelta += Mathf.Sign(wheel) * wheelZoomStep * zoomScale;
                }
            }

            if (inputRouter != null)
            {
                zoomDelta += inputRouter.CameraZoomKeys * keyZoomSpeed * zoomScale * Time.unscaledDeltaTime;
            }

            return zoomDelta;
        }

        private float GetZoomScale()
        {
            if (strategyCamera == null)
            {
                return 1f;
            }

            return Mathf.Max(0.1f, strategyCamera.orthographicSize / Mathf.Max(0.1f, minZoom));
        }

        private Vector3? TryReadMouseWorldPosition()
        {
            if (inputRouter == null
                || !inputRouter.IsChannelEnabled(StrategyInputChannel.Camera)
                || IsPointerOverUi())
            {
                return null;
            }

            Vector2 screen = inputRouter.CameraPointerPosition;
            if (screen.x < 0f || screen.y < 0f || screen.x > Screen.width || screen.y > Screen.height)
            {
                return null;
            }

            Vector3 screenPoint = new Vector3(screen.x, screen.y, Mathf.Abs(transform.position.z));
            return strategyCamera.ScreenToWorldPoint(screenPoint);
        }

        private void ClampToBounds()
        {
            if (!movementBounds.HasValue || strategyCamera == null)
            {
                return;
            }

            Bounds bounds = movementBounds.Value;
            Vector3 position = transform.position;

            float verticalExtent = strategyCamera.orthographicSize;
            float horizontalExtent = strategyCamera.orthographicSize * strategyCamera.aspect;

            position.x = ClampAxis(position.x, bounds.min.x + horizontalExtent, bounds.max.x - horizontalExtent, bounds.center.x);
            position.y = ClampAxis(position.y, bounds.min.y + verticalExtent, bounds.max.y - verticalExtent, bounds.center.y);

            transform.position = position;
        }

        private static float ClampAxis(float value, float min, float max, float fallback)
        {
            if (min > max)
            {
                return fallback;
            }

            return Mathf.Clamp(value, min, max);
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
