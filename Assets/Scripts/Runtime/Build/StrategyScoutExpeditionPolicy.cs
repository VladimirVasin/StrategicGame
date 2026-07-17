using System;

namespace ProjectUnknown.Strategy
{
    public static class StrategyScoutExpeditionPolicy
    {
        public const int MinimumDays = 1;
        public const int MaximumDays = 7;
        public const int DefaultDays = 1;
        public const float RationsPerDay = 1f;

        public static bool IsSupportedDuration(int days)
        {
            return days >= MinimumDays && days <= MaximumDays;
        }

        public static int ClampDurationDays(int days)
        {
            return Math.Min(MaximumDays, Math.Max(MinimumDays, days));
        }

        public static float GetRequiredRations(int days)
        {
            return ClampDurationDays(days) * RationsPerDay;
        }

        public static float GetDurationSeconds(int days)
        {
            return ClampDurationDays(days) * StrategyDayNightCycleController.DayLengthSeconds;
        }
    }
}
