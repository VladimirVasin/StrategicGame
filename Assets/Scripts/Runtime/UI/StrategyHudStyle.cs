using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public enum StrategyHudTextRole
    {
        Title,
        Section,
        Body,
        Metadata
    }

    public static class StrategyHudStyle
    {
        public static readonly Vector2 ReferenceResolution = new(1600f, 900f);

        public const float TopRailHeight = 70f;
        public const float SideMargin = 18f;

        public static Color Background => Html("#101816");
        public static Color Surface => Html("#18231F");
        public static Color Elevated => Html("#22302B");
        public static Color Primary => Html("#D79A45");
        public static Color BrassDark => Html("#76512E");
        public static Color Secondary => Html("#7EA7AF");
        public static Color Success => Html("#7FA66B");
        public static Color Danger => Html("#C65B4F");
        public static Color Warning => Html("#DDBB67");
        public static Color TextPrimary => Html("#E7E5D6");
        public static Color TextMuted => Html("#AAB8AE");
        public static Color Divider => new(0.72f, 0.78f, 0.70f, 0.16f);

        public static bool ReducedMotion => StrategyGameSettings.ReducedMotion;

        public static void ConfigureScaler(CanvasScaler scaler, float matchWidthOrHeight = 0.5f)
        {
            if (scaler == null)
            {
                return;
            }

            float uiScale = Mathf.Clamp(StrategyGameSettings.UiScale, 0.85f, 1.25f);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution / uiScale;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = Mathf.Clamp01(matchWidthOrHeight);
            scaler.referencePixelsPerUnit = 100f;
        }

        public static void RefreshCanvasScalers()
        {
            CanvasScaler[] scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include);
            for (int i = 0; i < scalers.Length; i++)
            {
                if (scalers[i].GetComponent<StrategyCinematicLetterboxView>() != null)
                {
                    continue;
                }

                ConfigureScaler(scalers[i], scalers[i].matchWidthOrHeight);
            }
        }

        public static void StylePanel(Image image, Color color, bool raycastTarget = false)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = StrategyUiThemeProvider.GetPanelSprite();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.color = new Color(1f, 1f, 1f, color.a);
            image.raycastTarget = raycastTarget;
        }

        public static void StyleInset(Image image, Color color, bool raycastTarget = false)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = StrategyUiThemeProvider.GetInsetSprite();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.color = color;
            image.raycastTarget = raycastTarget;
        }

        public static void StyleRailModule(Image image, bool interactive = false)
        {
            if (image == null)
            {
                return;
            }

            StyleInset(
                image,
                new Color(Surface.r, Surface.g, Surface.b, 0.42f),
                interactive);
            image.pixelsPerUnitMultiplier = 2.5f;
        }

        public static void StyleCompactPanel(Image image, Color color, bool raycastTarget = false)
        {
            StyleInset(image, color, raycastTarget);
        }

        public static void StyleButton(Button button, Image image, bool primary = false)
        {
            if (button == null || image == null)
            {
                return;
            }

            image.sprite = StrategyUiThemeProvider.GetButtonSprite();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.color = primary ? Primary : Elevated;
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = Color.Lerp(image.color, Color.white, primary ? 0.10f : 0.08f);
            colors.pressedColor = Color.Lerp(image.color, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(image.color.r, image.color.g, image.color.b, 0.42f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = ReducedMotion ? 0.01f : 0.10f;
            button.colors = colors;
        }

        public static void StyleText(Text text, StrategyHudTextRole role, Color? overrideColor = null)
        {
            if (text == null)
            {
                return;
            }

            text.font = StrategyUiThemeProvider.Font;
            text.color = overrideColor ?? (role == StrategyHudTextRole.Metadata ? TextMuted : TextPrimary);
            text.fontSize = role switch
            {
                StrategyHudTextRole.Title => 24,
                StrategyHudTextRole.Section => 16,
                StrategyHudTextRole.Body => 14,
                _ => 12
            };
            text.fontStyle = role is StrategyHudTextRole.Title or StrategyHudTextRole.Section
                ? FontStyle.Bold
                : FontStyle.Normal;
            text.raycastTarget = false;
        }

        public static Outline AddShadow(GameObject target, float alpha = 0.55f)
        {
            if (target == null)
            {
                return null;
            }

            Outline outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
            return outline;
        }

        public static Image AddDivider(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject divider = new(name, typeof(RectTransform), typeof(Image));
            divider.transform.SetParent(parent, false);
            RectTransform rect = divider.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = divider.GetComponent<Image>();
            image.color = new Color(Primary.r, Primary.g, Primary.b, 0.48f);
            image.raycastTarget = false;
            return image;
        }

        private static Color Html(string value)
        {
            return ColorUtility.TryParseHtmlString(value, out Color color) ? color : Color.white;
        }
    }
}
