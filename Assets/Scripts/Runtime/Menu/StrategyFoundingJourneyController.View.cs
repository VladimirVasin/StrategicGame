using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFoundingJourneyController
    {
        private static readonly Color JourneyPanelColor = new(0.018f, 0.030f, 0.030f, 0.025f);
        private static readonly Color JourneyButtonColor = new(0.105f, 0.145f, 0.135f, 0.98f);
        private static readonly Color JourneyButtonHoverColor = new(0.16f, 0.235f, 0.205f, 1f);
        private static readonly Color JourneySelectedColor = new(0.20f, 0.285f, 0.225f, 1f);
        private static readonly Color JourneyGoldColor = new(0.90f, 0.70f, 0.35f, 1f);
        private static readonly Color JourneyMutedColor = new(0.72f, 0.80f, 0.76f, 1f);

        private StrategyFoundingJourneyPresentation presentationController;
        private StrategyFoundingJourneyAudio journeyAudioController;
        private StrategyFoundingJourneyAtmosphere atmosphereController;
        private GameObject storyRoot;
        private GameObject questionRoot;
        private GameObject summaryRoot;
        private GameObject loadingRoot;
        private Text storyChapterText;
        private Text storyTitleText;
        private Text storyBodyText;
        private Text storyProgressText;
        private Text questionChapterText;
        private Text questionTitleText;
        private Text questionContextText;
        private Text summaryBodyText;
        private Text summaryNoteText;
        private Text loadingTitleText;
        private Text loadingDetailText;
        private Text preparationText;
        private Text loadingPercentText;
        private Text nextButtonLabel;
        private Image loadingProgressFill;
        private Button backButton;
        private Button nextButton;
        private Button skipStoryButton;
        private Button balancedDefaultsButton;
        private Button changeAnswersButton;
        private Button beginButton;
        private readonly Button[] optionButtons = new Button[3];
        private readonly Text[] optionLabels = new Text[3];
        private readonly Text[] optionDescriptions = new Text[3];
        private readonly Image[] optionImages = new Image[3];
        private readonly Image[] optionAccents = new Image[3];
        private readonly string[] optionIds = new string[3];
        private Toggle reducedMotionToggle;

        private void BuildJourneyView()
        {
            GameObject canvasObject = new(
                "FoundingJourneyCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform backdrop = CreateRect("Backdrop", canvasObject.transform);
            Stretch(backdrop);
            backdrop.gameObject.AddComponent<Image>().color = new Color(0.01f, 0.015f, 0.018f, 1f);

            RectTransform backgroundStage = CreateRect("BackgroundStage", canvasObject.transform);
            Stretch(backgroundStage, new Vector2(-36f, -24f), new Vector2(36f, 24f));
            Image backgroundA = CreateBackground("BackgroundA", backgroundStage);
            Image backgroundB = CreateBackground("BackgroundB", backgroundStage);
            backgroundB.color = new Color(1f, 1f, 1f, 0f);

            RectTransform shade = CreateRect("CinematicShade", canvasObject.transform);
            Stretch(shade);
            shade.gameObject.AddComponent<Image>().color = new Color(0.005f, 0.012f, 0.014f, 0.14f);

            RectTransform atmosphereRoot = CreateRect("Atmosphere", backgroundStage);
            Stretch(atmosphereRoot);
            atmosphereController = atmosphereRoot.gameObject.AddComponent<StrategyFoundingJourneyAtmosphere>();
            atmosphereController.Configure(atmosphereRoot, reducedMotion);

            RectTransform panel = CreateRect("JourneyPanel", canvasObject.transform);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = new Vector2(0.45f, 1f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            panel.gameObject.AddComponent<Image>().color = JourneyPanelColor;
            BuildNarrativeGradient(panel);

            BuildStoryPanel(panel);
            BuildQuestionPanel(panel);
            BuildSummaryPanel(panel);
            BuildLoadingPanel(panel);

            backButton = CreateButton("BackButton", panel, "Back", 15, JourneyButtonColor, out _);
            SetRect(backButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(54f, 54f), new Vector2(142f, 48f));

            preparationText = CreateText(
                "PreparationStatus",
                canvasObject.transform,
                "Preparing the valley",
                12,
                TextAnchor.UpperRight,
                new Color(0.82f, 0.88f, 0.84f, 0.78f));
            SetRect(preparationText.rectTransform, Vector2.one, Vector2.one, new Vector2(-230f, -44f), new Vector2(410f, 26f));
            reducedMotionToggle = CreateReducedMotionToggle(canvasObject.transform);
            BuildCinematicChrome(canvasObject.transform);

            RectTransform curtainRoot = CreateRect("OpeningCurtain", canvasObject.transform);
            Stretch(curtainRoot);
            Image curtainImage = curtainRoot.gameObject.AddComponent<Image>();
            curtainImage.color = Color.black;
            curtainImage.raycastTarget = true;
            CanvasGroup curtain = curtainRoot.gameObject.AddComponent<CanvasGroup>();

            CanvasGroup[] storyRevealGroups =
            {
                storyChapterText.gameObject.AddComponent<CanvasGroup>(),
                storyTitleText.gameObject.AddComponent<CanvasGroup>(),
                storyBodyText.gameObject.AddComponent<CanvasGroup>(),
                storyProgressText.gameObject.AddComponent<CanvasGroup>(),
                WrapButtonForReveal(nextButton),
                WrapButtonForReveal(skipStoryButton)
            };
            presentationController = canvasObject.AddComponent<StrategyFoundingJourneyPresentation>();
            presentationController.Configure(
                backgroundA,
                backgroundB,
                atmosphereRoot,
                curtain,
                storyRevealGroups,
                reducedMotion);
            AttachJourneyButtonFeedback();
            journeyAudioController = gameObject.AddComponent<StrategyFoundingJourneyAudio>();
            journeyAudioController.Configure(journeyCamera);
            StrategyUiInputModuleBootstrap.Ensure();
        }

        private void BuildStoryPanel(RectTransform panel)
        {
            storyRoot = CreateRect("StoryPanel", panel).gameObject;
            Stretch(storyRoot.GetComponent<RectTransform>());
            storyChapterText = CreateText("Chapter", storyRoot.transform, string.Empty, 14, TextAnchor.UpperLeft, JourneyGoldColor);
            storyChapterText.fontStyle = FontStyle.Bold;
            SetRect(storyChapterText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -82f), new Vector2(560f, 28f));
            storyTitleText = CreateText("Title", storyRoot.transform, string.Empty, 36, TextAnchor.UpperLeft, Color.white);
            storyTitleText.fontStyle = FontStyle.Bold;
            SetRect(storyTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -128f), new Vector2(590f, 100f));
            storyBodyText = CreateText("Body", storyRoot.transform, string.Empty, 18, TextAnchor.UpperLeft, JourneyMutedColor);
            storyBodyText.lineSpacing = 1.12f;
            SetRect(storyBodyText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -248f), new Vector2(570f, 220f));
            storyProgressText = CreateText("StoryProgress", storyRoot.transform, string.Empty, 13, TextAnchor.MiddleRight, JourneyMutedColor);
            SetRect(storyProgressText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(558f, 166f), new Vector2(80f, 26f));

            nextButton = CreateButton("StoryNextButton", storyRoot.transform, "Continue", 17, JourneyButtonColor, out nextButtonLabel);
            SetRect(nextButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(64f, 102f), new Vector2(310f, 58f));
            skipStoryButton = CreateButton("SkipStoryButton", storyRoot.transform, "Skip story", 14, new Color(0.08f, 0.11f, 0.11f, 0.96f), out _);
            SetRect(skipStoryButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(392f, 102f), new Vector2(180f, 58f));
        }

        private void BuildQuestionPanel(RectTransform panel)
        {
            questionRoot = CreateRect("QuestionPanel", panel).gameObject;
            Stretch(questionRoot.GetComponent<RectTransform>());
            questionChapterText = CreateText("ChoiceChapter", questionRoot.transform, string.Empty, 13, TextAnchor.UpperLeft, JourneyGoldColor);
            questionChapterText.fontStyle = FontStyle.Bold;
            SetRect(questionChapterText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -58f), new Vector2(560f, 26f));
            questionTitleText = CreateText("ChoiceTitle", questionRoot.transform, string.Empty, 29, TextAnchor.UpperLeft, Color.white);
            questionTitleText.fontStyle = FontStyle.Bold;
            SetRect(questionTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -98f), new Vector2(590f, 78f));
            questionContextText = CreateText("ChoiceContext", questionRoot.transform, string.Empty, 14, TextAnchor.UpperLeft, JourneyMutedColor);
            SetRect(questionContextText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -180f), new Vector2(580f, 54f));
            for (int i = 0; i < optionButtons.Length; i++)
            {
                CreateOptionCard(questionRoot.transform, i, -270f - i * 118f);
            }

            balancedDefaultsButton = CreateButton("BalancedDefaultsButton", questionRoot.transform, "Use balanced defaults", 14, new Color(0.08f, 0.11f, 0.11f, 0.96f), out _);
            SetRect(balancedDefaultsButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(392f, 42f), new Vector2(230f, 48f));
            questionRoot.SetActive(false);
        }

        private void BuildSummaryPanel(RectTransform panel)
        {
            summaryRoot = CreateRect("SummaryPanel", panel).gameObject;
            Stretch(summaryRoot.GetComponent<RectTransform>());
            Text chapter = CreateText("SummaryChapter", summaryRoot.transform, "OUR REFUGE", 14, TextAnchor.UpperLeft, JourneyGoldColor);
            chapter.fontStyle = FontStyle.Bold;
            SetRect(chapter.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -70f), new Vector2(560f, 26f));
            Text title = CreateText("SummaryTitle", summaryRoot.transform, "Here we begin again.", 33, TextAnchor.UpperLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -112f), new Vector2(580f, 60f));
            summaryBodyText = CreateText("SummaryBody", summaryRoot.transform, string.Empty, 15, TextAnchor.UpperLeft, Color.white);
            summaryBodyText.fontStyle = FontStyle.Bold;
            summaryBodyText.lineSpacing = 0.95f;
            SetRect(summaryBodyText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -190f), new Vector2(560f, 370f));
            summaryNoteText = CreateText("SummaryNote", summaryRoot.transform, string.Empty, 13, TextAnchor.UpperLeft, JourneyMutedColor);
            SetRect(summaryNoteText.rectTransform, Vector2.zero, Vector2.zero, new Vector2(64f, 212f), new Vector2(550f, 52f));
            beginButton = CreateButton("BeginButton", summaryRoot.transform, "Begin the settlement", 17, new Color(0.20f, 0.34f, 0.25f, 1f), out _);
            SetRect(beginButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(64f, 102f), new Vector2(310f, 58f));
            changeAnswersButton = CreateButton("ChangeAnswersButton", summaryRoot.transform, "Change answers", 14, JourneyButtonColor, out _);
            SetRect(changeAnswersButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(392f, 102f), new Vector2(190f, 58f));
            summaryRoot.SetActive(false);
        }

        private void BuildLoadingPanel(RectTransform panel)
        {
            loadingRoot = CreateRect("LoadingPanel", panel).gameObject;
            Stretch(loadingRoot.GetComponent<RectTransform>());
            loadingTitleText = CreateText("LoadingTitle", loadingRoot.transform, "Preparing the valley", 31, TextAnchor.UpperLeft, Color.white);
            loadingTitleText.fontStyle = FontStyle.Bold;
            SetRect(loadingTitleText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(64f, 92f), new Vector2(560f, 54f));
            loadingDetailText = CreateText("LoadingDetail", loadingRoot.transform, string.Empty, 15, TextAnchor.UpperLeft, JourneyMutedColor);
            SetRect(loadingDetailText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(64f, 36f), new Vector2(520f, 38f));
            RectTransform track = CreateRect("LoadingTrack", loadingRoot.transform);
            SetRect(track, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(64f, -28f), new Vector2(500f, 10f));
            track.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.25f, 0.23f, 1f);
            RectTransform fill = CreateRect("LoadingProgressFill", track);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            loadingProgressFill = fill.gameObject.AddComponent<Image>();
            loadingProgressFill.color = JourneyGoldColor;
            loadingPercentText = CreateText("LoadingPercent", loadingRoot.transform, "0%", 14, TextAnchor.MiddleRight, JourneyGoldColor);
            SetRect(loadingPercentText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(508f, -58f), new Vector2(56f, 28f));
            loadingRoot.SetActive(false);
        }

        private void CreateOptionCard(Transform parent, int index, float topY)
        {
            RectTransform root = CreateRect("ChoiceOption" + index, parent);
            SetRect(root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, topY), new Vector2(560f, 98f));
            Image image = root.gameObject.AddComponent<Image>();
            image.color = JourneyButtonColor;
            image.sprite = StrategyUiThemeProvider.GetButtonSprite();
            image.type = Image.Type.Sliced;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = BuildButtonColors(JourneyButtonColor);
            RectTransform accent = CreateRect("SelectedAccent", root);
            accent.anchorMin = Vector2.zero;
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.anchoredPosition = Vector2.zero;
            accent.sizeDelta = new Vector2(6f, 0f);
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = new Color(JourneyGoldColor.r, JourneyGoldColor.g, JourneyGoldColor.b, 0f);
            accentImage.raycastTarget = false;
            Text label = CreateText("Label", root, string.Empty, 17, TextAnchor.UpperLeft, Color.white);
            label.fontStyle = FontStyle.Bold;
            label.raycastTarget = false;
            Stretch(label.rectTransform, new Vector2(22f, 13f), new Vector2(-18f, -48f));
            Text description = CreateText("Description", root, string.Empty, 12, TextAnchor.UpperLeft, JourneyMutedColor);
            description.raycastTarget = false;
            Stretch(description.rectTransform, new Vector2(22f, 48f), new Vector2(-18f, -10f));
            optionButtons[index] = button;
            optionImages[index] = image;
            optionAccents[index] = accentImage;
            optionLabels[index] = label;
            optionDescriptions[index] = description;
        }

        private static Image CreateBackground(string name, Transform parent)
        {
            RectTransform shotRoot = CreateRect(name, parent);
            Stretch(shotRoot);
            RectTransform visual = CreateRect("Artwork", shotRoot);
            Stretch(visual);
            Image image = visual.gameObject.AddComponent<Image>();
            image.preserveAspect = false;
            image.raycastTarget = false;
            AspectRatioFitter fitter = visual.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            return image;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string label,
            int fontSize,
            Color color,
            out Text labelText)
        {
            RectTransform root = CreateRect(name, parent);
            Image image = root.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = StrategyUiThemeProvider.GetButtonSprite();
            image.type = Image.Type.Sliced;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = BuildButtonColors(color);
            labelText = CreateText("Label", root, label, fontSize, TextAnchor.MiddleCenter, Color.white);
            labelText.fontStyle = FontStyle.Bold;
            labelText.raycastTarget = false;
            Stretch(labelText.rectTransform, new Vector2(12f, 0f), new Vector2(-12f, 0f));
            return button;
        }

        private static ColorBlock BuildButtonColors(Color normal)
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = normal;
            colors.highlightedColor = JourneyButtonHoverColor;
            colors.selectedColor = JourneyButtonHoverColor;
            colors.pressedColor = JourneyGoldColor * 0.82f;
            colors.disabledColor = new Color(normal.r, normal.g, normal.b, 0.45f);
            colors.colorMultiplier = 1f;
            return colors;
        }

        private Toggle CreateReducedMotionToggle(Transform parent)
        {
            RectTransform root = CreateRect("ReducedMotion", parent);
            SetRect(root, Vector2.one, Vector2.one, new Vector2(-122f, -80f), new Vector2(220f, 30f));
            Toggle toggle = root.gameObject.AddComponent<Toggle>();
            RectTransform box = CreateRect("Box", root);
            box.anchorMin = new Vector2(0f, 0.5f);
            box.anchorMax = new Vector2(0f, 0.5f);
            box.anchoredPosition = new Vector2(13f, 0f);
            box.sizeDelta = new Vector2(24f, 24f);
            Image boxImage = box.gameObject.AddComponent<Image>();
            boxImage.color = JourneyButtonColor;
            RectTransform mark = CreateRect("Checkmark", box);
            Stretch(mark, new Vector2(5f, 5f), new Vector2(-5f, -5f));
            Image markImage = mark.gameObject.AddComponent<Image>();
            markImage.color = JourneyGoldColor;
            Text label = CreateText("Label", root, "Reduce motion", 12, TextAnchor.MiddleLeft, JourneyMutedColor);
            Stretch(label.rectTransform, new Vector2(36f, 0f), Vector2.zero);
            toggle.targetGraphic = boxImage;
            toggle.graphic = markImage;
            return toggle;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int size,
            TextAnchor anchor,
            Color color)
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

        private void SetJourneyBackground(StrategyFoundingStoryPanel panel, bool immediate)
        {
            presentationController.ShowBackground(panel, immediate);
            journeyAudioController.SetPanel(panel);
        }

        private void RefreshPreparationStatus()
        {
            float progress = Mathf.Clamp01(PreparationProgress);
            preparationText.text = IsMapReady
                ? "VALLEY READY"
                : PreparationStage.ToUpperInvariant() + "  " + Mathf.RoundToInt(progress * 100f) + "%";
            loadingPercentText.text = Mathf.RoundToInt(progress * 100f) + "%";
            loadingProgressFill.rectTransform.anchorMax = new Vector2(progress, 1f);
            if (page == JourneyPage.Launching && !IsMapReady)
            {
                loadingTitleText.text = "Preparing the valley";
                loadingDetailText.text = PreparationStage;
            }
        }

        private void SetOptionSelected(int index, bool selected)
        {
            optionImages[index].color = selected ? JourneySelectedColor : JourneyButtonColor;
            optionAccents[index].color = new Color(
                JourneyGoldColor.r,
                JourneyGoldColor.g,
                JourneyGoldColor.b,
                selected ? 1f : 0f);
            optionLabels[index].color = selected ? JourneyGoldColor : Color.white;
        }

        private void SetControlsInteractable(bool interactable)
        {
            nextButton.interactable = interactable;
            skipStoryButton.interactable = interactable;
            balancedDefaultsButton.interactable = interactable;
            changeAnswersButton.interactable = interactable;
            beginButton.interactable = interactable;
            for (int i = 0; i < optionButtons.Length; i++)
            {
                optionButtons[i].interactable = interactable;
            }
        }
    }
}
