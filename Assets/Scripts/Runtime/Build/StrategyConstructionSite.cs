using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyConstructionSite : MonoBehaviour
    {
        public const int MaxBuilders = 2;

        private readonly List<StrategyResidentAgent> builders = new();
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
        private Bounds footprintBounds;
        private int visualVariant;
        private int deliveredLogs;
        private int deliveredStone;
        private int buildHits;
        private int buildHitsRequired = 18;
        private bool hasBegun;
        private bool completed;

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

        public void Begin()
        {
            hasBegun = true;
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
            if (resident == null || builders.Contains(resident) || builders.Count >= MaxBuilders)
            {
                return false;
            }

            builders.Add(resident);
            StrategyDebugLogger.Info(
                "Construction",
                "BuilderRegistered",
                StrategyDebugLogger.F("tool", tool),
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("builder", resident.FullName),
                StrategyDebugLogger.F("builderCount", builders.Count));
            return true;
        }

        public void UnregisterBuilder(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return;
            }

            builders.Remove(resident);
        }

        public bool TryGetBuilder(int index, out StrategyResidentAgent resident)
        {
            resident = index >= 0 && index < builders.Count ? builders[index] : null;
            return resident != null;
        }

        public bool TryFindResourcePickup(
            StrategyResidentAgent worker,
            out StrategyStorageYard storage,
            out StrategyConstructionResourceKind kind,
            out Vector2Int pickupCell)
        {
            storage = null;
            kind = StrategyConstructionResourceKind.None;
            pickupCell = default;

            if (completed || worker == null || !hasBegun || ResourcesComplete)
            {
                return false;
            }

            if (NeededLogs > 0
                && StrategyStorageYard.TryFindConstructionPickup(this, StrategyConstructionResourceKind.Logs, footprintBounds.center, out storage, out pickupCell))
            {
                kind = StrategyConstructionResourceKind.Logs;
                return true;
            }

            if (NeededStone > 0
                && StrategyStorageYard.TryFindConstructionPickup(this, StrategyConstructionResourceKind.Stone, footprintBounds.center, out storage, out pickupCell))
            {
                kind = StrategyConstructionResourceKind.Stone;
                return true;
            }

            return false;
        }

        public bool TryFindDropoffCell(out Vector2Int cell)
        {
            return TryFindAdjacentWorkCell(out cell);
        }

        public bool TryFindBuildWorkCell(out Vector2Int cell)
        {
            return TryFindAdjacentWorkCell(out cell);
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
            return "Ресурсы: Logs "
                + deliveredLogs
                + "/"
                + cost.Logs
                + ", Камень "
                + deliveredStone
                + "/"
                + cost.Stone
                + "\n"
                + "Строители: "
                + builders.Count
                + "/"
                + MaxBuilders
                + "\n"
                + "Прогресс: "
                + Mathf.RoundToInt(Progress * 100f)
                + "%";
        }

        private bool TryFindAdjacentWorkCell(out Vector2Int cell)
        {
            List<Vector2Int> candidates = new();
            for (int radius = 1; radius <= 4; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y < blockFootprint.y + radius; y++)
                {
                    for (int x = -radius; x < blockFootprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == blockFootprint.x + radius - 1
                            || y == blockFootprint.y + radius - 1;
                        if (!isEdge)
                        {
                            continue;
                        }

                        Vector2Int candidate = blockOrigin + new Vector2Int(x, y);
                        if (map != null && map.IsCellWalkable(candidate))
                        {
                            candidates.Add(candidate);
                        }
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
                spriteRenderer.sprite = StrategyConstructionSpriteFactory.GetConstructionSprite(tool, visualVariant, stage);
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
        }
    }
}
