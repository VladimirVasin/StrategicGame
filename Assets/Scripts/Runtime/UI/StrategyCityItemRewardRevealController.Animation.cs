using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyCityItemRewardRevealController
    {
        private const float RevealDuration = 0.72f;
        private const float ReducedRevealDuration = 0.12f;
        private const float FlightDuration = 0.62f;
        private const float ReducedFlightDuration = 0.16f;
        private static readonly Vector2 CardHomePosition = new(0f, 25f);

        private float phaseElapsed;
        private float floatingElapsed;
        private Vector2 flightStart;
        private Vector2 flightControl;
        private Vector2 flightEnd;
        private Quaternion flightStartRotation;
        private Vector3 flightStartScale;

        private void BeginRevealAnimation()
        {
            State = StrategyCityItemRewardRevealState.Revealing;
            phaseElapsed = 0f;
            floatingElapsed = 0f;
            confirmButton.interactable = false;
            confirmGroup.alpha = 0f;
            confirmGroup.interactable = false;
            confirmGroup.blocksRaycasts = false;
            cardGroup.alpha = 0f;
            cardRoot.anchoredPosition = CardHomePosition + new Vector2(0f, -42f);
            cardRoot.localScale = reducedMotion ? Vector3.one * 0.96f : Vector3.one * 0.68f;
            cardRoot.localRotation = reducedMotion
                ? Quaternion.identity
                : Quaternion.Euler(0f, 0f, -4.5f);
            SetBackdropAlpha(0.70f);
            SetGlowAlpha(0f);
            if (raysRoot != null)
            {
                raysRoot.localScale = Vector3.one * 0.75f;
            }

            ResetSparkTransforms();
        }

        private void BeginFlightAnimation()
        {
            State = StrategyCityItemRewardRevealState.FlyingToChest;
            phaseElapsed = 0f;
            confirmButton.interactable = false;
            confirmGroup.interactable = false;
            confirmGroup.blocksRaycasts = false;
            flightStart = cardRoot.anchoredPosition;
            flightEnd = ResolveFlightDestination();
            flightStartRotation = cardRoot.localRotation;
            flightStartScale = cardRoot.localScale;
            Vector2 midpoint = (flightStart + flightEnd) * 0.5f;
            float lift = Mathf.Max(120f, Mathf.Abs(flightEnd.y - flightStart.y) * 0.22f);
            flightControl = midpoint + new Vector2(0f, lift);
        }

        private void AdvanceAnimation(float deltaTime)
        {
            switch (State)
            {
                case StrategyCityItemRewardRevealState.Revealing:
                    AdvanceReveal(deltaTime);
                    break;
                case StrategyCityItemRewardRevealState.AwaitingConfirmation:
                    AdvanceFloating(deltaTime);
                    break;
                case StrategyCityItemRewardRevealState.FlyingToChest:
                    AdvanceFlight(deltaTime);
                    break;
            }
        }

        private void AdvanceReveal(float deltaTime)
        {
            phaseElapsed += deltaTime;
            float duration = reducedMotion ? ReducedRevealDuration : RevealDuration;
            float t = duration > 0f ? Mathf.Clamp01(phaseElapsed / duration) : 1f;
            float smooth = SmoothStep(t);
            float scaleT = reducedMotion ? smooth : OutBack(t);

            cardGroup.alpha = Mathf.Clamp01(t * 2.3f);
            cardRoot.anchoredPosition = Vector2.LerpUnclamped(
                CardHomePosition + new Vector2(0f, -42f),
                CardHomePosition,
                smooth);
            cardRoot.localScale = Vector3.one * Mathf.LerpUnclamped(
                reducedMotion ? 0.96f : 0.68f,
                1f,
                scaleT);
            cardRoot.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Lerp(-4.5f, 0f, smooth));
            SetBackdropAlpha(Mathf.Lerp(0.70f, 0.86f, smooth));
            SetGlowAlpha((reducedMotion ? 0.12f : 0.27f) * smooth);
            if (raysRoot != null && !reducedMotion)
            {
                raysRoot.localScale = Vector3.one * Mathf.Lerp(0.75f, 1f, smooth);
                raysRoot.localRotation = Quaternion.Euler(0f, 0f, t * 4f);
            }

            AdvanceSparkTransforms(deltaTime, smooth);
            if (t >= 1f)
            {
                EnterAwaitingConfirmation();
            }
        }

        private void EnterAwaitingConfirmation()
        {
            State = StrategyCityItemRewardRevealState.AwaitingConfirmation;
            phaseElapsed = 0f;
            floatingElapsed = 0f;
            cardGroup.alpha = 1f;
            cardRoot.anchoredPosition = CardHomePosition;
            cardRoot.localScale = Vector3.one;
            cardRoot.localRotation = Quaternion.identity;
            confirmGroup.alpha = 1f;
            confirmGroup.interactable = true;
            confirmGroup.blocksRaycasts = true;
            confirmButton.interactable = true;
            confirmFeedback?.SuppressNextFocusCue();
            SelectConfirmButton();
        }

        private void AdvanceFloating(float deltaTime)
        {
            if (reducedMotion)
            {
                ApplyAwaitingPose();
                return;
            }

            floatingElapsed += deltaTime;
            float bob = Mathf.Sin(floatingElapsed * 1.65f) * 6f;
            float tilt = Mathf.Sin((floatingElapsed * 1.12f) + 0.6f) * 0.65f;
            cardRoot.anchoredPosition = CardHomePosition + new Vector2(0f, bob);
            cardRoot.localScale = Vector3.one;
            cardRoot.localRotation = Quaternion.Euler(0f, 0f, tilt);
            float glowPulse = 0.245f + Mathf.Sin(floatingElapsed * 2.1f) * 0.025f;
            SetGlowAlpha(glowPulse);
            raysRoot.localRotation = Quaternion.Euler(0f, 0f, floatingElapsed * 1.6f);
            AdvanceSparkTransforms(deltaTime, 1f);
        }

        private void AdvanceFlight(float deltaTime)
        {
            phaseElapsed += deltaTime;
            float duration = reducedMotion ? ReducedFlightDuration : FlightDuration;
            float t = duration > 0f ? Mathf.Clamp01(phaseElapsed / duration) : 1f;
            float eased = t * t * (3f - (2f * t));
            Vector2 position = reducedMotion
                ? Vector2.LerpUnclamped(flightStart, flightEnd, eased)
                : QuadraticBezier(flightStart, flightControl, flightEnd, eased);
            cardRoot.anchoredPosition = position;
            cardRoot.localScale = Vector3.LerpUnclamped(
                flightStartScale,
                Vector3.one * 0.07f,
                Mathf.Pow(eased, 1.25f));
            float spin = reducedMotion ? 0f : Mathf.Lerp(0f, -14f, eased);
            cardRoot.localRotation = flightStartRotation * Quaternion.Euler(0f, 0f, spin);
            cardGroup.alpha = 1f - Mathf.Clamp01((t - 0.78f) / 0.22f);
            confirmGroup.alpha = 1f - Mathf.Clamp01(t * 3f);
            SetBackdropAlpha(0.86f * (1f - eased));
            SetGlowAlpha(0.26f * (1f - eased));
            if (raysRoot != null && !reducedMotion)
            {
                raysRoot.localScale = Vector3.one * Mathf.Lerp(1f, 0.25f, eased);
            }

            if (t >= 1f)
            {
                FinishPresentation();
            }
        }

        private void ApplyAwaitingPose()
        {
            if (cardRoot == null)
            {
                return;
            }

            cardRoot.anchoredPosition = CardHomePosition;
            cardRoot.localScale = Vector3.one;
            cardRoot.localRotation = Quaternion.identity;
            SetGlowAlpha(0.12f);
        }

        private Vector2 ResolveFlightDestination()
        {
            Canvas.ForceUpdateCanvases();
            if (inventoryHud != null
                && inventoryHud.TryGetRewardDestination(out Vector2 screenPoint)
                && rewardCanvasRect != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rewardCanvasRect,
                    screenPoint,
                    null,
                    out Vector2 localPoint))
            {
                return localPoint;
            }

            Rect canvasBounds = rewardCanvasRect != null
                ? rewardCanvasRect.rect
                : new Rect(-800f, -450f, 1600f, 900f);
            return new Vector2(canvasBounds.xMin + 292f, canvasBounds.yMax - 40f);
        }

        private void AdvanceSparkTransforms(float deltaTime, float visibility)
        {
            if (reducedMotion)
            {
                return;
            }

            floatingElapsed += State == StrategyCityItemRewardRevealState.Revealing
                ? deltaTime
                : 0f;
            for (int index = 0; index < sparks.Length; index++)
            {
                RectTransform spark = sparks[index];
                if (spark == null)
                {
                    continue;
                }

                float wave = floatingElapsed * (0.55f + (index % 5) * 0.08f) + index;
                spark.anchoredPosition = sparkHomes[index]
                    + new Vector2(Mathf.Sin(wave) * 5f, Mathf.Cos(wave * 0.83f) * 8f);
                float pulse = 0.55f + Mathf.Sin(wave * 2.1f) * 0.30f;
                Image image = spark.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(
                        PaleGold.r,
                        PaleGold.g,
                        PaleGold.b,
                        Mathf.Clamp01(pulse * visibility));
                }
            }
        }

        private void ResetSparkTransforms()
        {
            for (int index = 0; index < sparks.Length; index++)
            {
                if (sparks[index] != null)
                {
                    sparks[index].anchoredPosition = sparkHomes[index];
                }
            }
        }

        private void ResetAnimationVisuals()
        {
            phaseElapsed = 0f;
            floatingElapsed = 0f;
            if (cardRoot != null)
            {
                cardRoot.anchoredPosition = CardHomePosition;
                cardRoot.localScale = Vector3.one;
                cardRoot.localRotation = Quaternion.identity;
            }

            if (cardGroup != null)
            {
                cardGroup.alpha = 0f;
            }

            if (confirmGroup != null)
            {
                confirmGroup.alpha = 0f;
                confirmGroup.interactable = false;
                confirmGroup.blocksRaycasts = false;
            }

            if (confirmButton != null)
            {
                confirmButton.interactable = false;
            }

            SetBackdropAlpha(0f);
            SetGlowAlpha(0f);
            ResetSparkTransforms();
        }

        private void SetBackdropAlpha(float alpha)
        {
            if (backdropImage == null)
            {
                return;
            }

            Color color = backdropImage.color;
            color.a = Mathf.Clamp01(alpha);
            backdropImage.color = color;
        }

        private void SetGlowAlpha(float alpha)
        {
            if (glowImage == null)
            {
                return;
            }

            Color color = glowImage.color;
            color.a = Mathf.Clamp01(alpha);
            glowImage.color = color;
        }

        private void SelectConfirmButton()
        {
            if (confirmButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
            }
        }

        private static float SmoothStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - (2f * value));
        }

        private static float OutBack(float value)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float shifted = Mathf.Clamp01(value) - 1f;
            return 1f + (c3 * shifted * shifted * shifted) + (c1 * shifted * shifted);
        }

        private static Vector2 QuadraticBezier(
            Vector2 start,
            Vector2 control,
            Vector2 end,
            float value)
        {
            float inverse = 1f - value;
            return (inverse * inverse * start)
                + (2f * inverse * value * control)
                + (value * value * end);
        }
    }
}
