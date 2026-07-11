using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyConfirmationDialogController : MonoBehaviour
    {
        private CanvasGroup rootGroup;
        private Text titleText;
        private Text bodyText;
        private Text confirmText;
        private Text cancelText;
        private Action confirmCallback;
        private bool initialized;
        private bool locked;

        public bool IsOpen => rootGroup != null && rootGroup.blocksRaycasts;

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

        private void Awake()
        {
            Configure();
        }

        public void Show(
            string title,
            string body,
            string confirmLabel,
            string cancelLabel,
            Action onConfirm)
        {
            Configure();
            locked = false;
            confirmCallback = onConfirm;
            titleText.text = title;
            bodyText.text = body;
            confirmText.text = confirmLabel;
            cancelText.text = cancelLabel;
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Notify);
            StrategyDebugLogger.Info("UI", "ConfirmationOpened", StrategyDebugLogger.F("title", title));
        }

        public void Hide()
        {
            if (rootGroup == null)
            {
                return;
            }

            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }

        private void Confirm()
        {
            if (locked)
            {
                return;
            }

            locked = true;
            Action callback = confirmCallback;
            confirmCallback = null;
            Hide();
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Confirm);
            StrategyDebugLogger.Info("UI", "ConfirmationAccepted");
            callback?.Invoke();
        }

        private void Cancel()
        {
            if (locked)
            {
                return;
            }

            locked = true;
            confirmCallback = null;
            Hide();
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Cancel);
            StrategyDebugLogger.Info("UI", "ConfirmationCancelled");
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("ConfirmationDialogCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 265;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = CreateUiObject("Root", canvasObject.transform).GetComponent<RectTransform>();
            Stretch(root, 0f, 0f, 0f, 0f);
            Image shade = root.gameObject.AddComponent<Image>();
            shade.color = new Color(0.01f, 0.012f, 0.012f, 0.58f);
            rootGroup = root.gameObject.AddComponent<CanvasGroup>();

            RectTransform panel = CreateUiObject("Panel", root).GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(520f, 278f);

            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.055f, 0.075f, 0.075f, 0.98f);
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
            accentImage.color = new Color(0.86f, 0.54f, 0.28f, 1f);
            accentImage.raycastTarget = false;

            titleText = CreateText("Title", panel, "Confirm", 25, TextAnchor.UpperLeft, Color.white);
            titleText.fontStyle = FontStyle.Bold;
            SetTopStretch(titleText.rectTransform, 28f, 26f, 28f, 34f);

            RectTransform line = CreateUiObject("Line", panel).GetComponent<RectTransform>();
            SetTopStretch(line, 28f, 76f, 28f, 2f);
            Image lineImage = line.gameObject.AddComponent<Image>();
            lineImage.color = new Color(1f, 1f, 1f, 0.22f);
            lineImage.raycastTarget = false;

            bodyText = CreateText("Body", panel, string.Empty, 16, TextAnchor.UpperLeft, new Color(0.83f, 0.90f, 0.86f));
            bodyText.resizeTextForBestFit = true;
            bodyText.resizeTextMinSize = 12;
            bodyText.resizeTextMaxSize = 16;
            SetTopStretch(bodyText.rectTransform, 28f, 96f, 28f, 78f);

            CreateButton(panel, "ConfirmButton", new Vector2(-112f, 28f), new Color(0.42f, 0.20f, 0.16f, 0.98f), true, out confirmText);
            CreateButton(panel, "CancelButton", new Vector2(112f, 28f), new Color(0.13f, 0.18f, 0.18f, 0.98f), false, out cancelText);
        }

        private void CreateButton(
            RectTransform parent,
            string name,
            Vector2 anchoredPosition,
            Color color,
            bool confirm,
            out Text label)
        {
            RectTransform root = CreateUiObject(name, parent).GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = anchoredPosition;
            root.sizeDelta = new Vector2(188f, 46f);

            Image image = root.gameObject.AddComponent<Image>();
            image.color = color;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(confirm ? Confirm : Cancel);

            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.14f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(color.r, color.g, color.b, 0.45f);
            button.colors = colors;

            label = CreateText("Label", root, confirm ? "Confirm" : "Cancel", 16, TextAnchor.MiddleCenter, Color.white);
            label.fontStyle = FontStyle.Bold;
            Stretch(label.rectTransform, 0f, 0f, 0f, 1f);
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor, Color color)
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
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

            StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                Destroy(standalone);
            }

            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            inputModule.enabled = true;
        }
    }
}
