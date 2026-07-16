using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyStarterGoalSequenceController : MonoBehaviour
    {
        private StrategyGoalsController goals;
        private StrategyBuildMenuController buildMenu;
        private StrategyBuildPlacementController placement;
        private StrategyStarterGoalPhase phase;
        private int completedHouses;
        private bool foragerCampCompleted;
        private bool lumberjackCampCompleted;
        private bool stonecutterCampCompleted;
        private bool scoutLodgeCompleted;
        private bool storageYardCompleted;
        private bool granaryCompleted;

        public bool IsComplete => phase == StrategyStarterGoalPhase.Complete;

        public void RefreshFromWorld()
        {
            CountExistingBuildings();
            RefreshPhase();
        }

        public void Configure(
            StrategyGoalsController goalsController,
            StrategyBuildMenuController buildMenuController,
            StrategyBuildPlacementController placementController)
        {
            if (placement != null)
            {
                placement.BuildingCompleted -= HandleBuildingCompleted;
            }

            goals = goalsController;
            buildMenu = buildMenuController;
            placement = placementController;

            if (placement != null)
            {
                placement.BuildingCompleted += HandleBuildingCompleted;
            }

            CountExistingBuildings();
            RefreshPhase();
            StrategyDebugLogger.Info(
                "StarterGoals",
                "Configured",
                StrategyDebugLogger.F("houses", completedHouses),
                StrategyDebugLogger.F("foragerCamp", foragerCampCompleted),
                StrategyDebugLogger.F("lumberjackCamp", lumberjackCampCompleted),
                StrategyDebugLogger.F("stonecutterCamp", stonecutterCampCompleted),
                StrategyDebugLogger.F("scoutLodge", scoutLodgeCompleted),
                StrategyDebugLogger.F("storageYard", storageYardCompleted),
                StrategyDebugLogger.F("granary", granaryCompleted));
        }

        private void OnDestroy()
        {
            if (placement != null)
            {
                placement.BuildingCompleted -= HandleBuildingCompleted;
            }
        }

        private void HandleBuildingCompleted(StrategyPlacedBuilding building)
        {
            if (building == null || phase == StrategyStarterGoalPhase.Complete)
            {
                return;
            }

            if (building.Tool == StrategyBuildTool.House)
            {
                completedHouses = Mathf.Min(StrategyStarterBuildProgression.TargetHouseCount, completedHouses + 1);
                StrategyDebugLogger.Info("StarterGoals", "HouseCompleted", StrategyDebugLogger.F("count", completedHouses));
            }
            else if (building.Tool == StrategyBuildTool.LumberjackCamp)
            {
                lumberjackCampCompleted = true;
                StrategyDebugLogger.Info("StarterGoals", "LumberjackCampCompleted");
            }
            else if (building.Tool == StrategyBuildTool.ForagerCamp)
            {
                foragerCampCompleted = true;
                StrategyDebugLogger.Info("StarterGoals", "ForagerCampCompleted");
            }
            else if (building.Tool == StrategyBuildTool.StonecutterCamp)
            {
                stonecutterCampCompleted = true;
                StrategyDebugLogger.Info("StarterGoals", "StonecutterCampCompleted");
            }
            else if (building.Tool == StrategyBuildTool.ScoutLodge)
            {
                scoutLodgeCompleted = true;
                StrategyDebugLogger.Info("StarterGoals", "ScoutLodgeCompleted");
            }
            else if (building.Tool == StrategyBuildTool.StorageYard)
            {
                storageYardCompleted = true;
                StrategyDebugLogger.Info("StarterGoals", "StorageYardCompleted");
            }
            else if (building.Tool == StrategyBuildTool.Granary)
            {
                granaryCompleted = true;
                StrategyDebugLogger.Info("StarterGoals", "GranaryCompleted");
            }
            else
            {
                return;
            }

            RefreshPhase();
        }

        private void RefreshPhase()
        {
            if (goals == null || buildMenu == null)
            {
                StrategyDebugLogger.Warn(
                    "StarterGoals",
                    "RefreshSkipped",
                    StrategyDebugLogger.F("hasGoals", goals != null),
                    StrategyDebugLogger.F("hasBuildMenu", buildMenu != null));
                return;
            }

            CompleteSatisfiedActiveGoals();
            StrategyStarterGoalPhase nextPhase = StrategyStarterBuildProgression.Evaluate(
                new StrategyStarterBuildProgressState(
                    completedHouses,
                    foragerCampCompleted,
                    lumberjackCampCompleted,
                    stonecutterCampCompleted,
                    scoutLodgeCompleted,
                    storageYardCompleted,
                    granaryCompleted));

            switch (nextPhase)
            {
                case StrategyStarterGoalPhase.Houses:
                    StartHousePhase();
                    return;
                case StrategyStarterGoalPhase.ForagerCamp:
                    StartForagerCampPhase();
                    return;
                case StrategyStarterGoalPhase.ProductionCamps:
                    StartCampPhase();
                    return;
                case StrategyStarterGoalPhase.ScoutLodge:
                    StartScoutLodgePhase();
                    return;
                case StrategyStarterGoalPhase.Storage:
                    StartStoragePhase();
                    return;
                default:
                    CompleteSequence();
                    return;
            }
        }

        private void StartHousePhase()
        {
            phase = StrategyStarterGoalPhase.Houses;
            ApplyBaseToolLock();
            goals.SetGoals(new StrategyGoalDefinition(
                StrategyGoalKind.BuildThreeHouses,
                "Build 3 Houses (" + completedHouses + "/" + StrategyStarterBuildProgression.TargetHouseCount + ")",
                "Secure shelter before expanding production."));
            StrategyDebugLogger.Info("StarterGoals", "HousePhaseReady", StrategyDebugLogger.F("houses", completedHouses));
        }

        private void StartForagerCampPhase()
        {
            phase = StrategyStarterGoalPhase.ForagerCamp;
            ApplyBaseToolLock();
            goals.SetGoals(new StrategyGoalDefinition(
                StrategyGoalKind.BuildForagerCamp,
                "Build Forager Camp",
                "Move forage food production outside homes."));
            StrategyDebugLogger.Info(
                "StarterGoals",
                "ForagerCampPhaseReady",
                StrategyDebugLogger.F("foragerCamp", foragerCampCompleted));
        }

        private void StartCampPhase()
        {
            phase = StrategyStarterGoalPhase.ProductionCamps;
            ApplyBaseToolLock();
            goals.SetGoals(
                new StrategyGoalDefinition(StrategyGoalKind.BuildLumberjackCamp, "Build Lumberjack Camp"),
                new StrategyGoalDefinition(StrategyGoalKind.BuildStonecutterCamp, "Build Stonecutter Camp"));

            if (lumberjackCampCompleted)
            {
                goals.CompleteGoal(StrategyGoalKind.BuildLumberjackCamp);
            }

            if (stonecutterCampCompleted)
            {
                goals.CompleteGoal(StrategyGoalKind.BuildStonecutterCamp);
            }

            StrategyDebugLogger.Info(
                "StarterGoals",
                "CampPhaseReady",
                StrategyDebugLogger.F("lumberjackCamp", lumberjackCampCompleted),
                StrategyDebugLogger.F("stonecutterCamp", stonecutterCampCompleted));
        }

        private void StartScoutLodgePhase()
        {
            phase = StrategyStarterGoalPhase.ScoutLodge;
            ApplyBaseToolLock();
            goals.SetGoals(new StrategyGoalDefinition(
                StrategyGoalKind.BuildScoutLodge,
                "Build Scout Lodge",
                "Raise an expedition base for the settlement's first Scout."));
            StrategyDebugLogger.Info(
                "StarterGoals",
                "ScoutLodgePhaseReady",
                StrategyDebugLogger.F("scoutLodge", scoutLodgeCompleted));
        }

        private void StartStoragePhase()
        {
            phase = StrategyStarterGoalPhase.Storage;
            ApplyBaseToolLock();
            goals.SetGoals(
                new StrategyGoalDefinition(
                    StrategyGoalKind.BuildStorageYard,
                    "Build Storage Yard",
                    "Move construction supplies out of the temporary caravan cart."),
                new StrategyGoalDefinition(
                    StrategyGoalKind.BuildGranary,
                    "Build Granary",
                    "Centralize food reserves before the first winter."));

            if (storageYardCompleted)
            {
                goals.CompleteGoal(StrategyGoalKind.BuildStorageYard);
            }

            if (granaryCompleted)
            {
                goals.CompleteGoal(StrategyGoalKind.BuildGranary);
            }

            StrategyDebugLogger.Info(
                "StarterGoals",
                "StoragePhaseReady",
                StrategyDebugLogger.F("storageYard", storageYardCompleted),
                StrategyDebugLogger.F("granary", granaryCompleted));
        }

        private void CompleteSequence()
        {
            if (phase != StrategyStarterGoalPhase.Complete)
            {
                CompleteSatisfiedActiveGoals();
            }

            phase = StrategyStarterGoalPhase.Complete;
            buildMenu.ClearAllowedTools();
            StrategyDebugLogger.Info("StarterGoals", "SequenceCompleted");
        }

        private void ApplyBaseToolLock()
        {
            buildMenu.SetAllowedTools(StrategyStarterBuildProgression.BaseTools);
        }

        private void CountExistingBuildings()
        {
            completedHouses = 0;
            foragerCampCompleted = false;
            lumberjackCampCompleted = false;
            stonecutterCampCompleted = false;
            scoutLodgeCompleted = false;
            storageYardCompleted = false;
            granaryCompleted = false;

            IReadOnlyList<StrategyPlacedBuilding> buildings = placement != null ? placement.PlacedBuildings : null;
            if (buildings == null)
            {
                return;
            }

            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building == null)
                {
                    continue;
                }

                if (building.Tool == StrategyBuildTool.House)
                {
                    completedHouses = Mathf.Min(StrategyStarterBuildProgression.TargetHouseCount, completedHouses + 1);
                }
                else if (building.Tool == StrategyBuildTool.LumberjackCamp)
                {
                    lumberjackCampCompleted = true;
                }
                else if (building.Tool == StrategyBuildTool.ForagerCamp)
                {
                    foragerCampCompleted = true;
                }
                else if (building.Tool == StrategyBuildTool.StonecutterCamp)
                {
                    stonecutterCampCompleted = true;
                }
                else if (building.Tool == StrategyBuildTool.ScoutLodge)
                {
                    scoutLodgeCompleted = true;
                }
                else if (building.Tool == StrategyBuildTool.StorageYard)
                {
                    storageYardCompleted = true;
                }
                else if (building.Tool == StrategyBuildTool.Granary)
                {
                    granaryCompleted = true;
                }
            }
        }

        private void CompleteSatisfiedActiveGoals()
        {
            if (completedHouses >= StrategyStarterBuildProgression.TargetHouseCount)
            {
                CompleteGoalIfActive(StrategyGoalKind.BuildThreeHouses);
            }

            if (foragerCampCompleted)
            {
                CompleteGoalIfActive(StrategyGoalKind.BuildForagerCamp);
            }

            if (lumberjackCampCompleted)
            {
                CompleteGoalIfActive(StrategyGoalKind.BuildLumberjackCamp);
            }

            if (stonecutterCampCompleted)
            {
                CompleteGoalIfActive(StrategyGoalKind.BuildStonecutterCamp);
            }

            if (scoutLodgeCompleted)
            {
                CompleteGoalIfActive(StrategyGoalKind.BuildScoutLodge);
            }

            if (storageYardCompleted)
            {
                CompleteGoalIfActive(StrategyGoalKind.BuildStorageYard);
            }

            if (granaryCompleted)
            {
                CompleteGoalIfActive(StrategyGoalKind.BuildGranary);
            }
        }

        private void CompleteGoalIfActive(StrategyGoalKind kind)
        {
            if (goals != null && goals.IsGoalActive(kind))
            {
                goals.CompleteGoal(kind);
            }
        }

    }
}
