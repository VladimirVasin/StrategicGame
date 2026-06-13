using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyRefugeeDialogController : MonoBehaviour
    {
        private CanvasGroup rootGroup;
        private Text bodyText;
        private Text familyText;
        private Action<bool> decisionCallback;
        private bool initialized;
        private bool decisionLocked;

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

        public void Show(IReadOnlyList<StrategyResidentAgent> family, Action<bool> onDecision)
        {
            Configure();
            decisionCallback = onDecision;
            decisionLocked = false;
            if (bodyText != null)
            {
                bodyText.text = "A refugee family has reached the campfire. They are asking for shelter in the settlement.";
            }

            if (familyText != null)
            {
                familyText.text = BuildFamilyText(family);
            }

            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;
            StrategyDebugLogger.Info(
                "Refugees",
                "DialogOpened",
                StrategyDebugLogger.F("members", family != null ? family.Count : 0));
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

        private void Choose(bool accepted)
        {
            if (decisionLocked)
            {
                return;
            }

            decisionLocked = true;
            Hide();
            StrategyDebugLogger.Info(
                "Refugees",
                accepted ? "DialogAccepted" : "DialogRejected");
            decisionCallback?.Invoke(accepted);
            decisionCallback = null;
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("RefugeeDialogCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 240;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

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
            panel.sizeDelta = new Vector2(560f, 390f);

            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.06f, 0.09f, 0.09f, 0.98f);
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.52f);
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

            Text title = CreateText("Title", panel, "Refugees", 27, TextAnchor.UpperLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetTopStretch(title.rectTransform, 28f, 24f, 28f, 34f);

            Text subtitle = CreateText("Subtitle", panel, "settlement decision", 14, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            subtitle.fontStyle = FontStyle.Bold;
            SetTopStretch(subtitle.rectTransform, 28f, 60f, 28f, 20f);

            RectTransform line = CreateUiObject("Line", panel).GetComponent<RectTransform>();
            SetTopStretch(line, 28f, 92f, 28f, 2f);
            Image lineImage = line.gameObject.AddComponent<Image>();
            lineImage.color = new Color(1f, 1f, 1f, 0.22f);
            lineImage.raycastTarget = false;

            bodyText = CreateText("Body", panel, string.Empty, 15, TextAnchor.UpperLeft, new Color(0.83f, 0.90f, 0.86f));
            bodyText.resizeTextForBestFit = true;
            bodyText.resizeTextMinSize = 12;
            bodyText.resizeTextMaxSize = 15;
            SetTopStretch(bodyText.rectTransform, 28f, 112f, 28f, 56f);

            RectTransform familyBox = CreateUiObject("FamilyBox", panel).GetComponent<RectTransform>();
            SetTopStretch(familyBox, 28f, 182f, 28f, 106f);
            Image familyBackground = familyBox.gameObject.AddComponent<Image>();
            familyBackground.color = new Color(1f, 1f, 1f, 0.12f);
            familyText = CreateText("FamilyText", familyBox, string.Empty, 14, TextAnchor.UpperLeft, new Color(0.88f, 0.94f, 0.90f));
            familyText.fontStyle = FontStyle.Bold;
            familyText.resizeTextForBestFit = true;
            familyText.resizeTextMinSize = 10;
            familyText.resizeTextMaxSize = 14;
            Stretch(familyText.rectTransform, 14f, 10f, 14f, 10f);

            CreateDecisionButton(panel, "Accept", "Accept", new Vector2(-112f, 28f), new Color(0.22f, 0.39f, 0.30f, 0.98f), true);
            CreateDecisionButton(panel, "Reject", "Refuse", new Vector2(112f, 28f), new Color(0.34f, 0.18f, 0.17f, 0.98f), false);
        }

        private void CreateDecisionButton(RectTransform parent, string name, string label, Vector2 anchoredPosition, Color color, bool accepted)
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
            button.onClick.AddListener(() => Choose(accepted));

            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.14f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(color.r, color.g, color.b, 0.45f);
            button.colors = colors;

            Text text = CreateText("Label", root, label, 16, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            Stretch(text.rectTransform, 0f, 0f, 0f, 1f);
        }

        private static string BuildFamilyText(IReadOnlyList<StrategyResidentAgent> family)
        {
            if (family == null || family.Count <= 0)
            {
                return "Family: no data";
            }

            StringBuilder builder = new StringBuilder(192);
            for (int i = 0; i < family.Count; i++)
            {
                StrategyResidentAgent resident = family[i];
                if (resident == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                string role = i == 0
                    ? "male"
                    : i == 1
                        ? "female"
                        : resident.Gender == StrategyResidentGender.Male
                            ? "son"
                            : "daughter";
                builder.Append(resident.FullName);
                builder.Append("  -  ");
                builder.Append(role);
                builder.Append(", ");
                builder.Append(resident.DisplayAgeYears);
                builder.Append(" ");
                builder.Append(GetAgeSuffix(resident.DisplayAgeYears));
            }

            return builder.ToString();
        }

        private static string GetAgeSuffix(int age)
        {
            int mod100 = age % 100;
            if (mod100 >= 11 && mod100 <= 14)
            {
                return "years";
            }

            return age % 10 == 1
                ? "year"
                : age % 10 >= 2 && age % 10 <= 4
                    ? "years"
                    : "years";
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
