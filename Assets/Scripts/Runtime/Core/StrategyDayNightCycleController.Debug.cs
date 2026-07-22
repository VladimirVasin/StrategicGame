using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyDayNightCycleController
    {
        private const float DebugNightApproachHour = 21f + 50f / 60f;
        private const float DebugClockBoundaryEpsilonSeconds = 0.01f;

        internal bool DebugMoveToNightApproach()
        {
            float currentElapsedSeconds = CurrentElapsedSeconds;
            float targetElapsedSeconds = CalculateDebugNightApproachElapsedSeconds(
                currentElapsedSeconds);
            if (targetElapsedSeconds <= currentElapsedSeconds + Mathf.Epsilon)
            {
                return false;
            }

            RestoreElapsedSeconds(targetElapsedSeconds);
            if (overlayRenderer != null)
            {
                ApplyVisuals(true);
            }

            StrategyDebugLogger.Info(
                "DayNight",
                "DebugMovedToNightApproach",
                StrategyDebugLogger.F("fromElapsedSeconds", currentElapsedSeconds),
                StrategyDebugLogger.F("toElapsedSeconds", targetElapsedSeconds),
                StrategyDebugLogger.F("clock", CurrentCalendarSnapshot.ClockText));
            return true;
        }

        internal static float CalculateDebugNightApproachElapsedSeconds(
            float currentElapsedSeconds)
        {
            float normalizedElapsedSeconds = Mathf.Max(0f, currentElapsedSeconds);
            float currentPhase = Mathf.Repeat(
                normalizedElapsedSeconds / CycleSeconds,
                1f);
            float targetPhase = (DebugNightApproachHour - ClockStartHour)
                / HoursPerDay;
            if (currentPhase >= targetPhase)
            {
                return normalizedElapsedSeconds;
            }

            int dayIndex = Mathf.FloorToInt(
                normalizedElapsedSeconds / CycleSeconds);
            return dayIndex * CycleSeconds
                + targetPhase * CycleSeconds
                + DebugClockBoundaryEpsilonSeconds;
        }
    }
}
