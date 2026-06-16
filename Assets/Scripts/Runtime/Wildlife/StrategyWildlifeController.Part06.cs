using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {

        private bool TryFindRabbitGroupCenterMapWide(int group, HashSet<Vector2Int> usedCells, out Vector2Int cell)
        {
            cell = default;
            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;

            for (int attempt = 0; attempt < SpawnSearchAttempts; attempt++)
            {
                int x = Hash(map.ActiveSeed, group, attempt, 431, 463) % map.Width;
                int y = Hash(map.ActiveSeed, group, attempt, 487, 541) % map.Height;
                Vector2Int candidate = new Vector2Int(x, y);
                if (usedCells.Contains(candidate) || !IsRabbitGroupCenterCandidate(candidate))
                {
                    continue;
                }

                float score = GetRabbitGroupCenterScore(candidate);
                if (hasCampCell)
                {
                    float distance = Vector2Int.Distance(candidate, campCell);
                    score += group < NearCampRabbitGroups
                        ? Mathf.Clamp(RabbitCampMaxDistance - distance, 0f, RabbitCampMaxDistance) * 0.08f
                        : Mathf.Clamp(distance - RabbitCampMaxDistance, 0f, 36f) * 0.035f;
                }

                score -= GetUsedCellSpacingPenalty(candidate, usedCells, 8, 0.24f);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            cell = bestCell;
            return true;
        }

        private float GetRabbitGroupCenterScore(Vector2Int candidate)
        {
            return GetRabbitSpawnTerrainScore(candidate) + CountWalkableNeighbors(candidate, 2) * 0.28f;
        }

        private bool TryFindRabbitSpawnCell(
            Vector2Int groupCenter,
            int group,
            int slot,
            HashSet<Vector2Int> usedCells,
            out Vector2Int cell)
        {
            for (int radius = 0; radius <= 4; radius++)
            {
                List<Vector2Int> candidates = new();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (radius > 0 && Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = groupCenter + new Vector2Int(x, y);
                        if (!usedCells.Contains(candidate) && IsRabbitSpawnCandidate(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    int index = Hash(map.ActiveSeed, group, slot, radius, 563) % candidates.Count;
                    cell = candidates[index];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private bool IsRabbitGroupCenterCandidate(Vector2Int cell)
        {
            return IsRabbitSpawnCandidate(cell)
                && GetRabbitSpawnTerrainScore(cell) > 0f
                && CountWalkableNeighbors(cell, 2) >= 6;
        }

        private StrategyFishSpecies PickFishSpecies(int shoal, Vector2Int shoalCenter)
        {
            int roll = Hash(map.ActiveSeed, shoal, shoalCenter.x, shoalCenter.y, 997) % 3;
            return roll switch
            {
                1 => StrategyFishSpecies.Carp,
                2 => StrategyFishSpecies.Perch,
                _ => StrategyFishSpecies.Minnow
            };
        }

        private StrategyBirdSpecies PickBirdSpecies(int bird)
        {
            int roll = Hash(map.ActiveSeed, bird, 811, 829, 853) % 10;
            return roll switch
            {
                <= 1 => StrategyBirdSpecies.Duck,
                <= 4 => StrategyBirdSpecies.Crow,
                _ => StrategyBirdSpecies.Sparrow
            };
        }

        private bool TryFindBirdSpawnCell(
            StrategyBirdSpecies species,
            int bird,
            HashSet<Vector2Int> usedBirdCells,
            out Vector2Int cell)
        {
            cell = default;
            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;

            for (int attempt = 0; attempt < SpawnSearchAttempts; attempt++)
            {
                int x = Hash(map.ActiveSeed, bird, attempt, 821, 859) % map.Width;
                int y = Hash(map.ActiveSeed, bird, attempt, 887, 907) % map.Height;
                Vector2Int candidate = new Vector2Int(x, y);
                if (usedBirdCells.Contains(candidate) || !IsBirdSpawnCandidate(species, candidate))
                {
                    continue;
                }

                float score = GetBirdSpawnTerrainScore(species, candidate);
                if (hasCampCell)
                {
                    float campDistance = Vector2Int.Distance(candidate, campCell);
                    if (species == StrategyBirdSpecies.Sparrow)
                    {
                        score += Mathf.Clamp(8f - campDistance, 0f, 8f) * 0.05f;
                    }
                    else if (species == StrategyBirdSpecies.Crow)
                    {
                        score += Mathf.Clamp(campDistance - 4f, 0f, 12f) * 0.04f;
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            cell = bestCell;
            return true;
        }

        private bool TryFindFishShoalCenter(
            FishWaterRegion region,
            int shoal,
            HashSet<Vector2Int> usedWaterCells,
            out Vector2Int cell)
        {
            cell = default;
            if (region == null || region.Cells.Count <= 0)
            {
                return false;
            }

            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;

            for (int attempt = 0; attempt < SpawnSearchAttempts; attempt++)
            {
                int index = Hash(map.ActiveSeed, region.Id, shoal, attempt, 601) % region.Cells.Count;
                Vector2Int candidate = region.Cells[index];
                if (usedWaterCells.Contains(candidate) || !IsFishShoalCenterCandidate(candidate, region.Id))
                {
                    continue;
                }

                float score = GetFishSpawnTerrainScore(candidate);
                if (hasCampCell)
                {
                    score += Mathf.Clamp(Vector2Int.Distance(candidate, campCell) - 5f, 0f, 14f) * 0.04f;
                }

                score -= GetUsedCellSpacingPenalty(candidate, usedWaterCells, 5, 0.35f);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            cell = bestCell;
            return true;
        }

        private bool TryFindFishSpawnCell(
            FishWaterRegion region,
            Vector2Int shoalCenter,
            int shoal,
            int slot,
            HashSet<Vector2Int> usedWaterCells,
            out Vector2Int cell)
        {
            if (region == null)
            {
                cell = default;
                return false;
            }

            for (int radius = 0; radius <= 5; radius++)
            {
                List<Vector2Int> candidates = new();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (radius > 0 && Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = shoalCenter + new Vector2Int(x, y);
                        if (!usedWaterCells.Contains(candidate)
                            && IsLakeFishSpawnCandidate(candidate, region.Id)
                            && CountWaterNeighbors(candidate, 1, CityMapWaterKind.Lake) >= 1)
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    int index = Hash(map.ActiveSeed, shoal, slot, radius, 761) % candidates.Count;
                    cell = candidates[index];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private bool IsFishShoalCenterCandidate(Vector2Int cell, int regionId)
        {
            return IsLakeFishSpawnCandidate(cell, regionId)
                && CountWaterNeighbors(cell, 1, CityMapWaterKind.Lake) >= 2
                && GetFishSpawnTerrainScore(cell) > 0f;
        }

        private bool TryFindHerdCenter(int herd, HashSet<Vector2Int> usedCells, out Vector2Int cell)
        {
            cell = default;
            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;

            for (int attempt = 0; attempt < SpawnSearchAttempts; attempt++)
            {
                int x = Hash(map.ActiveSeed, herd, attempt, 191, 251) % map.Width;
                int y = Hash(map.ActiveSeed, herd, attempt, 257, 313) % map.Height;
                Vector2Int candidate = new Vector2Int(x, y);
                if (usedCells.Contains(candidate) || !IsHerdCenterCandidate(candidate))
                {
                    continue;
                }

                float score = GetSpawnTerrainScore(candidate)
                    + CountWalkableNeighbors(candidate, 3) * 0.35f
                    - GetUsedCellSpacingPenalty(candidate, usedCells, 10, 0.35f);
                if (hasCampCell)
                {
                    score += Mathf.Clamp(Vector2Int.Distance(candidate, campCell) - CampAvoidRadius, 0f, 18f) * 0.12f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            cell = bestCell;
            return true;
        }

        private bool TryFindHerdSpawnCell(
            Vector2Int herdCenter,
            int herd,
            int slot,
            HashSet<Vector2Int> usedCells,
            out Vector2Int cell)
        {
            for (int radius = 0; radius <= 5; radius++)
            {
                List<Vector2Int> candidates = new();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (radius > 0 && Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = herdCenter + new Vector2Int(x, y);
                        if (!usedCells.Contains(candidate) && IsHerdSpawnCandidate(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    int index = Hash(map.ActiveSeed, herd, slot, radius, 337) % candidates.Count;
                    cell = candidates[index];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private bool IsHerdCenterCandidate(Vector2Int cell)
        {
            return IsHerdSpawnCandidate(cell)
                && GetSpawnTerrainScore(cell) > 0f
                && CountWalkableNeighbors(cell, 2) >= 5;
        }

        private bool IsHerdSpawnCandidate(Vector2Int cell)
        {
            if (map == null
                || !IsLandWildlifeTravelCell(cell)
                || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell)
                || mapCell.Kind == CityMapCellKind.Water)
            {
                return false;
            }

            if (hasCampCell)
            {
                float campDistance = Vector2Int.Distance(cell, campCell);
                if (campDistance < CampAvoidRadius)
                {
                    return false;
                }
            }

            return IsHiddenNearSettlementSpawnCell(cell, WildlifeSettlementSpawnKind.Deer)
                && (mapCell.Kind == CityMapCellKind.Meadow
                || mapCell.Kind == CityMapCellKind.Grass
                || mapCell.Kind == CityMapCellKind.Forest
                || mapCell.Kind == CityMapCellKind.Dirt);
        }

        private bool IsRabbitSpawnCandidate(Vector2Int cell)
        {
            if (map == null
                || !IsLandWildlifeTravelCell(cell)
                || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell)
                || mapCell.Kind == CityMapCellKind.Water
                || mapCell.Kind == CityMapCellKind.Shore)
            {
                return false;
            }

            if (hasCampCell && Vector2Int.Distance(cell, campCell) < CampAvoidRadius)
            {
                return false;
            }

            return IsHiddenNearSettlementSpawnCell(cell, WildlifeSettlementSpawnKind.Rabbit)
                && (mapCell.Kind == CityMapCellKind.Meadow
                || mapCell.Kind == CityMapCellKind.Grass
                || mapCell.Kind == CityMapCellKind.Forest
                || mapCell.Kind == CityMapCellKind.Dirt);
        }

        private bool IsFishSpawnCandidate(Vector2Int cell)
        {
            return IsFishSpawnCandidate(cell, CityMapWaterKind.Lake);
        }

        private bool IsFishSpawnCandidate(Vector2Int cell, CityMapWaterKind waterKind)
        {
            if (map == null
                || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell)
                || mapCell.Kind != CityMapCellKind.Water
                || mapCell.WaterKind != waterKind)
            {
                return false;
            }

            if (hasCampCell && Vector2Int.Distance(cell, campCell) < 5)
            {
                return false;
            }

            return IsHiddenNearSettlementSpawnCell(cell, WildlifeSettlementSpawnKind.Fish);
        }

        private bool IsLakeFishSpawnCandidate(Vector2Int cell, int regionId)
        {
            return IsFishSpawnCandidate(cell, CityMapWaterKind.Lake)
                && lakeRegionByCell.TryGetValue(cell, out int candidateRegionId)
                && candidateRegionId == regionId;
        }

        private bool IsDeerMigrationCandidate(Vector2Int cell)
        {
            if (map == null
                || !IsLandWildlifeTravelCell(cell)
                || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell)
                || mapCell.Kind == CityMapCellKind.Water
                || mapCell.Kind == CityMapCellKind.Shore)
            {
                return false;
            }

            if (hasCampCell && Vector2Int.Distance(cell, campCell) < CampAvoidRadius)
            {
                return false;
            }

            if (GetSettlementPressure(cell) > DeerMigrationSettlementLimit)
            {
                return false;
            }

            return IsHiddenNearSettlementSpawnCell(cell, WildlifeSettlementSpawnKind.Deer)
                && (mapCell.Kind == CityMapCellKind.Meadow
                || mapCell.Kind == CityMapCellKind.Grass
                || mapCell.Kind == CityMapCellKind.Forest
                || mapCell.Kind == CityMapCellKind.Dirt);
        }

        private float GetDeerMigrationScore(Vector2Int cell)
        {
            float score = GetSpawnTerrainScore(cell)
                + CountWalkableNeighbors(cell, 2) * 0.16f
                - GetSettlementPressure(cell) * 1.25f;
            if (hasCampCell)
            {
                score += Mathf.Clamp(Vector2Int.Distance(cell, campCell) - CampAvoidRadius, 0f, 22f) * 0.035f;
            }

            return score;
        }

        private bool IsRabbitMigrationCandidate(Vector2Int cell)
        {
            return IsRabbitSpawnCandidate(cell)
                && GetSettlementPressure(cell) <= RabbitMigrationSettlementLimit;
        }
    }
}
