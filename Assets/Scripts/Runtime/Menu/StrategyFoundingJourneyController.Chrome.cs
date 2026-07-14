using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFoundingJourneyController
    {
        private static void BuildNarrativeGradient(RectTransform panel)
        {
            float[] widths = { 1f, 0.88f, 0.76f, 0.64f, 0.52f, 0.40f, 0.28f, 0.16f };
            float[] alphas = { 0.025f, 0.035f, 0.045f, 0.055f, 0.070f, 0.085f, 0.105f, 0.125f };
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
            CreateOverlayBand(
                "TopVignette",
                parent,
                new Vector2(0f, 0.91f),
                Vector2.one,
                new Color(0f, 0f, 0f, 0.07f));
            CreateOverlayBand(
                "BottomVignette",
                parent,
                Vector2.zero,
                new Vector2(1f, 0.09f),
                new Color(0f, 0f, 0f, 0.09f));
            CreateOverlayBand(
                "LeftVignetteOuter",
                parent,
                Vector2.zero,
                new Vector2(0.035f, 1f),
                new Color(0f, 0f, 0f, 0.24f));
            CreateOverlayBand(
                "LeftVignetteInner",
                parent,
                Vector2.zero,
                new Vector2(0.075f, 1f),
                new Color(0f, 0f, 0f, 0.08f));
            CreateOverlayBand(
                "RightVignetteOuter",
                parent,
                new Vector2(0.965f, 0f),
                Vector2.one,
                new Color(0f, 0f, 0f, 0.24f));
            CreateOverlayBand(
                "RightVignetteInner",
                parent,
                new Vector2(0.925f, 0f),
                Vector2.one,
                new Color(0f, 0f, 0f, 0.08f));

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

        private void AttachJourneyButtonFeedback()
        {
            StrategyUiButtonFeedback.Attach(backButton, StrategyUiButtonFeedbackProfile.Cinematic);
            StrategyUiButtonFeedback.Attach(nextButton, StrategyUiButtonFeedbackProfile.Cinematic);
            StrategyUiButtonFeedback.Attach(
                skipStoryButton,
                StrategyUiButtonFeedbackProfile.Cinematic,
                StrategyHudSfxKind.Click);
            StrategyUiButtonFeedback.Attach(balancedDefaultsButton, StrategyUiButtonFeedbackProfile.Cinematic);
            StrategyUiButtonFeedback.Attach(
                changeAnswersButton,
                StrategyUiButtonFeedbackProfile.Cinematic,
                StrategyHudSfxKind.Click);
            StrategyUiButtonFeedback.Attach(beginButton, StrategyUiButtonFeedbackProfile.Cinematic);
            for (int i = 0; i < optionButtons.Length; i++)
            {
                StrategyUiButtonFeedback.Attach(optionButtons[i], StrategyUiButtonFeedbackProfile.Cinematic);
            }
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
            wrapper.localRotation = buttonRect.localRotation;
            wrapper.localScale = buttonRect.localScale;

            buttonRect.SetParent(wrapper, false);
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.one;
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            buttonRect.localRotation = Quaternion.identity;
            buttonRect.localScale = Vector3.one;
            return wrapper.gameObject.AddComponent<CanvasGroup>();
        }

        private void RefreshJourneyButtonMotion(bool value)
        {
            StrategyUiButtonFeedback[] feedback = GetComponentsInChildren<StrategyUiButtonFeedback>(true);
            for (int i = 0; i < feedback.Length; i++)
            {
                feedback[i].SetReducedMotion(value);
            }
        }
    }
}
