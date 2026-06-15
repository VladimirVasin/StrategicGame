using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyWorldInspectHudController : MonoBehaviour
    {
        private const float Width = 326f;
        private const float Height = 166f;
        private const float AnimationSpeed = 10f;
        private const float BottomInset = 18f;

        private RectTransform panel;
        private CanvasGroup group;
        private Image iconImage;
        private Text titleText;
        private Text subtitleText;
        private Text bodyText;
        private float visibility;
        private float targetVisibility;
        private float rightInset = 18f;

        public void Configure(float initialRightInset)
        {
            rightInset = Mathf.Max(0f, initialRightInset);
            EnsureUi();
            ApplyPosition();
        }

        public void SetRightInset(float inset)
        {
            rightInset = Mathf.Max(0f, inset);
        }

        public void Show(StrategyWorldInspectInfo info)
        {
            if (!info.IsValid)
            {
                Hide();
                return;
            }

            EnsureUi();
            titleText.text = info.Title;
            subtitleText.text = BuildSubtitle(info);
            bodyText.text = info.Body;
            iconImage.sprite = info.Icon;
            iconImage.color = info.Icon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            targetVisibility = 1f;
        }

        public void Hide()
        {
            targetVisibility = 0f;
        }

        private void Update()
        {
            if (panel == null || group == null)
            {
                return;
            }

            visibility = Mathf.MoveTowards(visibility, targetVisibility, Time.unscaledDeltaTime * AnimationSpeed);
            float eased = visibility * visibility * (3f - 2f * visibility);
            group.alpha = eased;
            group.blocksRaycasts = false;
            group.interactable = false;
            ApplyPosition();
        }

        private void EnsureUi()
        {
            if (panel != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("WorldInspectHudCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 27;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            panel = CreateUiObject("WorldInspectPanel", canvasObject.transform).GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(1f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(1f, 0f);
            panel.sizeDelta = new Vector2(Width, Height);

            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.035f, 0.050f, 0.047f, 0.94f);
            background.raycastTarget = false;

            group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            RectTransform accent = CreateUiObject("Accent", panel).GetComponent<RectTransform>();
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.sizeDelta = new Vector2(4f, 0f);
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = new Color(0.86f, 0.62f, 0.26f, 1f);
            accentImage.raycastTarget = false;

            RectTransform iconFrame = CreateUiObject("IconFrame", panel).GetComponent<RectTransform>();
            SetTopLeft(iconFrame, 18f, 18f, 54f, 54f);
            Image iconFrameImage = iconFrame.gameObject.AddComponent<Image>();
            iconFrameImage.color = new Color(1f, 1f, 1f, 0.08f);
            iconFrameImage.raycastTarget = false;

            RectTransform iconRect = CreateUiObject("Icon", iconFrame).GetComponent<RectTransform>();
            SetOffsets(iconRect, 5f, 5f, 5f, 5f);
            iconImage = iconRect.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            titleText = CreateText("Title", panel, 19, TextAnchor.UpperLeft, Color.white);
            SetTopLeft(titleText.rectTransform, 84f, 18f, 218f, 28f);
            subtitleText = CreateText("Subtitle", panel, 12, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            SetTopLeft(subtitleText.rectTransform, 84f, 48f, 218f, 20f);

            RectTransform divider = CreateUiObject("Divider", panel).GetComponent<RectTransform>();
            SetTopLeft(divider, 18f, 82f, Width - 36f, 2f);
            Image dividerImage = divider.gameObject.AddComponent<Image>();
            dividerImage.color = new Color(1f, 1f, 1f, 0.22f);
            dividerImage.raycastTarget = false;

            bodyText = CreateText("Body", panel, 13, TextAnchor.UpperLeft, new Color(0.84f, 0.90f, 0.88f));
            SetTopLeft(bodyText.rectTransform, 18f, 96f, Width - 36f, 54f);
        }

        private string BuildSubtitle(StrategyWorldInspectInfo info)
        {
            if (!info.HasCell)
            {
                return info.Subtitle;
            }

            string cellText = "Cell " + info.Cell.x + ", " + info.Cell.y;
            return string.IsNullOrWhiteSpace(info.Subtitle)
                ? cellText
                : info.Subtitle + " / " + cellText;
        }

        private void ApplyPosition()
        {
            if (panel == null)
            {
                return;
            }

            panel.anchoredPosition = new Vector2(-rightInset, BottomInset - (1f - visibility) * 18f);
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static Text CreateText(string name, Transform parent, int fontSize, TextAnchor anchor, Color color)
        {
            RectTransform rect = CreateUiObject(name, parent).GetComponent<RectTransform>();
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static void SetOffsets(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetTopLeft(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(left, -top);
        }
    }
}
