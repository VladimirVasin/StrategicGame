using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        private static void UpdateMainMenuLaunchSmoke()
        {
            if (StrategySceneCatalog.IsMainMenuScene(EditorSceneManager.GetActiveScene()))
            {
                StrategyMapPreloadCoordinator preloader =
                    Object.FindAnyObjectByType<StrategyMapPreloadCoordinator>();
                Require(preloader != null, "Menu preloader failed before launch");
                VerifyMainMenuLaunchWatchdog(preloader);
                if (!launchRequestedBySmoke)
                {
                    Require(preloader.RequestNewSettlement(), "New settlement launch request was rejected");
                    launchRequestedBySmoke = true;
                }

                return;
            }

            if (StrategySceneCatalog.IsFoundingJourneyScene(EditorSceneManager.GetActiveScene()))
            {
                StrategyMapPreloadCoordinator preloader = StrategyMapPreloadCoordinator.Active;
                StrategyFoundingJourneyController journey =
                    Object.FindAnyObjectByType<StrategyFoundingJourneyController>();
                Require(preloader != null, "Map preloader did not survive into the founding journey");
                Require(journey != null && journey.IsConfigured, "Founding journey bootstrap failed");
                Require(
                    Object.FindAnyObjectByType<StrategyFoundingJourneyPresentation>() != null,
                    "Founding journey cinematic presentation was not created");
                Require(
                    Object.FindAnyObjectByType<StrategyFoundingJourneyAudio>() != null,
                    "Founding journey ambience controller was not created");
                Require(
                    Object.FindObjectsByType<StrategyUiButtonFeedback>(FindObjectsInactive.Include).Length >= 9,
                    "Founding journey button feedback was not fully attached");
                Require(StrategyGameContext.Current == null, "Gameplay bootstrapped during the founding journey");
                VerifyMainMenuLaunchWatchdog(preloader);
                if (preloader.Phase == StrategyPreloadPhase.AwaitingFoundingDecision
                    && !journey.IsLaunching)
                {
                    Require(
                        journey.BeginBalancedSettlementForVerification(),
                        "Founding journey could not resolve its balanced start profile");
                }

                return;
            }

            Require(StrategySceneCatalog.IsGameplayScene(EditorSceneManager.GetActiveScene()), "Launch opened an unexpected scene");
            StrategyGameContext context = StrategyGameContext.Current;
            Require(context == null || context.State != StrategyGameContextState.Failed, "Prepared gameplay bootstrap failed: " + context?.FailureReason);
            if (context == null || !context.IsReady)
            {
                VerifyGameplayBootstrapWatchdog(context);
                return;
            }

            if (++gameplayFramesAfterLaunch < RequiredPlayFrames)
            {
                return;
            }

            CityMapController map = Object.FindAnyObjectByType<CityMapController>();
            StrategyPopulationController population = Object.FindAnyObjectByType<StrategyPopulationController>();
            Require(map != null && map.IsGenerated, "Prepared map was not accepted by gameplay");
            Require(population != null && population.TotalResidentCount > 0, "Gameplay did not start after menu launch");
            StrategyFoundingStartState foundingStart = map.GetComponent<StrategyFoundingStartState>();
            Require(foundingStart != null && foundingStart.HasCampCell, "Founding start state was not transferred with the prepared map");
            Require(
                foundingStart.Preferences != null
                    && foundingStart.Preferences.ProfileId == StrategyFoundingChoiceIds.BalancedProfile,
                "Founding preferences were not preserved through gameplay bootstrap");
            Require(
                population.TryGetCampCell(out Vector2Int campCell)
                    && campCell == foundingStart.CampCell,
                "Gameplay camp did not use the selected founding start cell");
            VerifyFoundingCaravanOrigin(foundingStart);
            Require(StrategyMapPreloadCoordinator.Active == null, "Menu preloader survived gameplay bootstrap");
            VerifyRuntimeInput(context);
            CompletePlayMode(true, "PASS: menu launched prepared gameplay through founding journey");
        }

        private static void VerifyFoundingCaravanOrigin(StrategyFoundingStartState foundingStart)
        {
            if (!foundingStart.HasCaravanOrigin)
            {
                return;
            }

            StrategyBuildPlacementController placement =
                Object.FindAnyObjectByType<StrategyBuildPlacementController>();
            Require(placement != null, "Build placement was missing after founding launch");
            for (int i = 0; i < placement.PlacedBuildings.Count; i++)
            {
                StrategyPlacedBuilding building = placement.PlacedBuildings[i];
                if (building != null
                    && building.Tool == StrategyBuildTool.StarterCaravanCart
                    && building.Origin == foundingStart.CaravanOrigin)
                {
                    VerifyNoFoundingForageOverlap(foundingStart);
                    return;
                }
            }

            Require(false, "Starter caravan did not use its reserved founding origin");
        }

        private static void VerifyNoFoundingForageOverlap(StrategyFoundingStartState foundingStart)
        {
            StrategyForageResourceController forage =
                Object.FindAnyObjectByType<StrategyForageResourceController>();
            Require(forage != null, "Forage resources were missing after founding launch");
            RectInt reserved = new RectInt(
                foundingStart.CaravanOrigin,
                StrategyStartSiteSelector.CaravanReservedFootprint);
            for (int i = 0; i < forage.Nodes.Count; i++)
            {
                StrategyForageNode node = forage.Nodes[i];
                Require(
                    node == null || !reserved.Contains(node.Cell),
                    "Forage generated inside the reserved founding caravan footprint");
            }
        }
    }
}
