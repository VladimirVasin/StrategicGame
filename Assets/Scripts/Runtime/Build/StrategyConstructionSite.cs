using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyConstructionSite : MonoBehaviour
    {
        private const float BuilderRequestInterval = 2f;

        private readonly List<StrategyResidentAgent> builders = new();
        private readonly List<Vector2Int> bridgeCells = new();
        private readonly List<Vector2Int> bridgeWorkCells = new();
        private readonly HashSet<int> futureHomeResidentIds = new();
        private StrategyBuildPlacementController placement;
        private CityMapController map;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer logsRenderer;
        private SpriteRenderer stoneRenderer;
        private SpriteRenderer planksRenderer;
        private StrategyBuildTool tool;
        private string title;
        private Color color;
        private StrategyConstructionResourceCost cost;
        private Vector2Int origin;
        private Vector2Int footprint;
        private Vector2Int blockOrigin;
        private Vector2Int blockFootprint;
        private Vector2Int bridgeStartCell;
        private Vector2Int bridgeEndCell;
        private Bounds footprintBounds;
        private int visualVariant;
        private int deliveredLogs;
        private int deliveredStone;
        private int deliveredPlanks;
        private int buildHits;
        private int buildHitsRequired = 18;
        private float builderRequestTimer;
        private bool hasBegun;
        private bool completed;
        private bool hasBridgeSpan;

        public StrategyBuildTool Tool => tool;
        public string Title => title;
        public Color Color => color;
        public StrategyConstructionResourceCost Cost => cost;
        public Vector2Int Origin => origin;
        public Vector2Int Footprint => footprint;
        public Vector2Int BlockOrigin => blockOrigin;
        public Vector2Int BlockFootprint => blockFootprint;
        public Bounds FootprintBounds => footprintBounds;
        public Bounds SelectionBounds => spriteRenderer != null ? spriteRenderer.bounds : footprintBounds;
        public int VisualVariant => visualVariant;
        public int DeliveredLogs => deliveredLogs;
        public int DeliveredStone => deliveredStone;
        public int DeliveredPlanks => deliveredPlanks;
        public int NeededLogs => Mathf.Max(0, cost.Logs - deliveredLogs);
        public int NeededStone => Mathf.Max(0, cost.Stone - deliveredStone);
        public int NeededPlanks => Mathf.Max(0, cost.Planks - deliveredPlanks);
        public bool ResourcesComplete => NeededLogs <= 0 && NeededStone <= 0 && NeededPlanks <= 0;
        public bool IsCompleted => completed;
        public int DeliveredResourceTotal => deliveredLogs + deliveredStone + deliveredPlanks;
        public float DeliveredResourceFraction => cost.Total <= 0 ? 1f : Mathf.Clamp01(DeliveredResourceTotal / (float)cost.Total);
        public float BuildableProgressLimit => ResourcesComplete ? 1f : DeliveredResourceFraction;
        public int BuildableHitLimit => Mathf.Clamp(Mathf.CeilToInt(BuildableProgressLimit * buildHitsRequired), 0, buildHitsRequired);
        public float Progress => buildHitsRequired <= 0 ? 1f : Mathf.Min(Mathf.Clamp01(buildHits / (float)buildHitsRequired), BuildableProgressLimit);
        public bool CanBuildWithDeliveredResources => !completed && buildHitsRequired > 0 && buildHits < BuildableHitLimit;
        public int BuilderCount => builders.Count;
        public IReadOnlyList<StrategyResidentAgent> Builders => builders;
        public bool HasBridgeSpan => hasBridgeSpan;
        public IReadOnlyList<Vector2Int> BridgeCells => bridgeCells;
        public Vector2Int BridgeStartCell => bridgeStartCell;
        public Vector2Int BridgeEndCell => bridgeEndCell;

        public void Configure(
            StrategyBuildPlacementController placementController,
            CityMapController mapController,
            StrategyBuildToolInfo toolInfo,
            Vector2Int siteOrigin,
            Bounds bounds,
            Vector2Int siteBlockOrigin,
            Vector2Int siteBlockFootprint,
            int selectedVisualVariant,
            SpriteRenderer renderer)
        {
            placement = placementController;
            map = mapController;
            tool = toolInfo.Tool;
            title = toolInfo.Title;
            color = toolInfo.Color;
            cost = toolInfo.Cost;
            origin = siteOrigin;
            footprint = toolInfo.Footprint;
            blockOrigin = siteBlockOrigin;
            blockFootprint = siteBlockFootprint;
            footprintBounds = bounds;
            visualVariant = selectedVisualVariant;
            spriteRenderer = renderer;
            buildHitsRequired = Mathf.Max(16, cost.Total * 4 + footprint.x * footprint.y * 5);
            EnsureStockRenderers();
            UpdateVisuals();
            EnsureWorldShadow();
            EnsureClickCollider();
        }

        public void ConfigureBridgeSpan(
            IReadOnlyList<Vector2Int> cells,
            Vector2Int startBankCell,
            Vector2Int endBankCell)
        {
            bridgeCells.Clear();
            bridgeWorkCells.Clear();
            if (cells == null || cells.Count <= 0)
            {
                hasBridgeSpan = false;
                return;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                if (!bridgeCells.Contains(cell))
                {
                    bridgeCells.Add(cell);
                }
            }

            AddBridgeWorkCell(startBankCell);
            AddBridgeWorkCell(endBankCell);
            bridgeStartCell = startBankCell;
            bridgeEndCell = endBankCell;
            hasBridgeSpan = bridgeCells.Count > 0;
            buildHitsRequired = Mathf.Max(buildHitsRequired, cost.Total * 4 + bridgeCells.Count * 5);
            UpdateVisuals();
            EnsureWorldShadow();
            EnsureClickCollider();
        }

        public void Begin()
        {
            hasBegun = true;
            builderRequestTimer = 0f;
            StrategyDebugLogger.Info(
                "Construction",
                "SiteStarted",
                StrategyDebugLogger.F("tool", tool),
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("costLogs", cost.Logs),
                StrategyDebugLogger.F("costStone", cost.Stone),
                StrategyDebugLogger.F("costPlanks", cost.Planks),
                StrategyDebugLogger.F("builders", builders.Count));
        }

        public bool RegisterBuilder(StrategyResidentAgent resident)
        {
            return RegisterBuilder(resident, false);
        }

        public bool RegisterBuilder(StrategyResidentAgent resident, bool futureHomeResident)
        {
            if (resident == null
                || builders.Contains(resident)
                || !resident.CanAcceptWorkAssignment)
            {
                return false;
            }

            builders.Add(resident);
            if (futureHomeResident && resident.ResidentId > 0)
            {
                futureHomeResidentIds.Add(resident.ResidentId);
            }

            StrategyDebugLogger.Info(
                "Construction",
                "BuilderRegistered",
                StrategyDebugLogger.F("tool", tool),
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("builder", resident.FullName),
                StrategyDebugLogger.F("builderCount", builders.Count),
                StrategyDebugLogger.F("futureHomeResident", futureHomeResident));
            return true;
        }

        public void UnregisterBuilder(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return;
            }

            builders.Remove(resident);
            if (resident.ResidentId > 0)
            {
                futureHomeResidentIds.Remove(resident.ResidentId);
            }
        }

        public bool TryGetBuilder(int index, out StrategyResidentAgent resident)
        {
            resident = index >= 0 && index < builders.Count ? builders[index] : null;
            return resident != null;
        }

        public bool IsFutureHomeResident(StrategyResidentAgent resident)
        {
            return resident != null
                && resident.ResidentId > 0
                && futureHomeResidentIds.Contains(resident.ResidentId);
        }

        private void Update()
        {
            if (!hasBegun || completed)
            {
                return;
            }

            builderRequestTimer -= Time.deltaTime;
            if (builderRequestTimer > 0f)
            {
                return;
            }

            builderRequestTimer = BuilderRequestInterval;
            StrategyStorageYard.TryAssignBuildersToSite(this);
        }

        public bool TryFindResourcePickup(
            StrategyResidentAgent worker,
            out IStrategyConstructionResourceSource source,
            out StrategyConstructionResourceKind kind,
            out Vector2Int pickupCell,
            out int pickupAmount)
        {
            source = null;
            kind = StrategyConstructionResourceKind.None;
            pickupCell = default;
            pickupAmount = 0;

            if (completed || worker == null || !hasBegun || ResourcesComplete)
            {
                return false;
            }

            int logsPickupAmount = Mathf.Min(StrategyProductionStorage.BuilderCarryLimit, NeededLogs);
            if (logsPickupAmount > 0
                && StrategyStorageYard.TryFindConstructionPickup(this, StrategyConstructionResourceKind.Logs, footprintBounds.center, logsPickupAmount, out source, out pickupCell, out pickupAmount))
            {
                kind = StrategyConstructionResourceKind.Logs;
                return true;
            }

            int stonePickupAmount = Mathf.Min(StrategyProductionStorage.BuilderCarryLimit, NeededStone);
            if (stonePickupAmount > 0
                && StrategyStorageYard.TryFindConstructionPickup(this, StrategyConstructionResourceKind.Stone, footprintBounds.center, stonePickupAmount, out source, out pickupCell, out pickupAmount))
            {
                kind = StrategyConstructionResourceKind.Stone;
                return true;
            }

            int planksPickupAmount = Mathf.Min(StrategyProductionStorage.BuilderCarryLimit, NeededPlanks);
            if (planksPickupAmount > 0
                && StrategyStorageYard.TryFindConstructionPickup(this, StrategyConstructionResourceKind.Planks, footprintBounds.center, planksPickupAmount, out source, out pickupCell, out pickupAmount))
            {
                kind = StrategyConstructionResourceKind.Planks;
                return true;
            }

            return false;
        }

        public bool TryFindDropoffCell(out Vector2Int cell)
        {
            if (TryFindBridgeWorkCell(out cell))
            {
                return true;
            }

            return TryFindAdjacentWorkCell(blockOrigin, blockFootprint, 4, out cell);
        }

        public bool TryFindBuildWorkCell(out Vector2Int cell)
        {
            if (TryFindBridgeWorkCell(out cell))
            {
                return true;
            }

            if (TryFindCloseBuildWorkCell(out cell))
            {
                return true;
            }

            return TryFindAdjacentWorkCell(blockOrigin, blockFootprint, 2, out cell);
        }

        public void AddDeliveredResource(StrategyConstructionResourceKind kind, int amount)
        {
            if (amount <= 0 || completed)
            {
                return;
            }

            if (kind == StrategyConstructionResourceKind.Logs)
            {
                deliveredLogs = Mathf.Min(cost.Logs, deliveredLogs + amount);
            }
            else if (kind == StrategyConstructionResourceKind.Stone)
            {
                deliveredStone = Mathf.Min(cost.Stone, deliveredStone + amount);
            }
            else if (kind == StrategyConstructionResourceKind.Planks)
            {
                deliveredPlanks = Mathf.Min(cost.Planks, deliveredPlanks + amount);
            }

            UpdateVisuals();
            PlayDeliveredResourceEffect(kind, amount);
            StrategyDebugLogger.Info(
                "Construction",
                "ResourceDelivered",
                StrategyDebugLogger.F("tool", tool),
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("resource", kind),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("deliveredLogs", deliveredLogs),
                StrategyDebugLogger.F("deliveredStone", deliveredStone),
                StrategyDebugLogger.F("deliveredPlanks", deliveredPlanks),
                StrategyDebugLogger.F("resourcesComplete", ResourcesComplete));
        }

        public void ReceiveBuildHit(StrategyResidentAgent builder, Vector3 hitWorld)
        {
            if (completed || !CanBuildWithDeliveredResources || builder == null || !builders.Contains(builder))
            {
                return;
            }

            buildHits = Mathf.Min(buildHits + 1, BuildableHitLimit);
            UpdateVisuals();
            PlayBuildHitEffect(hitWorld);
            StrategyDebugLogger.Info(
                "Construction",
                "BuildHit",
                StrategyDebugLogger.F("tool", tool),
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("builder", builder.FullName),
                StrategyDebugLogger.F("progress", Progress),
                StrategyDebugLogger.F("buildableProgressLimit", BuildableProgressLimit));

            if (buildHits >= buildHitsRequired)
            {
                CompleteConstruction();
            }
        }

        public string GetHudStatusText()
        {
            string resources = "Resources: Logs "
                + deliveredLogs
                + "/"
                + cost.Logs
                + ", Stone "
                + deliveredStone
                + "/"
                + cost.Stone;
            if (cost.Planks > 0 || deliveredPlanks > 0)
            {
                resources += ", Planks " + deliveredPlanks + "/" + cost.Planks;
            }

            return resources
                + "\n"
                + "Builders: "
                + builders.Count
                + "\n"
                + "Progress: "
                + Mathf.RoundToInt(Progress * 100f)
                + "%"
                + "\n"
                + "Buildable now: "
                + Mathf.RoundToInt(BuildableProgressLimit * 100f)
                + "%";
        }

        private bool TryFindCloseBuildWorkCell(out Vector2Int cell)
        {
            List<Vector2Int> candidates = new();

            for (int x = -1; x <= footprint.x; x++)
            {
                AddWalkableCandidate(origin + new Vector2Int(x, -1), candidates);
            }

            if (candidates.Count > 0)
            {
                cell = candidates[Random.Range(0, candidates.Count)];
                return true;
            }

            for (int y = 0; y < footprint.y; y++)
            {
                AddWalkableCandidate(origin + new Vector2Int(-1, y), candidates);
                AddWalkableCandidate(origin + new Vector2Int(footprint.x, y), candidates);
            }

            if (candidates.Count > 0)
            {
                cell = candidates[Random.Range(0, candidates.Count)];
                return true;
            }

            for (int x = 0; x < footprint.x; x++)
            {
                AddWalkableCandidate(origin + new Vector2Int(x, footprint.y), candidates);
            }

            if (candidates.Count > 0)
            {
                cell = candidates[Random.Range(0, candidates.Count)];
                return true;
            }

            cell = default;
            return false;
        }

        private void AddBridgeWorkCell(Vector2Int candidate)
        {
            if (map != null && map.IsCellWalkable(candidate) && !bridgeWorkCells.Contains(candidate))
            {
                bridgeWorkCells.Add(candidate);
            }
        }

        private bool TryFindBridgeWorkCell(out Vector2Int cell)
        {
            for (int i = bridgeWorkCells.Count - 1; i >= 0; i--)
            {
                Vector2Int candidate = bridgeWorkCells[i];
                if (map != null && map.IsCellWalkable(candidate))
                {
                    cell = candidate;
                    return true;
                }

                bridgeWorkCells.RemoveAt(i);
            }

            cell = default;
            return false;
        }

        private void AddWalkableCandidate(Vector2Int candidate, List<Vector2Int> candidates)
        {
            if (map != null && map.IsCellWalkable(candidate) && !candidates.Contains(candidate))
            {
                candidates.Add(candidate);
            }
        }

        private bool TryFindAdjacentWorkCell(
            Vector2Int targetOrigin,
            Vector2Int targetFootprint,
            int maxRadius,
            out Vector2Int cell)
        {
            List<Vector2Int> candidates = new();
            for (int radius = 1; radius <= maxRadius; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y < targetFootprint.y + radius; y++)
                {
                    for (int x = -radius; x < targetFootprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == targetFootprint.x + radius - 1
                            || y == targetFootprint.y + radius - 1;
                        if (!isEdge)
                        {
                            continue;
                        }

                        AddWalkableCandidate(targetOrigin + new Vector2Int(x, y), candidates);
                    }
                }

                if (candidates.Count > 0)
                {
                    cell = candidates[Random.Range(0, candidates.Count)];
                    return true;
                }
            }

            cell = default;
            return false;
        }

    }
}
