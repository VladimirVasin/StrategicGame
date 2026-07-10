using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private readonly StrategyResidentColdState coldState = new();

        public float ColdExposure => coldState.Exposure;
        public int LastColdResolutionDayIndex => coldState.LastResolvedDayIndex;
        public StrategyResidentColdCondition ColdCondition => coldState.Condition;
        public bool IsColdAffected => ColdCondition != StrategyResidentColdCondition.Healthy;
        public float ColdMovementSpeedMultiplier => coldState.MovementSpeedMultiplier;
        public string ColdStatusText => ColdCondition switch
        {
            StrategyResidentColdCondition.Chilled => "chilled",
            StrategyResidentColdCondition.Sick => "sick from cold",
            StrategyResidentColdCondition.Critical => "critical hypothermia",
            _ => "warm"
        };

        public void ApplyOvernightCold(float minimumCelsius, int dayIndex)
        {
            if (deathRequested || IsPendingRefugee)
            {
                return;
            }

            StrategyResidentColdCondition previous = coldState.Condition;
            float vulnerability = GetColdVulnerability();
            bool fatal = coldState.ApplyNight(minimumCelsius, dayIndex, vulnerability, out float mortalityChance);
            StrategyResidentColdCondition current = coldState.Condition;
            if (previous != current)
            {
                StrategyDebugLogger.Info(
                    "Population",
                    "ResidentColdConditionChanged",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("previous", previous),
                    StrategyDebugLogger.F("current", current),
                    StrategyDebugLogger.F("exposure", coldState.Exposure),
                    StrategyDebugLogger.F("minimumIndoorCelsius", minimumCelsius));
            }

            if (fatal)
            {
                population?.TryKillResidentFromCold(this, minimumCelsius, coldState.Exposure, mortalityChance);
            }
        }

        public void RestoreColdState(float exposure, int lastResolvedDayIndex)
        {
            coldState.Restore(exposure, lastResolvedDayIndex);
        }

        public void RestorePersistentConditionState(
            float savedNutritionDebt,
            int savedDaysHungry,
            int savedLastNutritionDayIndex,
            float savedColdExposure,
            int savedLastColdResolutionDayIndex,
            IReadOnlyList<int> savedChildIds)
        {
            nutritionDebt = Mathf.Clamp(savedNutritionDebt, 0f, MaxNutritionDebt);
            daysHungry = Mathf.Clamp(savedDaysHungry, 0, MaxHungryDays);
            lastNutritionDayIndex = savedLastNutritionDayIndex;
            coldState.Restore(savedColdExposure, savedLastColdResolutionDayIndex);
            childIds.Clear();
            if (savedChildIds == null)
            {
                return;
            }

            for (int i = 0; i < savedChildIds.Count; i++)
            {
                if (savedChildIds[i] > 0 && !childIds.Contains(savedChildIds[i]))
                {
                    childIds.Add(savedChildIds[i]);
                }
            }
        }

        private float GetColdVulnerability()
        {
            float vulnerability = lifeStage == StrategyResidentLifeStage.Child ? 1.35f : 1f;
            if (ageYears >= 50f)
            {
                vulnerability *= Mathf.Clamp(1f + (ageYears - 50f) * 0.025f, 1f, 1.75f);
            }

            vulnerability *= 1f + NutritionSeverityLevel * 0.08f;
            return vulnerability;
        }
    }
}
