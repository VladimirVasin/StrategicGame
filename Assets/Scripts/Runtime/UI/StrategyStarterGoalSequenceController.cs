using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyStarterGoalSequenceController : MonoBehaviour
    {
        private const int TargetHouseCount = 3;

        private StrategyGoalsController goals;
        private StrategyBuildMenuController buildMenu;
        private StrategyBuildPlacementController placement;
        private StarterGoalPhase phase;
        private int completedHouses;
        private bool foragerCampCompleted;
        private bool lumberjackCampCompleted;
        private bool stonecutterCampCompleted;

        public bool IsComplete => phase == StarterGoalPhase.Complete;

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
                StrategyDebugLogger.F("stonecutterCamp", stonecutterCampCompleted));
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
            if (building == null || phase == StarterGoalPhase.Complete)
            {
                return;
            }

            if (building.Tool == StrategyBuildTool.House)
            {
                completedHouses = Mathf.Min(TargetHouseCount, completedHouses + 1);
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

            if (completedHouses < TargetHouseCount)
            {
                StartHousePhase();
                return;
            }

            if (!foragerCampCompleted)
            {
                if (phase == StarterGoalPhase.Houses)
                {
                    goals.CompleteGoal(StrategyGoalKind.BuildThreeHouses);
                }

                StartForagerCampPhase();
                return;
            }

            if (!lumberjackCampCompleted || !stonecutterCampCompleted)
            {
                if (phase == StarterGoalPhase.Houses)
                {
                    goals.CompleteGoal(StrategyGoalKind.BuildThreeHouses);
                }
                else if (phase == StarterGoalPhase.ForagerCamp)
                {
                    goals.CompleteGoal(StrategyGoalKind.BuildForagerCamp);
                }

                StartCampPhase();
                return;
            }

            CompleteSequence();
        }

        private void StartHousePhase()
        {
            phase = StarterGoalPhase.Houses;
            buildMenu.SetAllowedTools(new[] { StrategyBuildTool.House });
            goals.SetGoals(new StrategyGoalDefinition(
                StrategyGoalKind.BuildThreeHouses,
                "Build 3 Houses (" + completedHouses + "/" + TargetHouseCount + ")",
                "Secure shelter before expanding production."));
            StrategyDebugLogger.Info("StarterGoals", "HousePhaseReady", StrategyDebugLogger.F("houses", completedHouses));
        }

        private void StartForagerCampPhase()
        {
            phase = StarterGoalPhase.ForagerCamp;
            buildMenu.SetAllowedTools(new[] { StrategyBuildTool.ForagerCamp });
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
            phase = StarterGoalPhase.ProductionCamps;
            buildMenu.SetAllowedTools(new[] { StrategyBuildTool.LumberjackCamp, StrategyBuildTool.StonecutterCamp });
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

        private void CompleteSequence()
        {
            if (phase != StarterGoalPhase.Complete)
            {
                CompleteGoalIfActive(StrategyGoalKind.BuildForagerCamp);
                CompleteGoalIfActive(StrategyGoalKind.BuildLumberjackCamp);
                CompleteGoalIfActive(StrategyGoalKind.BuildStonecutterCamp);
            }

            phase = StarterGoalPhase.Complete;
            buildMenu.ClearAllowedTools();
            StrategyDebugLogger.Info("StarterGoals", "SequenceCompleted");
        }

        private void CountExistingBuildings()
        {
            completedHouses = 0;
            foragerCampCompleted = false;
            lumberjackCampCompleted = false;
            stonecutterCampCompleted = false;

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
                    completedHouses = Mathf.Min(TargetHouseCount, completedHouses + 1);
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
            }
        }

        private void CompleteGoalIfActive(StrategyGoalKind kind)
        {
            if (goals != null && goals.IsGoalActive(kind))
            {
                goals.CompleteGoal(kind);
            }
        }

        private enum StarterGoalPhase
        {
            None,
            Houses,
            ForagerCamp,
            ProductionCamps,
            Complete
        }
    }
}
