using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyConstructionSite : MonoBehaviour
    {
        public const int MaxBuilders = 2;
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
        public int NeededLogs => Mathf.Max(0, cost.Logs - deliveredLogs);
        public int NeededStone => Mathf.Max(0, cost.Stone - deliveredStone);
        public bool ResourcesComplete => NeededLogs <= 0 && NeededStone <= 0;
        public bool IsCompleted => completed;
        public float Progress => buildHitsRequired <= 0 ? 1f : Mathf.Clamp01(buildHits / (float)buildHitsRequired);
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
                StrategyDebugLogger.F("builders", builders.Count));
        }

        public bool RegisterBuilder(StrategyResidentAgent resident)
        {
            return RegisterBuilder(resident, false);
        }

        public bool RegisterBuilder(StrategyResidentAgent resident, bool futureHomeResident)
        {
            if (resident == null || builders.Contains(resident) || builders.Count >= MaxBuilders)
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
            if (!hasBegun || completed || builders.Count >= MaxBuilders)
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
            out Vector2Int pickupCell)
        {
            source = null;
            kind = StrategyConstructionResourceKind.None;
            pickupCell = default;

            if (completed || worker == null || !hasBegun || ResourcesComplete)
            {
                return false;
            }

            if (NeededLogs > 0
                && StrategyStorageYard.TryFindConstructionPickup(this, StrategyConstructionResourceKind.Logs, footprintBounds.center, out source, out pickupCell))
            {
                kind = StrategyConstructionResourceKind.Logs;
                return true;
            }

            if (NeededStone > 0
                && StrategyStorageYard.TryFindConstructionPickup(this, StrategyConstructionResourceKind.Stone, footprintBounds.center, out source, out pickupCell))
            {
                kind = StrategyConstructionResourceKind.Stone;
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

            UpdateVisuals();
            StrategyDebugLogger.Info(
                "Construction",
                "ResourceDelivered",
                StrategyDebugLogger.F("tool", tool),
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("resource", kind),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("deliveredLogs", deliveredLogs),
                StrategyDebugLogger.F("deliveredStone", deliveredStone),
                StrategyDebugLogger.F("resourcesComplete", ResourcesComplete));
        }

        public void ReceiveBuildHit(StrategyResidentAgent builder, Vector3 hitWorld)
        {
            if (completed || !ResourcesComplete || builder == null || !builders.Contains(builder))
            {
                return;
            }

            buildHits++;
            UpdateVisuals();
            StrategyDebugLogger.Info(
                "Construction",
                "BuildHit",
                StrategyDebugLogger.F("tool", tool),
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("builder", builder.FullName),
                StrategyDebugLogger.F("progress", Progress));

            if (buildHits >= buildHitsRequired)
            {
                CompleteConstruction();
            }
        }

        public string GetHudStatusText()
        {
            return "Resources: Logs "
                + deliveredLogs
                + "/"
                + cost.Logs
                + ", Stone "
                + deliveredStone
                + "/"
                + cost.Stone
                + "\n"
                + "Builders: "
                + builders.Count
                + "/"
                + MaxBuilders
                + "\n"
                + "Progress: "
                + Mathf.RoundToInt(Progress * 100f)
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

        private void CompleteConstruction()
        {
            if (completed)
            {
                return;
            }

            completed = true;
            StrategyStorageYard.ReleaseConstructionReservations(this);
            StrategyDebugLogger.Info(
                "Construction",
                "Completed",
                StrategyDebugLogger.F("tool", tool),
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("builders", builders.Count));

            for (int i = builders.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent builder = builders[i];
                if (builder != null)
                {
                    builder.NotifyConstructionCompleted(this);
                }
            }

            placement?.CompleteConstructionSite(this);
        }

        private void EnsureStockRenderers()
        {
            if (logsRenderer == null)
            {
                GameObject logsObject = new GameObject("Construction Logs");
                logsObject.transform.SetParent(transform, false);
                logsRenderer = logsObject.AddComponent<SpriteRenderer>();
            }

            if (stoneRenderer == null)
            {
                GameObject stoneObject = new GameObject("Construction Stone");
                stoneObject.transform.SetParent(transform, false);
                stoneRenderer = stoneObject.AddComponent<SpriteRenderer>();
            }
        }

        private void UpdateVisuals()
        {
            if (spriteRenderer != null)
            {
                int stage = ResourcesComplete
                    ? Mathf.Clamp(1 + Mathf.FloorToInt(Progress * (StrategyConstructionSpriteFactory.StageCount - 1)), 1, StrategyConstructionSpriteFactory.StageCount - 1)
                    : Mathf.Clamp(Mathf.FloorToInt(((deliveredLogs + deliveredStone) / Mathf.Max(1f, cost.Total)) * 2f), 0, 2);
                spriteRenderer.sprite = tool == StrategyBuildTool.Bridge
                    ? StrategyConstructionSpriteFactory.GetBridgeConstructionSprite(footprint, stage)
                    : StrategyConstructionSpriteFactory.GetConstructionSprite(tool, visualVariant, stage);
                spriteRenderer.color = Color.white;
            }

            EnsureStockRenderers();
            if (logsRenderer != null)
            {
                Vector3 logsWorld = new Vector3(footprintBounds.center.x - 0.82f, footprintBounds.min.y + 0.30f, -0.12f);
                logsRenderer.sprite = StrategyConstructionSpriteFactory.GetConstructionLogsSprite(deliveredLogs);
                logsRenderer.gameObject.SetActive(deliveredLogs > 0 && logsRenderer.sprite != null);
                logsRenderer.transform.localPosition = transform.InverseTransformPoint(logsWorld);
                StrategyWorldSorting.Apply(logsRenderer, logsWorld, 1);
            }

            if (stoneRenderer != null)
            {
                Vector3 stoneWorld = new Vector3(footprintBounds.center.x + 0.78f, footprintBounds.min.y + 0.28f, -0.12f);
                stoneRenderer.sprite = StrategyConstructionSpriteFactory.GetConstructionStoneSprite(deliveredStone);
                stoneRenderer.gameObject.SetActive(deliveredStone > 0 && stoneRenderer.sprite != null);
                stoneRenderer.transform.localPosition = transform.InverseTransformPoint(stoneWorld);
                StrategyWorldSorting.Apply(stoneRenderer, stoneWorld, 1);
            }
        }

        private void EnsureClickCollider()
        {
            BoxCollider2D box = GetComponent<BoxCollider2D>();
            if (box == null)
            {
                box = gameObject.AddComponent<BoxCollider2D>();
            }

            Bounds clickBounds = spriteRenderer != null ? spriteRenderer.bounds : footprintBounds;
            Vector3 localCenter = transform.InverseTransformPoint(clickBounds.center);
            Vector3 localSize = transform.InverseTransformVector(clickBounds.size);

            box.isTrigger = true;
            box.offset = new Vector2(localCenter.x, localCenter.y);
            box.size = new Vector2(
                Mathf.Max(0.5f, Mathf.Abs(localSize.x)),
                Mathf.Max(0.5f, Mathf.Abs(localSize.y)));
        }

        private void OnDestroy()
        {
            StrategyStorageYard.ReleaseConstructionReservations(this);
            for (int i = builders.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent builder = builders[i];
                if (builder != null)
                {
                    builder.ClearConstructionSite(this);
                }
            }

            builders.Clear();
            bridgeCells.Clear();
            bridgeWorkCells.Clear();
            futureHomeResidentIds.Clear();
        }
    }
}
