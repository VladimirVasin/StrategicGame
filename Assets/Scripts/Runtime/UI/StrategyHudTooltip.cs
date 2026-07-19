using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public enum StrategyHudTooltipPlacement
    {
        Automatic,
        Above,
        Below
    }

    [DisallowMultipleComponent]
    public sealed class StrategyHudTooltip : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private const float PointerDelay = 0.32f;

        private RectTransform target;
        private string content;
        private StrategyHudTooltipPlacement placement;
        private float showTimer;
        private bool pending;

        public StrategyHudTooltipPlacement Placement => placement;

        public static StrategyHudTooltip Attach(
            GameObject targetObject,
            string text,
            StrategyHudTooltipPlacement preferredPlacement = StrategyHudTooltipPlacement.Automatic)
        {
            if (targetObject == null)
            {
                return null;
            }

            StrategyHudTooltip tooltip = targetObject.GetComponent<StrategyHudTooltip>()
                ?? targetObject.AddComponent<StrategyHudTooltip>();
            tooltip.target = targetObject.transform as RectTransform;
            tooltip.content = text ?? string.Empty;
            tooltip.placement = preferredPlacement;
            return tooltip;
        }

        public void SetText(string text)
        {
            content = text ?? string.Empty;
        }

        private void Update()
        {
            if (!pending)
            {
                return;
            }

            showTimer -= Time.unscaledDeltaTime;
            if (showTimer <= 0f)
            {
                pending = false;
                StrategyHudTooltipPresenter.Show(target, content, placement);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pending = !string.IsNullOrWhiteSpace(content);
            showTimer = PointerDelay;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Hide();
        }

        public void OnSelect(BaseEventData eventData)
        {
            pending = false;
            StrategyHudTooltipPresenter.Show(target, content, placement);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            Hide();
        }

        private void OnDisable()
        {
            Hide();
        }

        private void Hide()
        {
            pending = false;
            StrategyHudTooltipPresenter.Hide(target);
        }
    }

    internal sealed class StrategyHudTooltipPresenter : MonoBehaviour
    {
        internal const float Width = 284f;
        internal const float MinWidth = 160f;
        internal const float EdgeMargin = 12f;
        internal const float TargetGap = 10f;
        private static StrategyHudTooltipPresenter instance;

        private Canvas canvas;
        private RectTransform canvasRect;
        private RectTransform panel;
        private Text label;
        private RectTransform owner;
        private StrategyHudTooltipPlacement ownerPlacement;

        public static void Show(
            RectTransform target,
            string text,
            StrategyHudTooltipPlacement placement)
        {
            if (target == null || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            Ensure();
            instance?.ShowInternal(target, text, placement);
        }

        public static void Hide(RectTransform target)
        {
            if (instance == null || (target != null && instance.owner != target))
            {
                return;
            }

            instance.owner = null;
            if (instance.panel != null)
            {
                instance.panel.gameObject.SetActive(false);
            }
        }

        internal static void RefreshVisible()
        {
            if (instance == null
                || instance.owner == null
                || instance.panel == null
                || !instance.panel.gameObject.activeInHierarchy)
            {
                return;
            }

            instance.ShowInternal(
                instance.owner,
                instance.label.text,
                instance.ownerPlacement);
        }

        private static void Ensure()
        {
            if (instance != null || !StrategyRuntimeObjectCreationGuard.CanCreateSceneObjects)
            {
                return;
            }

            GameObject root = new("Strategy HUD Tooltip Presenter");
            instance = root.AddComponent<StrategyHudTooltipPresenter>();
            instance.BuildView();
        }

        private void BuildView()
        {
            GameObject canvasObject = new(
                "TooltipCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 420;
            StrategyHudStyle.ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());
            canvasRect = canvasObject.GetComponent<RectTransform>();

            panel = CreateRect("TooltipPanel", canvasObject.transform);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = Vector2.zero;
            panel.sizeDelta = new Vector2(Width, 60f);
            Image background = panel.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(background, new Color(
                StrategyHudStyle.Background.r,
                StrategyHudStyle.Background.g,
                StrategyHudStyle.Background.b,
                0.98f));
            StrategyHudStyle.AddShadow(panel.gameObject, 0.72f);

            RectTransform accent = CreateRect("Accent", panel);
            accent.anchorMin = Vector2.zero;
            accent.anchorMax = new Vector2(0f, 1f);
            accent.sizeDelta = new Vector2(4f, 0f);
            accent.anchoredPosition = Vector2.zero;
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = StrategyHudStyle.Primary;
            accentImage.raycastTarget = false;

            label = CreateRect("Text", panel).gameObject.AddComponent<Text>();
            StrategyHudStyle.StyleText(label, StrategyHudTextRole.Body);
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(16f, 10f);
            label.rectTransform.offsetMax = new Vector2(-14f, -10f);
            panel.gameObject.SetActive(false);
        }

        private void ShowInternal(
            RectTransform target,
            string text,
            StrategyHudTooltipPlacement placement)
        {
            owner = target;
            ownerPlacement = placement;
            label.text = text.Trim();
            float width = Mathf.Clamp(label.preferredWidth + 34f, MinWidth, Width);
            panel.sizeDelta = new Vector2(width, 60f);
            Canvas.ForceUpdateCanvases();
            float height = Mathf.Clamp(label.preferredHeight + 24f, 48f, 148f);
            panel.sizeDelta = new Vector2(width, height);
            panel.gameObject.SetActive(true);

            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Canvas targetCanvas = target.GetComponentInParent<Canvas>();
            Camera targetCamera = GetEventCamera(targetCanvas);
            Camera tooltipCamera = GetEventCamera(canvas);
            Vector2 bottom = RectTransformUtility.WorldToScreenPoint(targetCamera, corners[0]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(targetCamera, corners[2]);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                bottom,
                tooltipCamera,
                out Vector2 localBottom);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                topRight,
                tooltipCamera,
                out Vector2 localTopRight);

            Rect targetRect = Rect.MinMaxRect(
                localBottom.x,
                localBottom.y,
                localTopRight.x,
                localTopRight.y);
            Rect panelRect = ResolvePanelRect(
                targetRect,
                canvasRect.rect.size,
                new Vector2(width, height),
                placement);
            panel.pivot = Vector2.zero;
            panel.anchoredPosition = panelRect.position;
            panel.SetAsLastSibling();
        }

        internal static Rect ResolvePanelRect(
            Rect targetRect,
            Vector2 canvasSize,
            Vector2 panelSize,
            StrategyHudTooltipPlacement placement)
        {
            float halfWidth = canvasSize.x * 0.5f;
            float halfHeight = canvasSize.y * 0.5f;
            float minX = -halfWidth + EdgeMargin;
            float maxX = halfWidth - panelSize.x - EdgeMargin;
            float minY = -halfHeight + EdgeMargin;
            float maxY = halfHeight - panelSize.y - EdgeMargin;
            float x = Mathf.Clamp(targetRect.xMin, minX, maxX);
            bool fitsAbove = targetRect.yMax + TargetGap + panelSize.y <= halfHeight - EdgeMargin;
            bool fitsBelow = targetRect.yMin - TargetGap - panelSize.y >= -halfHeight + EdgeMargin;
            bool placeAbove = placement == StrategyHudTooltipPlacement.Above
                ? fitsAbove || !fitsBelow
                : placement == StrategyHudTooltipPlacement.Below
                    ? !fitsBelow && fitsAbove
                    : !fitsBelow;
            float y = placeAbove
                ? targetRect.yMax + TargetGap
                : targetRect.yMin - TargetGap - panelSize.y;
            y = Mathf.Clamp(y, minY, maxY);
            return new Rect(x, y, panelSize.x, panelSize.y);
        }

        private static Camera GetEventCamera(Canvas targetCanvas)
        {
            return targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject obj = new(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj.GetComponent<RectTransform>();
        }
    }
}
