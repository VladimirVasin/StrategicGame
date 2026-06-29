using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyTrailController
    {
        private static readonly bool RouteNetworkEnabled = false;

        private const float RouteNetworkTickSeconds = 1.0f;
        private const float RouteNetworkScanSeconds = 8.0f;
        private const float RouteNetworkRetrySeconds = 12.0f;
        private const int RouteNetworkSupportTraversals = 13;
        private const int RouteNetworkQueriesPerTick = 1;
        private const int RouteNetworkCandidateLimit = 4;
        private const int RouteNetworkEndpointSearchRadius = 3;

        private readonly StrategyTrailPathfinder routeNetworkPathfinder = new();
        private readonly Queue<RouteNetworkTask> routeNetworkQueue = new();
        private readonly HashSet<string> routeNetworkPendingKeys = new();
        private readonly Dictionary<string, float> routeNetworkRetryTimes = new();
        private readonly HashSet<StrategyPlacedBuilding> routeNetworkConnectedBuildings = new();
        private readonly List<StrategyPlacedBuilding> routeNetworkBuildingsScratch = new();
        private readonly List<StrategyPlacedBuilding> routeNetworkRemoveScratch = new();
        private readonly List<RouteNetworkCandidate> routeNetworkCandidateScratch = new();
        private readonly List<Vector2Int> routeNetworkRouteCellsScratch = new();
        private StrategyBuildPlacementController routeNetworkPlacement;
        private float routeNetworkTickTimer;
        private float routeNetworkScanTimer;
        private bool routeNetworkNeedsScan = true;

        public void ConfigureRouteNetwork(StrategyBuildPlacementController placementController)
        {
            if (!RouteNetworkEnabled)
            {
                UnsubscribeRouteNetworkPlacement();
                return;
            }

            if (routeNetworkPlacement == placementController)
            {
                routeNetworkNeedsScan = true;
                return;
            }

            UnsubscribeRouteNetworkPlacement();
            routeNetworkPlacement = placementController;
            if (routeNetworkPlacement != null)
            {
                routeNetworkPlacement.BuildingCompleted += HandleRouteNetworkBuildingCompleted;
            }

            routeNetworkNeedsScan = true;
        }

        private void ResetRouteNetwork()
        {
            routeNetworkQueue.Clear();
            routeNetworkPendingKeys.Clear();
            routeNetworkRetryTimes.Clear();
            routeNetworkConnectedBuildings.Clear();
            routeNetworkBuildingsScratch.Clear();
            routeNetworkRemoveScratch.Clear();
            routeNetworkCandidateScratch.Clear();
            routeNetworkRouteCellsScratch.Clear();
            routeNetworkTickTimer = 0f;
            routeNetworkScanTimer = 0f;
            routeNetworkNeedsScan = true;
        }

        private void UpdateRouteNetwork(float elapsed)
        {
            if (!RouteNetworkEnabled || map == null)
            {
                return;
            }

            float delta = Mathf.Max(0f, elapsed);
            routeNetworkTickTimer += delta;
            routeNetworkScanTimer += delta;
            if (routeNetworkTickTimer < RouteNetworkTickSeconds)
            {
                return;
            }

            routeNetworkTickTimer = 0f;
            if (routeNetworkNeedsScan || routeNetworkScanTimer >= RouteNetworkScanSeconds)
            {
                routeNetworkScanTimer = 0f;
                routeNetworkNeedsScan = false;
                ScheduleRouteNetworkMaintenance();
            }

            for (int i = 0; i < RouteNetworkQueriesPerTick && routeNetworkQueue.Count > 0; i++)
            {
                ProcessRouteNetworkTask(routeNetworkQueue.Dequeue());
            }
        }

        private void RequestRouteNetworkScan()
        {
            if (!RouteNetworkEnabled)
            {
                return;
            }

            routeNetworkNeedsScan = true;
        }

        private void UnsubscribeRouteNetworkPlacement()
        {
            if (routeNetworkPlacement != null)
            {
                routeNetworkPlacement.BuildingCompleted -= HandleRouteNetworkBuildingCompleted;
                routeNetworkPlacement = null;
            }
        }

        private void HandleRouteNetworkBuildingCompleted(StrategyPlacedBuilding building)
        {
            if (!IsRouteNetworkEligibleBuilding(building))
            {
                return;
            }

            routeNetworkNeedsScan = true;
            RebuildRouteNetworkBuildings();
            if (routeNetworkConnectedBuildings.Count <= 0)
            {
                routeNetworkConnectedBuildings.Add(building);
                return;
            }

            EnqueueNearestConnectedRouteTasks(building, RouteNetworkSupportTraversals, RouteNetworkCandidateLimit);
        }

        private void ScheduleRouteNetworkMaintenance()
        {
            RebuildRouteNetworkBuildings();
            if (routeNetworkBuildingsScratch.Count <= 1)
            {
                return;
            }

            for (int i = 0; i < routeNetworkBuildingsScratch.Count; i++)
            {
                StrategyPlacedBuilding building = routeNetworkBuildingsScratch[i];
                if (routeNetworkConnectedBuildings.Contains(building))
                {
                    EnqueueNearestConnectedRouteTasks(building, 1, 1);
                }
                else
                {
                    EnqueueNearestConnectedRouteTasks(building, RouteNetworkSupportTraversals, RouteNetworkCandidateLimit);
                }
            }
        }

        private void RebuildRouteNetworkBuildings()
        {
            routeNetworkBuildingsScratch.Clear();
            IReadOnlyList<StrategyPlacedBuilding> buildings = StrategyPlacedBuilding.ActiveBuildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (IsRouteNetworkEligibleBuilding(building))
                {
                    routeNetworkBuildingsScratch.Add(building);
                }
            }

            routeNetworkRemoveScratch.Clear();
            foreach (StrategyPlacedBuilding connected in routeNetworkConnectedBuildings)
            {
                if (!ContainsRouteNetworkBuilding(connected))
                {
                    routeNetworkRemoveScratch.Add(connected);
                }
            }

            for (int i = 0; i < routeNetworkRemoveScratch.Count; i++)
            {
                routeNetworkConnectedBuildings.Remove(routeNetworkRemoveScratch[i]);
            }

            if (routeNetworkConnectedBuildings.Count <= 0 && routeNetworkBuildingsScratch.Count > 0)
            {
                routeNetworkConnectedBuildings.Add(routeNetworkBuildingsScratch[0]);
            }
        }

        private bool ContainsRouteNetworkBuilding(StrategyPlacedBuilding building)
        {
            if (building == null)
            {
                return false;
            }

            for (int i = 0; i < routeNetworkBuildingsScratch.Count; i++)
            {
                if (routeNetworkBuildingsScratch[i] == building)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnqueueNearestConnectedRouteTasks(StrategyPlacedBuilding building, int traversals, int limit)
        {
            if (!IsRouteNetworkEligibleBuilding(building) || routeNetworkConnectedBuildings.Count <= 0)
            {
                return;
            }

            routeNetworkCandidateScratch.Clear();
            foreach (StrategyPlacedBuilding connected in routeNetworkConnectedBuildings)
            {
                if (!IsRouteNetworkEligibleBuilding(connected) || connected == building)
                {
                    continue;
                }

                float distance = (connected.FootprintBounds.center - building.FootprintBounds.center).sqrMagnitude;
                routeNetworkCandidateScratch.Add(new RouteNetworkCandidate(connected, distance));
            }

            routeNetworkCandidateScratch.Sort((left, right) => left.Distance.CompareTo(right.Distance));
            int enqueued = 0;
            for (int i = 0; i < routeNetworkCandidateScratch.Count && enqueued < limit; i++)
            {
                if (TryEnqueueRouteNetworkTask(building, routeNetworkCandidateScratch[i].Building, traversals))
                {
                    enqueued++;
                }
            }
        }

        private bool TryEnqueueRouteNetworkTask(StrategyPlacedBuilding from, StrategyPlacedBuilding to, int traversals)
        {
            if (!IsRouteNetworkEligibleBuilding(from) || !IsRouteNetworkEligibleBuilding(to) || from == to)
            {
                return false;
            }

            string key = GetBuildingRouteKey(from, to);
            if (routeNetworkPendingKeys.Contains(key))
            {
                return false;
            }

            if (routeNetworkRetryTimes.TryGetValue(key, out float retryTime) && Time.time < retryTime)
            {
                return false;
            }

            routeNetworkPendingKeys.Add(key);
            routeNetworkQueue.Enqueue(new RouteNetworkTask(from, to, Mathf.Max(1, traversals), key));
            return true;
        }

        private void ProcessRouteNetworkTask(RouteNetworkTask task)
        {
            routeNetworkPendingKeys.Remove(task.Key);
            if (!IsRouteNetworkEligibleBuilding(task.From)
                || !IsRouteNetworkEligibleBuilding(task.To)
                || !ContainsActiveRouteNetworkBuilding(task.From)
                || !ContainsActiveRouteNetworkBuilding(task.To))
            {
                return;
            }

            if (!TryRecordRouteNetworkTraversal(task))
            {
                routeNetworkRetryTimes[task.Key] = Time.time + RouteNetworkRetrySeconds;
                StrategyDebugLogger.Info(
                    "Map",
                    "TrailNetworkRouteDeferred",
                    StrategyDebugLogger.F("fromTool", task.From.Tool),
                    StrategyDebugLogger.F("fromOrigin", task.From.Origin),
                    StrategyDebugLogger.F("toTool", task.To.Tool),
                    StrategyDebugLogger.F("toOrigin", task.To.Origin));
                return;
            }

            routeNetworkRetryTimes.Remove(task.Key);
            routeNetworkConnectedBuildings.Add(task.From);
            routeNetworkConnectedBuildings.Add(task.To);
            if (task.RemainingTraversals > 1)
            {
                TryEnqueueRouteNetworkTask(task.From, task.To, task.RemainingTraversals - 1);
            }
        }

        private bool TryRecordRouteNetworkTraversal(RouteNetworkTask task)
        {
            if (!TryGetRouteNetworkEndpoint(task.From, out Vector2Int startCell)
                || !TryGetRouteNetworkEndpoint(task.To, out Vector2Int targetCell)
                || !routeNetworkPathfinder.TryBuildPath(map, startCell, targetCell))
            {
                return false;
            }

            StrategyTrailRouteCellBuilder.BuildRouteCells(
                map,
                startCell,
                routeNetworkPathfinder.RawCells,
                routeNetworkRouteCellsScratch);
            if (routeNetworkRouteCellsScratch.Count < 2)
            {
                return false;
            }

            RecordBuildingRouteTraversal(task.From, task.To, routeNetworkRouteCellsScratch);
            return true;
        }

        private bool TryGetRouteNetworkEndpoint(StrategyPlacedBuilding building, out Vector2Int cell)
        {
            cell = default;
            if (!IsRouteNetworkEligibleBuilding(building) || map == null)
            {
                return false;
            }

            float bestScore = float.MaxValue;
            Vector3 center = building.FootprintBounds.center;
            Vector2Int origin = building.Origin;
            Vector2Int footprint = building.Footprint;
            int minX = origin.x - RouteNetworkEndpointSearchRadius;
            int minY = origin.y - RouteNetworkEndpointSearchRadius;
            int maxX = origin.x + footprint.x + RouteNetworkEndpointSearchRadius;
            int maxY = origin.y + footprint.y + RouteNetworkEndpointSearchRadius;

            for (int y = minY; y < maxY; y++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    Vector2Int candidate = new Vector2Int(x, y);
                    if (GetWearRejectReason(candidate) != null)
                    {
                        continue;
                    }

                    Vector3 candidateWorld = map.GetCellCenterWorld(x, y);
                    float score = (candidateWorld - center).sqrMagnitude;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        cell = candidate;
                    }
                }
            }

            return bestScore < float.MaxValue;
        }

        private static bool IsRouteNetworkEligibleBuilding(StrategyPlacedBuilding building)
        {
            return building != null && building.Tool != StrategyBuildTool.Bridge;
        }

        private static bool ContainsActiveRouteNetworkBuilding(StrategyPlacedBuilding building)
        {
            IReadOnlyList<StrategyPlacedBuilding> buildings = StrategyPlacedBuilding.ActiveBuildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                if (buildings[i] == building)
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct RouteNetworkTask
        {
            public RouteNetworkTask(StrategyPlacedBuilding from, StrategyPlacedBuilding to, int remainingTraversals, string key)
            {
                From = from;
                To = to;
                RemainingTraversals = remainingTraversals;
                Key = key;
            }

            public StrategyPlacedBuilding From { get; }
            public StrategyPlacedBuilding To { get; }
            public int RemainingTraversals { get; }
            public string Key { get; }
        }

        private readonly struct RouteNetworkCandidate
        {
            public RouteNetworkCandidate(StrategyPlacedBuilding building, float distance)
            {
                Building = building;
                Distance = distance;
            }

            public StrategyPlacedBuilding Building { get; }
            public float Distance { get; }
        }
    }
}
