using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentItemRewardRevealController
    {
        private GameObject root;
        private CanvasGroup cardGroup;
        private RectTransform cardRoot;
        private Image artworkImage;
        private Text titleText;
        private Text ownerText;
        private Text descriptionText;
        private Button confirmButton;
        private bool viewConfigured;

        private void EnsureView()
        {
            if (viewConfigured)
            {
                return;
            }

            viewConfigured = true;
            StrategyUiInputModuleBootstrap.Ensure();
            root = new GameObject(
                "ResidentItemRewardRevealCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 325;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform shade = CreateRect("Shade", root.transform);
            Stretch(shade);
            Image shadeImage = shade.gameObject.AddComponent<Image>();
            shadeImage.color = new Color(0.005f, 0.009f, 0.008f, 0.86f);

            RectTransform halo = CreateRect("Halo", shade);
            SetCenter(halo, 0f, 10f, 620f, 620f);
            Image haloImage = halo.gameObject.AddComponent<Image>();
            haloImage.color = new Color(0.62f, 0.42f, 0.15f, 0.15f);
            haloImage.raycastTarget = false;

            cardRoot = CreateRect("PersonalItemCard", shade);
            SetCenter(cardRoot, 0f, 12f, 470f, 650f);
            cardGroup = cardRoot.gameObject.AddComponent<CanvasGroup>();
            Image card = cardRoot.gameObject.AddComponent<Image>();
            card.sprite = StrategyUiThemeProvider.GetPanelSprite();
            card.type = Image.Type.Sliced;
            card.color = new Color(0.055f, 0.105f, 0.088f, 1f);
            Outline outline = cardRoot.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.91f, 0.66f, 0.27f, 0.96f);
            outline.effectDistance = new Vector2(4f, -4f);

            Text category = CreateText(
                "Category",
                cardRoot,
                "ЛИЧНАЯ НАХОДКА СКАУТА",
                13,
                TextAnchor.MiddleCenter,
                new Color(0.95f, 0.72f, 0.32f));
            category.fontStyle = FontStyle.Bold;
            SetTopStretch(category.rectTransform, 28f, 20f, 28f, 24f);

            titleText = CreateText(
                "Title",
                cardRoot,
                string.Empty,
                31,
                TextAnchor.MiddleCenter,
                Color.white);
            titleText.fontStyle = FontStyle.Bold;
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 20;
            titleText.resizeTextMaxSize = 31;
            SetTopStretch(titleText.rectTransform, 30f, 50f, 30f, 56f);

            ownerText = CreateText(
                "Owner",
                cardRoot,
                string.Empty,
                14,
                TextAnchor.MiddleCenter,
                new Color(0.76f, 0.84f, 0.77f));
            SetTopStretch(ownerText.rectTransform, 30f, 108f, 30f, 24f);

            RectTransform artFrame = CreateRect("ArtworkFrame", cardRoot);
            SetTopCenter(artFrame, 0f, 144f, 330f, 300f);
            Image frameImage = artFrame.gameObject.AddComponent<Image>();
            frameImage.color = new Color(0.018f, 0.031f, 0.028f, 1f);
            Outline artOutline = artFrame.gameObject.AddComponent<Outline>();
            artOutline.effectColor = new Color(0.60f, 0.40f, 0.16f, 0.92f);
            artOutline.effectDistance = new Vector2(3f, -3f);
            RectTransform art = CreateRect("Artwork", artFrame);
            Stretch(art, 14f, 14f, 14f, 14f);
            artworkImage = art.gameObject.AddComponent<Image>();
            artworkImage.preserveAspect = true;
            artworkImage.raycastTarget = false;

            descriptionText = CreateText(
                "Description",
                cardRoot,
                string.Empty,
                17,
                TextAnchor.UpperCenter,
                new Color(0.91f, 0.91f, 0.80f));
            descriptionText.fontStyle = FontStyle.Italic;
            descriptionText.lineSpacing = 1.1f;
            SetTopStretch(descriptionText.rectTransform, 38f, 466f, 38f, 82f);

            RectTransform buttonRoot = CreateRect("Confirm", cardRoot);
            SetBottomCenter(buttonRoot, 0f, 28f, 220f, 48f);
            Image buttonImage = buttonRoot.gameObject.AddComponent<Image>();
            Color buttonColor = new(0.28f, 0.45f, 0.31f, 1f);
            buttonImage.color = buttonColor;
            confirmButton = buttonRoot.gameObject.AddComponent<Button>();
            confirmButton.targetGraphic = buttonImage;
            ConfigureButtonColors(confirmButton, buttonColor);
            confirmButton.onClick.AddListener(() => TryConfirm());
            StrategyUiButtonFeedback.Attach(
                confirmButton,
                StrategyUiButtonFeedbackProfile.Standard,
                null);
            Text buttonLabel = CreateText(
                "Label",
                buttonRoot,
                "ЗАБРАТЬ",
                16,
                TextAnchor.MiddleCenter,
                Color.white);
            buttonLabel.fontStyle = FontStyle.Bold;
            Stretch(buttonLabel.rectTransform);
            root.SetActive(false);
        }

        private void Populate(
            StrategyResidentItemDefinition definition,
            StrategyResidentAgent resident,
            Sprite artwork,
            string headline)
        {
            titleText.text = string.IsNullOrWhiteSpace(headline)
                ? definition.Title
                : headline;
            ownerText.text = "Теперь у " + resident.FullName;
            descriptionText.text = definition.Description;
            artworkImage.sprite = artwork;
            artworkImage.enabled = artwork != null;
            confirmButton.interactable = false;
        }

        private void ApplyReveal(float progress)
        {
            cardGroup.alpha = progress;
            float scale = reducedMotion ? 1f : Mathf.Lerp(0.82f, 1f, progress);
            cardRoot.localScale = new Vector3(scale, scale, 1f);
            float turn = reducedMotion ? 0f : Mathf.Lerp(-2.5f, 0f, progress);
            cardRoot.localRotation = Quaternion.Euler(0f, 0f, turn);
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject obj = new(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj.GetComponent<RectTransform>();
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int size,
            TextAnchor alignment,
            Color color)
        {
            Text text = CreateRect(name, parent).gameObject.AddComponent<Text>();
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

        private static void ConfigureButtonColors(Button button, Color color)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.Lerp(color, Color.black, 0.20f);
            colors.disabledColor = new Color(color.r, color.g, color.b, 0.42f);
            button.colors = colors;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, 0f, 0f, 0f, 0f);
        }

        private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetCenter(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetTopCenter(RectTransform rect, float x, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -top);
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

        private static void SetBottomCenter(RectTransform rect, float x, float bottom, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(x, bottom);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
