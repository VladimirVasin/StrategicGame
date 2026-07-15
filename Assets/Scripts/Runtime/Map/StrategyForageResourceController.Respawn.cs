using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyForageResourceController
    {
        private const float RespawnSecondsMin = 38f;
        private const float RespawnSecondsMax = 72f;
        private const float RespawnRetrySecondsMin = 5f;
        private const float RespawnRetrySecondsMax = 10f;
        private const float CampSupportSpawnSecondsMin = 12f;
        private const float CampSupportSpawnSecondsMax = 22f;
        private const int RespawnNearTreeMaxRadius = 5;
        private const int CampForageSupportTarget = 10;
        private const int CampForageSoftCap = 18;

        private readonly List<ForageRespawnRequest> pendingRespawns = new();
        private readonly List<ForageRespawnCandidate> respawnCandidates = new();
        private readonly List<StrategyForagerCamp> foragerCampQuery = new();
        private int respawnSequence;
        private float campSupportSpawnTimer;

        public float HandleNodeGathered(StrategyForageNode node, StrategyResourceType resource)
        {
            if (node != null)
            {
                UnregisterNode(node);
            }

            float delay = GetSeasonalForageRespawnDelay(resource);
            pendingRespawns.Add(new ForageRespawnRequest(resource, delay, ++respawnSequence));
            return delay;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (pendingRespawns.Count > 0)
            {
                TickRespawns(deltaTime);
            }

            TickCampSupportSpawns(deltaTime);
        }

        private void TickRespawns(float deltaTime)
        {
            for (int i = pendingRespawns.Count - 1; i >= 0; i--)
            {
                ForageRespawnRequest request = pendingRespawns[i];
                request.RemainingSeconds -= deltaTime;
                if (request.RemainingSeconds > 0f)
                {
                    pendingRespawns[i] = request;
                    continue;
                }

                if (TryRespawnNearForagerCamp(request.Resource, request.Salt))
                {
                    pendingRespawns.RemoveAt(i);
                    continue;
                }

                request.RemainingSeconds = Random.Range(RespawnRetrySecondsMin, RespawnRetrySecondsMax);
                pendingRespawns[i] = request;
            }
        }

        private void TickCampSupportSpawns(float deltaTime)
        {
            if (map == null || forageRoot == null || nodes.Count >= MaxForageNodes)
            {
                return;
            }

            campSupportSpawnTimer -= deltaTime;
            if (campSupportSpawnTimer > 0f)
            {
                return;
            }

            campSupportSpawnTimer = GetSeasonalCampSupportSpawnDelay();
            if (TryFindCampNeedingForage(out StrategyForagerCamp camp, out int localNodes)
                && TryRespawnNearForagerCamp(StrategyResourceType.None, ++respawnSequence, camp))
            {
                StrategyDebugLogger.Info(
                    "Forage",
                    "CampSupportSpawned",
                    StrategyDebugLogger.F("campOrigin", camp.Origin),
                    StrategyDebugLogger.F("localNodesBefore", localNodes),
                    StrategyDebugLogger.F("target", GetSeasonalCampSupportTarget()),
                    StrategyDebugLogger.F("season", StrategyDayNightCycleController.CurrentCalendarSnapshot.SeasonLabel));
            }
        }

        private bool TryRespawnNearForagerCamp(
            StrategyResourceType preferredResource,
            int salt,
            StrategyForagerCamp forcedCamp = null)
        {
            if (map == null || forageRoot == null)
            {
                return false;
            }

            RemoveMissingNodes();
            if (nodes.Count >= MaxForageNodes)
            {
                return false;
            }

            StrategyForestryController forestry = StrategyForestryController.Active;
            if (forestry == null)
            {
                return false;
            }

            List<StrategyForagerCamp> camps = GetForagerCampQuery(forcedCamp);
            if (camps.Count <= 0)
            {
                return false;
            }

            respawnCandidates.Clear();
            IReadOnlyList<StrategyForestryTree> trees = forestry.Trees;
            for (int i = 0; i < trees.Count; i++)
            {
                StrategyForestryTree tree = trees[i];
                if (!IsRespawnAnchorTree(tree))
                {
                    continue;
                }

                if (!IsTreeNearForagerCamp(tree.Cell, camps))
                {
                    continue;
                }

                AddTreeRespawnCandidates(tree.Cell, preferredResource, salt + i * 37, camps);
            }

            if (respawnCandidates.Count <= 0)
            {
                return false;
            }

            respawnCandidates.Sort((left, right) => left.Score.CompareTo(right.Score));
            ForageRespawnCandidate best = respawnCandidates[0];
            if (!TryCreateNode(best.Cell, best.Resource, best.Salt))
            {
                return false;
            }

            StrategyDebugLogger.Info(
                "Forage",
                forcedCamp != null ? "NodeSpawnedNearForagerCamp" : "NodeRespawnedNearForagerCamp",
                StrategyDebugLogger.F("resource", best.Resource),
                StrategyDebugLogger.F("cell", best.Cell),
                StrategyDebugLogger.F("anchorCell", best.AnchorCell),
                StrategyDebugLogger.F("campOrigin", best.CampOrigin),
                StrategyDebugLogger.F("campLocalNodes", best.CampNodeCount),
                StrategyDebugLogger.F("pending", pendingRespawns.Count));
            return true;
        }

        private void AddTreeRespawnCandidates(
            Vector2Int anchorCell,
            StrategyResourceType preferredResource,
            int salt,
            IReadOnlyList<StrategyForagerCamp> camps)
        {
            for (int radius = 1; radius <= RespawnNearTreeMaxRadius; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
                        {
                            continue;
                        }

                        Vector2Int cell = anchorCell + new Vector2Int(x, y);
                        if (!TryGetRespawnResource(cell, preferredResource, salt, out StrategyResourceType resource))
                        {
                            continue;
                        }

                        if (!TryFindCampForRespawnCell(
                            cell,
                            camps,
                            out Vector2Int campOrigin,
                            out int campNodeCount,
                            out int campDistanceSqr))
                        {
                            continue;
                        }

                        float score = radius * 10f
                            + campNodeCount * 3.5f
                            + campDistanceSqr * 0.018f
                            + Hash01(map.ActiveSeed, cell.x, cell.y, salt)
                            + Hash01(map.ActiveSeed, anchorCell.x, anchorCell.y, salt + 17) * 0.35f;
                        respawnCandidates.Add(new ForageRespawnCandidate(
                            cell,
                            anchorCell,
                            campOrigin,
                            resource,
                            salt,
                            score,
                            campNodeCount));
                    }
                }
            }
        }

        private bool TryFindCampForRespawnCell(
            Vector2Int cell,
            IReadOnlyList<StrategyForagerCamp> camps,
            out Vector2Int campOrigin,
            out int campNodeCount,
            out int campDistanceSqr)
        {
            campOrigin = default;
            campNodeCount = 0;
            campDistanceSqr = 0;
            if (camps == null || camps.Count <= 0)
            {
                return false;
            }

            bool found = false;
            int bestNodeCount = int.MaxValue;
            int bestDistanceSqr = int.MaxValue;
            int radiusSqr = StrategyForagerCamp.WorkRadius * StrategyForagerCamp.WorkRadius;
            for (int i = 0; i < camps.Count; i++)
            {
                StrategyForagerCamp camp = camps[i];
                if (!TryGetForagerCampCenterCell(camp, out Vector2Int center))
                {
                    continue;
                }

                Vector2Int delta = cell - center;
                int distanceSqr = delta.x * delta.x + delta.y * delta.y;
                if (distanceSqr > radiusSqr)
                {
                    continue;
                }

                int localNodes = CountForageNodesNear(center, StrategyForagerCamp.WorkRadius);
                if (localNodes >= GetSeasonalCampForageSoftCap())
                {
                    continue;
                }

                if (localNodes > bestNodeCount
                    || (localNodes == bestNodeCount && distanceSqr >= bestDistanceSqr))
                {
                    continue;
                }

                found = true;
                bestNodeCount = localNodes;
                bestDistanceSqr = distanceSqr;
                campOrigin = camp.Origin;
                campNodeCount = localNodes;
                campDistanceSqr = distanceSqr;
            }

            return found;
        }

        private bool TryFindCampNeedingForage(out StrategyForagerCamp camp, out int localNodes)
        {
            camp = null;
            localNodes = 0;
            List<StrategyForagerCamp> camps = GetForagerCampQuery(null);
            float bestScore = float.MaxValue;
            for (int i = 0; i < camps.Count; i++)
            {
                StrategyForagerCamp candidate = camps[i];
                if (!TryGetForagerCampCenterCell(candidate, out Vector2Int center))
                {
                    continue;
                }

                int count = CountForageNodesNear(center, StrategyForagerCamp.WorkRadius);
                if (count >= GetSeasonalCampSupportTarget())
                {
                    continue;
                }

                float score = count * 100f
                    + Hash01(map.ActiveSeed, candidate.Origin.x, candidate.Origin.y, respawnSequence + i * 11);
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                camp = candidate;
                localNodes = count;
            }

            return camp != null;
        }

        private bool IsTreeNearForagerCamp(Vector2Int treeCell, IReadOnlyList<StrategyForagerCamp> camps)
        {
            if (camps == null || camps.Count <= 0)
            {
                return false;
            }

            int radius = StrategyForagerCamp.WorkRadius + RespawnNearTreeMaxRadius;
            int radiusSqr = radius * radius;
            for (int i = 0; i < camps.Count; i++)
            {
                if (!TryGetForagerCampCenterCell(camps[i], out Vector2Int center))
                {
                    continue;
                }

                Vector2Int delta = treeCell - center;
                if (delta.x * delta.x + delta.y * delta.y <= radiusSqr)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetRespawnResource(
            Vector2Int cell,
            StrategyResourceType preferredResource,
            int salt,
            out StrategyResourceType resource)
        {
            resource = StrategyResourceType.None;
            if (usedCells.Contains(cell)
                || StrategyPointOfInterestController.Active?.HasPointAt(cell) == true
                || IsTooCloseToStarter(cell, 3)
                || !HasLocalForageRoom(cell)
                || !map.IsCellWalkable(cell)
                || !map.IsCellBuildable(cell)
                || StrategyTrailController.Active?.HasRouteRoadAt(cell) == true
                || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return false;
            }

            if (preferredResource != StrategyResourceType.None
                && IsResourceAllowedOnTerrain(preferredResource, mapCell.Kind)
                && ShouldKeepPreferredRespawnResource(preferredResource, cell, salt))
            {
                resource = preferredResource;
                return true;
            }

            return TryChooseRespawnResource(mapCell.Kind, cell.x, cell.y, salt, out resource);
        }

        private static bool IsRespawnAnchorTree(StrategyForestryTree tree)
        {
            return tree != null
                && tree.IsMature
                && !tree.IsFalling
                && !tree.IsFelled;
        }

        private bool TryChooseRespawnResource(CityMapCellKind kind, int x, int y, int salt, out StrategyResourceType resource)
        {
            float pick = Hash01(map.ActiveSeed, x, y, salt + 203);
            resource = ChooseSeasonalForageResource(kind, pick);
            return resource != StrategyResourceType.None
                && IsResourceAllowedOnTerrain(resource, kind);
        }

        private List<StrategyForagerCamp> GetForagerCampQuery(StrategyForagerCamp forcedCamp)
        {
            foragerCampQuery.Clear();
            if (forcedCamp != null)
            {
                foragerCampQuery.Add(forcedCamp);
                return foragerCampQuery;
            }

            StrategyPlacedBuilding.CopyActiveComponents(foragerCampQuery);
            return foragerCampQuery;
        }

        private struct ForageRespawnRequest
        {
            public readonly StrategyResourceType Resource;
            public readonly int Salt;
            public float RemainingSeconds;

            public ForageRespawnRequest(StrategyResourceType resource, float remainingSeconds, int salt)
            {
                Resource = resource;
                RemainingSeconds = remainingSeconds;
                Salt = salt;
            }
        }

        private readonly struct ForageRespawnCandidate
        {
            public readonly Vector2Int Cell;
            public readonly Vector2Int AnchorCell;
            public readonly Vector2Int CampOrigin;
            public readonly StrategyResourceType Resource;
            public readonly int Salt;
            public readonly float Score;
            public readonly int CampNodeCount;

            public ForageRespawnCandidate(
                Vector2Int cell,
                Vector2Int anchorCell,
                Vector2Int campOrigin,
                StrategyResourceType resource,
                int salt,
                float score,
                int campNodeCount)
            {
                Cell = cell;
                AnchorCell = anchorCell;
                CampOrigin = campOrigin;
                Resource = resource;
                Salt = salt;
                Score = score;
                CampNodeCount = campNodeCount;
            }
        }
    }
}
