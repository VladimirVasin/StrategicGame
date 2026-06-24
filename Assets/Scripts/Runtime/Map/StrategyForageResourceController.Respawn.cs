using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyForageResourceController
    {
        private const float RespawnSecondsMin = 70f;
        private const float RespawnSecondsMax = 130f;
        private const float RespawnRetrySecondsMin = 8f;
        private const float RespawnRetrySecondsMax = 16f;
        private const int RespawnNearTreeMaxRadius = 4;

        private readonly List<ForageRespawnRequest> pendingRespawns = new();
        private readonly List<ForageRespawnCandidate> respawnCandidates = new();
        private int respawnSequence;

        public float HandleNodeGathered(StrategyForageNode node, StrategyResourceType resource)
        {
            if (node != null)
            {
                UnregisterNode(node);
            }

            float delay = Random.Range(RespawnSecondsMin, RespawnSecondsMax);
            pendingRespawns.Add(new ForageRespawnRequest(resource, delay, ++respawnSequence));
            return delay;
        }

        private void Update()
        {
            if (pendingRespawns.Count <= 0)
            {
                return;
            }

            TickRespawns(Time.deltaTime);
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

                if (TryRespawnNearTree(request.Resource, request.Salt))
                {
                    pendingRespawns.RemoveAt(i);
                    continue;
                }

                request.RemainingSeconds = Random.Range(RespawnRetrySecondsMin, RespawnRetrySecondsMax);
                pendingRespawns[i] = request;
            }
        }

        private bool TryRespawnNearTree(StrategyResourceType preferredResource, int salt)
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

            respawnCandidates.Clear();
            IReadOnlyList<StrategyForestryTree> trees = forestry.Trees;
            for (int i = 0; i < trees.Count; i++)
            {
                StrategyForestryTree tree = trees[i];
                if (!IsRespawnAnchorTree(tree))
                {
                    continue;
                }

                AddTreeRespawnCandidates(tree.Cell, preferredResource, salt + i * 37);
            }

            if (respawnCandidates.Count <= 0)
            {
                return false;
            }

            respawnCandidates.Sort((left, right) => left.Score.CompareTo(right.Score));
            ForageRespawnCandidate best = respawnCandidates[0];
            CreateNode(best.Cell, best.Resource, best.Salt);
            StrategyDebugLogger.Info(
                "Forage",
                "NodeRespawnedNearTree",
                StrategyDebugLogger.F("resource", best.Resource),
                StrategyDebugLogger.F("cell", best.Cell),
                StrategyDebugLogger.F("anchorCell", best.AnchorCell),
                StrategyDebugLogger.F("pending", pendingRespawns.Count));
            return true;
        }

        private void AddTreeRespawnCandidates(Vector2Int anchorCell, StrategyResourceType preferredResource, int salt)
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

                        float score = radius * 10f
                            + Hash01(map.ActiveSeed, cell.x, cell.y, salt)
                            + Hash01(map.ActiveSeed, anchorCell.x, anchorCell.y, salt + 17) * 0.35f;
                        respawnCandidates.Add(new ForageRespawnCandidate(cell, anchorCell, resource, salt, score));
                    }
                }
            }
        }

        private bool TryGetRespawnResource(
            Vector2Int cell,
            StrategyResourceType preferredResource,
            int salt,
            out StrategyResourceType resource)
        {
            resource = StrategyResourceType.None;
            if (usedCells.Contains(cell)
                || IsTooCloseToStarter(cell, 3)
                || !map.IsCellWalkable(cell)
                || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return false;
            }

            if (preferredResource != StrategyResourceType.None
                && IsResourceAllowedOnTerrain(preferredResource, mapCell.Kind))
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
            resource = kind switch
            {
                CityMapCellKind.Forest => pick < 0.56f
                    ? StrategyResourceType.Mushrooms
                    : pick < 0.82f
                        ? StrategyResourceType.Berries
                        : StrategyResourceType.Roots,
                CityMapCellKind.Meadow => pick < 0.56f
                    ? StrategyResourceType.Berries
                    : pick < 0.82f
                        ? StrategyResourceType.Roots
                        : StrategyResourceType.Mushrooms,
                CityMapCellKind.Grass => pick < 0.52f
                    ? StrategyResourceType.Roots
                    : pick < 0.82f
                        ? StrategyResourceType.Berries
                        : StrategyResourceType.Mushrooms,
                CityMapCellKind.Dirt => pick < 0.75f ? StrategyResourceType.Roots : StrategyResourceType.Mushrooms,
                _ => StrategyResourceType.None
            };
            return resource != StrategyResourceType.None
                && IsResourceAllowedOnTerrain(resource, kind);
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
            public readonly StrategyResourceType Resource;
            public readonly int Salt;
            public readonly float Score;

            public ForageRespawnCandidate(
                Vector2Int cell,
                Vector2Int anchorCell,
                StrategyResourceType resource,
                int salt,
                float score)
            {
                Cell = cell;
                AnchorCell = anchorCell;
                Resource = resource;
                Salt = salt;
                Score = score;
            }
        }
    }
}
