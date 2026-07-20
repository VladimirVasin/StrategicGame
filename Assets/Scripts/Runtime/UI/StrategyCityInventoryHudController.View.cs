using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyCityInventoryHudController
    {
        private static readonly Color Gold = new(0.90f, 0.67f, 0.29f, 1f);
        private static readonly Color MutedGold = new(0.82f, 0.66f, 0.38f, 1f);
        private static readonly Color PanelColor = new(0.055f, 0.075f, 0.075f, 0.98f);
        private static Sprite chestSprite;

        private void EnsureUi()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            StrategyUiInputModuleBootstrap.Ensure();
            GameObject canvasObject = new(
                "CityInventoryHudCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            hudCanvas = canvasObject.GetComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 170;

            StrategyHudStyle.ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());

            CreateLauncher(canvasObject.transform);
            CreateOverlay(canvasObject.transform);
        }

        private void CreateLauncher(Transform parent)
        {
            launcherRoot = CreateUiObject("CityInventoryButton", parent).GetComponent<RectTransform>();
            launcherRoot.anchorMin = new Vector2(0f, 1f);
            launcherRoot.anchorMax = new Vector2(0f, 1f);
            launcherRoot.pivot = new Vector2(0f, 1f);
            launcherRoot.anchoredPosition = new Vector2(458f, -5f);
            launcherRoot.sizeDelta = new Vector2(146f, 60f);

            Image background = launcherRoot.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleRailModule(background, true);
            Outline outline = launcherRoot.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.40f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);

            RectTransform iconFrame = CreateUiObject("ChestIconFrame", launcherRoot).GetComponent<RectTransform>();
            SetTopLeft(iconFrame, 9f, 7f, 28f, 28f);
            Image frame = iconFrame.gameObject.AddComponent<Image>();
            frame.color = new Color(1f, 1f, 1f, 0.07f);
            frame.raycastTarget = false;
            RectTransform iconRect = CreateUiObject("ChestIcon", iconFrame).GetComponent<RectTransform>();
            Stretch(iconRect, 4f, 4f, 4f, 4f);
            launcherChestIcon = iconRect;
            launcherChestIconRestScale = iconRect.localScale;
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = GetChestSprite();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Text label = CreateText(
                "Label",
                launcherRoot,
                "Inventory",
                14,
                TextAnchor.MiddleLeft,
                new Color(0.95f, 0.88f, 0.62f));
            StrategyLocalizedTextBinding.Bind(
                label,
                StrategyLocalizationTables.Hud,
                "hud.inventory.launcher");
            label.fontStyle = FontStyle.Bold;
            SetOffsets(label.rectTransform, 46f, 0f, 34f, 0f);

            badgeRoot = CreateUiObject("OwnedTypeBadge", launcherRoot);
            RectTransform badgeRect = badgeRoot.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1f, 0.5f);
            badgeRect.anchorMax = new Vector2(1f, 0.5f);
            badgeRect.pivot = new Vector2(1f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(-8f, 0f);
            badgeRect.sizeDelta = new Vector2(24f, 24f);
            Image badgeImage = badgeRoot.AddComponent<Image>();
            badgeImage.color = new Color(0.58f, 0.30f, 0.12f, 1f);
            badgeImage.raycastTarget = false;
            badgeText = CreateText(
                "Count",
                badgeRect,
                "0",
                12,
                TextAnchor.MiddleCenter,
                Color.white);
            badgeText.fontStyle = FontStyle.Bold;
            Stretch(badgeText.rectTransform, 0f, 0f, 0f, 1f);
            badgeRoot.SetActive(false);

            Button button = launcherRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(Toggle);
            ConfigureButtonColors(button, background.color);
            StrategyUiButtonFeedback.Attach(button);
            launcherTooltip = StrategyHudTooltip.Attach(
                launcherRoot.gameObject,
                StrategyLocalization.Get(
                    StrategyLocalizationTables.Hud,
                    "hud.inventory.tooltip"));
        }

        private void CreateOverlay(Transform parent)
        {
            overlayRoot = CreateUiObject("CityInventoryOverlay", parent);
            RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
            Stretch(overlayRect, 0f, 0f, 0f, 0f);

            RectTransform backdropRoot = CreateUiObject("Backdrop", overlayRoot.transform)
                .GetComponent<RectTransform>();
            Stretch(backdropRoot, 0f, 0f, 0f, 0f);
            Image backdrop = backdropRoot.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0.01f, 0.015f, 0.015f, 0.58f);
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
                new Vector2(0f, -18f),
                0.985f,
                PanelOpenDuration,
                PanelCloseDuration);
            panelTransition.SetVisible(false, true);
        }

        private void CreatePanel(Transform parent)
        {
            panelRoot = CreateUiObject("CityInventoryPanel", parent).GetComponent<RectTransform>();
            panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            panelRoot.pivot = new Vector2(0.5f, 0.5f);
            panelRoot.anchoredPosition = new Vector2(0f, -8f);
            panelRoot.sizeDelta = new Vector2(900f, 600f);
            Image background = panelRoot.gameObject.AddComponent<Image>();
            StrategyHudStyle.StylePanel(background, new Color(0.88f, 0.92f, 0.86f, 0.98f), true);
            Outline outline = panelRoot.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.62f);
            outline.effectDistance = new Vector2(2f, -2f);

            RectTransform accent = CreateUiObject("GoldAccent", panelRoot).GetComponent<RectTransform>();
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.sizeDelta = new Vector2(5f, 0f);
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = Gold;
            accentImage.raycastTarget = false;

            Text title = CreateText("Title", panelRoot, "CITY INVENTORY", 25, TextAnchor.UpperLeft, Color.white);
            StrategyLocalizedTextBinding.Bind(
                title,
                StrategyLocalizationTables.Hud,
                "hud.inventory.title");
            title.fontStyle = FontStyle.Bold;
            SetTopStretch(title.rectTransform, 30f, 24f, 90f, 32f);
            Text subtitle = CreateText(
                "Subtitle",
                panelRoot,
                "THE SETTLEMENT CHEST  /  SPECIAL ITEMS",
                12,
                TextAnchor.UpperLeft,
                MutedGold);
            StrategyLocalizedTextBinding.Bind(
                subtitle,
                StrategyLocalizationTables.Hud,
                "hud.inventory.subtitle");
            subtitle.fontStyle = FontStyle.Bold;
            SetTopStretch(subtitle.rectTransform, 30f, 58f, 90f, 18f);

            RectTransform closeRoot = CreateUiObject("Close", panelRoot).GetComponent<RectTransform>();
            closeRoot.anchorMin = new Vector2(1f, 1f);
            closeRoot.anchorMax = new Vector2(1f, 1f);
            closeRoot.pivot = new Vector2(1f, 1f);
            closeRoot.anchoredPosition = new Vector2(-26f, -24f);
            closeRoot.sizeDelta = new Vector2(40f, 34f);
            Image closeImage = closeRoot.gameObject.AddComponent<Image>();
            closeImage.color = new Color(0.11f, 0.15f, 0.16f, 0.98f);
            closeButton = closeRoot.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(() => SetOpen(false));
            ConfigureButtonColors(closeButton, closeImage.color);
            StrategyUiButtonFeedback.Attach(closeButton, StrategyUiButtonFeedbackProfile.Compact);
            Text closeText = CreateText("CloseText", closeRoot, "X", 16, TextAnchor.MiddleCenter, Color.white);
            closeText.fontStyle = FontStyle.Bold;
            Stretch(closeText.rectTransform, 0f, 0f, 0f, 1f);

            RectTransform divider = CreateUiObject("HeaderDivider", panelRoot).GetComponent<RectTransform>();
            SetTopStretch(divider, 30f, 88f, 30f, 2f);
            Image dividerImage = divider.gameObject.AddComponent<Image>();
            dividerImage.color = new Color(1f, 1f, 1f, 0.17f);
            dividerImage.raycastTarget = false;

            CreateItemViewport();
            CreateDetailPanel();
            CreateEmptyState();
        }

        private void CreateItemViewport()
        {
            RectTransform viewport = CreateUiObject("ItemViewport", panelRoot).GetComponent<RectTransform>();
            itemViewportRoot = viewport.gameObject;
            SetTopLeft(viewport, 24f, 104f, 530f, 470f);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0.035f, 0.050f, 0.050f, 0.94f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            itemContent = CreateUiObject("ItemContent", viewport).GetComponent<RectTransform>();
            itemContent.anchorMin = new Vector2(0f, 1f);
            itemContent.anchorMax = new Vector2(1f, 1f);
            itemContent.pivot = new Vector2(0.5f, 1f);
            itemContent.anchoredPosition = Vector2.zero;
            itemContent.sizeDelta = new Vector2(0f, 1f);
            VerticalLayoutGroup list = itemContent.gameObject.AddComponent<VerticalLayoutGroup>();
            list.padding = new RectOffset(10, 10, 10, 10);
            list.spacing = 8f;
            list.childAlignment = TextAnchor.UpperLeft;
            list.childControlWidth = true;
            list.childControlHeight = false;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
            ContentSizeFitter fitter = itemContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = itemContent;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
        }

        private void CreateDetailPanel()
        {
            RectTransform detail = CreateUiObject("ItemDetail", panelRoot).GetComponent<RectTransform>();
            detailPanelRoot = detail.gameObject;
            SetTopLeft(detail, 570f, 104f, 306f, 470f);
            Image detailBackground = detail.gameObject.AddComponent<Image>();
            detailBackground.color = new Color(0.075f, 0.10f, 0.095f, 0.97f);
            detailBackground.raycastTarget = false;

            RectTransform iconFrame = CreateUiObject("IconFrame", detail).GetComponent<RectTransform>();
            SetTopLeft(iconFrame, 20f, 20f, 76f, 76f);
            Image frame = iconFrame.gameObject.AddComponent<Image>();
            frame.color = new Color(1f, 1f, 1f, 0.07f);
            frame.raycastTarget = false;
            RectTransform iconRect = CreateUiObject("Icon", iconFrame).GetComponent<RectTransform>();
            Stretch(iconRect, 9f, 9f, 9f, 9f);
            detailIcon = iconRect.gameObject.AddComponent<Image>();
            detailIcon.preserveAspect = true;
            detailIcon.raycastTarget = false;

            detailName = CreateText("Name", detail, "Select an item", 19, TextAnchor.UpperLeft, Color.white);
            detailName.fontStyle = FontStyle.Bold;
            SetTopLeft(detailName.rectTransform, 112f, 24f, 174f, 48f);
            detailQuantity = CreateText("Quantity", detail, string.Empty, 12, TextAnchor.UpperLeft, MutedGold);
            detailQuantity.fontStyle = FontStyle.Bold;
            SetTopLeft(detailQuantity.rectTransform, 112f, 70f, 174f, 22f);

            Text storyLabel = CreateText("StoryLabel", detail, "STORY", 11, TextAnchor.UpperLeft, MutedGold);
            StrategyLocalizedTextBinding.Bind(
                storyLabel,
                StrategyLocalizationTables.Hud,
                "hud.inventory.story");
            storyLabel.fontStyle = FontStyle.Bold;
            SetTopStretch(storyLabel.rectTransform, 20f, 120f, 20f, 18f);
            detailDescription = CreateText("Description", detail, string.Empty, 13, TextAnchor.UpperLeft, new Color(0.84f, 0.88f, 0.84f));
            detailDescription.lineSpacing = 1.12f;
            SetTopStretch(detailDescription.rectTransform, 20f, 144f, 20f, 126f);

            Text effectLabel = CreateText("EffectLabel", detail, "EFFECT", 11, TextAnchor.UpperLeft, MutedGold);
            StrategyLocalizedTextBinding.Bind(
                effectLabel,
                StrategyLocalizationTables.Hud,
                "hud.inventory.effect");
            effectLabel.fontStyle = FontStyle.Bold;
            SetTopStretch(effectLabel.rectTransform, 20f, 292f, 20f, 18f);
            detailEffect = CreateText("Effect", detail, string.Empty, 13, TextAnchor.UpperLeft, new Color(0.76f, 0.86f, 0.80f));
            detailEffect.lineSpacing = 1.12f;
            SetTopStretch(detailEffect.rectTransform, 20f, 316f, 20f, 118f);

            Text readOnly = CreateText(
                "ReadOnlyNote",
                detail,
                "Stored safely for the whole city",
                11,
                TextAnchor.LowerCenter,
                new Color(0.60f, 0.68f, 0.65f));
            StrategyLocalizedTextBinding.Bind(
                readOnly,
                StrategyLocalizationTables.Hud,
                "hud.inventory.read_only");
            SetBottomStretch(readOnly.rectTransform, 18f, 16f, 18f, 24f);
        }

        private void CreateEmptyState()
        {
            emptyStateRoot = CreateUiObject("EmptyState", panelRoot);
            RectTransform emptyRect = emptyStateRoot.GetComponent<RectTransform>();
            SetTopLeft(emptyRect, 24f, 104f, 852f, 470f);
            Image emptyBackground = emptyStateRoot.AddComponent<Image>();
            emptyBackground.color = new Color(0.035f, 0.050f, 0.050f, 0.98f);
            emptyBackground.raycastTarget = false;

            RectTransform chestRect = CreateUiObject("EmptyChest", emptyRect).GetComponent<RectTransform>();
            chestRect.anchorMin = new Vector2(0.5f, 0.5f);
            chestRect.anchorMax = new Vector2(0.5f, 0.5f);
            chestRect.pivot = new Vector2(0.5f, 0.5f);
            chestRect.anchoredPosition = new Vector2(0f, 54f);
            chestRect.sizeDelta = new Vector2(92f, 72f);
            Image chest = chestRect.gameObject.AddComponent<Image>();
            chest.sprite = GetChestSprite();
            chest.preserveAspect = true;
            chest.color = new Color(1f, 1f, 1f, 0.82f);
            chest.raycastTarget = false;

            emptyStateTitle = CreateText(
                "EmptyTitle",
                emptyRect,
                "The city chest is empty",
                21,
                TextAnchor.MiddleCenter,
                Color.white);
            StrategyLocalizedTextBinding.Bind(
                emptyStateTitle,
                StrategyLocalizationTables.Hud,
                "hud.inventory.empty_title");
            emptyStateTitle.fontStyle = FontStyle.Bold;
            SetCenter(emptyStateTitle.rectTransform, 0f, -16f, 620f, 34f);
            emptyStateBody = CreateText(
                "EmptyBody",
                emptyRect,
                "Special finds and keepsakes will appear here as the settlement's story unfolds.",
                14,
                TextAnchor.UpperCenter,
                new Color(0.70f, 0.78f, 0.74f));
            StrategyLocalizedTextBinding.Bind(
                emptyStateBody,
                StrategyLocalizationTables.Hud,
                "hud.inventory.empty_body");
            SetCenter(emptyStateBody.rectTransform, 0f, -62f, 600f, 58f);
        }

        private void UpdateRewardReceivedFeedback()
        {
            if (!rewardFeedbackActive || launcherChestIcon == null)
            {
                return;
            }

            float normalizedTime = Mathf.Clamp01(
                (Time.unscaledTime - rewardFeedbackStartedAt) / RewardFeedbackDuration);
            if (normalizedTime >= 1f)
            {
                ResetRewardReceivedFeedback();
                return;
            }

            float firstPulseTime = Mathf.Clamp01(normalizedTime / 0.58f);
            float echoPulseTime = Mathf.Clamp01((normalizedTime - 0.48f) / 0.52f);
            float firstPulse = Mathf.Sin(firstPulseTime * Mathf.PI) * 0.24f;
            float echoPulse = Mathf.Sin(echoPulseTime * Mathf.PI) * 0.08f;
            launcherChestIcon.localScale = launcherChestIconRestScale
                * (1f + firstPulse + echoPulse);
        }

        private void ResetRewardReceivedFeedback()
        {
            rewardFeedbackActive = false;
            rewardFeedbackStartedAt = 0f;
            if (launcherChestIcon != null)
            {
                launcherChestIcon.localScale = launcherChestIconRestScale;
            }
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

        private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetOffsets(RectTransform rect, float left, float top, float right, float bottom)
        {
            Stretch(rect, left, top, right, bottom);
        }

        private static void SetTopLeft(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
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

        private static void SetCenter(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
