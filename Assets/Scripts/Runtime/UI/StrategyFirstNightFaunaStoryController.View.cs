using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFirstNightFaunaStoryController
    {
        private const string ReducedMotionKey = "ProjectUnknown.Founding.ReducedMotion";
        private static readonly Color PanelColor = new(0.018f, 0.030f, 0.030f, 0.18f);
        private static readonly Color ButtonColor = new(0.105f, 0.145f, 0.135f, 0.98f);
        private static readonly Color ButtonHoverColor = new(0.16f, 0.235f, 0.205f, 1f);
        private static readonly Color GoldColor = new(0.90f, 0.70f, 0.35f, 1f);
        private static readonly Color MutedColor = new(0.72f, 0.80f, 0.76f, 1f);

        private GameObject storyCanvasRoot;
        private StrategyFoundingJourneyPresentation presentationController;
        private Text storyChapterText;
        private Text storyTitleText;
        private Text storyBodyText;
        private Text storyProgressText;
        private Text continueButtonLabel;
        private Button continueButton;
        private Button skipButton;
        private StrategyUiButtonFeedback continueButtonFeedback;
        private bool viewConfigured;

        private void EnsureView()
        {
            if (viewConfigured)
            {
                return;
            }

            viewConfigured = true;
            bool reducedMotion = PlayerPrefs.GetInt(ReducedMotionKey, 0) != 0;
            GameObject canvasObject = new(
                "FirstNightFaunaStoryCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            storyCanvasRoot = canvasObject;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform backdrop = CreateRect("Backdrop", canvasObject.transform);
            Stretch(backdrop);
            Image backdropImage = backdrop.gameObject.AddComponent<Image>();
            backdropImage.color = new Color(0.01f, 0.015f, 0.018f, 1f);
            backdropImage.raycastTarget = true;

            RectTransform backgroundStage = CreateRect("BackgroundStage", canvasObject.transform);
            Stretch(backgroundStage, new Vector2(-36f, -24f), new Vector2(36f, 24f));
            Image backgroundA = CreateBackground("BackgroundA", backgroundStage);
            Image backgroundB = CreateBackground("BackgroundB", backgroundStage);
            backgroundB.color = new Color(1f, 1f, 1f, 0f);

            RectTransform atmosphereRoot = CreateRect("Atmosphere", backgroundStage);
            Stretch(atmosphereRoot);
            StrategyFoundingJourneyAtmosphere atmosphere =
                atmosphereRoot.gameObject.AddComponent<StrategyFoundingJourneyAtmosphere>();
            atmosphere.Configure(atmosphereRoot, reducedMotion);

            RectTransform shade = CreateRect("CinematicShade", canvasObject.transform);
            Stretch(shade);
            Image shadeImage = shade.gameObject.AddComponent<Image>();
            shadeImage.color = new Color(0.005f, 0.012f, 0.014f, 0.16f);
            shadeImage.raycastTarget = false;

            RectTransform panel = CreateRect("NarrativePanel", canvasObject.transform);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = new Vector2(0.45f, 1f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = PanelColor;
            panelImage.raycastTarget = false;
            BuildNarrativeGradient(panel);
            BuildStoryCopy(panel);
            BuildCinematicChrome(canvasObject.transform);

            RectTransform curtainRoot = CreateRect("OpeningCurtain", canvasObject.transform);
            Stretch(curtainRoot);
            Image curtainImage = curtainRoot.gameObject.AddComponent<Image>();
            curtainImage.color = Color.black;
            curtainImage.raycastTarget = true;
            CanvasGroup curtain = curtainRoot.gameObject.AddComponent<CanvasGroup>();

            CanvasGroup[] revealGroups =
            {
                storyChapterText.gameObject.AddComponent<CanvasGroup>(),
                storyTitleText.gameObject.AddComponent<CanvasGroup>(),
                storyBodyText.gameObject.AddComponent<CanvasGroup>(),
                storyProgressText.gameObject.AddComponent<CanvasGroup>(),
                WrapButtonForReveal(continueButton),
                WrapButtonForReveal(skipButton)
            };

            presentationController = canvasObject.AddComponent<StrategyFoundingJourneyPresentation>();
            presentationController.Configure(
                backgroundA,
                backgroundB,
                atmosphereRoot,
                curtain,
                revealGroups,
                reducedMotion);
            continueButtonFeedback = StrategyUiButtonFeedback.Attach(
                continueButton,
                StrategyUiButtonFeedbackProfile.Cinematic);
            StrategyUiButtonFeedback.Attach(
                skipButton,
                StrategyUiButtonFeedbackProfile.Cinematic);
            continueButton.onClick.AddListener(AdvanceStory);
            skipButton.onClick.AddListener(SkipStory);
            StrategyUiInputModuleBootstrap.Ensure();
            canvasObject.SetActive(false);
        }

        private void BuildStoryCopy(RectTransform panel)
        {
            Text collection = CreateText(
                "Collection",
                panel,
                "A SETTLEMENT CHRONICLE",
                12,
                TextAnchor.UpperLeft,
                MutedColor);
            collection.fontStyle = FontStyle.Bold;
            SetRect(collection.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -52f), new Vector2(560f, 24f));

            storyChapterText = CreateText("Chapter", panel, string.Empty, 14, TextAnchor.UpperLeft, GoldColor);
            storyChapterText.fontStyle = FontStyle.Bold;
            SetRect(storyChapterText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -88f), new Vector2(560f, 28f));
            storyTitleText = CreateText("Title", panel, string.Empty, 36, TextAnchor.UpperLeft, Color.white);
            storyTitleText.fontStyle = FontStyle.Bold;
            SetRect(storyTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -134f), new Vector2(590f, 108f));
            storyBodyText = CreateText("Body", panel, string.Empty, 18, TextAnchor.UpperLeft, MutedColor);
            storyBodyText.lineSpacing = 1.12f;
            SetRect(storyBodyText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -264f), new Vector2(570f, 226f));
            storyProgressText = CreateText("Progress", panel, string.Empty, 13, TextAnchor.MiddleRight, MutedColor);
            SetRect(storyProgressText.rectTransform, Vector2.zero, Vector2.zero, new Vector2(558f, 166f), new Vector2(80f, 26f));

            continueButton = CreateButton("ContinueButton", panel, "Continue", 17, ButtonColor, out continueButtonLabel);
            SetRect(continueButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(64f, 102f), new Vector2(310f, 58f));
            skipButton = CreateButton("SkipButton", panel, "Skip story", 14, new Color(0.08f, 0.11f, 0.11f, 0.96f), out _);
            SetRect(skipButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(392f, 102f), new Vector2(180f, 58f));
        }

        private void HideView()
        {
            if (storyCanvasRoot == null)
            {
                return;
            }

            if (EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject != null
                && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(storyCanvasRoot.transform))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            storyCanvasRoot.SetActive(false);
        }

        private void DisposeView()
        {
            if (storyCanvasRoot != null)
            {
                storyCanvasRoot.SetActive(false);
                Destroy(storyCanvasRoot);
            }

            storyCanvasRoot = null;
            presentationController = null;
            storyChapterText = null;
            storyTitleText = null;
            storyBodyText = null;
            storyProgressText = null;
            continueButtonLabel = null;
            continueButton = null;
            skipButton = null;
            continueButtonFeedback = null;
            viewConfigured = false;
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
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = color;
            colors.highlightedColor = ButtonHoverColor;
            colors.selectedColor = ButtonHoverColor;
            colors.pressedColor = GoldColor * 0.82f;
            colors.disabledColor = new Color(color.r, color.g, color.b, 0.45f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            labelText = CreateText("Label", root, label, fontSize, TextAnchor.MiddleCenter, Color.white);
            labelText.fontStyle = FontStyle.Bold;
            labelText.raycastTarget = false;
            Stretch(labelText.rectTransform, new Vector2(12f, 0f), new Vector2(-12f, 0f));
            return button;
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
            text.raycastTarget = false;
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

        private static CanvasGroup WrapButtonForReveal(Button button)
        {
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            Transform originalParent = buttonRect.parent;
            int siblingIndex = buttonRect.GetSiblingIndex();
            RectTransform wrapper = CreateRect(buttonRect.name + "Reveal", originalParent);
            wrapper.SetSiblingIndex(siblingIndex);
            wrapper.anchorMin = buttonRect.anchorMin;
            wrapper.anchorMax = buttonRect.anchorMax;
            wrapper.pivot = buttonRect.pivot;
            wrapper.anchoredPosition = buttonRect.anchoredPosition;
            wrapper.sizeDelta = buttonRect.sizeDelta;
            buttonRect.SetParent(wrapper, false);
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.one;
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            return wrapper.gameObject.AddComponent<CanvasGroup>();
        }

        private static void BuildNarrativeGradient(RectTransform panel)
        {
            float[] widths = { 1f, 0.84f, 0.68f, 0.52f, 0.36f, 0.20f };
            float[] alphas = { 0.035f, 0.048f, 0.062f, 0.080f, 0.102f, 0.128f };
            for (int i = 0; i < widths.Length; i++)
            {
                CreateOverlayBand(
                    "NarrativeShade" + i,
                    panel,
                    Vector2.zero,
                    new Vector2(widths[i], 1f),
                    new Color(0.006f, 0.014f, 0.016f, alphas[i]));
            }
        }

        private static void BuildCinematicChrome(Transform parent)
        {
            CreateOverlayBand("TopVignette", parent, new Vector2(0f, 0.91f), Vector2.one, new Color(0f, 0f, 0f, 0.07f));
            CreateOverlayBand("BottomVignette", parent, Vector2.zero, new Vector2(1f, 0.09f), new Color(0f, 0f, 0f, 0.09f));
            CreateOverlayBand("LeftVignette", parent, Vector2.zero, new Vector2(0.05f, 1f), new Color(0f, 0f, 0f, 0.22f));
            CreateOverlayBand("RightVignette", parent, new Vector2(0.95f, 0f), Vector2.one, new Color(0f, 0f, 0f, 0.22f));

            RectTransform topBar = CreateRect("TopLetterbox", parent);
            SetRect(topBar, new Vector2(0f, 1f), Vector2.one, Vector2.zero, new Vector2(0f, 24f));
            Image topImage = topBar.gameObject.AddComponent<Image>();
            topImage.color = new Color(0.002f, 0.004f, 0.005f, 0.82f);
            topImage.raycastTarget = false;
            RectTransform bottomBar = CreateRect("BottomLetterbox", parent);
            SetRect(bottomBar, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 24f));
            Image bottomImage = bottomBar.gameObject.AddComponent<Image>();
            bottomImage.color = new Color(0.002f, 0.004f, 0.005f, 0.82f);
            bottomImage.raycastTarget = false;
        }

        private static void CreateOverlayBand(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }
    }
}
