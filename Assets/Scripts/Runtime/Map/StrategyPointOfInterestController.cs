using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyPointOfInterestController : MonoBehaviour
    {
        private const float TemporarilyUnreachableSeconds = 20f;
        private const string PauseReason = "PointOfInterestNotice";

        private readonly List<StrategyPointOfInterest> points = new();
        private readonly Dictionary<StrategyPointOfInterest, float> temporarilyUnreachable = new();
        private readonly List<StrategyPointOfInterest> pointScratch = new();
        private readonly Queue<PointNotice> pendingNotices = new();

        private CityMapController map;
        private StrategyFogOfWarController fog;
        private StrategyBuildPlacementController placement;
        private StrategyNaturePropController nature;
        private StrategyTimeScaleController timeScale;
        private StrategyPointOfInterestDialogController dialog;
        private Transform pointRoot;
        private Vector2Int campCell;
        private bool configured;
        private bool noticeOpen;
        private bool pauseHeld;
        private int activeNoticeToken;
        private int noticeOpenedFrame;

        public static StrategyPointOfInterestController Active { get; private set; }
        public IReadOnlyList<StrategyPointOfInterest> Points => points;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActive()
        {
            Active = null;
        }

        public void Configure(
            CityMapController mapController,
            StrategyFogOfWarController fogController,
            StrategyBuildPlacementController placementController,
            StrategyTimeScaleController timeScaleController,
            StrategyPointOfInterestDialogController dialogController,
            Vector2Int startupCampCell,
            bool generateImmediately)
        {
            Configure(
                mapController,
                fogController,
                placementController,
                null,
                timeScaleController,
                dialogController,
                startupCampCell,
                generateImmediately);
        }

        public void Configure(
            CityMapController mapController,
            StrategyFogOfWarController fogController,
            StrategyBuildPlacementController placementController,
            StrategyNaturePropController natureController,
            StrategyTimeScaleController timeScaleController,
            StrategyPointOfInterestDialogController dialogController,
            Vector2Int startupCampCell,
            bool generateImmediately)
        {
            ClearPointObjects();
            CancelPendingNotices();
            map = mapController;
            fog = fogController;
            placement = placementController;
            nature = natureController;
            timeScale = timeScaleController;
            dialog = dialogController;
            campCell = startupCampCell;
            configured = map != null && fog != null;
            Active = this;
            EnsurePointRoot();

            if (configured && generateImmediately)
            {
                GenerateDefaultPoints();
            }

            StrategyDebugLogger.Info(
                "PointOfInterest",
                "Configured",
                StrategyDebugLogger.F("generateImmediately", generateImmediately),
                StrategyDebugLogger.F("campCell", campCell),
                StrategyDebugLogger.F("placedBuildings", placement != null ? placement.PlacedBuildings.Count : 0),
                StrategyDebugLogger.F("points", points.Count));
        }

        public bool TryReserveNearestDiscovered(
            StrategyResidentAgent resident,
            Vector2Int origin,
            out StrategyPointOfInterest point)
        {
            point = null;
            if (!configured || resident == null || map == null || fog == null)
            {
                return false;
            }

            PruneMissingPoints();
            PruneTemporarilyUnreachable();
            long bestDistanceSquared = long.MaxValue;
            StrategyPointOfInterest best = null;
            for (int i = 0; i < points.Count; i++)
            {
                StrategyPointOfInterest candidate = points[i];
                if (candidate == null
                    || candidate.IsInvestigated
                    || candidate.IsReserved && !candidate.IsReservedBy(resident)
                    || temporarilyUnreachable.ContainsKey(candidate)
                    || !map.IsCellWalkable(candidate.Cell)
                    || !fog.IsCellPersistentlyExplored(candidate.Cell))
                {
                    continue;
                }

                long deltaX = (long)candidate.Cell.x - origin.x;
                long deltaY = (long)candidate.Cell.y - origin.y;
                long distanceSquared = deltaX * deltaX + deltaY * deltaY;
                if (best != null
                    && (distanceSquared > bestDistanceSquared
                        || distanceSquared == bestDistanceSquared
                        && string.CompareOrdinal(candidate.StableId, best.StableId) >= 0))
                {
                    continue;
                }

                best = candidate;
                bestDistanceSquared = distanceSquared;
            }

            if (best == null || !best.TryReserve(resident))
            {
                return false;
            }

            point = best;
            StrategyDebugLogger.Info(
                "PointOfInterest",
                "Reserved",
                StrategyDebugLogger.F("id", point.StableId),
                StrategyDebugLogger.F("cell", point.Cell),
                StrategyDebugLogger.F("resident", resident.FullName));
            return true;
        }

        public void ReleaseReservation(
            StrategyPointOfInterest point,
            StrategyResidentAgent resident)
        {
            if (point == null || resident == null || !points.Contains(point))
            {
                return;
            }

            bool wasReserved = point.IsReservedBy(resident);
            point.ReleaseReservation(resident);
            if (wasReserved)
            {
                StrategyDebugLogger.Info(
                    "PointOfInterest",
                    "ReservationReleased",
                    StrategyDebugLogger.F("id", point.StableId),
                    StrategyDebugLogger.F("resident", resident.FullName));
            }
        }

        public void MarkTemporarilyUnreachable(
            StrategyPointOfInterest point,
            StrategyResidentAgent resident)
        {
            if (point == null
                || resident == null
                || !points.Contains(point)
                || !point.IsReservedBy(resident))
            {
                return;
            }

            point.ReleaseReservation(resident);
            temporarilyUnreachable[point] = Time.time + TemporarilyUnreachableSeconds;
            StrategyDebugLogger.Warn(
                "PointOfInterest",
                "TemporarilyUnreachable",
                StrategyDebugLogger.F("id", point.StableId),
                StrategyDebugLogger.F("cell", point.Cell),
                StrategyDebugLogger.F("resident", resident.FullName),
                StrategyDebugLogger.F("retrySeconds", TemporarilyUnreachableSeconds));
        }

        public bool CompleteInvestigation(
            StrategyPointOfInterest point,
            StrategyResidentAgent resident)
        {
            if (point == null
                || resident == null
                || !points.Contains(point)
                || !point.MarkInvestigated(resident))
            {
                return false;
            }

            temporarilyUnreachable.Remove(point);
            pendingNotices.Enqueue(new PointNotice(
                GetInvestigationTitle(point.ResourceKind),
                GetInvestigationBody(point, resident)));
            StrategyDebugLogger.Info(
                "PointOfInterest",
                "Investigated",
                StrategyDebugLogger.F("id", point.StableId),
                StrategyDebugLogger.F("cell", point.Cell),
                StrategyDebugLogger.F("resourceKind", point.ResourceKind),
                StrategyDebugLogger.F("resident", resident.FullName));
            TryShowNextNotice();
            return true;
        }

        private void Update()
        {
            PruneTemporarilyUnreachable();
            if (noticeOpen
                && Time.frameCount > noticeOpenedFrame
                && (dialog == null || !dialog.IsOpen))
            {
                HandleNoticeAcknowledged(activeNoticeToken);
            }

            TryShowNextNotice();
        }

        private void GenerateDefaultPoints()
        {
            if (!configured || map == null)
            {
                return;
            }

            ClearPointObjects();
            HashSet<Vector2Int> forageCells = CaptureForageCells();
            List<StrategyPointOfInterestPlan> planned = StrategyPointOfInterestPlacement.SelectResourcePlans(
                map.Width,
                map.Height,
                map.ActiveSeed,
                campCell,
                StrategyPointOfInterestPlacement.DefaultResourcePointCount,
                map.IsCellWalkable,
                cell => CanUsePlannedCell(cell, forageCells));
            bool layoutComplete = HasExpectedMineralDistribution(planned);
            if (!layoutComplete)
            {
                StrategyDebugLogger.Warn(
                    "PointOfInterest",
                    "OwnedMineralLayoutShort",
                    StrategyDebugLogger.F("planned", planned.Count),
                    StrategyDebugLogger.F("target", StrategyPointOfInterestPlacement.DefaultResourcePointCount),
                    StrategyDebugLogger.F("coal", CountPlannedKind(planned, StrategyPointOfInterestResourceKind.Coal)),
                    StrategyDebugLogger.F("iron", CountPlannedKind(planned, StrategyPointOfInterestResourceKind.Iron)));
            }

            for (int i = 0; i < planned.Count; i++)
            {
                StrategyPointOfInterestPlan plan = planned[i];
                int mineralAmount = plan.HasMineralSite ? GetInitialMineralAmount(plan, i) : 0;
                if (plan.HasMineralSite
                    && (nature == null
                        || !nature.TryCreatePointOfInterestMineral(
                            plan.ResourceKind,
                            plan.MineralOrigin,
                            mineralAmount,
                            i)))
                {
                    StrategyDebugLogger.Warn(
                        "PointOfInterest",
                        "OwnedMineralCreateFailed",
                        StrategyDebugLogger.F("index", i),
                        StrategyDebugLogger.F("resourceKind", plan.ResourceKind),
                        StrategyDebugLogger.F("origin", plan.MineralOrigin));
                    continue;
                }

                if (!TryCreatePoint(
                        StrategyPointOfInterest.BuildStableId(plan.Cell),
                        plan.Cell,
                        plan.ResourceKind,
                        plan.HasMineralSite,
                        plan.MineralOrigin,
                        false)
                    && plan.HasMineralSite)
                {
                    nature.TryRemovePointOfInterestMineral(
                        plan.ResourceKind,
                        plan.MineralOrigin);
                }
            }

            StrategyDebugLogger.Info(
                "PointOfInterest",
                "Generated",
                StrategyDebugLogger.F("seed", map.ActiveSeed),
                StrategyDebugLogger.F("points", points.Count),
                StrategyDebugLogger.F("target", StrategyPointOfInterestPlacement.DefaultResourcePointCount),
                StrategyDebugLogger.F("coal", CountResourceKind(StrategyPointOfInterestResourceKind.Coal)),
                StrategyDebugLogger.F("iron", CountResourceKind(StrategyPointOfInterestResourceKind.Iron)),
                StrategyDebugLogger.F("layoutComplete", layoutComplete),
                StrategyDebugLogger.F("campCell", campCell));
        }

        private bool TryCreatePoint(
            string stableId,
            Vector2Int cell,
            StrategyPointOfInterestResourceKind resourceKind,
            bool hasMineralSite,
            Vector2Int mineralOrigin,
            bool investigated)
        {
            EnsurePointRoot();
            GameObject pointObject = null;
            try
            {
                Vector3 world = map.GetCellCenterWorld(cell.x, cell.y);
                pointObject = new GameObject("Point of Interest " + stableId);
                pointObject.transform.SetParent(pointRoot, false);
                pointObject.transform.position = new Vector3(world.x, world.y, -0.105f);
                SpriteRenderer renderer = pointObject.AddComponent<SpriteRenderer>();
                StrategyPointOfInterest point = pointObject.AddComponent<StrategyPointOfInterest>();
                point.Configure(
                    map,
                    stableId,
                    cell,
                    resourceKind,
                    hasMineralSite,
                    mineralOrigin,
                    investigated,
                    renderer);
                points.Add(point);
                return true;
            }
            catch (Exception exception)
            {
                if (pointObject != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(pointObject);
                    }
                    else
                    {
                        DestroyImmediate(pointObject);
                    }
                }

                StrategyDebugLogger.Error(
                    "PointOfInterest",
                    "MarkerCreateFailed",
                    StrategyDebugLogger.F("id", stableId),
                    StrategyDebugLogger.F("cell", cell),
                    StrategyDebugLogger.F("exception", exception.GetType().Name));
                return false;
            }
        }

        private HashSet<Vector2Int> CaptureForageCells()
        {
            HashSet<Vector2Int> cells = new();
            StrategyForageResourceController forage = StrategyForageResourceController.Active;
            if (forage == null)
            {
                return cells;
            }

            IReadOnlyList<StrategyForageNode> nodes = forage.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null)
                {
                    cells.Add(nodes[i].Cell);
                }
            }

            return cells;
        }

        private void ClearPointObjects()
        {
            temporarilyUnreachable.Clear();
            for (int i = points.Count - 1; i >= 0; i--)
            {
                StrategyPointOfInterest point = points[i];
                if (point == null)
                {
                    continue;
                }

                if (point.HasMineralSite)
                {
                    nature?.TryRemovePointOfInterestMineral(
                        point.ResourceKind,
                        point.MineralOrigin);
                }

                point.ReleaseMapBuildability();
                if (Application.isPlaying)
                {
                    Destroy(point.gameObject);
                }
                else
                {
                    DestroyImmediate(point.gameObject);
                }
            }

            points.Clear();
        }

        private void PruneMissingPoints()
        {
            for (int i = points.Count - 1; i >= 0; i--)
            {
                if (points[i] == null)
                {
                    points.RemoveAt(i);
                }
            }
        }

        private void PruneTemporarilyUnreachable()
        {
            if (temporarilyUnreachable.Count <= 0)
            {
                return;
            }

            pointScratch.Clear();
            foreach (KeyValuePair<StrategyPointOfInterest, float> pair in temporarilyUnreachable)
            {
                if (pair.Key == null || pair.Value <= Time.time)
                {
                    pointScratch.Add(pair.Key);
                }
            }

            for (int i = 0; i < pointScratch.Count; i++)
            {
                temporarilyUnreachable.Remove(pointScratch[i]);
            }

            pointScratch.Clear();
        }

        private void EnsurePointRoot()
        {
            if (pointRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("Points of Interest");
            root.transform.SetParent(transform, false);
            pointRoot = root.transform;
        }

        private static string FormatCell(Vector2Int cell)
        {
            return "(" + cell.x + ", " + cell.y + ")";
        }

        private void OnDisable()
        {
            CancelPendingNotices();
        }

        private void OnDestroy()
        {
            CancelPendingNotices();
            ClearPointObjects();
            if (Active == this)
            {
                Active = null;
            }
        }

    }
}
