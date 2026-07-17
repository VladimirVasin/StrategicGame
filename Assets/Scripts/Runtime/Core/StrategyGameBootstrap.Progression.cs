using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static void ConfigureProgression(
            StrategyGameContext context,
            CityMapController map,
            StrategyBuildMenuController buildMenu,
            StrategyBuildPlacementController placement,
            StrategyPopulationController population,
            StrategyCameraController cameraController,
            StrategyWorldSelectionController selection,
            StrategyProfessionHudController professionHud,
            StrategySettlementFaunaController settlementFauna,
            StrategyTimeScaleController timeScale,
            StrategyInputRouter inputRouter)
        {
            StrategyGoalsHudController goalsHud = context.GetOrCreate<StrategyGoalsHudController>("Strategy Goals HUD");
            StrategyGoalsController goals = context.GetOrCreate<StrategyGoalsController>("Strategy Goals");

            goals.Configure(goalsHud);
            StrategyStarterGoalSequenceController starterGoals = context.GetOrCreate<StrategyStarterGoalSequenceController>("Strategy Starter Goals");

            starterGoals.Configure(goals, buildMenu, placement);

            StrategyScoutAssignmentDialogController scoutAssignmentDialog =
                context.GetOrCreate<StrategyScoutAssignmentDialogController>("Strategy Scout Assignment Dialog");
            scoutAssignmentDialog.SetInputRouter(inputRouter);
            scoutAssignmentDialog.Configure();
            StrategyScoutLodgeOnboardingController scoutOnboarding =
                context.GetOrCreate<StrategyScoutLodgeOnboardingController>("Strategy Scout Lodge Onboarding");
            scoutOnboarding.Configure(
                placement,
                population,
                cameraController,
                selection,
                scoutAssignmentDialog,
                timeScale,
                inputRouter);
            selection.SetScoutLodgeOnboarding(scoutOnboarding);
            professionHud.SetScoutLodgeOnboarding(scoutOnboarding);

            StrategyFirstNightFaunaStoryController firstNightFaunaStory =
                context.GetOrCreate<StrategyFirstNightFaunaStoryController>("Strategy First Night Fauna Story");
            firstNightFaunaStory.Configure(timeScale, inputRouter);
            StrategyInGameCinematicPlayer inGameCinematicPlayer =
                context.GetOrCreate<StrategyInGameCinematicPlayer>("Strategy In-Game Cinematic Player");
            inGameCinematicPlayer.Configure(cameraController, timeScale, inputRouter);
            StrategyFirstNightFaunaEventController firstNightFaunaEvent =
                context.GetOrCreate<StrategyFirstNightFaunaEventController>("Strategy First Night Fauna Event");
            firstNightFaunaEvent.Configure(
                settlementFauna,
                firstNightFaunaStory,
                inGameCinematicPlayer,
                population,
                map);

            StrategyFirstWinterController firstWinter = context.GetOrCreate<StrategyFirstWinterController>("Strategy First Winter Progression");

            firstWinter.Configure(goals, starterGoals, population);
            StrategyDebugLogger.Info("Bootstrap", "ProgressionReady");
        }
    }
}
