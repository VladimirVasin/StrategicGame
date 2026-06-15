using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyProfessionHudController
    {

        private static void ConfigureButtonColors(Button button, Color baseColor)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.14f);
            colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.22f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.09f, 0.11f, 0.11f, 0.88f);
            button.colors = colors;
        }

        private static void ConfigureScrollbarColors(Scrollbar scrollbar, Color baseColor)
        {
            ColorBlock colors = scrollbar.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.20f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.22f);
            scrollbar.colors = colors;
        }

        private static void SetOffsets(RectTransform rect, float left, float top, float right, float bottom)
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

        private static void SetBottomStretch(RectTransform rect, float left, float bottom, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
        }

        private static void SetTopLeft(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetRightMiddle(RectTransform rect, float right, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-right, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private struct ProfessionSnapshot
        {
            public ProfessionSnapshot(StrategyProfessionType type, string title, string subtitle, Color accent)
            {
                Type = type;
                Title = title;
                Subtitle = subtitle;
                Accent = accent;
                Assigned = 0;
                Capacity = 0;
                FreeWorkers = 0;
                IsUnlimited = false;
            }

            public StrategyProfessionType Type;
            public string Title;
            public string Subtitle;
            public Color Accent;
            public int Assigned;
            public int Capacity;
            public int FreeWorkers;
            public bool IsUnlimited;
        }

        private sealed class ProfessionRow
        {
            public StrategyProfessionType Type;
            public RectTransform Root;
            public Image Background;
            public Text Title;
            public Text Subtitle;
            public Text Count;
            public Button MinusButton;
            public Button PlusButton;
        }
    }
}
