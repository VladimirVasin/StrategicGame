using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static void ConfigureProgression(
            StrategyGameContext context,
            StrategyBuildMenuController buildMenu,
            StrategyBuildPlacementController placement,
            StrategyPopulationController population)
        {
            StrategyGoalsHudController goalsHud = context.GetOrCreate<StrategyGoalsHudController>("Strategy Goals HUD");
            StrategyGoalsController goals = context.GetOrCreate<StrategyGoalsController>("Strategy Goals");

            goals.Configure(goalsHud);
            StrategyStarterGoalSequenceController starterGoals = context.GetOrCreate<StrategyStarterGoalSequenceController>("Strategy Starter Goals");

            starterGoals.Configure(goals, buildMenu, placement);

            StrategyFirstWinterController firstWinter = context.GetOrCreate<StrategyFirstWinterController>("Strategy First Winter Progression");

            firstWinter.Configure(goals, starterGoals, population);
            StrategyDebugLogger.Info("Bootstrap", "ProgressionReady");
        }
    }
}
