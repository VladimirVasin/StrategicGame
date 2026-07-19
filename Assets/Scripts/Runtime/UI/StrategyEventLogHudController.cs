using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyEventLogHudController : MonoBehaviour
    {
        private const int MaxEntries = 4;
        private const float EntryLifetime = 8f;
        private const float FadeSeconds = 1.25f;

        private static readonly List<PendingEvent> PendingEvents = new();

        private readonly List<EventEntry> entries = new();
        private readonly List<Text> entryTexts = new();
        private readonly List<Image> entryAccents = new();
        private RectTransform panelRoot;
        private CanvasGroup canvasGroup;
        private StrategyBuildMenuController buildMenu;
        private bool initialized;

        public static StrategyEventLogHudController Active { get; private set; }

        internal static void ResetSessionState()
        {
            PendingEvents.Clear();
        }

        public static void Notify(string message, Color color)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (Active == null)
            {
                PendingEvents.Add(new PendingEvent(message, color));
                return;
            }

            Active.AddEntry(message, color);
        }

        public void Configure()
        {
            if (!initialized)
            {
                initialized = true;
                Active = this;
                BuildUi();
                FlushPendingEvents();
            }
        }

        private void Awake()
        {
            Configure();
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        private void Update()
        {
            if (!initialized)
            {
                Configure();
            }

            float delta = Time.unscaledDeltaTime;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                entries[i].TimeLeft -= delta;
                if (entries[i].TimeLeft <= 0f)
                {
                    entries.RemoveAt(i);
                }
            }

            UpdatePanelPosition(delta);
            RefreshRows();
        }

        private void UpdatePanelPosition(float delta)
        {
            if (panelRoot == null)
            {
                return;
            }

            buildMenu ??= Object.FindAnyObjectByType<StrategyBuildMenuController>();
            float targetY = buildMenu != null && buildMenu.IsMenuOpen ? 386f : 20f;
            float y = StrategyHudStyle.ReducedMotion
                ? targetY
                : Mathf.Lerp(panelRoot.anchoredPosition.y, targetY, 1f - Mathf.Exp(-12f * delta));
            panelRoot.anchoredPosition = new Vector2(16f, y);
        }

        private void AddEntry(string message, Color color)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                EventEntry existing = entries[i];
                if (existing.Message != message)
                {
                    continue;
                }

                entries.RemoveAt(i);
                existing.Count++;
                existing.TimeLeft = EntryLifetime;
                existing.Color = color;
                entries.Insert(0, existing);
                RefreshRows();
                return;
            }

            entries.Insert(0, new EventEntry(message, color, EntryLifetime));
            while (entries.Count > MaxEntries)
            {
                entries.RemoveAt(entries.Count - 1);
            }

            RefreshRows();
        }

        private void FlushPendingEvents()
        {
            for (int i = 0; i < PendingEvents.Count; i++)
            {
                AddEntry(PendingEvents[i].Message, PendingEvents[i].Color);
            }

            PendingEvents.Clear();
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("EventLogHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 29;

            StrategyHudStyle.ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());

            panelRoot = CreateUiObject("EventLogPanel", canvasObject.transform).GetComponent<RectTransform>();
            panelRoot.anchorMin = new Vector2(0f, 0f);
            panelRoot.anchorMax = new Vector2(0f, 0f);
            panelRoot.pivot = new Vector2(0f, 0f);
            panelRoot.anchoredPosition = new Vector2(16f, 20f);
            panelRoot.sizeDelta = new Vector2(320f, CalculatePanelHeight(1));

            Image panelBackground = panelRoot.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(panelBackground, new Color(
                StrategyHudStyle.Background.r,
                StrategyHudStyle.Background.g,
                StrategyHudStyle.Background.b,
                0.94f));
            StrategyHudStyle.AddShadow(panelRoot.gameObject, 0.52f);

            canvasGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Text title = CreateText(
                "Title",
                panelRoot,
                "SETTLEMENT NOTICES",
                12,
                TextAnchor.MiddleLeft,
                StrategyHudStyle.Primary);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.offsetMin = new Vector2(14f, -26f);
            title.rectTransform.offsetMax = new Vector2(-14f, -4f);

            for (int i = 0; i < MaxEntries; i++)
            {
                RectTransform row = CreateUiObject("EventRow" + i, panelRoot).GetComponent<RectTransform>();
                row.anchorMin = new Vector2(0f, 1f);
                row.anchorMax = new Vector2(1f, 1f);
                row.pivot = new Vector2(0.5f, 1f);
                row.offsetMin = new Vector2(10f, -70f - i * 44f);
                row.offsetMax = new Vector2(-10f, -30f - i * 44f);
                Image rowBackground = row.gameObject.AddComponent<Image>();
                rowBackground.color = new Color(
                    StrategyHudStyle.Surface.r,
                    StrategyHudStyle.Surface.g,
                    StrategyHudStyle.Surface.b,
                    0.92f);
                rowBackground.raycastTarget = false;

                RectTransform accent = CreateUiObject("Accent", row).GetComponent<RectTransform>();
                accent.anchorMin = Vector2.zero;
                accent.anchorMax = new Vector2(0f, 1f);
                accent.sizeDelta = new Vector2(4f, 0f);
                accent.anchoredPosition = Vector2.zero;
                Image accentImage = accent.gameObject.AddComponent<Image>();
                accentImage.raycastTarget = false;
                entryAccents.Add(accentImage);

                Text text = CreateText("EventEntry" + i, row, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                text.lineSpacing = 0.9f;
                RectTransform rect = text.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(14f, 0f);
                rect.offsetMax = new Vector2(-10f, 0f);
                entryTexts.Add(text);
            }
        }

        private void RefreshRows()
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = entries.Count > 0 ? 1f : 0f;
            if (panelRoot != null)
            {
                panelRoot.sizeDelta = new Vector2(320f, CalculatePanelHeight(entries.Count));
            }

            for (int i = 0; i < entryTexts.Count; i++)
            {
                Text text = entryTexts[i];
                bool visible = i < entries.Count;
                text.transform.parent.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                EventEntry entry = entries[i];
                Color color = entry.Color;
                color.a = Mathf.Clamp01(entry.TimeLeft / FadeSeconds);
                text.text = entry.Message + (entry.Count > 1 ? "  ×" + entry.Count : string.Empty);
                text.color = color;
                if (i < entryAccents.Count)
                {
                    entryAccents[i].color = color;
                }
            }
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static float CalculatePanelHeight(int count)
        {
            return 34f + Mathf.Max(1, count) * 44f;
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

        private sealed class EventEntry
        {
            public EventEntry(string message, Color color, float timeLeft)
            {
                Message = message;
                Color = color;
                TimeLeft = timeLeft;
                Count = 1;
            }

            public string Message;
            public Color Color;
            public float TimeLeft;
            public int Count;
        }

        private readonly struct PendingEvent
        {
            public PendingEvent(string message, Color color)
            {
                Message = message;
                Color = color;
            }

            public readonly string Message;
            public readonly Color Color;
        }
    }
}
