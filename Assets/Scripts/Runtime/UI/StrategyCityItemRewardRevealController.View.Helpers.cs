using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyCityItemRewardRevealController
    {
        private static Sprite GetRadialGlowSprite()
        {
            if (radialGlowSprite != null)
            {
                return radialGlowSprite;
            }

            const int size = 64;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                name = "City Item Reward Radial Glow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[size * size];
            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.2f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            radialGlowSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            radialGlowSprite.name = "City Item Reward Radial Glow";
            return radialGlowSprite;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            Text text = CreateRect(name, parent).gameObject.AddComponent<Text>();
            text.font = StrategyUiThemeProvider.Font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void ConfigureButtonColors(Button button, Color baseColor)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.14f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.22f);
            colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.40f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, 0f, 0f, 0f, 0f);
        }

        private static void Stretch(
            RectTransform rect,
            float left,
            float top,
            float right,
            float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetCenter(
            RectTransform rect,
            float x,
            float y,
            float width,
            float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetTopCenter(
            RectTransform rect,
            float x,
            float top,
            float width,
            float height)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetTopStretch(
            RectTransform rect,
            float left,
            float top,
            float right,
            float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetBottomStretch(
            RectTransform rect,
            float left,
            float bottom,
            float right,
            float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
        }
    }
}
