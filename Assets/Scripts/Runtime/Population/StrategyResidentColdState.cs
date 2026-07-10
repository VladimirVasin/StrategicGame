using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyResidentColdCondition
    {
        Healthy,
        Chilled,
        Sick,
        Critical
    }

    public sealed class StrategyResidentColdState
    {
        private const float MaxExposure = 7f;

        public float Exposure { get; private set; }
        public int LastResolvedDayIndex { get; private set; } = -1;
        public StrategyResidentColdCondition Condition => EvaluateCondition(Exposure);
        public float MovementSpeedMultiplier => Condition switch
        {
            StrategyResidentColdCondition.Chilled => 0.96f,
            StrategyResidentColdCondition.Sick => 0.86f,
            StrategyResidentColdCondition.Critical => 0.72f,
            _ => 1f
        };

        public bool ApplyNight(float minimumCelsius, int dayIndex, float vulnerability, out float mortalityChance)
        {
            mortalityChance = 0f;
            if (dayIndex <= LastResolvedDayIndex)
            {
                return false;
            }

            LastResolvedDayIndex = dayIndex;
            if (minimumCelsius >= 10f)
            {
                Exposure = Mathf.Max(0f, Exposure - 1.25f);
            }
            else if (minimumCelsius >= 5f)
            {
                Exposure = Mathf.Max(0f, Exposure - 0.25f);
            }
            else if (minimumCelsius >= 0f)
            {
                Exposure = Mathf.Min(MaxExposure, Exposure + 0.75f * vulnerability);
            }
            else
            {
                Exposure = Mathf.Min(MaxExposure, Exposure + 1.35f * vulnerability);
            }

            if (Condition != StrategyResidentColdCondition.Critical)
            {
                return false;
            }

            mortalityChance = Mathf.Clamp(0.025f + (Exposure - 4f) * 0.018f, 0f, 0.14f) * vulnerability;
            return Random.value < mortalityChance;
        }

        public void Restore(float exposure, int lastResolvedDayIndex)
        {
            Exposure = Mathf.Clamp(exposure, 0f, MaxExposure);
            LastResolvedDayIndex = lastResolvedDayIndex;
        }

        public static StrategyResidentColdCondition EvaluateCondition(float exposure)
        {
            if (exposure >= 4f)
            {
                return StrategyResidentColdCondition.Critical;
            }

            if (exposure >= 2.25f)
            {
                return StrategyResidentColdCondition.Sick;
            }

            return exposure >= 0.75f
                ? StrategyResidentColdCondition.Chilled
                : StrategyResidentColdCondition.Healthy;
        }
    }
}
