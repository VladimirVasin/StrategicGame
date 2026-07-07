using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyForageResourceController : MonoBehaviour
    {
        private const int MaxForageNodes = 380;
        private const int InitialForageNodeTarget = 310;
        private const int LocalDensityRadius = 5;
        private const int LocalDensityMaxNodes = 5;
        private const int StarterSearchMinRadius = 4;
        private const int StarterSearchMaxRadius = 14;
        private const int StarterBerries = 5;
        private const int StarterRoots = 4;
        private const int StarterMushrooms = 3;

        private readonly List<StrategyForageNode> nodes = new();
        private readonly HashSet<Vector2Int> usedCells = new();
        private CityMapController map;
        private Transform forageRoot;
        private bool hasStarterCell;
        private Vector2Int starterCell;

        public static StrategyForageResourceController Active { get; private set; }
        public IReadOnlyList<StrategyForageNode> Nodes => nodes;

        public void Configure(CityMapController mapController)
        {
            Configure(mapController, false, Vector2Int.zero);
        }

        public void Configure(CityMapController mapController, Vector2Int campCell)
        {
            Configure(mapController, true, campCell);
        }

        private void Configure(CityMapController mapController, bool hasCamp, Vector2Int campCell)
        {
            map = mapController;
            hasStarterCell = hasCamp;
            starterCell = campCell;
            Active = this;
            EnsureRoot();
            GenerateForage();
        }

        public void RegisterNode(StrategyForageNode node)
        {
            if (node != null && !nodes.Contains(node))
            {
                nodes.Add(node);
            }
        }

        public void UnregisterNode(StrategyForageNode node)
        {
            if (node != null && nodes.Remove(node))
            {
                usedCells.Remove(node.Cell);
            }
        }

        public bool TryReserveForHouse(
            StrategyPlacedBuilding house,
            StrategyResidentAgent resident,
            int radius,
            Func<StrategyResourceType, bool> acceptsResource,
            out StrategyForageNode node,
            out Vector2Int workCell)
        {
            return TryReserveForBuilding(house, resident, radius, acceptsResource, out node, out workCell);
        }

        public bool TryReserveForWorksite(
            StrategyPlacedBuilding worksite,
            StrategyResidentAgent resident,
            int radius,
            Func<StrategyResourceType, bool> acceptsResource,
            out StrategyForageNode node,
            out Vector2Int workCell)
        {
            return TryReserveForBuilding(worksite, resident, radius, acceptsResource, out node, out workCell);
        }

        private bool TryReserveForBuilding(
            StrategyPlacedBuilding owner,
            StrategyResidentAgent resident,
            int radius,
            Func<StrategyResourceType, bool> acceptsResource,
            out StrategyForageNode node,
            out Vector2Int workCell)
        {
            node = null;
            workCell = default;
            if (map == null || owner == null || resident == null)
            {
                return false;
            }

            RemoveMissingNodes();
            Vector2Int center = owner.Origin + new Vector2Int(owner.Footprint.x / 2, owner.Footprint.y / 2);
            int radiusSqr = radius * radius;
            float bestScore = float.MaxValue;
            StrategyForageNode bestNode = null;

            for (int i = 0; i < nodes.Count; i++)
            {
                StrategyForageNode candidate = nodes[i];
                if (candidate == null
                    || !candidate.IsReady
                    || !map.IsCellWalkable(candidate.Cell)
                    || (acceptsResource != null && !acceptsResource(candidate.ResourceType)))
                {
                    continue;
                }

                Vector2Int delta = candidate.Cell - center;
                int distanceSqr = delta.x * delta.x + delta.y * delta.y;
                if (distanceSqr > radiusSqr)
                {
                    continue;
                }

                float score = distanceSqr
                    + (candidate.transform.position - resident.transform.position).sqrMagnitude * 0.2f
                    + Hash01(map.ActiveSeed, candidate.Cell.x, candidate.Cell.y, resident.ResidentId) * 0.65f;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestNode = candidate;
                }
            }

            if (bestNode == null || !bestNode.TryReserve(resident, owner))
            {
                return false;
            }

            node = bestNode;
            workCell = bestNode.Cell;
            return true;
        }

        private void GenerateForage()
        {
            if (map == null)
            {
                return;
            }

            ClearExisting();
            EnsureStarterForageNodes();

            int totalCells = map.Width * map.Height;
            int initialTarget = Mathf.Min(GetSeasonalInitialForageNodeTarget(), MaxForageNodes);
            for (int i = 0; i < totalCells && nodes.Count < initialTarget; i++)
            {
                int cellIndex = StrategyMapDistributionUtility.GetShuffledIndex(map.ActiveSeed, i, totalCells, 6101);
                int x = cellIndex % map.Width;
                int y = cellIndex / map.Width;
                Vector2Int cell = new Vector2Int(x, y);
                if (usedCells.Contains(cell)
                    || IsTooCloseToStarter(cell, 3)
                    || !map.IsCellWalkable(cell)
                    || !map.TryGetCell(x, y, out CityMapCell mapCell)
                    || !TryChooseResource(mapCell.Kind, x, y, out StrategyResourceType resource))
                {
                    continue;
                }

                if (!HasLocalForageRoom(cell))
                {
                    continue;
                }

                CreateNode(cell, resource, 1100 + nodes.Count);
            }

            StrategyDebugLogger.Info(
                "Forage",
                "Generated",
                StrategyDebugLogger.F("nodes", nodes.Count),
                StrategyDebugLogger.F("season", StrategyDayNightCycleController.CurrentCalendarSnapshot.SeasonLabel),
                StrategyDebugLogger.F("max", MaxForageNodes),
                StrategyDebugLogger.F("starterCell", hasStarterCell ? starterCell : Vector2Int.zero));
        }

        private void EnsureStarterForageNodes()
        {
            if (!hasStarterCell || map == null)
            {
                return;
            }

            CreateStarterNodes(StrategyResourceType.Berries, StarterBerries, 3001);
            CreateStarterNodes(StrategyResourceType.Roots, StarterRoots, 4001);
            CreateStarterNodes(StrategyResourceType.Mushrooms, StarterMushrooms, 5001);
        }

        private void CreateStarterNodes(StrategyResourceType resource, int targetCount, int salt)
        {
            int created = 0;
            List<Vector2Int> candidates = new();
            for (int radius = StarterSearchMinRadius; radius <= StarterSearchMaxRadius; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
                        {
                            continue;
                        }

                        Vector2Int cell = starterCell + new Vector2Int(x, y);
                        if (usedCells.Contains(cell)
                            || !map.IsCellWalkable(cell)
                            || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell)
                            || !IsResourceAllowedOnTerrain(resource, mapCell.Kind))
                        {
                            continue;
                        }

                        candidates.Add(cell);
                    }
                }

                candidates.Sort((left, right) =>
                {
                    float leftScore = Hash01(map.ActiveSeed, left.x, left.y, salt);
                    float rightScore = Hash01(map.ActiveSeed, right.x, right.y, salt);
                    return leftScore.CompareTo(rightScore);
                });

                for (int i = 0; i < candidates.Count && created < targetCount && nodes.Count < MaxForageNodes; i++)
                {
                    CreateNode(candidates[i], resource, salt + created);
                    created++;
                }

                if (created >= targetCount)
                {
                    return;
                }
            }

            StrategyDebugLogger.Warn(
                "Forage",
                "StarterForageShort",
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("created", created),
                StrategyDebugLogger.F("target", targetCount),
                StrategyDebugLogger.F("starterCell", starterCell));
        }

        private void CreateNode(Vector2Int cell, StrategyResourceType resource, int salt)
        {
            if (map == null || forageRoot == null || usedCells.Contains(cell) || nodes.Count >= MaxForageNodes)
            {
                return;
            }

            Vector3 center = map.GetCellCenterWorld(cell.x, cell.y);
            Vector2 jitter = GetJitter(cell.x, cell.y, salt) * map.CellSize;
            GameObject nodeObject = new GameObject("Forage " + resource);
            nodeObject.transform.SetParent(forageRoot, false);
            nodeObject.transform.position = new Vector3(center.x + jitter.x, center.y + jitter.y, -0.105f);

            SpriteRenderer renderer = nodeObject.AddComponent<SpriteRenderer>();
            renderer.color = Color.white;
            int variant = Hash(map.ActiveSeed, cell.x, cell.y, salt, 19) % 4;
            StrategyForageNode node = nodeObject.AddComponent<StrategyForageNode>();
            node.Configure(this, resource, cell, GetYield(resource), variant, renderer);
            usedCells.Add(cell);
        }

        private bool TryChooseResource(CityMapCellKind kind, int x, int y, out StrategyResourceType resource)
        {
            resource = StrategyResourceType.None;
            float cluster = StrategyMapDistributionUtility.ClusterScore(map.ActiveSeed, x, y, 6203, 0.041f, 0.112f);
            float chance = StrategyMapDistributionUtility.ApplyClusterToChance(
                GetSeasonalForageChance(kind),
                cluster,
                0.22f,
                2.35f);
            if (chance <= 0f || Hash01(map.ActiveSeed, x, y, 1409) > chance)
            {
                return false;
            }

            float pick = Hash01(map.ActiveSeed, x, y, 1423);
            resource = ChooseSeasonalForageResource(kind, pick);
            return resource != StrategyResourceType.None;
        }

        private static bool IsResourceAllowedOnTerrain(StrategyResourceType resource, CityMapCellKind kind)
        {
            return resource switch
            {
                StrategyResourceType.Berries => kind == CityMapCellKind.Meadow
                    || kind == CityMapCellKind.Grass
                    || kind == CityMapCellKind.Forest,
                StrategyResourceType.Mushrooms => kind == CityMapCellKind.Forest
                    || kind == CityMapCellKind.Meadow
                    || kind == CityMapCellKind.Dirt,
                StrategyResourceType.Roots => kind == CityMapCellKind.Grass
                    || kind == CityMapCellKind.Meadow
                    || kind == CityMapCellKind.Dirt
                    || kind == CityMapCellKind.Forest,
                _ => false
            };
        }

        private static float GetForageChance(CityMapCellKind kind)
        {
            return kind switch
            {
                CityMapCellKind.Forest => 0.033f,
                CityMapCellKind.Meadow => 0.026f,
                CityMapCellKind.Grass => 0.018f,
                CityMapCellKind.Dirt => 0.010f,
                _ => 0f
            };
        }

        private static int GetYield(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Berries ? 2 : 1;
        }

        private bool HasLocalForageRoom(Vector2Int cell)
        {
            return CountForageNodesNear(cell, LocalDensityRadius) < LocalDensityMaxNodes;
        }

        private int CountForageNodesNear(Vector2Int center, int radius)
        {
            int radiusSqr = radius * radius;
            int count = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                StrategyForageNode node = nodes[i];
                if (node == null)
                {
                    continue;
                }

                Vector2Int delta = node.Cell - center;
                if (delta.x * delta.x + delta.y * delta.y <= radiusSqr)
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryGetForagerCampCenterCell(StrategyForagerCamp camp, out Vector2Int cell)
        {
            cell = default;
            return camp != null
                && map != null
                && map.TryWorldToCell(camp.FootprintBounds.center, out cell);
        }

        private bool IsTooCloseToStarter(Vector2Int cell, int radius)
        {
            return hasStarterCell
                && Mathf.Abs(cell.x - starterCell.x) <= radius
                && Mathf.Abs(cell.y - starterCell.y) <= radius;
        }

        private Vector2 GetJitter(int x, int y, int salt)
        {
            return new Vector2(
                Mathf.Lerp(-0.24f, 0.24f, Hash01(map.ActiveSeed, x, y, salt + 13)),
                Mathf.Lerp(-0.18f, 0.18f, Hash01(map.ActiveSeed, x, y, salt + 29)));
        }

        private void EnsureRoot()
        {
            if (forageRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("Forage Resources");
            root.transform.SetParent(transform, false);
            forageRoot = root.transform;
        }

        private void ClearExisting()
        {
            if (forageRoot != null)
            {
                for (int i = forageRoot.childCount - 1; i >= 0; i--)
                {
                    Transform child = forageRoot.GetChild(i);
                    if (child != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            nodes.Clear();
            usedCells.Clear();
            pendingRespawns.Clear();
        }

        private void RemoveMissingNodes()
        {
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                if (nodes[i] == null)
                {
                    nodes.RemoveAt(i);
                }
            }
        }

        private static float Hash01(int a, int b, int c, int d)
        {
            return (Hash(a, b, c, d, 911) & 0x00FFFFFF) / 16777215f;
        }

        private static int Hash(int a, int b, int c, int d, int e)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + a;
                hash = hash * 31 + b;
                hash = hash * 31 + c;
                hash = hash * 31 + d;
                hash = hash * 31 + e;
                hash ^= hash << 13;
                hash ^= hash >> 17;
                hash ^= hash << 5;
                return Mathf.Abs(hash);
            }
        }
    }
}
