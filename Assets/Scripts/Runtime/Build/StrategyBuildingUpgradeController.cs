using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyBuildingUpgradeInstallFailureReason
    {
        None,
        InvalidTarget,
        AlreadyInstalled,
        NotEnoughResources,
        NoSpace
    }

    [DisallowMultipleComponent]
    public sealed class StrategyBuildingUpgradeController : MonoBehaviour
    {
        private const int ChickensPerCoop = 3;

        private readonly HashSet<Vector2Int> occupiedVisualCells = new();
        private readonly List<StrategyChickenAgent> chickens = new();
        private CityMapController map;
        private Transform upgradeRoot;
        private Transform chickenRoot;

        public void Configure(CityMapController mapController)
        {
            map = mapController;
            EnsureUpgradeRoot();
        }

        public bool TryInstallUpgrade(
            StrategyPlacedBuilding building,
            StrategyBuildingUpgradeType type,
            out StrategyBuildingUpgrade upgrade)
        {
            return TryInstallUpgrade(building, type, out upgrade, out _);
        }

        public bool TryInstallUpgrade(
            StrategyPlacedBuilding building,
            StrategyBuildingUpgradeType type,
            out StrategyBuildingUpgrade upgrade,
            out StrategyBuildingUpgradeInstallFailureReason failureReason)
        {
            return TryInstallUpgrade(
                building,
                type,
                true,
                "upgrade_" + type,
                out upgrade,
                out failureReason);
        }

        public bool TryInstallDefaultGardenBeds(
            StrategyPlacedBuilding building,
            out StrategyBuildingUpgrade upgrade,
            out StrategyBuildingUpgradeInstallFailureReason failureReason)
        {
            return TryInstallUpgrade(
                building,
                StrategyBuildingUpgradeType.GardenBeds,
                false,
                "default_house_garden",
                out upgrade,
                out failureReason);
        }

        private bool TryInstallUpgrade(
            StrategyPlacedBuilding building,
            StrategyBuildingUpgradeType type,
            bool spendResources,
            string installReason,
            out StrategyBuildingUpgrade upgrade,
            out StrategyBuildingUpgradeInstallFailureReason failureReason)
        {
            upgrade = null;
            failureReason = StrategyBuildingUpgradeInstallFailureReason.None;
            if (building == null
                || map == null
                || building.Tool != StrategyBuildTool.House)
            {
                failureReason = StrategyBuildingUpgradeInstallFailureReason.InvalidTarget;
                return false;
            }

            if (building.HasUpgrade(type))
            {
                failureReason = StrategyBuildingUpgradeInstallFailureReason.AlreadyInstalled;
                return false;
            }

            Vector2Int size = GetUpgradeFootprint(type);
            if (!TryFindUpgradeOrigin(building, size, type, out Vector2Int origin))
            {
                failureReason = StrategyBuildingUpgradeInstallFailureReason.NoSpace;
                return false;
            }

            StrategyConstructionResourceCost cost = GetUpgradeCost(type);
            Bounds bounds = map.GetCellRectWorld(origin, size);
            if (spendResources
                && !StrategyStorageYard.TrySpendConstructionResources(cost, bounds.center, installReason))
            {
                failureReason = StrategyBuildingUpgradeInstallFailureReason.NotEnoughResources;
                return false;
            }

            GameObject upgradeObject = new GameObject(GetUpgradeName(type));
            upgradeObject.transform.SetParent(upgradeRoot, false);
            upgradeObject.transform.position = GetUpgradeAnchor(bounds);

            SpriteRenderer renderer = upgradeObject.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategyBuildingUpgradeSpriteFactory.GetSprite(type);
            StrategyWorldSorting.Apply(renderer, upgradeObject.transform.position);
            AttachUpgradeShadow(renderer, type, size);

            upgrade = upgradeObject.AddComponent<StrategyBuildingUpgrade>();
            StrategyResourceType producedResource = DetermineProducedResource(building, origin, type);
            upgrade.Configure(type, building, origin, size, bounds, producedResource);
            if (!building.TryRegisterUpgrade(upgrade))
            {
                Destroy(upgradeObject);
                upgrade = null;
                failureReason = StrategyBuildingUpgradeInstallFailureReason.AlreadyInstalled;
                return false;
            }

            StrategyBuildingUpgradeAnimator animator = upgradeObject.AddComponent<StrategyBuildingUpgradeAnimator>();
            animator.Configure(renderer, type, upgrade);

            MarkVisualCells(origin, size);
            if (type == StrategyBuildingUpgradeType.ChickenCoop)
            {
                SpawnChickensForCoop(upgrade);
            }

            StrategyDebugLogger.Info(
                "BuildingUpgrade",
                "Installed",
                StrategyDebugLogger.F("type", type),
                StrategyDebugLogger.F("houseOrigin", building.Origin),
                StrategyDebugLogger.F("upgradeOrigin", origin),
                StrategyDebugLogger.F("costLogs", spendResources ? cost.Logs : 0),
                StrategyDebugLogger.F("costStone", spendResources ? cost.Stone : 0),
                StrategyDebugLogger.F("costPlanks", spendResources ? cost.Planks : 0),
                StrategyDebugLogger.F("reason", installReason));
            return true;
        }

        public static StrategyConstructionResourceCost GetUpgradeCost(StrategyBuildingUpgradeType type)
        {
            return type == StrategyBuildingUpgradeType.GardenBeds
                ? new StrategyConstructionResourceCost(2, 1)
                : new StrategyConstructionResourceCost(3, 1, 2);
        }

        public static bool CanAffordUpgrade(StrategyBuildingUpgradeType type)
        {
            return StrategyStorageYard.CanAffordConstruction(GetUpgradeCost(type));
        }

        private bool TryFindUpgradeOrigin(
            StrategyPlacedBuilding building,
            Vector2Int size,
            StrategyBuildingUpgradeType type,
            out Vector2Int origin)
        {
            Vector2Int[] preferred = GetPreferredOrigins(building, size, type);
            for (int i = 0; i < preferred.Length; i++)
            {
                if (CanPlaceVisual(preferred[i], size))
                {
                    origin = preferred[i];
                    return true;
                }
            }

            for (int radius = 1; radius <= 5; radius++)
            {
                int minX = building.Origin.x - radius - size.x + 1;
                int maxX = building.Origin.x + building.Footprint.x + radius - 1;
                int minY = building.Origin.y - radius - size.y + 1;
                int maxY = building.Origin.y + building.Footprint.y + radius - 1;

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        Vector2Int candidate = new Vector2Int(x, y);
                        if (Overlaps(candidate, size, building.Origin, building.Footprint)
                            || !CanPlaceVisual(candidate, size))
                        {
                            continue;
                        }

                        origin = candidate;
                        return true;
                    }
                }
            }

            origin = default;
            return false;
        }

        private Vector2Int[] GetPreferredOrigins(StrategyPlacedBuilding building, Vector2Int size, StrategyBuildingUpgradeType type)
        {
            if (type == StrategyBuildingUpgradeType.GardenBeds)
            {
                return new[]
                {
                    new Vector2Int(building.Origin.x, building.Origin.y - size.y),
                    new Vector2Int(building.Origin.x + building.Footprint.x, building.Origin.y),
                    new Vector2Int(building.Origin.x - size.x, building.Origin.y),
                    new Vector2Int(building.Origin.x, building.Origin.y + building.Footprint.y)
                };
            }

            return new[]
            {
                new Vector2Int(building.Origin.x + building.Footprint.x, building.Origin.y + building.Footprint.y - 1),
                new Vector2Int(building.Origin.x - size.x, building.Origin.y + building.Footprint.y - 1),
                new Vector2Int(building.Origin.x + building.Footprint.x, building.Origin.y),
                new Vector2Int(building.Origin.x - size.x, building.Origin.y)
            };
        }

        private bool CanPlaceVisual(Vector2Int origin, Vector2Int size)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    Vector2Int cell = new Vector2Int(origin.x + x, origin.y + y);
                    if (!map.IsCellWalkable(cell)
                        || !map.IsCellBuildable(cell)
                        || occupiedVisualCells.Contains(cell))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void MarkVisualCells(Vector2Int origin, Vector2Int size)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    occupiedVisualCells.Add(new Vector2Int(origin.x + x, origin.y + y));
                }
            }
        }

        private void EnsureUpgradeRoot()
        {
            if (upgradeRoot != null)
            {
                return;
            }

            GameObject rootObject = new GameObject("Building Upgrades");
            rootObject.transform.SetParent(transform, false);
            upgradeRoot = rootObject.transform;
        }

        private void SpawnChickensForCoop(StrategyBuildingUpgrade coop)
        {
            if (coop == null || map == null)
            {
                return;
            }

            EnsureChickenRoot();
            HashSet<Vector2Int> usedCells = new();
            for (int i = 0; i < ChickensPerCoop; i++)
            {
                bool foundSpawnCell = TryFindChickenSpawnCell(coop, usedCells, i, out Vector2Int spawnCell);
                Vector3 spawnWorld = foundSpawnCell
                    ? map.GetCellCenterWorld(spawnCell.x, spawnCell.y)
                    : GetFallbackChickenSpawnWorld(coop, i);

                if (foundSpawnCell)
                {
                    usedCells.Add(spawnCell);
                }

                GameObject chickenObject = new GameObject("Chicken");
                chickenObject.transform.SetParent(chickenRoot, false);

                SpriteRenderer renderer = chickenObject.AddComponent<SpriteRenderer>();
                renderer.sprite = StrategyChickenSpriteFactory.GetSprite();

                StrategyChickenAgent chicken = chickenObject.AddComponent<StrategyChickenAgent>();
                chicken.Configure(map, coop, spawnWorld, renderer);
                chickens.Add(chicken);
            }
        }

        private bool TryFindChickenSpawnCell(
            StrategyBuildingUpgrade coop,
            HashSet<Vector2Int> usedCells,
            int variant,
            out Vector2Int cell)
        {
            List<Vector2Int> candidates = new();
            for (int radius = 1; radius <= 4; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y < coop.Footprint.y + radius; y++)
                {
                    for (int x = -radius; x < coop.Footprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == coop.Footprint.x + radius - 1
                            || y == coop.Footprint.y + radius - 1;
                        if (!isEdge)
                        {
                            continue;
                        }

                        Vector2Int candidate = coop.Origin + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate)
                            && !IsUpgradeCell(candidate, coop)
                            && !usedCells.Contains(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    int index = Mathf.Abs(coop.Origin.x * 19 + coop.Origin.y * 23 + variant * 5) % candidates.Count;
                    cell = candidates[index];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private Vector3 GetFallbackChickenSpawnWorld(StrategyBuildingUpgrade coop, int variant)
        {
            Bounds bounds = coop.FootprintBounds;
            float angle = variant * (Mathf.PI * 2f / ChickensPerCoop);
            float radius = Mathf.Max(0.45f, map.CellSize * 0.85f);
            return new Vector3(
                bounds.center.x + Mathf.Cos(angle) * radius,
                bounds.center.y + Mathf.Sin(angle) * radius,
                -0.09f);
        }

        private void EnsureChickenRoot()
        {
            if (chickenRoot != null)
            {
                return;
            }

            GameObject rootObject = new GameObject("Chickens");
            rootObject.transform.SetParent(transform, false);
            chickenRoot = rootObject.transform;
        }

        private static Vector2Int GetUpgradeFootprint(StrategyBuildingUpgradeType type)
        {
            return type == StrategyBuildingUpgradeType.GardenBeds ? new Vector2Int(2, 1) : Vector2Int.one;
        }

        private static Vector3 GetUpgradeAnchor(Bounds bounds)
        {
            return new Vector3(bounds.center.x, bounds.min.y + 0.05f, -0.12f);
        }

        private static void AttachUpgradeShadow(
            SpriteRenderer renderer,
            StrategyBuildingUpgradeType type,
            Vector2Int size)
        {
            if (renderer == null)
            {
                return;
            }

            if (type == StrategyBuildingUpgradeType.GardenBeds)
            {
                StrategyShadowCaster2D.Attach(
                    renderer,
                    StrategyShadowShape.SoftEllipse,
                    new Vector2(0.04f, -0.02f),
                    new Vector2(Mathf.Max(0.8f, size.x * 0.46f), 0.16f),
                    0.12f,
                    -4,
                    0f,
                    false);
                return;
            }

            StrategyShadowCaster2D.Attach(
                renderer,
                StrategyShadowShape.CastOval,
                new Vector2(0.08f, -0.05f),
                new Vector2(Mathf.Max(0.46f, size.x * 0.48f), 0.20f),
                0.22f,
                -5,
                -5f,
                true);
        }

        private static StrategyResourceType DetermineProducedResource(
            StrategyPlacedBuilding building,
            Vector2Int origin,
            StrategyBuildingUpgradeType type)
        {
            if (type == StrategyBuildingUpgradeType.ChickenCoop)
            {
                return StrategyResourceType.Eggs;
            }

            if (type != StrategyBuildingUpgradeType.GardenBeds)
            {
                return StrategyResourceType.None;
            }

            StrategyResourceType[] crops =
            {
                StrategyResourceType.Turnip,
                StrategyResourceType.Cabbage,
                StrategyResourceType.Onion,
                StrategyResourceType.Carrot,
                StrategyResourceType.Potato
            };
            int hash = Mathf.Abs(
                building.Origin.x * 97
                + building.Origin.y * 53
                + origin.x * 31
                + origin.y * 17);
            return crops[hash % crops.Length];
        }

        private static bool Overlaps(Vector2Int aOrigin, Vector2Int aSize, Vector2Int bOrigin, Vector2Int bSize)
        {
            return aOrigin.x < bOrigin.x + bSize.x
                && aOrigin.x + aSize.x > bOrigin.x
                && aOrigin.y < bOrigin.y + bSize.y
                && aOrigin.y + aSize.y > bOrigin.y;
        }

        private static bool IsUpgradeCell(Vector2Int cell, StrategyBuildingUpgrade upgrade)
        {
            return cell.x >= upgrade.Origin.x
                && cell.x < upgrade.Origin.x + upgrade.Footprint.x
                && cell.y >= upgrade.Origin.y
                && cell.y < upgrade.Origin.y + upgrade.Footprint.y;
        }

        private static string GetUpgradeName(StrategyBuildingUpgradeType type)
        {
            return type == StrategyBuildingUpgradeType.GardenBeds ? "Garden Beds" : "Chicken Coop";
        }
    }
}
