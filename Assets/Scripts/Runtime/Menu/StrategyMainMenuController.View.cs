using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyMainMenuController
    {
        private static readonly Color PanelColor = new(0.025f, 0.045f, 0.04f, 0.92f);
        private static readonly Color SettingsColor = new(0.045f, 0.055f, 0.06f, 0.96f);
        private static readonly Color ButtonColor = new(0.12f, 0.18f, 0.15f, 0.98f);
        private static readonly Color ButtonHoverColor = new(0.18f, 0.29f, 0.22f, 1f);
        private static readonly Color GoldColor = new(0.88f, 0.70f, 0.35f, 1f);
        private static readonly Color MutedColor = new(0.70f, 0.78f, 0.72f, 1f);

        private void BuildView()
        {
            GameObject canvasObject = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform shade = CreateRect("BackdropShade", canvasObject.transform);
            Stretch(shade);
            shade.gameObject.AddComponent<Image>().color = new Color(0.01f, 0.02f, 0.018f, 0.22f);

            RectTransform leftBand = CreateRect("MenuBand", canvasObject.transform);
            leftBand.anchorMin = Vector2.zero;
            leftBand.anchorMax = new Vector2(0.43f, 1f);
            leftBand.offsetMin = Vector2.zero;
            leftBand.offsetMax = Vector2.zero;
            leftBand.gameObject.AddComponent<Image>().color = PanelColor;

            BuildBrand(leftBand);
            BuildActions(leftBand);
            BuildLoading(leftBand);
            BuildSettings(canvasObject.transform);

            Text version = CreateText("Version", canvasObject.transform, "v" + Application.version, 12, TextAnchor.LowerRight, new Color(0.75f, 0.80f, 0.76f, 0.72f));
            SetRect(version.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-112f, 26f), new Vector2(180f, 24f));
            EnsureEventSystem();
        }

        private static void BuildBrand(RectTransform parent)
        {
            RectTransform accent = CreateRect("BrandAccent", parent);
            SetRect(accent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(82f, -118f), new Vector2(5f, 112f));
            accent.gameObject.AddComponent<Image>().color = GoldColor;

            Text title = CreateText("Title", parent, "PROJECT\nUNKNOWN", 49, TextAnchor.UpperLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            title.lineSpacing = 0.78f;
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(112f, -110f), new Vector2(470f, 130f));

            Text subtitle = CreateText("Subtitle", parent, "Build a settlement that can outlast winter.", 16, TextAnchor.UpperLeft, MutedColor);
            SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(112f, -246f), new Vector2(430f, 52f));
        }

        private void BuildActions(RectTransform parent)
        {
            actionsRoot = CreateRect("Actions", parent).gameObject;
            RectTransform root = actionsRoot.GetComponent<RectTransform>();
            SetRect(root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(112f, -70f), new Vector2(380f, 390f));

            continueButton = CreateButton("Continue", root, "Continue", new Vector2(0f, 132f), out Text continueLabel);
            continueLabel.alignment = TextAnchor.UpperLeft;
            continueLabel.rectTransform.offsetMin = new Vector2(22f, 18f);
            continueLabel.rectTransform.offsetMax = new Vector2(-18f, -5f);
            continueDetailText = CreateText("SaveDetail", continueButton.transform, string.Empty, 12, TextAnchor.LowerLeft, MutedColor);
            continueDetailText.raycastTarget = false;
            Stretch(continueDetailText.rectTransform, new Vector2(22f, 7f), new Vector2(-18f, -26f));

            newButton = CreateButton("NewSettlement", root, "New Settlement", new Vector2(0f, 58f), out _);
            settingsButton = CreateButton("Settings", root, "Settings", new Vector2(0f, -16f), out _);
            quitButton = CreateButton("Quit", root, "Quit", new Vector2(0f, -90f), out _);

            preloadStatusText = CreateText("PreloadStatus", root, "Preparing settlement", 12, TextAnchor.MiddleLeft, new Color(0.70f, 0.79f, 0.73f, 0.82f));
            SetRect(preloadStatusText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, -154f), new Vector2(360f, 30f));
        }

        private void BuildLoading(RectTransform parent)
        {
            loadingRoot = CreateRect("Loading", parent).gameObject;
            RectTransform root = loadingRoot.GetComponent<RectTransform>();
            SetRect(root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(112f, -60f), new Vector2(440f, 220f));

            Text title = CreateText("LoadingTitle", root, "PREPARING SETTLEMENT", 24, TextAnchor.UpperLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(0f, 40f));
            loadingStatusText = CreateText("LoadingStatus", root, "Shaping terrain", 15, TextAnchor.MiddleLeft, MutedColor);
            SetRect(loadingStatusText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 20f), new Vector2(-70f, 32f));
            loadingPercentText = CreateText("LoadingPercent", root, "0%", 15, TextAnchor.MiddleRight, GoldColor);
            SetRect(loadingPercentText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-56f, 20f), new Vector2(56f, 32f));

            RectTransform track = CreateRect("ProgressTrack", root);
            SetRect(track, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -16f), new Vector2(0f, 10f));
            track.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.26f, 0.23f, 1f);
            RectTransform fill = CreateRect("ProgressFill", track);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            progressFill = fill.gameObject.AddComponent<Image>();
            progressFill.color = GoldColor;
            loadingRoot.SetActive(false);
        }

        private void BuildSettings(Transform canvas)
        {
            settingsRoot = CreateRect("Settings", canvas).gameObject;
            RectTransform panel = settingsRoot.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.55f, 0f);
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            panel.gameObject.AddComponent<Image>().color = SettingsColor;

            Text title = CreateText("SettingsTitle", panel, "SETTINGS", 30, TextAnchor.UpperLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(82f, -96f), new Vector2(400f, 46f));
            Text subtitle = CreateText("SettingsSubtitle", panel, "Audio and display", 14, TextAnchor.UpperLeft, MutedColor);
            SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(82f, -143f), new Vector2(400f, 28f));

            masterSlider = CreateSlider(panel, "MasterVolume", "Master volume", -230f);
            musicSlider = CreateSlider(panel, "MusicVolume", "Music", -320f);
            sfxSlider = CreateSlider(panel, "SfxVolume", "Effects", -410f);
            fullscreenToggle = CreateToggle(panel, "Fullscreen", "Fullscreen", -500f);
            settingsBackButton = CreateButton("SettingsBack", panel, "Back", new Vector2(82f, -640f), out _);
            settingsBackButton.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 1f);
            settingsBackButton.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 1f);
            settingsRoot.SetActive(false);
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, out Text text)
        {
            RectTransform root = CreateRect(name, parent);
            root.anchorMin = new Vector2(0f, 0.5f);
            root.anchorMax = new Vector2(0f, 0.5f);
            root.pivot = new Vector2(0f, 0.5f);
            root.anchoredPosition = anchoredPosition;
            root.sizeDelta = new Vector2(340f, 58f);
            Image image = root.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            RectTransform accent = CreateRect("HoverAccent", root);
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.anchoredPosition = Vector2.zero;
            accent.sizeDelta = new Vector2(5f, 0f);
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0f);
            accentImage.raycastTarget = false;
            text = CreateText("Label", root, label, 17, TextAnchor.MiddleLeft, Color.white);
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            Stretch(text.rectTransform, new Vector2(22f, 0f), new Vector2(-18f, 0f));
            StrategyMainMenuButtonHover hover = root.gameObject.AddComponent<StrategyMainMenuButtonHover>();
            hover.Configure(button, image, accentImage, text, ButtonColor, ButtonHoverColor, GoldColor);
            return button;
        }

        private static Slider CreateSlider(Transform parent, string name, string label, float y)
        {
            Text text = CreateText(name + "Label", parent, label, 15, TextAnchor.MiddleLeft, Color.white);
            SetRect(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(82f, y), new Vector2(380f, 30f));
            RectTransform root = CreateRect(name, parent);
            SetRect(root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(82f, y - 36f), new Vector2(390f, 24f));
            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            RectTransform track = CreateRect("Track", root);
            Stretch(track, new Vector2(0f, 8f), new Vector2(0f, -8f));
            track.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.23f, 0.22f, 1f);
            RectTransform fillArea = CreateRect("FillArea", root);
            Stretch(fillArea, new Vector2(0f, 8f), new Vector2(0f, -8f));
            RectTransform fill = CreateRect("Fill", fillArea);
            Stretch(fill);
            fill.gameObject.AddComponent<Image>().color = GoldColor;
            RectTransform handle = CreateRect("Handle", root);
            handle.sizeDelta = new Vector2(18f, 24f);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color(0.93f, 0.87f, 0.70f, 1f);
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            return slider;
        }

        private static Toggle CreateToggle(Transform parent, string name, string label, float y)
        {
            RectTransform root = CreateRect(name, parent);
            SetRect(root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(82f, y), new Vector2(390f, 34f));
            Toggle toggle = root.gameObject.AddComponent<Toggle>();
            RectTransform box = CreateRect("Box", root);
            box.anchorMin = new Vector2(0f, 0.5f);
            box.anchorMax = new Vector2(0f, 0.5f);
            box.anchoredPosition = new Vector2(13f, 0f);
            box.sizeDelta = new Vector2(26f, 26f);
            Image background = box.gameObject.AddComponent<Image>();
            background.color = new Color(0.18f, 0.23f, 0.22f, 1f);
            RectTransform mark = CreateRect("Checkmark", box);
            Stretch(mark, new Vector2(5f, 5f), new Vector2(-5f, -5f));
            Image checkmark = mark.gameObject.AddComponent<Image>();
            checkmark.color = GoldColor;
            Text text = CreateText("Label", root, label, 15, TextAnchor.MiddleLeft, Color.white);
            Stretch(text.rectTransform, new Vector2(46f, 0f), Vector2.zero);
            toggle.targetGraphic = background;
            toggle.graphic = checkmark;
            return toggle;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor, Color color)
        {
            RectTransform root = CreateRect(name, parent);
            Text text = root.gameObject.AddComponent<Text>();
            text.font = StrategyUiThemeProvider.Font;
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj.GetComponent<RectTransform>();
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(
                Mathf.Approximately(anchorMin.x, anchorMax.x) ? anchorMin.x : 0.5f,
                Mathf.Approximately(anchorMin.y, anchorMax.y) ? anchorMin.y : 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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

        private static void EnsureEventSystem()
        {
            StrategyUiInputModuleBootstrap.Ensure();
        }
    }
}
