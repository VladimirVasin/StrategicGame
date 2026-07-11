using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyEventLogHudController : MonoBehaviour
    {
        private const int MaxEntries = 4;
        private const float EntryLifetime = 6.5f;
        private const float FadeSeconds = 1.25f;

        private static readonly List<PendingEvent> PendingEvents = new();

        private readonly List<EventEntry> entries = new();
        private readonly List<Text> entryTexts = new();
        private CanvasGroup canvasGroup;
        private bool initialized;

        public static StrategyEventLogHudController Active { get; private set; }

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

            RefreshRows();
        }

        private void AddEntry(string message, Color color)
        {
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

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform panel = CreateUiObject("EventLogPanel", canvasObject.transform).GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 1f);
            panel.anchorMax = new Vector2(0.5f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.anchoredPosition = new Vector2(0f, -18f);
            panel.sizeDelta = new Vector2(420f, 92f);

            canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            for (int i = 0; i < MaxEntries; i++)
            {
                Text text = CreateText("EventEntry" + i, panel, string.Empty, 14, TextAnchor.MiddleCenter, Color.white);
                RectTransform rect = text.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -i * 22f);
                rect.sizeDelta = new Vector2(0f, 22f);
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
            for (int i = 0; i < entryTexts.Count; i++)
            {
                Text text = entryTexts[i];
                bool visible = i < entries.Count;
                text.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                EventEntry entry = entries[i];
                Color color = entry.Color;
                color.a = Mathf.Clamp01(entry.TimeLeft / FadeSeconds);
                text.text = entry.Message;
                text.color = color;
            }
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

        private sealed class EventEntry
        {
            public EventEntry(string message, Color color, float timeLeft)
            {
                Message = message;
                Color = color;
                TimeLeft = timeLeft;
            }

            public string Message;
            public Color Color;
            public float TimeLeft;
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
