using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResourceOverviewHudController
    {
        private static readonly Color Gold = new(0.90f, 0.67f, 0.29f, 1f);
        private static readonly Color MutedGold = new(0.82f, 0.66f, 0.38f, 1f);

        private void EnsureUi()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            StrategyUiInputModuleBootstrap.Ensure();
            GameObject canvasObject = new(
                "StrategyResourceOverviewHudCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            hudCanvas = canvasObject.GetComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 180;
            StrategyHudStyle.ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());

            CreateOverlay(canvasObject.transform);
            CreateLauncher(canvasObject.transform);
        }

        private void CreateLauncher(Transform parent)
        {
            launcherRoot = CreateUiObject("SettlementStoresButton", parent)
                .GetComponent<RectTransform>();
            SetTopLeft(launcherRoot, 16f, 5f, 270f, 60f);

            Image background = launcherRoot.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleRailModule(background, true);
            StrategyHudStyle.AddShadow(launcherRoot.gameObject, 0.40f);

            Text title = CreateText(
                "Title",
                launcherRoot,
                "SETTLEMENT STORES",
                11,
                TextAnchor.UpperLeft,
                StrategyHudStyle.Primary);
            title.fontStyle = FontStyle.Bold;
            SetTopStretch(title.rectTransform, 10f, 5f, 48f, 17f);

            launcherSummary = CreateText(
                "ConstructionSummary",
                launcherRoot,
                "Logs 0  |  Stone 0",
                14,
                TextAnchor.MiddleLeft,
                StrategyHudStyle.TextPrimary);
            launcherSummary.fontStyle = FontStyle.Bold;
            SetTopStretch(launcherSummary.rectTransform, 10f, 23f, 48f, 29f);

            RectTransform allBadge = CreateUiObject("AllResourcesBadge", launcherRoot)
                .GetComponent<RectTransform>();
            allBadge.anchorMin = new Vector2(1f, 0.5f);
            allBadge.anchorMax = new Vector2(1f, 0.5f);
            allBadge.pivot = new Vector2(1f, 0.5f);
            allBadge.anchoredPosition = new Vector2(-9f, 0f);
            allBadge.sizeDelta = new Vector2(34f, 24f);
            Image badgeBackground = allBadge.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(
                badgeBackground,
                new Color(1f, 1f, 1f, 0.08f));
            badgeBackground.raycastTarget = false;
            Text badge = CreateText(
                "Label",
                allBadge,
                "ALL",
                9,
                TextAnchor.MiddleCenter,
                MutedGold);
            badge.fontStyle = FontStyle.Bold;
            Stretch(badge.rectTransform, 0f, 0f, 0f, 1f);

            Button button = launcherRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(Toggle);
            ConfigureButtonColors(button, background.color);
            StrategyUiButtonFeedback.Attach(
                button,
                StrategyUiButtonFeedbackProfile.Compact);
            StrategyHudTooltip.Attach(
                launcherRoot.gameObject,
                "Logs and Stone show construction-ready stock. Open all resources stored across the settlement.",
                StrategyHudTooltipPlacement.Below);
        }

        private void CreateOverlay(Transform parent)
        {
            overlayRoot = CreateUiObject("ResourceOverviewOverlay", parent);
            RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
            Stretch(overlayRect, 0f, 0f, 0f, 0f);

            RectTransform backdropRoot = CreateUiObject("OutsideClickShield", overlayRoot.transform)
                .GetComponent<RectTransform>();
            Stretch(backdropRoot, 0f, 0f, 0f, 0f);
            Image backdrop = backdropRoot.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.012f);
            Button backdropButton = backdropRoot.gameObject.AddComponent<Button>();
            backdropButton.targetGraphic = backdrop;
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.onClick.AddListener(() => SetOpen(false));

            overlayGroup = overlayRoot.AddComponent<CanvasGroup>();
            CreatePanel(overlayRoot.transform);
            panelTransition = overlayRoot.AddComponent<StrategyUiPanelTransition>();
            panelTransition.Configure(
                overlayGroup,
                panelRoot,
                new Vector2(0f, 12f),
                0.985f,
                PanelOpenDuration,
                PanelCloseDuration);
            panelTransition.SetVisible(false, true);
        }

        private void CreatePanel(Transform parent)
        {
            panelRoot = CreateUiObject("ResourceOverviewPanel", parent)
                .GetComponent<RectTransform>();
            SetTopLeft(panelRoot, 16f, 72f, 650f, 440f);
            Image background = panelRoot.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(
                background,
                new Color(0.055f, 0.075f, 0.075f, 0.985f),
                true);
            StrategyHudStyle.AddShadow(panelRoot.gameObject, 0.68f);

            RectTransform accent = CreateUiObject("GoldAccent", panelRoot)
                .GetComponent<RectTransform>();
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.sizeDelta = new Vector2(4f, 0f);
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = Gold;
            accentImage.raycastTarget = false;

            Text title = CreateText(
                "Title",
                panelRoot,
                "ALL RESOURCES",
                21,
                TextAnchor.UpperLeft,
                Color.white);
            title.fontStyle = FontStyle.Bold;
            SetTopStretch(title.rectTransform, 24f, 18f, 72f, 28f);

            Text subtitle = CreateText(
                "Subtitle",
                panelRoot,
                "STORED ACROSS THE SETTLEMENT",
                11,
                TextAnchor.UpperLeft,
                MutedGold);
            subtitle.fontStyle = FontStyle.Bold;
            SetTopStretch(subtitle.rectTransform, 24f, 49f, 72f, 18f);

            RectTransform closeRoot = CreateUiObject("Close", panelRoot)
                .GetComponent<RectTransform>();
            closeRoot.anchorMin = new Vector2(1f, 1f);
            closeRoot.anchorMax = new Vector2(1f, 1f);
            closeRoot.pivot = new Vector2(1f, 1f);
            closeRoot.anchoredPosition = new Vector2(-18f, -18f);
            closeRoot.sizeDelta = new Vector2(36f, 32f);
            Image closeImage = closeRoot.gameObject.AddComponent<Image>();
            closeButton = closeRoot.gameObject.AddComponent<Button>();
            StrategyHudStyle.StyleButton(closeButton, closeImage);
            closeButton.onClick.AddListener(() => SetOpen(false));
            StrategyUiButtonFeedback.Attach(
                closeButton,
                StrategyUiButtonFeedbackProfile.Compact);
            Text closeText = CreateText(
                "CloseText",
                closeRoot,
                "X",
                15,
                TextAnchor.MiddleCenter,
                Color.white);
            closeText.fontStyle = FontStyle.Bold;
            Stretch(closeText.rectTransform, 0f, 0f, 0f, 1f);

            RectTransform divider = CreateUiObject("HeaderDivider", panelRoot)
                .GetComponent<RectTransform>();
            SetTopStretch(divider, 24f, 78f, 24f, 2f);
            Image dividerImage = divider.gameObject.AddComponent<Image>();
            dividerImage.color = StrategyHudStyle.Divider;
            dividerImage.raycastTarget = false;

            CreateResourceSections();
        }

        private void RefreshLauncherSummary()
        {
            if (launcherSummary == null)
            {
                return;
            }

            StrategyConstructionResourceCost resources =
                StrategyResourceQueryService.GetConstructionResources();
            launcherSummary.text = "Logs " + resources.Logs
                + "  |  Stone " + resources.Stone;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            Text text = CreateUiObject(name, parent).AddComponent<Text>();
            text.font = StrategyUiThemeProvider.Font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void ConfigureButtonColors(Button button, Color baseColor)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.10f);
            colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.16f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.45f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
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

        private static void SetTopLeft(
            RectTransform rect,
            float left,
            float top,
            float width,
            float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
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
    }
}
