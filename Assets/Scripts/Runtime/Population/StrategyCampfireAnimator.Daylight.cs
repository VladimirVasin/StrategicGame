using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyCampfireAnimator
    {
        private void UpdateInitialDaylightFire()
        {
            frameTimer += Time.deltaTime;
            while (frameTimer >= FrameDuration)
            {
                frameTimer -= FrameDuration;
                frameIndex = (frameIndex + 1) % StrategyCampfireSpriteFactory.FrameCount;
            }

            EnsureAmbientRenderer();
            ambientFrameTimer += Time.deltaTime;
            while (ambientFrameTimer >= AmbientFrameDuration)
            {
                ambientFrameTimer -= AmbientFrameDuration;
                ambientFrameIndex = (ambientFrameIndex + 1) % StrategyCampfireAmbientSpriteFactory.FrameCount;
            }

            ApplyInitialDaylightFireVisuals();
        }

        private void ApplyInitialDaylightFireVisuals()
        {
            float daylightFade = StrategyCinematicVisualMath.DawnToNoonFadeOutFactor(
                StrategyDayNightCycleController.CurrentDayPhase);
            if (daylightFade <= 0.001f)
            {
                ExtinguishForDaylight();
                return;
            }

            float ember = Mathf.Clamp01(daylightFade);
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sprite = StrategyCampfireSpriteFactory.GetBaseFrame(frameIndex);
                spriteRenderer.color = Color.white;
            }

            transform.localScale = baseScale;
            EnsureFlameRenderer();
            if (flameRenderer != null)
            {
                flameRenderer.enabled = ember > 0.035f;
                flameRenderer.sprite = StrategyCampfireSpriteFactory.GetFlameFrame(frameIndex);
                flameRenderer.color = new Color(
                    1f,
                    Mathf.Lerp(0.50f, 1f, ember),
                    Mathf.Lerp(0.24f, 1f, ember),
                    1f);
                flameRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.18f, 1f, ember);
            }

            if (ambientRenderer != null)
            {
                ambientRenderer.enabled = ember > 0.05f;
                ambientRenderer.sprite = StrategyCampfireAmbientSpriteFactory.GetFrame(ambientFrameIndex);
                ambientRenderer.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.05f, 0.85f, ember));
            }
        }

        private void ApplyExtinguishedVisuals()
        {
            float daylightFade = StrategyCinematicVisualMath.DawnToNoonFadeOutFactor(
                StrategyDayNightCycleController.CurrentDayPhase);
            bool fullyOut = daylightFade <= 0.001f;
            float ember = fullyOut ? 0f : Mathf.Lerp(0.35f, 1f, daylightFade);
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sprite = StrategyCampfireRelightSpriteFactory.GetBaseFrame(fullyOut ? 0 : frameIndex);
                spriteRenderer.color = Color.white;
            }

            transform.localScale = baseScale;
            EnsureFlameRenderer();
            if (flameRenderer != null)
            {
                flameRenderer.enabled = !fullyOut && ember > 0.035f;
                flameRenderer.sprite = StrategyCampfireRelightSpriteFactory.GetEmberFlameFrame(frameIndex);
                flameRenderer.color = new Color(
                    Mathf.Lerp(0.72f, 1f, ember),
                    Mathf.Lerp(0.32f, 0.76f, ember),
                    Mathf.Lerp(0.20f, 0.38f, ember),
                    1f);
                flameRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 0.78f, ember);
            }

            if (ambientRenderer != null)
            {
                ambientRenderer.enabled = false;
            }
        }

        private void ExtinguishForDaylight()
        {
            StrategyCalendarSnapshot snapshot = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            bool changed = !extinguished || relighting || !walkabilityReleased || initialDaylightFireActive;
            relighting = false;
            extinguished = true;
            initialDaylightFireActive = false;
            burnAge = BurnoutDelaySeconds + BurnoutDurationSeconds;
            relightTimer = 0f;
            relightDuration = 0f;
            ReleaseCampCell();
            ApplyExtinguishedVisuals();

            if (changed && daylightExtinguishedDayIndex != snapshot.DayIndex)
            {
                daylightExtinguishedDayIndex = snapshot.DayIndex;
                StrategyDebugLogger.Info(
                    "Campfire",
                    "CampfireExtinguishedForDaylight",
                    StrategyDebugLogger.F("cell", blockedCell),
                    StrategyDebugLogger.F("day", snapshot.DisplayDay),
                    StrategyDebugLogger.F("phase", snapshot.PhaseLabel));
            }
        }

        private static bool IsNightFireTime()
        {
            return StrategyDayNightCycleController.CurrentCalendarSnapshot.Phase
                == StrategyTimeOfDayPhase.Night;
        }

        private static bool IsInitialDaylightFireTime()
        {
            StrategyCalendarSnapshot snapshot = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            return snapshot.DayIndex == 0
                && snapshot.Phase != StrategyTimeOfDayPhase.Night
                && StrategyCinematicVisualMath.DawnToNoonFadeOutFactor(snapshot.DayPhase) > 0.001f;
        }

        private static bool IsFullyDaylightExtinguishedTime()
        {
            StrategyTimeOfDayPhase phase = StrategyDayNightCycleController.CurrentCalendarSnapshot.Phase;
            return phase == StrategyTimeOfDayPhase.Noon
                || phase == StrategyTimeOfDayPhase.Afternoon
                || phase == StrategyTimeOfDayPhase.Dusk;
        }
    }
}
