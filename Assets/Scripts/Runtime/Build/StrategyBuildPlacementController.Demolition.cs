using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyBuildPlacementController
    {
        private readonly List<StrategyPlacedBuilding> pendingBuildingDemolitions = new();

        private bool QueueBuildingDemolition(StrategyPlacedBuilding building)
        {
            if (building == null
                || map == null
                || building.IsDemolishing
                || !building.BeginDemolition())
            {
                return false;
            }

            pendingBuildingDemolitions.Add(building);
            StrategyDebugLogger.Info(
                "Build",
                "BuildingDemolitionQueued",
                StrategyDebugLogger.F("tool", building.Tool),
                StrategyDebugLogger.F("origin", building.Origin));
            return true;
        }

        private void LateUpdate()
        {
            FlushPendingBuildingDemolitions();
        }

        public void FlushPendingBuildingDemolitions()
        {
            for (int i = pendingBuildingDemolitions.Count - 1; i >= 0; i--)
            {
                StrategyPlacedBuilding building = pendingBuildingDemolitions[i];
                pendingBuildingDemolitions.RemoveAt(i);
                if (building != null)
                {
                    DemolishBuildingImmediately(building, true);
                }
            }
        }

        private bool DemolishBuildingImmediately(
            StrategyPlacedBuilding building,
            bool leaveStoredResources)
        {
            if (building == null || map == null)
            {
                return false;
            }

            if (!building.IsDemolishing)
            {
                building.BeginDemolition();
            }

            StrategyBuildTool tool = building.Tool;
            Vector2Int origin = building.Origin;
            if (tool == StrategyBuildTool.House)
            {
                population?.UnregisterHouse(building);
                building.DetachResidentsForDemolition();
            }

            StrategyBuildingResourceManifest manifest =
                StrategyBuildingResourceManifest.Capture(building);
            AddProductionWorkInProgress(building, manifest);

            int droppedAmount = 0;
            if (leaveStoredResources && manifest.HasAny)
            {
                droppedAmount = manifest.TotalAmount;
                SpawnBuildingResourceDrops(building, manifest);
            }

            manifest.ClearCapturedStores();
            ClearBuildingSpecialInventory(building);

            if (tool == StrategyBuildTool.Bridge && building.BridgeCells.Count > 0)
            {
                UnmarkOccupiedCells(building.BridgeCells);
                map.SetBridgeCellsWalkable(building.BridgeCells, false);
            }
            else
            {
                GetWalkBlockFootprint(
                    tool,
                    building.Origin,
                    building.Footprint,
                    out Vector2Int blockOrigin,
                    out Vector2Int blockFootprint);
                UnmarkOccupied(blockOrigin, blockFootprint);
                map.SetCellsWalkable(blockOrigin, blockFootprint, true);
            }

            placedBuildings.Remove(building);
            Destroy(building.gameObject);
            fog?.RequestRefresh();
            StrategyDebugLogger.Info(
                "Build",
                "BuildingDemolished",
                StrategyDebugLogger.F("tool", tool),
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("resourcesDropped", droppedAmount),
                StrategyDebugLogger.F("leaveStoredResources", leaveStoredResources),
                StrategyDebugLogger.F("placedCount", placedBuildings.Count));
            return true;
        }

        private static void AddProductionWorkInProgress(
            StrategyPlacedBuilding building,
            StrategyBuildingResourceManifest manifest)
        {
            if (building.TryGetComponent(out StrategySawmill sawmill))
            {
                manifest.Add(StrategyResourceType.Planks, sawmill.PendingPlanksForDemolition);
            }

            if (building.TryGetComponent(out StrategyKiln kiln))
            {
                manifest.Add(StrategyResourceType.Pottery, kiln.PendingPotteryForDemolition);
            }

            if (building.TryGetComponent(out StrategyForge forge))
            {
                manifest.Add(StrategyResourceType.Tools, forge.PendingToolsForDemolition);
            }

        }

        private static void ClearBuildingSpecialInventory(StrategyPlacedBuilding building)
        {
            building.Resources?.RestorePreparedDishState(null, null, 0f);
            if (building.TryGetComponent(out StrategySawmill sawmill))
            {
                sawmill.ClearPendingPlanksForDemolition();
            }

            if (building.TryGetComponent(out StrategyKiln kiln))
            {
                kiln.ClearPendingPotteryForDemolition();
            }

            if (building.TryGetComponent(out StrategyForge forge))
            {
                forge.ClearPendingToolsForDemolition();
            }
        }

        private void SpawnBuildingResourceDrops(
            StrategyPlacedBuilding building,
            StrategyBuildingResourceManifest manifest)
        {
            int dropIndex = 0;
            int logs = manifest.GetAmount(StrategyResourceType.Logs);
            int stone = manifest.GetAmount(StrategyResourceType.Stone);
            int planks = manifest.GetAmount(StrategyResourceType.Planks);
            if (logs > 0 || stone > 0 || planks > 0)
            {
                GetDropPlacement(building, dropIndex++, out Vector2Int cell, out Vector3 world);
                StrategyLooseConstructionResourcePile.Create(
                    map,
                    cell,
                    world,
                    logs,
                    stone,
                    planks);
            }

            foreach (StrategyResourceType resource in Enum.GetValues(typeof(StrategyResourceType)))
            {
                if (resource == StrategyResourceType.None
                    || resource == StrategyResourceType.Logs
                    || resource == StrategyResourceType.Stone
                    || resource == StrategyResourceType.Planks)
                {
                    continue;
                }

                int amount = manifest.GetAmount(resource);
                if (amount <= 0)
                {
                    continue;
                }

                GetDropPlacement(building, dropIndex++, out Vector2Int cell, out Vector3 world);
                StrategyLooseCarriedResourcePile.Create(map, cell, world, resource, amount);
            }

            IReadOnlyList<StrategyPreparedDishStack> preparedDishes = manifest.PreparedDishStacks;
            for (int i = 0; i < preparedDishes.Count; i++)
            {
                StrategyPreparedDishStack stack = preparedDishes[i];
                if (stack.Recipe == null || stack.Amount <= 0)
                {
                    continue;
                }

                GetDropPlacement(building, dropIndex++, out Vector2Int cell, out Vector3 world);
                StrategyLooseCarriedResourcePile.CreatePreparedDishes(
                    map,
                    cell,
                    world,
                    stack.Recipe.Id,
                    stack.Amount,
                    0f);
            }

            if (manifest.PreparedDishLeftoverRations > 0f)
            {
                GetDropPlacement(building, dropIndex, out Vector2Int cell, out Vector3 world);
                StrategyLooseCarriedResourcePile.CreatePreparedDishes(
                    map,
                    cell,
                    world,
                    string.Empty,
                    0,
                    manifest.PreparedDishLeftoverRations);
            }
        }

        private void GetDropPlacement(
            StrategyPlacedBuilding building,
            int dropIndex,
            out Vector2Int cell,
            out Vector3 world)
        {
            if (building.Tool == StrategyBuildTool.Bridge && building.BridgeCells.Count > 0)
            {
                cell = building.BridgeStartCell;
            }
            else
            {
                int width = Mathf.Max(1, building.Footprint.x);
                int height = Mathf.Max(1, building.Footprint.y);
                int cellIndex = dropIndex % (width * height);
                cell = building.Origin + new Vector2Int(cellIndex % width, cellIndex / width);
            }

            world = map.GetCellRectWorld(cell, Vector2Int.one).center;
        }
    }

    public sealed class StrategyBuildingResourceManifest
    {
        private static readonly int ResourceCount =
            Enum.GetValues(typeof(StrategyResourceType)).Length;

        private readonly int[] amounts = new int[ResourceCount];
        private readonly List<StrategyResourceStore> capturedStores = new();
        private readonly List<StrategyPreparedDishStack> preparedDishStacks = new();
        private float preparedDishLeftoverRations;

        public bool HasAny => TotalAmount > 0;
        public IReadOnlyList<StrategyPreparedDishStack> PreparedDishStacks => preparedDishStacks;
        public float PreparedDishLeftoverRations => preparedDishLeftoverRations;

        public int TotalAmount
        {
            get
            {
                int total = 0;
                for (int i = 1; i < amounts.Length; i++)
                {
                    total = SaturatingAdd(total, amounts[i]);
                }

                for (int i = 0; i < preparedDishStacks.Count; i++)
                {
                    total = SaturatingAdd(total, preparedDishStacks[i].Amount);
                }

                if (preparedDishLeftoverRations > 0f)
                {
                    total = SaturatingAdd(total, 1);
                }

                return total;
            }
        }

        public static StrategyBuildingResourceManifest Capture(StrategyPlacedBuilding building)
        {
            StrategyBuildingResourceManifest manifest = new();
            if (building == null)
            {
                return manifest;
            }

            MonoBehaviour[] components = building.GetComponents<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is IStrategyResourceStoreOwner owner)
                {
                    manifest.CaptureStore(owner.ResourceStore);
                }
            }

            StrategyHouseResourceStore houseStore = building.Resources;
            if (houseStore != null)
            {
                houseStore.CopyPreparedDishStacks(manifest.preparedDishStacks);
                manifest.preparedDishLeftoverRations = Mathf.Max(0f, houseStore.LeftoverRations);
            }

            return manifest;
        }

        public static StrategyBuildingResourceManifest Capture(
            IReadOnlyList<StrategyResourceStore> stores)
        {
            StrategyBuildingResourceManifest manifest = new();
            if (stores == null)
            {
                return manifest;
            }

            for (int i = 0; i < stores.Count; i++)
            {
                manifest.CaptureStore(stores[i]);
            }

            return manifest;
        }

        public int GetAmount(StrategyResourceType resource)
        {
            int index = (int)resource;
            return index > 0 && index < amounts.Length ? amounts[index] : 0;
        }

        public void Add(StrategyResourceType resource, int amount)
        {
            int index = (int)resource;
            if (index <= 0 || index >= amounts.Length || amount <= 0)
            {
                return;
            }

            amounts[index] = SaturatingAdd(amounts[index], amount);
        }

        public void ClearCapturedStores()
        {
            for (int i = 0; i < capturedStores.Count; i++)
            {
                capturedStores[i]?.RestoreAmounts(null);
            }
        }

        private void CaptureStore(StrategyResourceStore store)
        {
            if (store == null || capturedStores.Contains(store))
            {
                return;
            }

            capturedStores.Add(store);
            int[] snapshot = store.CaptureAmounts();
            int count = Mathf.Min(snapshot.Length, amounts.Length);
            for (int i = 1; i < count; i++)
            {
                Add((StrategyResourceType)i, snapshot[i]);
            }
        }

        private static int SaturatingAdd(int left, int right)
        {
            long sum = (long)Mathf.Max(0, left) + Mathf.Max(0, right);
            return sum >= int.MaxValue ? int.MaxValue : (int)sum;
        }
    }
}
