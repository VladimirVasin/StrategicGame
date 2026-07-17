using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    public sealed class StrategyCinematicLetterboxView : MonoBehaviour
    {
        public const float DefaultTargetAspectRatio = 2.39f;
        public const float DefaultMinimumBarFraction = 0.055f;
        public const float MaximumBarFraction = 0.24f;
        public const int DefaultSortingOrder = 295;

        private Image inputShield;
        private RectTransform topBar;
        private RectTransform bottomBar;
        private float targetAspectRatio = DefaultTargetAspectRatio;
        private float minimumBarFraction = DefaultMinimumBarFraction;
        private float barFraction;
        private float reveal;
        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;

        public float Reveal => reveal;
        public float BarFraction => barFraction;
        public float SafeHeightFraction =>
            StrategyInGameCinematicMath.CalculateSafeHeightFraction(barFraction);
        public bool IsInputShieldActive => inputShield != null
            && inputShield.gameObject.activeSelf;
        internal Image InputShieldImage => inputShield;

        public void Configure(
            float targetAspect,
            float minimumScreenBarFraction,
            int sortingOrder = DefaultSortingOrder)
        {
            targetAspectRatio = Mathf.Max(1f, targetAspect);
            minimumBarFraction = Mathf.Clamp(
                minimumScreenBarFraction,
                0f,
                MaximumBarFraction);
            EnsureView(sortingOrder);
            RefreshDimensions(true);
        }

        public void SetReveal(float progress)
        {
            reveal = Mathf.Clamp01(progress);
            ApplyLayout();
        }

        public void HideImmediate()
        {
            SetReveal(0f);
        }

        public void SetInputShieldActive(bool active)
        {
            if (active && inputShield == null)
            {
                Canvas canvas = GetComponent<Canvas>();
                EnsureView(canvas != null ? canvas.sortingOrder : DefaultSortingOrder);
            }

            if (inputShield != null)
            {
                inputShield.gameObject.SetActive(active);
            }
        }

        private void Awake()
        {
            EnsureView(DefaultSortingOrder);
            RefreshDimensions(true);
            HideImmediate();
        }

        private void LateUpdate()
        {
            RefreshDimensions(false);
        }

        private void OnDisable()
        {
            SetInputShieldActive(false);
            HideImmediate();
        }

        private void EnsureView(int sortingOrder)
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (inputShield == null)
            {
                inputShield = CreateInputShield();
            }

            if (topBar == null)
            {
                topBar = CreateBar("Top Letterbox Bar");
            }

            if (bottomBar == null)
            {
                bottomBar = CreateBar("Bottom Letterbox Bar");
            }
        }

        private Image CreateInputShield()
        {
            GameObject shieldObject = new(
                "Cinematic Input Shield",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            shieldObject.transform.SetParent(transform, false);
            RectTransform rect = shieldObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = shieldObject.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
            shieldObject.SetActive(false);
            return image;
        }

        private RectTransform CreateBar(string objectName)
        {
            GameObject barObject = new(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            barObject.transform.SetParent(transform, false);
            RectTransform rect = barObject.GetComponent<RectTransform>();
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = barObject.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            return rect;
        }

        private void RefreshDimensions(bool force)
        {
            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            if (!force && width == lastScreenWidth && height == lastScreenHeight)
            {
                return;
            }

            lastScreenWidth = width;
            lastScreenHeight = height;
            barFraction = StrategyInGameCinematicMath.CalculateLetterboxBarFraction(
                width / (float)height,
                targetAspectRatio,
                minimumBarFraction,
                MaximumBarFraction);
            ApplyLayout();
        }

        private void ApplyLayout()
        {
            if (topBar == null || bottomBar == null)
            {
                return;
            }

            float topMin = 1f - barFraction * reveal;
            topBar.anchorMin = new Vector2(0f, topMin);
            topBar.anchorMax = new Vector2(1f, topMin + barFraction);
            topBar.offsetMin = Vector2.zero;
            topBar.offsetMax = Vector2.zero;

            float bottomMax = barFraction * reveal;
            bottomBar.anchorMin = new Vector2(0f, bottomMax - barFraction);
            bottomBar.anchorMax = new Vector2(1f, bottomMax);
            bottomBar.offsetMin = Vector2.zero;
            bottomBar.offsetMax = Vector2.zero;
        }
    }
}
