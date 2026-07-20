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
        private bool languageSubscribed;

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

            if (!languageSubscribed)
            {
                StrategyLocalization.LanguageChanged += HandleLanguageChanged;
                languageSubscribed = true;
            }

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

            if (languageSubscribed)
            {
                StrategyLocalization.LanguageChanged -= HandleLanguageChanged;
                languageSubscribed = false;
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
            goals.SetGoals(CreateHouseGoal());
            StrategyDebugLogger.Info("StarterGoals", "HousePhaseReady", StrategyDebugLogger.F("houses", completedHouses));
        }

        private void StartForagerCampPhase()
        {
            phase = StrategyStarterGoalPhase.ForagerCamp;
            ApplyBaseToolLock();
            goals.SetGoals(CreateForagerCampGoal());
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
                CreateLumberjackCampGoal(),
                CreateStonecutterCampGoal());

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
            goals.SetGoals(CreateScoutLodgeGoal());
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
                CreateStorageYardGoal(),
                CreateGranaryGoal());

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

        private void HandleLanguageChanged()
        {
            switch (phase)
            {
                case StrategyStarterGoalPhase.Houses:
                    goals?.ReplaceGoalText(CreateHouseGoal());
                    break;
                case StrategyStarterGoalPhase.ForagerCamp:
                    goals?.ReplaceGoalText(CreateForagerCampGoal());
                    break;
                case StrategyStarterGoalPhase.ProductionCamps:
                    goals?.ReplaceGoalText(CreateLumberjackCampGoal(), CreateStonecutterCampGoal());
                    break;
                case StrategyStarterGoalPhase.ScoutLodge:
                    goals?.ReplaceGoalText(CreateScoutLodgeGoal());
                    break;
                case StrategyStarterGoalPhase.Storage:
                    goals?.ReplaceGoalText(CreateStorageYardGoal(), CreateGranaryGoal());
                    break;
            }
        }

        private StrategyGoalDefinition CreateHouseGoal()
        {
            return new StrategyGoalDefinition(
                StrategyGoalKind.BuildThreeHouses,
                H(
                    "goals.starter.houses.title",
                    completedHouses,
                    StrategyStarterBuildProgression.TargetHouseCount),
                H("goals.starter.houses.description"));
        }

        private static StrategyGoalDefinition CreateForagerCampGoal()
        {
            return new StrategyGoalDefinition(
                StrategyGoalKind.BuildForagerCamp,
                H("goals.starter.forager_camp.title"),
                H("goals.starter.forager_camp.description"));
        }

        private static StrategyGoalDefinition CreateLumberjackCampGoal()
        {
            return new StrategyGoalDefinition(
                StrategyGoalKind.BuildLumberjackCamp,
                H("goals.starter.lumberjack_camp.title"));
        }

        private static StrategyGoalDefinition CreateStonecutterCampGoal()
        {
            return new StrategyGoalDefinition(
                StrategyGoalKind.BuildStonecutterCamp,
                H("goals.starter.stonecutter_camp.title"));
        }

        private static StrategyGoalDefinition CreateScoutLodgeGoal()
        {
            return new StrategyGoalDefinition(
                StrategyGoalKind.BuildScoutLodge,
                H("goals.starter.scout_lodge.title"),
                H("goals.starter.scout_lodge.description"));
        }

        private static StrategyGoalDefinition CreateStorageYardGoal()
        {
            return new StrategyGoalDefinition(
                StrategyGoalKind.BuildStorageYard,
                H("goals.starter.storage_yard.title"),
                H("goals.starter.storage_yard.description"));
        }

        private static StrategyGoalDefinition CreateGranaryGoal()
        {
            return new StrategyGoalDefinition(
                StrategyGoalKind.BuildGranary,
                H("goals.starter.granary.title"),
                H("goals.starter.granary.description"));
        }

        private static string H(string key, params object[] arguments)
        {
            return StrategyLocalization.Get(StrategyLocalizationTables.Hud, key, arguments);
        }

    }
}
