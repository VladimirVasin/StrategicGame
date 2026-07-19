using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyPointOfInterestDialogController : MonoBehaviour
    {
        private CanvasGroup rootGroup;
        private Text titleText;
        private Text bodyText;
        private StrategyUiPanelTransition panelTransition;
        private Action acknowledgedCallback;
        private GameObject okButtonRoot;
        private bool initialized;
        private bool acknowledgementLocked;
        private StrategyInputRouter inputRouter;
        private StrategyInputContextHandle inputContext;

        public bool IsOpen => panelTransition != null
            ? panelTransition.TargetVisible
            : rootGroup != null && rootGroup.blocksRaycasts;
        public bool IsInputShieldActive => (panelTransition != null
                ? panelTransition.IsInputShieldActive
                : rootGroup != null && rootGroup.blocksRaycasts)
            || inputContext != null && !inputContext.IsDisposed;
        public bool CanOpenWithoutStacking => inputRouter == null
            || !inputRouter.IsAvailable
            || inputRouter.ActiveContextCount == 0;

        public void SetInputRouter(StrategyInputRouter router)
        {
            inputContext?.Dispose();
            inputContext = null;
            inputRouter = router;
            RefreshInputContext(ShouldHoldInputContext);
        }

        public void Configure()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            EnsureEventSystem();
            BuildUi();
            Hide();
        }

        public void Show(string title, string body, Action onAcknowledged)
        {
            Configure();
            acknowledgementLocked = false;
            acknowledgedCallback = onAcknowledged;
            ClearChoiceCallbacks();
            SetChoiceMode(false);
            titleText.text = string.IsNullOrWhiteSpace(title) ? "Point of Interest" : title;
            bodyText.text = body ?? string.Empty;
            panelTransition.SetVisible(true);
            RefreshInputContext(true);
            if (Application.isPlaying)
            {
                StrategyHudSfxAudio.Play(StrategyHudSfxKind.Notify);
            }
            StrategyDebugLogger.Info(
                "PointsOfInterest",
                "DialogOpened",
                StrategyDebugLogger.F("title", titleText.text));
        }

        public void Hide()
        {
            if (rootGroup == null)
            {
                return;
            }

            panelTransition.SetVisible(false);
            RefreshInputContext(ShouldHoldInputContext);
        }

        public void Dismiss()
        {
            acknowledgementLocked = true;
            acknowledgedCallback = null;
            ClearChoiceCallbacks();
            Hide();
        }

        private bool ShouldHoldInputContext => IsOpen
            || (panelTransition != null && panelTransition.IsInputShieldActive);

        private void Awake()
        {
            Configure();
        }

        private void Update()
        {
            RefreshInputContext(ShouldHoldInputContext);
        }

        private void OnDisable()
        {
            acknowledgedCallback = null;
            ClearChoiceCallbacks();
            acknowledgementLocked = true;
            inputContext?.Dispose();
            inputContext = null;
            panelTransition?.SetVisible(false, true);
        }

        private void RefreshInputContext(bool open)
        {
            if (!open || inputRouter == null || !inputRouter.IsAvailable)
            {
                inputContext?.Dispose();
                inputContext = null;
                return;
            }

            if (inputContext == null || inputContext.IsDisposed)
            {
                inputContext = inputRouter.PushContext(
                    this,
                    StrategyInputChannel.All,
                    StrategyCancelMode.Swallow);
            }
        }

        private void Acknowledge()
        {
            if (acknowledgementLocked)
            {
                return;
            }

            acknowledgementLocked = true;
            Action callback = acknowledgedCallback;
            acknowledgedCallback = null;
            Hide();
            if (Application.isPlaying)
            {
                StrategyHudSfxAudio.Play(StrategyHudSfxKind.Confirm);
            }
            StrategyDebugLogger.Info("PointsOfInterest", "DialogAcknowledged");
            callback?.Invoke();
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject(
                "PointOfInterestDialogCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 270;

            StrategyHudStyle.ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());

            RectTransform root = CreateUiObject("Root", canvasObject.transform).GetComponent<RectTransform>();
            Stretch(root, 0f, 0f, 0f, 0f);
            Image shade = root.gameObject.AddComponent<Image>();
            shade.color = new Color(0.01f, 0.015f, 0.015f, 0.62f);
            rootGroup = root.gameObject.AddComponent<CanvasGroup>();

            RectTransform panel = CreateUiObject("Panel", root).GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(520f, 286f);

            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.055f, 0.08f, 0.08f, 0.98f);
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            outline.effectDistance = new Vector2(2f, -2f);

            RectTransform accent = CreateUiObject("Accent", panel).GetComponent<RectTransform>();
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.sizeDelta = new Vector2(5f, 0f);
            accent.anchoredPosition = Vector2.zero;
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = new Color(0.86f, 0.63f, 0.28f, 1f);
            accentImage.raycastTarget = false;

            titleText = CreateText(
                "Title",
                panel,
                "Point of Interest",
                26,
                TextAnchor.UpperLeft,
                Color.white);
            titleText.fontStyle = FontStyle.Bold;
            SetTopStretch(titleText.rectTransform, 28f, 25f, 28f, 34f);

            Text subtitle = CreateText(
                "Subtitle",
                panel,
                "SCOUT REPORT",
                13,
                TextAnchor.UpperLeft,
                new Color(0.86f, 0.70f, 0.42f));
            subtitle.fontStyle = FontStyle.Bold;
            SetTopStretch(subtitle.rectTransform, 28f, 61f, 28f, 18f);

            RectTransform line = CreateUiObject("Line", panel).GetComponent<RectTransform>();
            SetTopStretch(line, 28f, 91f, 28f, 2f);
            Image lineImage = line.gameObject.AddComponent<Image>();
            lineImage.color = new Color(1f, 1f, 1f, 0.22f);
            lineImage.raycastTarget = false;

            bodyText = CreateText(
                "Body",
                panel,
                string.Empty,
                16,
                TextAnchor.UpperLeft,
                new Color(0.83f, 0.90f, 0.86f));
            bodyText.resizeTextForBestFit = true;
            bodyText.resizeTextMinSize = 12;
            bodyText.resizeTextMaxSize = 16;
            SetTopStretch(bodyText.rectTransform, 28f, 112f, 28f, 90f);

            okButtonRoot = CreateOkButton(panel);
            CreateChoiceButtons(panel);

            panelTransition = root.gameObject.AddComponent<StrategyUiPanelTransition>();
            panelTransition.Configure(rootGroup, panel, new Vector2(0f, -16f), 0.965f, 0.18f, 0.13f);
            panelTransition.SetVisible(false, true);
        }

        private GameObject CreateOkButton(RectTransform parent)
        {
            RectTransform root = CreateUiObject("OkButton", parent).GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = new Vector2(0f, 26f);
            root.sizeDelta = new Vector2(188f, 46f);

            Color color = new Color(0.22f, 0.39f, 0.30f, 0.98f);
            Image image = root.gameObject.AddComponent<Image>();
            image.color = color;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(Acknowledge);

            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.14f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(color.r, color.g, color.b, 0.45f);
            button.colors = colors;
            StrategyUiButtonFeedback.Attach(button, StrategyUiButtonFeedbackProfile.Standard, null);

            Text label = CreateText("Label", root, "OK", 16, TextAnchor.MiddleCenter, Color.white);
            label.fontStyle = FontStyle.Bold;
            Stretch(label.rectTransform, 0f, 0f, 0f, 1f);
            return root.gameObject;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int size,
            TextAnchor anchor,
            Color color)
        {
            RectTransform root = CreateUiObject(name, parent).GetComponent<RectTransform>();
            Text text = root.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = StrategyUiThemeProvider.Font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetTopStretch(RectTransform rect, float left, float top, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void EnsureEventSystem()
        {
            StrategyUiInputModuleBootstrap.Ensure();
        }
    }
}
