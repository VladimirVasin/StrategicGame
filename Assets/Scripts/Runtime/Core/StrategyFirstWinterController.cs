using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyFirstWinterController : MonoBehaviour
    {
        private const float UpdateInterval = 0.75f;

        private StrategyGoalsController goals;
        private StrategyStarterGoalSequenceController starterGoals;
        private StrategyPopulationController population;
        private FirstWinterPhase phase;
        private float updateTimer;
        private bool foodPrepared;
        private bool fuelPrepared;
        private bool languageSubscribed;

        public bool FoodPrepared => foodPrepared;
        public bool FuelPrepared => fuelPrepared;
        public bool IsFirstWinterActive => phase == FirstWinterPhase.Winter;
        public bool HasPassedFirstWinter => phase == FirstWinterPhase.Complete;

        public void RestorePersistentState(bool savedFoodPrepared, bool savedFuelPrepared, bool savedPassed)
        {
            foodPrepared = savedFoodPrepared;
            fuelPrepared = savedFuelPrepared;
            phase = savedPassed ? FirstWinterPhase.Complete : FirstWinterPhase.WaitingForOnboarding;
            if (savedPassed)
            {
                goals?.ClearGoals();
            }

            updateTimer = 0f;
        }

        public void Configure(
            StrategyGoalsController goalsController,
            StrategyStarterGoalSequenceController starterGoalController,
            StrategyPopulationController populationController)
        {
            goals = goalsController;
            starterGoals = starterGoalController;
            population = populationController;
            phase = FirstWinterPhase.WaitingForOnboarding;
            updateTimer = 0f;

            if (!languageSubscribed)
            {
                StrategyLocalization.LanguageChanged += HandleLanguageChanged;
                languageSubscribed = true;
            }
        }

        private void OnDestroy()
        {
            if (languageSubscribed)
            {
                StrategyLocalization.LanguageChanged -= HandleLanguageChanged;
                languageSubscribed = false;
            }
        }

        private void Update()
        {
            updateTimer -= Time.deltaTime;
            if (updateTimer > 0f)
            {
                return;
            }

            updateTimer = UpdateInterval;
            EvaluateProgression();
        }

        private void EvaluateProgression()
        {
            if (starterGoals == null || !starterGoals.IsComplete || population == null)
            {
                return;
            }

            StrategyCalendarSnapshot calendar = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            population.UpdateHomelessColdExposure(calendar);
            if (calendar.Year > 1)
            {
                FinishFirstWinter();
                return;
            }

            if (calendar.Season == StrategySeason.Winter)
            {
                BeginWinter(calendar);
                return;
            }

            BeginOrUpdatePreparation(calendar);
        }

        private void BeginOrUpdatePreparation(StrategyCalendarSnapshot calendar)
        {
            if (phase != FirstWinterPhase.Preparation)
            {
                phase = FirstWinterPhase.Preparation;
                goals?.SetGoals(CreateFoodGoal(), CreateFuelGoal());
                StrategyEventLogHudController.Notify(
                    H("goals.winter.preparation.notice"),
                    StrategySeasonCalendar.GetSeasonAccentColor(StrategySeason.Autumn));
                StrategyDebugLogger.Info("FirstWinter", "PreparationStarted", StrategyDebugLogger.F("day", calendar.DisplayDay));
            }

            UpdatePreparationReadiness(calendar);
        }

        private void UpdatePreparationReadiness(StrategyCalendarSnapshot calendar)
        {
            StrategySeasonReadinessSnapshot readiness = StrategySeasonReadiness.Evaluate(calendar, population);
            float targetDays = StrategyFirstYearBalance.WinterPreparationDays;
            goals?.SetGoalProgress(
                StrategyGoalKind.PrepareWinterFood,
                readiness.FoodDays,
                targetDays,
                H("goals.winter.progress_days", Mathf.Min(readiness.FoodDays, targetDays), targetDays));
            goals?.SetGoalProgress(
                StrategyGoalKind.PrepareWinterFuel,
                readiness.FuelDays,
                targetDays,
                H("goals.winter.progress_days", Mathf.Min(readiness.FuelDays, targetDays), targetDays));
            if (!foodPrepared && readiness.CoversFood)
            {
                foodPrepared = true;
                goals?.CompleteGoal(StrategyGoalKind.PrepareWinterFood);
            }

            if (!fuelPrepared && readiness.CoversFuel)
            {
                fuelPrepared = true;
                goals?.CompleteGoal(StrategyGoalKind.PrepareWinterFuel);
            }
        }

        private void BeginWinter(StrategyCalendarSnapshot calendar)
        {
            if (phase == FirstWinterPhase.Winter)
            {
                return;
            }

            StrategySeasonReadinessSnapshot readiness = StrategySeasonReadiness.Evaluate(calendar, population);
            foodPrepared |= readiness.CoversFood;
            fuelPrepared |= readiness.CoversFuel;
            phase = FirstWinterPhase.Winter;
            goals?.SetGoals(CreateSurviveWinterGoal());
            StrategyEventLogHudController.Notify(
                foodPrepared && fuelPrepared
                    ? H("goals.winter.started_prepared")
                    : H("goals.winter.started_short"),
                StrategySeasonCalendar.GetSeasonAccentColor(StrategySeason.Winter));
            StrategyDebugLogger.Info(
                "FirstWinter",
                "WinterStarted",
                StrategyDebugLogger.F("foodPrepared", foodPrepared),
                StrategyDebugLogger.F("fuelPrepared", fuelPrepared),
                StrategyDebugLogger.F("residents", population.TotalResidentCount));
        }

        private void FinishFirstWinter()
        {
            if (phase == FirstWinterPhase.Complete)
            {
                return;
            }

            phase = FirstWinterPhase.Complete;
            goals?.ClearGoals();
            StrategyEventLogHudController.Notify(
                H("goals.winter.ended"),
                new Color(0.62f, 0.82f, 0.68f));
            StrategyDebugLogger.Info("FirstWinter", "WinterEnded", StrategyDebugLogger.F("residents", population.TotalResidentCount));
        }

        private void HandleLanguageChanged()
        {
            if (phase == FirstWinterPhase.Preparation)
            {
                goals?.ReplaceGoalText(CreateFoodGoal(), CreateFuelGoal());
                if (population != null)
                {
                    UpdatePreparationReadiness(StrategyDayNightCycleController.CurrentCalendarSnapshot);
                }
            }
            else if (phase == FirstWinterPhase.Winter)
            {
                goals?.ReplaceGoalText(CreateSurviveWinterGoal());
            }
        }

        private static StrategyGoalDefinition CreateFoodGoal()
        {
            return new StrategyGoalDefinition(
                StrategyGoalKind.PrepareWinterFood,
                H("goals.winter.food.title", StrategyFirstYearBalance.WinterPreparationDays),
                H("goals.winter.food.description"));
        }

        private static StrategyGoalDefinition CreateFuelGoal()
        {
            return new StrategyGoalDefinition(
                StrategyGoalKind.PrepareWinterFuel,
                H("goals.winter.fuel.title", StrategyFirstYearBalance.WinterPreparationDays),
                H("goals.winter.fuel.description"));
        }

        private static StrategyGoalDefinition CreateSurviveWinterGoal()
        {
            return new StrategyGoalDefinition(
                StrategyGoalKind.SurviveFirstWinter,
                H("goals.winter.survive.title"),
                H("goals.winter.survive.description"));
        }

        private static string H(string key, params object[] arguments)
        {
            return StrategyLocalization.Get(StrategyLocalizationTables.Hud, key, arguments);
        }

        private enum FirstWinterPhase
        {
            WaitingForOnboarding,
            Preparation,
            Winter,
            Complete
        }
    }
}
