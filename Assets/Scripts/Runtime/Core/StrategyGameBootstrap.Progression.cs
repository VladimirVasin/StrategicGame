using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static void ConfigureProgression(
            StrategyBuildMenuController buildMenu,
            StrategyBuildPlacementController placement,
            StrategyPopulationController population)
        {
            StrategyGoalsHudController goalsHud = Object.FindAnyObjectByType<StrategyGoalsHudController>();
            if (goalsHud == null)
            {
                goalsHud = new GameObject("Strategy Goals HUD").AddComponent<StrategyGoalsHudController>();
            }

            StrategyGoalsController goals = Object.FindAnyObjectByType<StrategyGoalsController>();
            if (goals == null)
            {
                goals = new GameObject("Strategy Goals").AddComponent<StrategyGoalsController>();
            }

            goals.Configure(goalsHud);
            StrategyStarterGoalSequenceController starterGoals = Object.FindAnyObjectByType<StrategyStarterGoalSequenceController>();
            if (starterGoals == null)
            {
                starterGoals = new GameObject("Strategy Starter Goals").AddComponent<StrategyStarterGoalSequenceController>();
            }

            starterGoals.Configure(goals, buildMenu, placement);

            StrategyFirstWinterController firstWinter = Object.FindAnyObjectByType<StrategyFirstWinterController>();
            if (firstWinter == null)
            {
                firstWinter = new GameObject("Strategy First Winter Progression")
                    .AddComponent<StrategyFirstWinterController>();
            }

            firstWinter.Configure(goals, starterGoals, population);
            StrategyDebugLogger.Info("Bootstrap", "ProgressionReady");
        }
    }
}
