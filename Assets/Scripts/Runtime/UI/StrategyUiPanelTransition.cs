using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyUiPanelTransition : MonoBehaviour
    {
        private const string ReducedMotionKey = "ProjectUnknown.Founding.ReducedMotion";
        private const float ReducedMotionDuration = 0.09f;

        private CanvasGroup canvasGroup;
        private RectTransform motionTarget;
        private Vector2 shownPosition;
        private Vector3 shownScale;
        private Vector2 hiddenOffset;
        private float hiddenScaleMultiplier;
        private float showDuration;
        private float hideDuration;
        private bool deactivateWhenHidden;
        private bool configured;
        private bool targetVisible;
        private bool animating;
        private float elapsed;
        private float duration;
        private float startAlpha;
        private float endAlpha;
        private Vector2 startPosition;
        private Vector2 endPosition;
        private Vector3 startScale;
        private Vector3 endScale;

        public bool TargetVisible => targetVisible;
        public bool IsInputShieldActive => canvasGroup != null && canvasGroup.blocksRaycasts;

        public void Configure(
            CanvasGroup group,
            RectTransform target,
            Vector2 collapsedOffset,
            float collapsedScale = 0.98f,
            float openingDuration = 0.18f,
            float closingDuration = 0.13f,
            bool deactivateOnHide = true)
        {
            canvasGroup = group;
            motionTarget = target;
            hiddenOffset = collapsedOffset;
            hiddenScaleMultiplier = Mathf.Clamp(collapsedScale, 0.01f, 1f);
            showDuration = Mathf.Max(0f, openingDuration);
            hideDuration = Mathf.Max(0f, closingDuration);
            deactivateWhenHidden = deactivateOnHide;
            configured = canvasGroup != null && motionTarget != null;

            if (!configured)
            {
                return;
            }

            shownPosition = motionTarget.anchoredPosition;
            shownScale = motionTarget.localScale;
        }

        public void SetVisible(bool visible, bool immediate = false)
        {
            if (!configured || canvasGroup == null || motionTarget == null)
            {
                return;
            }

            if (visible)
            {
                canvasGroup.gameObject.SetActive(true);
            }

            canvasGroup.interactable = visible;
            if (visible)
            {
                canvasGroup.blocksRaycasts = true;
            }

            if (immediate)
            {
                targetVisible = visible;
                animating = false;
                ApplyTargetState(visible);
                CompleteTransition();
                return;
            }

            if (animating && targetVisible == visible)
            {
                return;
            }

            targetVisible = visible;
            BeginTransition();
        }

        private void BeginTransition()
        {
            bool reducedMotion = PlayerPrefs.GetInt(ReducedMotionKey, 0) != 0;
            Vector2 effectiveOffset = reducedMotion ? Vector2.zero : hiddenOffset;
            float effectiveScale = reducedMotion ? 1f : hiddenScaleMultiplier;
            if (reducedMotion)
            {
                motionTarget.anchoredPosition = shownPosition;
                motionTarget.localScale = shownScale;
            }

            startAlpha = canvasGroup.alpha;
            endAlpha = targetVisible ? 1f : 0f;
            startPosition = motionTarget.anchoredPosition;
            endPosition = targetVisible ? shownPosition : shownPosition + effectiveOffset;
            startScale = motionTarget.localScale;
            endScale = targetVisible ? shownScale : shownScale * effectiveScale;
            elapsed = 0f;
            duration = targetVisible ? showDuration : hideDuration;
            if (reducedMotion)
            {
                duration = Mathf.Min(duration, ReducedMotionDuration);
            }

            animating = duration > 0f && !IsAtTarget();
            if (!animating)
            {
                ApplyTargetState(targetVisible, effectiveOffset, effectiveScale);
                CompleteTransition();
            }
        }

        private void Update()
        {
            if (!animating)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float eased = t * t * (3f - (2f * t));
            canvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, endAlpha, eased);
            motionTarget.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, eased);
            motionTarget.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);

            if (t >= 1f)
            {
                animating = false;
                CompleteTransition();
            }
        }

        private bool IsAtTarget()
        {
            return Mathf.Abs(startAlpha - endAlpha) <= 0.001f
                && (startPosition - endPosition).sqrMagnitude <= 0.001f
                && (startScale - endScale).sqrMagnitude <= 0.0001f;
        }

        private void ApplyTargetState(bool visible)
        {
            bool reducedMotion = PlayerPrefs.GetInt(ReducedMotionKey, 0) != 0;
            ApplyTargetState(
                visible,
                reducedMotion ? Vector2.zero : hiddenOffset,
                reducedMotion ? 1f : hiddenScaleMultiplier);
        }

        private void ApplyTargetState(bool visible, Vector2 effectiveOffset, float effectiveScale)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            motionTarget.anchoredPosition = visible ? shownPosition : shownPosition + effectiveOffset;
            motionTarget.localScale = visible ? shownScale : shownScale * effectiveScale;
        }

        private void CompleteTransition()
        {
            canvasGroup.alpha = targetVisible ? 1f : 0f;
            canvasGroup.interactable = targetVisible;
            canvasGroup.blocksRaycasts = targetVisible;
            if (!targetVisible && deactivateWhenHidden)
            {
                canvasGroup.gameObject.SetActive(false);
            }
        }
    }
}
