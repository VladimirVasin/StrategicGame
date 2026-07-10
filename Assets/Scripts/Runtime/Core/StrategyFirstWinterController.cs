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
                goals?.SetGoals(
                    new StrategyGoalDefinition(
                        StrategyGoalKind.PrepareWinterFood,
                        "Store " + StrategyFirstYearBalance.WinterPreparationDays + " days of food",
                        "Households need enough rations to cross the first winter."),
                    new StrategyGoalDefinition(
                        StrategyGoalKind.PrepareWinterFuel,
                        "Store " + StrategyFirstYearBalance.WinterPreparationDays + " days of firewood",
                        "Occupied houses consume firewood during winter nights."));
                StrategyEventLogHudController.Notify(
                    "Prepare for the first winter",
                    StrategySeasonCalendar.GetSeasonAccentColor(StrategySeason.Autumn));
                StrategyDebugLogger.Info("FirstWinter", "PreparationStarted", StrategyDebugLogger.F("day", calendar.DisplayDay));
            }

            StrategySeasonReadinessSnapshot readiness = StrategySeasonReadiness.Evaluate(calendar, population);
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
            goals?.SetGoals(new StrategyGoalDefinition(
                StrategyGoalKind.SurviveFirstWinter,
                "Endure the first winter",
                "Manage food, firewood, and shelter until summer returns."));
            StrategyEventLogHudController.Notify(
                foodPrepared && fuelPrepared
                    ? "The first winter has begun. The settlement is prepared."
                    : "The first winter has begun. Supplies are short.",
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
            StrategyEventLogHudController.Notify("The first winter has ended", new Color(0.62f, 0.82f, 0.68f));
            StrategyDebugLogger.Info("FirstWinter", "WinterEnded", StrategyDebugLogger.F("residents", population.TotalResidentCount));
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
