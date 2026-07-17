using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyCityItemRewardRevealController
    {
        private static readonly Color DeepGreen = new(0.035f, 0.075f, 0.068f, 1f);
        private static readonly Color CardGreen = new(0.070f, 0.125f, 0.106f, 1f);
        private static readonly Color Gold = new(0.92f, 0.69f, 0.28f, 1f);
        private static readonly Color PaleGold = new(1f, 0.88f, 0.58f, 1f);
        private static readonly Color MutedText = new(0.75f, 0.82f, 0.76f, 1f);
        private static Sprite radialGlowSprite;

        private GameObject rewardCanvasRoot;
        private Canvas rewardCanvas;
        private RectTransform rewardCanvasRect;
        private Image backdropImage;
        private RectTransform raysRoot;
        private readonly RectTransform[] rays = new RectTransform[16];
        private readonly RectTransform[] sparks = new RectTransform[18];
        private readonly Vector2[] sparkHomes = new Vector2[18];
        private RectTransform glowRoot;
        private Image glowImage;
        private RectTransform stageRoot;
        private RectTransform cardRoot;
        private CanvasGroup cardGroup;
        private Image artworkImage;
        private Text rewardTypeText;
        private Text rewardTitleText;
        private Text rewardEffectText;
        private Text rewardFlavorText;
        private RectTransform confirmRoot;
        private CanvasGroup confirmGroup;
        private Button confirmButton;
        private StrategyUiButtonFeedback confirmFeedback;
        private bool viewConfigured;

        private void EnsureView()
        {
            if (viewConfigured)
            {
                return;
            }

            viewConfigured = true;
            StrategyUiInputModuleBootstrap.Ensure();
            rewardCanvasRoot = new GameObject(
                "CityItemRewardRevealCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            rewardCanvasRoot.transform.SetParent(transform, false);
            rewardCanvasRect = rewardCanvasRoot.GetComponent<RectTransform>();
            rewardCanvas = rewardCanvasRoot.GetComponent<Canvas>();
            rewardCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rewardCanvas.sortingOrder = 320;

            CanvasScaler scaler = rewardCanvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            CreateBackdrop();
            CreateAtmosphere();
            CreateRewardCard();
            CreateConfirmationButton();
            rewardCanvasRoot.SetActive(false);
        }

        private void CreateBackdrop()
        {
            RectTransform backdrop = CreateRect("CinematicBackdrop", rewardCanvasRoot.transform);
            Stretch(backdrop);
            backdropImage = backdrop.gameObject.AddComponent<Image>();
            backdropImage.color = new Color(0.004f, 0.010f, 0.010f, 0f);
            backdropImage.raycastTarget = true;

            RectTransform upperLine = CreateRect("UpperGoldLine", backdrop);
            SetTopStretch(upperLine, 180f, 42f, 180f, 2f);
            Image upperLineImage = upperLine.gameObject.AddComponent<Image>();
            upperLineImage.color = new Color(Gold.r, Gold.g, Gold.b, 0.42f);
            upperLineImage.raycastTarget = false;

            RectTransform lowerLine = CreateRect("LowerGoldLine", backdrop);
            SetBottomStretch(lowerLine, 180f, 42f, 180f, 2f);
            Image lowerLineImage = lowerLine.gameObject.AddComponent<Image>();
            lowerLineImage.color = new Color(Gold.r, Gold.g, Gold.b, 0.42f);
            lowerLineImage.raycastTarget = false;

            Text heading = CreateText(
                "RewardHeading",
                backdrop,
                "THE CITY REMEMBERS",
                14,
                TextAnchor.MiddleCenter,
                PaleGold);
            heading.fontStyle = FontStyle.Bold;
            SetCenter(heading.rectTransform, 0f, 374f, 620f, 28f);
        }

        private void CreateAtmosphere()
        {
            glowRoot = CreateRect("RewardGlow", rewardCanvasRoot.transform);
            SetCenter(glowRoot, 0f, 8f, 760f, 760f);
            glowImage = glowRoot.gameObject.AddComponent<Image>();
            glowImage.sprite = GetRadialGlowSprite();
            glowImage.color = new Color(0.95f, 0.60f, 0.16f, 0f);
            glowImage.raycastTarget = false;

            raysRoot = CreateRect("RewardRays", rewardCanvasRoot.transform);
            SetCenter(raysRoot, 0f, 12f, 720f, 720f);
            for (int index = 0; index < rays.Length; index++)
            {
                RectTransform ray = CreateRect("Ray" + index, raysRoot);
                ray.anchorMin = new Vector2(0.5f, 0.5f);
                ray.anchorMax = new Vector2(0.5f, 0.5f);
                ray.pivot = new Vector2(0.5f, 0f);
                ray.anchoredPosition = Vector2.zero;
                ray.sizeDelta = new Vector2(index % 2 == 0 ? 5f : 2f, 340f);
                ray.localRotation = Quaternion.Euler(0f, 0f, index * (360f / rays.Length));
                Image image = ray.gameObject.AddComponent<Image>();
                image.color = new Color(PaleGold.r, PaleGold.g, PaleGold.b, index % 2 == 0 ? 0.12f : 0.06f);
                image.raycastTarget = false;
                rays[index] = ray;
            }

            for (int index = 0; index < sparks.Length; index++)
            {
                float angle = index * 2.39996f;
                float radius = 230f + ((index * 37) % 170);
                Vector2 home = new(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                RectTransform spark = CreateRect("RewardSpark" + index, rewardCanvasRoot.transform);
                spark.anchorMin = new Vector2(0.5f, 0.5f);
                spark.anchorMax = new Vector2(0.5f, 0.5f);
                spark.pivot = new Vector2(0.5f, 0.5f);
                spark.anchoredPosition = home;
                float size = 3f + (index % 3) * 2f;
                spark.sizeDelta = new Vector2(size, size);
                Image image = spark.gameObject.AddComponent<Image>();
                image.color = new Color(PaleGold.r, PaleGold.g, PaleGold.b, 0.72f);
                image.raycastTarget = false;
                sparks[index] = spark;
                sparkHomes[index] = home;
            }
        }

        private void CreateRewardCard()
        {
            stageRoot = CreateRect("RewardStage", rewardCanvasRoot.transform);
            SetCenter(stageRoot, 0f, 0f, 500f, 760f);

            cardRoot = CreateRect("RewardCard", stageRoot);
            SetCenter(cardRoot, 0f, 25f, 420f, 570f);
            cardGroup = cardRoot.gameObject.AddComponent<CanvasGroup>();

            Image outer = cardRoot.gameObject.AddComponent<Image>();
            outer.sprite = StrategyUiThemeProvider.GetPanelSprite();
            outer.type = Image.Type.Sliced;
            outer.color = new Color(0.12f, 0.075f, 0.025f, 1f);
            Outline outerOutline = cardRoot.gameObject.AddComponent<Outline>();
            outerOutline.effectColor = new Color(0.98f, 0.72f, 0.25f, 0.98f);
            outerOutline.effectDistance = new Vector2(4f, -4f);

            RectTransform inner = CreateRect("InnerCardFrame", cardRoot);
            Stretch(inner, 10f, 10f, 10f, 10f);
            Image innerImage = inner.gameObject.AddComponent<Image>();
            innerImage.sprite = StrategyUiThemeProvider.GetPanelSprite();
            innerImage.type = Image.Type.Sliced;
            innerImage.color = CardGreen;
            Outline innerOutline = inner.gameObject.AddComponent<Outline>();
            innerOutline.effectColor = new Color(0.57f, 0.38f, 0.13f, 0.92f);
            innerOutline.effectDistance = new Vector2(2f, -2f);

            Text collection = CreateText(
                "Collection",
                inner,
                "NEW CITY ITEM",
                12,
                TextAnchor.MiddleCenter,
                Gold);
            collection.fontStyle = FontStyle.Bold;
            SetTopStretch(collection.rectTransform, 24f, 15f, 24f, 20f);

            rewardTitleText = CreateText(
                "ItemTitle",
                inner,
                "UNKNOWN FIND",
                31,
                TextAnchor.MiddleCenter,
                Color.white);
            rewardTitleText.fontStyle = FontStyle.Bold;
            rewardTitleText.resizeTextForBestFit = true;
            rewardTitleText.resizeTextMinSize = 20;
            rewardTitleText.resizeTextMaxSize = 31;
            SetTopStretch(rewardTitleText.rectTransform, 24f, 37f, 24f, 52f);

            RectTransform uniqueBacking = CreateRect("UniqueBacking", inner);
            SetTopCenter(uniqueBacking, 0f, 94f, 164f, 22f);
            Image uniqueBackingImage = uniqueBacking.gameObject.AddComponent<Image>();
            uniqueBackingImage.color = Gold;
            uniqueBackingImage.raycastTarget = false;
            rewardTypeText = CreateText(
                "UniqueLabel",
                uniqueBacking,
                "UNIQUE CITY BOON",
                11,
                TextAnchor.MiddleCenter,
                DeepGreen);
            rewardTypeText.fontStyle = FontStyle.Bold;
            Stretch(rewardTypeText.rectTransform);

            RectTransform artFrame = CreateRect("ArtworkFrame", inner);
            SetTopCenter(artFrame, 0f, 127f, 344f, 250f);
            Image artFrameImage = artFrame.gameObject.AddComponent<Image>();
            artFrameImage.color = new Color(0.02f, 0.035f, 0.034f, 1f);
            Outline artOutline = artFrame.gameObject.AddComponent<Outline>();
            artOutline.effectColor = new Color(0.82f, 0.58f, 0.22f, 0.95f);
            artOutline.effectDistance = new Vector2(3f, -3f);

            RectTransform artworkRect = CreateRect("Artwork", artFrame);
            Stretch(artworkRect, 6f, 6f, 6f, 6f);
            artworkImage = artworkRect.gameObject.AddComponent<Image>();
            artworkImage.preserveAspect = true;
            artworkImage.raycastTarget = false;

            RectTransform divider = CreateRect("DescriptionDivider", inner);
            SetTopStretch(divider, 26f, 394f, 26f, 2f);
            Image dividerImage = divider.gameObject.AddComponent<Image>();
            dividerImage.color = new Color(Gold.r, Gold.g, Gold.b, 0.72f);
            dividerImage.raycastTarget = false;

            rewardEffectText = CreateText(
                "ItemEffect",
                inner,
                string.Empty,
                17,
                TextAnchor.UpperCenter,
                new Color(0.94f, 0.93f, 0.80f));
            rewardEffectText.fontStyle = FontStyle.Bold;
            rewardEffectText.lineSpacing = 1.05f;
            SetTopStretch(rewardEffectText.rectTransform, 30f, 410f, 30f, 66f);

            rewardFlavorText = CreateText(
                "ItemStory",
                inner,
                string.Empty,
                13,
                TextAnchor.UpperCenter,
                MutedText);
            rewardFlavorText.fontStyle = FontStyle.Italic;
            rewardFlavorText.lineSpacing = 1.05f;
            SetTopStretch(rewardFlavorText.rectTransform, 32f, 487f, 32f, 46f);

            Text footer = CreateText("Footer", inner, "CITY CHEST  /  PERMANENT", 10, TextAnchor.MiddleCenter, Gold);
            footer.fontStyle = FontStyle.Bold;
            SetBottomStretch(footer.rectTransform, 30f, 11f, 30f, 18f);
        }

        private void CreateConfirmationButton()
        {
            confirmRoot = CreateRect("ConfirmReward", stageRoot);
            SetCenter(confirmRoot, 0f, -306f, 344f, 58f);
            confirmGroup = confirmRoot.gameObject.AddComponent<CanvasGroup>();
            Image image = confirmRoot.gameObject.AddComponent<Image>();
            image.sprite = StrategyUiThemeProvider.GetButtonSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.28f, 0.20f, 0.075f, 1f);
            confirmButton = confirmRoot.gameObject.AddComponent<Button>();
            confirmButton.targetGraphic = image;
            ConfigureButtonColors(confirmButton, image.color);
            confirmButton.onClick.AddListener(HandleConfirmClicked);

            Text label = CreateText(
                "Label",
                confirmRoot,
                "SEND TO CITY CHEST",
                16,
                TextAnchor.MiddleCenter,
                Color.white);
            label.fontStyle = FontStyle.Bold;
            Stretch(label.rectTransform);
            confirmFeedback = StrategyUiButtonFeedback.Attach(
                confirmButton,
                StrategyUiButtonFeedbackProfile.Cinematic);
        }

        private void PopulateRewardView(StrategyCityItemDefinition definition, Sprite artwork)
        {
            rewardTitleText.text = definition.Title.ToUpperInvariant();
            rewardTypeText.text = definition.MaxStack == 1
                ? "UNIQUE CITY BOON"
                : "CITY ITEM";
            rewardEffectText.text = string.IsNullOrWhiteSpace(definition.EffectText)
                ? definition.Description
                : definition.EffectText;
            rewardFlavorText.text = definition.Description;
            rewardFlavorText.gameObject.SetActive(
                !string.IsNullOrWhiteSpace(definition.Description)
                && !string.Equals(
                    definition.Description,
                    rewardEffectText.text,
                    System.StringComparison.Ordinal));
            artworkImage.sprite = artwork;
            artworkImage.color = artwork != null
                ? Color.white
                : new Color(0.19f, 0.27f, 0.23f, 1f);
        }

        private void ApplyReducedMotionToView()
        {
            confirmFeedback?.SetReducedMotion(reducedMotion);
            for (int index = 0; index < sparks.Length; index++)
            {
                if (sparks[index] != null)
                {
                    sparks[index].gameObject.SetActive(!reducedMotion);
                }
            }

            if (raysRoot != null)
            {
                raysRoot.gameObject.SetActive(!reducedMotion);
            }
        }

        private void HandleConfirmClicked()
        {
            TryConfirm();
        }

        private void HideView()
        {
            if (EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject != null
                && rewardCanvasRoot != null
                && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(
                    rewardCanvasRoot.transform))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            rewardCanvasRoot?.SetActive(false);
        }

        private void DisposeView()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            }

            if (rewardCanvasRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(rewardCanvasRoot);
                }
                else
                {
                    DestroyImmediate(rewardCanvasRoot);
                }
            }

            rewardCanvasRoot = null;
            rewardCanvas = null;
            rewardCanvasRect = null;
            backdropImage = null;
            raysRoot = null;
            glowRoot = null;
            glowImage = null;
            stageRoot = null;
            cardRoot = null;
            cardGroup = null;
            artworkImage = null;
            rewardTypeText = null;
            rewardTitleText = null;
            rewardEffectText = null;
            rewardFlavorText = null;
            confirmRoot = null;
            confirmGroup = null;
            confirmButton = null;
            confirmFeedback = null;
            viewConfigured = false;
        }

    }
}
