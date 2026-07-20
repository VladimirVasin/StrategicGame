using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPauseMenuController
    {
        private static readonly Color ShadeColor = new(0.008f, 0.014f, 0.013f, 0.60f);
        private static readonly Color PanelColor = new(0.025f, 0.045f, 0.04f, 0.975f);
        private static readonly Color CardColor = new(0.055f, 0.078f, 0.068f, 0.98f);
        private static readonly Color ButtonColor = new(0.105f, 0.155f, 0.13f, 0.99f);
        private static readonly Color GoldColor = new(0.88f, 0.70f, 0.35f, 1f);
        private static readonly Color MutedColor = new(0.69f, 0.77f, 0.71f, 1f);

        private void BuildView()
        {
            StrategyUiInputModuleBootstrap.Ensure();
            GameObject canvasObject = new(
                "PauseMenuCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;

            StrategyHudStyle.ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());

            RectTransform root = CreateRect("Root", canvasObject.transform);
            Stretch(root);
            root.gameObject.AddComponent<Image>().color = ShadeColor;
            CanvasGroup rootGroup = root.gameObject.AddComponent<CanvasGroup>();

            RectTransform panel = CreateRect("MenuBand", root);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = new Vector2(0.415f, 1f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            panel.gameObject.AddComponent<Image>().color = PanelColor;

            RectTransform edge = CreateRect("PanelEdge", panel);
            edge.anchorMin = new Vector2(1f, 0f);
            edge.anchorMax = Vector2.one;
            edge.pivot = new Vector2(1f, 0.5f);
            edge.anchoredPosition = Vector2.zero;
            edge.sizeDelta = new Vector2(3f, 0f);
            edge.gameObject.AddComponent<Image>().color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.45f);

            BuildHeader(panel);
            BuildActions(panel);
            BuildSettings(panel);

            Text hint = CreateLocalizedText(
                "EscapeHint",
                panel,
                "pause.escape_hint",
                12,
                TextAnchor.LowerLeft,
                new Color(MutedColor.r, MutedColor.g, MutedColor.b, 0.74f));
            SetRect(
                hint.rectTransform,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(88f, 42f),
                new Vector2(360f, 24f));

            panelTransition = root.gameObject.AddComponent<StrategyUiPanelTransition>();
            panelTransition.Configure(rootGroup, panel, new Vector2(-38f, 0f), 0.992f, 0.17f, 0.12f);
        }

        private static void BuildHeader(RectTransform panel)
        {
            RectTransform topAccent = CreateRect("TopAccent", panel);
            topAccent.anchorMin = new Vector2(0f, 1f);
            topAccent.anchorMax = new Vector2(1f, 1f);
            topAccent.pivot = new Vector2(0.5f, 1f);
            topAccent.anchoredPosition = Vector2.zero;
            topAccent.sizeDelta = new Vector2(0f, 5f);
            topAccent.gameObject.AddComponent<Image>().color = GoldColor;

            Text kicker = CreateLocalizedText(
                "Kicker",
                panel,
                "pause.kicker",
                14,
                TextAnchor.UpperLeft,
                GoldColor);
            kicker.fontStyle = FontStyle.Bold;
            SetRect(
                kicker.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(88f, -78f),
                new Vector2(420f, 24f));

            Text title = CreateLocalizedText("Title", panel, "pause.title", 44, TextAnchor.UpperLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetRect(
                title.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(88f, -108f),
                new Vector2(480f, 58f));

            Text subtitle = CreateLocalizedText(
                "Subtitle",
                panel,
                "pause.subtitle",
                15,
                TextAnchor.UpperLeft,
                MutedColor);
            SetRect(
                subtitle.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(88f, -172f),
                new Vector2(455f, 54f));

            RectTransform divider = CreateRect("HeaderDivider", panel);
            SetRect(
                divider,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(88f, -239f),
                new Vector2(455f, 2f));
            divider.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.16f);
        }

        private void BuildActions(RectTransform panel)
        {
            actionsRoot = CreateRect("Actions", panel).gameObject;
            RectTransform root = actionsRoot.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(88f, -278f);
            root.sizeDelta = new Vector2(455f, 480f);

            resumeButton = CreateButton("Resume", root, "pause.resume", 0f, true);
            saveButton = CreateButton("SaveGame", root, "pause.save_game", -68f, false);
            settingsButton = CreateButton("Settings", root, "settings.open", -136f, false);
            mainMenuButton = CreateButton("MainMenu", root, "pause.main_menu", -204f, false);
            quitButton = CreateButton("Quit", root, "pause.quit_game", -272f, false);

            RectTransform statusCard = CreateRect("StatusCard", root);
            statusCard.anchorMin = new Vector2(0f, 1f);
            statusCard.anchorMax = new Vector2(0f, 1f);
            statusCard.pivot = new Vector2(0f, 1f);
            statusCard.anchoredPosition = new Vector2(0f, -350f);
            statusCard.sizeDelta = new Vector2(420f, 58f);
            statusCard.gameObject.AddComponent<Image>().color = CardColor;
            RectTransform statusAccent = CreateRect("Accent", statusCard);
            statusAccent.anchorMin = Vector2.zero;
            statusAccent.anchorMax = new Vector2(0f, 1f);
            statusAccent.pivot = new Vector2(0f, 0.5f);
            statusAccent.anchoredPosition = Vector2.zero;
            statusAccent.sizeDelta = new Vector2(4f, 0f);
            statusAccent.gameObject.AddComponent<Image>().color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.55f);
            statusText = CreateText("Status", statusCard, string.Empty, 13, TextAnchor.MiddleLeft, MutedColor);
            Stretch(statusText.rectTransform, new Vector2(18f, 4f), new Vector2(-14f, -4f));
        }

        private void BuildSettings(RectTransform panel)
        {
            settingsRoot = CreateRect("Settings", panel).gameObject;
            RectTransform root = settingsRoot.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(88f, -274f);
            root.sizeDelta = new Vector2(455f, 570f);

            Text title = CreateLocalizedText("SettingsTitle", root, "settings.title", 24, TextAnchor.UpperLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetTopRect(title.rectTransform, 0f, 0f, 420f, 34f);
            Text subtitle = CreateLocalizedText("SettingsSubtitle", root, "settings.subtitle", 13, TextAnchor.UpperLeft, MutedColor);
            SetTopRect(subtitle.rectTransform, 0f, -37f, 420f, 24f);

            masterSlider = CreateSlider(root, "MasterVolume", "settings.master_volume", -82f);
            musicSlider = CreateSlider(root, "MusicVolume", "settings.music", -154f);
            sfxSlider = CreateSlider(root, "SfxVolume", "settings.effects", -226f);
            uiScaleSlider = CreateSlider(root, "UiScale", "settings.interface_scale", -298f);
            uiScaleSlider.minValue = 0.85f;
            uiScaleSlider.maxValue = 1.25f;
            fullscreenToggle = CreateToggle(root, "Fullscreen", "settings.fullscreen", -378f);
            reducedMotionToggle = CreateToggle(root, "ReducedMotion", "settings.reduced_motion", -418f);
            languageButton = CreateButton("Language", root, string.Empty, -458f, false, out languageButtonLabel, false);
            settingsBackButton = CreateButton("SettingsBack", root, "menu.back", -520f, true);
            settingsRoot.SetActive(false);
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string labelKey,
            float y,
            bool primary,
            out Text labelText,
            bool localized = true)
        {
            RectTransform root = CreateRect(name, parent);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(0f, y);
            root.sizeDelta = new Vector2(420f, 56f);
            Image image = root.gameObject.AddComponent<Image>();
            image.color = primary ? GoldColor : ButtonColor;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = Color.Lerp(image.color, Color.white, primary ? 0.10f : 0.15f);
            colors.pressedColor = Color.Lerp(image.color, Color.black, 0.16f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(image.color.r, image.color.g, image.color.b, 0.42f);
            button.colors = colors;

            RectTransform accent = CreateRect("Accent", root);
            accent.anchorMin = Vector2.zero;
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.anchoredPosition = Vector2.zero;
            accent.sizeDelta = new Vector2(5f, 0f);
            accent.gameObject.AddComponent<Image>().color = primary
                ? new Color(1f, 0.92f, 0.70f, 0.95f)
                : new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.72f);

            string label = localized
                ? StrategyLocalization.Get(StrategyLocalizationTables.Menu, labelKey)
                : labelKey;
            labelText = CreateText(
                "Label",
                root,
                label,
                17,
                TextAnchor.MiddleLeft,
                primary ? new Color(0.10f, 0.095f, 0.07f, 1f) : Color.white);
            labelText.fontStyle = FontStyle.Bold;
            Stretch(labelText.rectTransform, new Vector2(22f, 0f), new Vector2(-18f, 0f));
            if (localized)
            {
                StrategyLocalizedTextBinding.Bind(labelText, StrategyLocalizationTables.Menu, labelKey);
            }
            StrategyUiButtonFeedback.Attach(button, StrategyUiButtonFeedbackProfile.Standard, null);
            return button;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string labelKey,
            float y,
            bool primary)
        {
            return CreateButton(name, parent, labelKey, y, primary, out _);
        }

        private static Slider CreateSlider(Transform parent, string name, string labelKey, float y)
        {
            Text text = CreateLocalizedText(name + "Label", parent, labelKey, 14, TextAnchor.MiddleLeft, Color.white);
            SetTopRect(text.rectTransform, 0f, y, 420f, 24f);
            RectTransform root = CreateRect(name, parent);
            SetTopRect(root, 0f, y - 34f, 420f, 24f);
            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            RectTransform track = CreateRect("Track", root);
            Stretch(track, new Vector2(0f, 8f), new Vector2(0f, -8f));
            track.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.22f, 0.19f, 1f);
            RectTransform fillArea = CreateRect("FillArea", root);
            Stretch(fillArea, new Vector2(0f, 8f), new Vector2(0f, -8f));
            RectTransform fill = CreateRect("Fill", fillArea);
            Stretch(fill);
            fill.gameObject.AddComponent<Image>().color = GoldColor;
            RectTransform handle = CreateRect("Handle", root);
            handle.sizeDelta = new Vector2(18f, 24f);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color(0.95f, 0.89f, 0.72f, 1f);
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            return slider;
        }

        private static Toggle CreateToggle(Transform parent, string name, string labelKey, float y)
        {
            RectTransform root = CreateRect(name, parent);
            SetTopRect(root, 0f, y, 420f, 34f);
            Toggle toggle = root.gameObject.AddComponent<Toggle>();
            RectTransform box = CreateRect("Box", root);
            box.anchorMin = new Vector2(0f, 0.5f);
            box.anchorMax = new Vector2(0f, 0.5f);
            box.anchoredPosition = new Vector2(13f, 0f);
            box.sizeDelta = new Vector2(26f, 26f);
            Image background = box.gameObject.AddComponent<Image>();
            background.color = new Color(0.16f, 0.22f, 0.19f, 1f);
            RectTransform mark = CreateRect("Checkmark", box);
            Stretch(mark, new Vector2(5f, 5f), new Vector2(-5f, -5f));
            Image checkmark = mark.gameObject.AddComponent<Image>();
            checkmark.color = GoldColor;
            Text text = CreateLocalizedText("Label", root, labelKey, 14, TextAnchor.MiddleLeft, Color.white);
            Stretch(text.rectTransform, new Vector2(46f, 0f), Vector2.zero);
            toggle.targetGraphic = background;
            toggle.graphic = checkmark;
            return toggle;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int size,
            TextAnchor alignment,
            Color color)
        {
            RectTransform root = CreateRect(name, parent);
            Text text = root.gameObject.AddComponent<Text>();
            text.font = StrategyUiThemeProvider.Font;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Text CreateLocalizedText(
            string name,
            Transform parent,
            string key,
            int size,
            TextAnchor alignment,
            Color color)
        {
            Text text = CreateText(
                name,
                parent,
                StrategyLocalization.Get(StrategyLocalizationTables.Menu, key),
                size,
                alignment,
                color);
            StrategyLocalizedTextBinding.Bind(text, StrategyLocalizationTables.Menu, key);
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject obj = new(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj.GetComponent<RectTransform>();
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(
                Mathf.Approximately(anchorMin.x, anchorMax.x) ? anchorMin.x : 0.5f,
                Mathf.Approximately(anchorMin.y, anchorMax.y) ? anchorMin.y : 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTopRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
