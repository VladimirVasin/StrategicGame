using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategySettlementFaunaController : MonoBehaviour
    {
        private const float PopulationRefreshSeconds = 4f;
        private const int MouseBuildingThreshold = 3;
        private const int CatBuildingThreshold = 5;
        private const int CatHouseThreshold = 3;
        private const int MaxCats = 6;
        private const int MaxMice = 20;

        private CityMapController map;
        private StrategyPopulationController population;
        private StrategyFogOfWarController fog;
        private StrategyBuildPlacementController placement;
        private Transform faunaRoot;
        private StrategySettlementFaunaTargets targets;
        private StrategyFirstNightFaunaStage firstNightStage = StrategyFirstNightFaunaStage.Dormant;
        private float refreshTimer;
        private bool dirty = true;

        public static StrategySettlementFaunaController Active { get; private set; }
        public StrategySettlementFaunaTargets Targets => targets;
        public StrategyFirstNightFaunaStage Stage => firstNightStage;
        public int LiveMouseCount => mice.Count;
        public int LiveCatCount => cats.Count;

        public void ResetForWorldRestore()
        {
            ClearFaunaPopulation();
            dirty = true;
            refreshTimer = 0f;
        }

        public void SetFirstNightStage(StrategyFirstNightFaunaStage stage)
        {
            int stageValue = (int)stage;
            if (stageValue < (int)StrategyFirstNightFaunaStage.Dormant
                || stageValue > (int)StrategyFirstNightFaunaStage.StoryCompleted)
            {
                StrategyDebugLogger.Warn(
                    "SettlementFauna",
                    "FirstNightStageRejected",
                    StrategyDebugLogger.F("stage", stageValue));
                return;
            }

            StrategyFirstNightFaunaStage previous = firstNightStage;
            firstNightStage = stage;
            dirty = true;
            refreshTimer = 0f;
            if (map != null)
            {
                RefreshPopulationTargets();
                UpdateFaunaPopulation();
            }

            if (previous != firstNightStage)
            {
                StrategyDebugLogger.Info(
                    "SettlementFauna",
                    "FirstNightStageChanged",
                    StrategyDebugLogger.F("previous", previous),
                    StrategyDebugLogger.F("current", firstNightStage),
                    StrategyDebugLogger.F("targetCats", targets.TargetCats),
                    StrategyDebugLogger.F("targetMice", targets.TargetMice));
            }
        }

        public void Configure(
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyFogOfWarController fogController,
            StrategyBuildPlacementController placementController)
        {
            if (placement != null)
            {
                placement.BuildingCompleted -= HandleBuildingCompleted;
            }

            map = mapController;
            population = populationController;
            fog = fogController;
            placement = placementController;
            if (placement != null)
            {
                placement.BuildingCompleted += HandleBuildingCompleted;
            }

            EnsureFaunaRoot();
            dirty = true;
            refreshTimer = 0f;
            RefreshPopulationTargets();
            StrategyDebugLogger.Info(
                "SettlementFauna",
                "Configured",
                StrategyDebugLogger.F("buildings", targets.CompletedBuildings),
                StrategyDebugLogger.F("occupiedHouses", targets.OccupiedHouses),
                StrategyDebugLogger.F("foodBuildings", targets.FoodBuildings),
                StrategyDebugLogger.F("targetCats", targets.TargetCats),
                StrategyDebugLogger.F("targetMice", targets.TargetMice));
        }

        private void Awake()
        {
            Active = this;
        }

        private void OnDestroy()
        {
            if (placement != null)
            {
                placement.BuildingCompleted -= HandleBuildingCompleted;
            }

            if (Active == this)
            {
                Active = null;
            }
        }

        private void Update()
        {
            if (map == null || Time.timeScale <= 0f)
            {
                return;
            }

            refreshTimer -= Time.deltaTime;
            if (!dirty && refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = PopulationRefreshSeconds;
            dirty = false;
            RefreshPopulationTargets();
            UpdateFaunaPopulation();
        }

        private void HandleBuildingCompleted(StrategyPlacedBuilding building)
        {
            if (building != null)
            {
                dirty = true;
            }
        }

        private void RefreshPopulationTargets()
        {
            IReadOnlyList<StrategyPlacedBuilding> buildings = StrategyPlacedBuilding.ActiveBuildings;
            int completed = 0;
            int occupiedHouses = 0;
            int foodBuildings = 0;
            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building == null || building.Tool == StrategyBuildTool.Bridge)
                {
                    continue;
                }

                completed++;
                if (building.Tool == StrategyBuildTool.House && building.ResidentCount > 0)
                {
                    occupiedHouses++;
                }

                if (IsMouseFoodBuilding(building))
                {
                    foodBuildings++;
                }
            }

            int rawMouseTarget = 2 + completed / 4 + foodBuildings;
            int targetMice = completed >= MouseBuildingThreshold && foodBuildings > 0
                ? Mathf.Clamp(Mathf.CeilToInt(rawMouseTarget / 3f), 1, MaxMice / 3)
                : 0;
            int targetCats = completed >= CatBuildingThreshold
                && occupiedHouses >= CatHouseThreshold
                && targetMice > 0
                    ? Mathf.Clamp(1 + completed / 8, 1, MaxCats)
                    : 0;
            StrategySettlementFaunaTargets organicTargets = new StrategySettlementFaunaTargets(
                completed,
                occupiedHouses,
                foodBuildings,
                targetCats,
                targetMice);
            StrategySettlementFaunaTargets next = StrategySettlementFaunaPolicy.ApplyFirstNightStage(
                organicTargets,
                firstNightStage);
            if (next.TargetCats != targets.TargetCats || next.TargetMice != targets.TargetMice)
            {
                StrategyDebugLogger.Info(
                    "SettlementFauna",
                    "PopulationTargetsChanged",
                    StrategyDebugLogger.F("buildings", completed),
                    StrategyDebugLogger.F("occupiedHouses", occupiedHouses),
                    StrategyDebugLogger.F("foodBuildings", foodBuildings),
                    StrategyDebugLogger.F("targetCats", next.TargetCats),
                    StrategyDebugLogger.F("targetMice", next.TargetMice));
            }

            targets = next;
        }

        private static bool IsMouseFoodBuilding(StrategyPlacedBuilding building)
        {
            return building.Tool == StrategyBuildTool.Granary
                || building.Tool == StrategyBuildTool.StorageYard
                || building.Tool == StrategyBuildTool.ChickenCoop
                || building.Tool == StrategyBuildTool.FisherHut
                || building.Tool == StrategyBuildTool.HunterCamp
                || building.Tool == StrategyBuildTool.ForagerCamp
                || building.Tool == StrategyBuildTool.StarterCaravanCart
                || building.Tool == StrategyBuildTool.House && building.Resources != null;
        }

        private void EnsureFaunaRoot()
        {
            if (faunaRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("Settlement Fauna");
            root.transform.SetParent(transform, false);
            faunaRoot = root.transform;
        }

        partial void UpdateFaunaPopulation();
        partial void ClearFaunaPopulation();
    }
}
