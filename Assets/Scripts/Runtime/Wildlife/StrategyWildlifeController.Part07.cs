using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {

        private float GetRabbitMigrationScore(Vector2Int cell)
        {
            return GetRabbitSpawnTerrainScore(cell)
                + CountWalkableNeighbors(cell, 2) * 0.12f
                - GetSettlementPressure(cell) * 1.1f;
        }

        private bool IsWolfMigrationCandidate(Vector2Int cell)
        {
            return IsWolfRoamCandidate(cell)
                && GetSettlementPressure(cell) <= WolfMigrationSettlementLimit
                && CountWalkableNeighbors(cell, 3) >= 6;
        }

        private float GetWolfMigrationScore(Vector2Int cell)
        {
            float score = GetWolfTerrainScore(cell)
                + CountWalkableNeighbors(cell, 3) * 0.12f
                - GetSettlementPressure(cell) * 5.0f;
            if (hasCampCell)
            {
                score += Mathf.Clamp(Vector2Int.Distance(cell, campCell) - WolfCampAvoidRadius, 0f, 34f) * 0.08f;
            }

            return score;
        }

        private bool IsBirdMigrationCandidate(StrategyBirdSpecies species, Vector2Int cell)
        {
            return IsBirdSpawnCandidate(species, cell)
                && GetSettlementPressure(cell) <= BirdMigrationSettlementLimit;
        }

        private float GetBirdMigrationScore(StrategyBirdSpecies species, Vector2Int cell)
        {
            return GetBirdSpawnTerrainScore(species, cell)
                - GetSettlementPressure(cell) * 0.55f;
        }

        private bool IsLakeFishMigrationCandidate(Vector2Int cell, int regionId)
        {
            return IsLakeFishSpawnCandidate(cell, regionId)
                && CountWaterNeighbors(cell, 1, CityMapWaterKind.Lake) >= 1;
        }

        private float GetFishMigrationScore(Vector2Int cell)
        {
            return GetFishSpawnTerrainScore(cell);
        }

        private bool IsWaterCellOfKind(Vector2Int cell, CityMapWaterKind waterKind)
        {
            return map != null
                && map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell)
                && mapCell.Kind == CityMapCellKind.Water
                && mapCell.WaterKind == waterKind;
        }

        private bool IsBirdSpawnCandidate(StrategyBirdSpecies species, Vector2Int cell)
        {
            if (map == null || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return false;
            }

            return species switch
            {
                StrategyBirdSpecies.Duck => mapCell.Kind == CityMapCellKind.Water
                    || (mapCell.Kind == CityMapCellKind.Shore && map.IsCellWalkable(cell)),
                StrategyBirdSpecies.Crow => mapCell.Kind == CityMapCellKind.Forest
                    || (map.IsCellWalkable(cell)
                        && (mapCell.Kind == CityMapCellKind.Dirt
                            || mapCell.Kind == CityMapCellKind.Grass
                            || mapCell.Kind == CityMapCellKind.Meadow)),
                _ => map.IsCellWalkable(cell)
                    && (mapCell.Kind == CityMapCellKind.Meadow
                        || mapCell.Kind == CityMapCellKind.Grass
                        || mapCell.Kind == CityMapCellKind.Dirt
                        || mapCell.Kind == CityMapCellKind.Shore)
            };
        }

        private int CountWalkableNeighbors(Vector2Int center, int radius)
        {
            int count = 0;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    Vector2Int candidate = center + new Vector2Int(x, y);
                    if (map.IsCellWalkable(candidate))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private float GetUsedCellSpacingPenalty(
            Vector2Int candidate,
            HashSet<Vector2Int> usedCells,
            int preferredDistance,
            float weight)
        {
            if (usedCells == null || usedCells.Count <= 0 || preferredDistance <= 0 || weight <= 0f)
            {
                return 0f;
            }

            float penalty = 0f;
            foreach (Vector2Int usedCell in usedCells)
            {
                float distance = Vector2Int.Distance(candidate, usedCell);
                if (distance < preferredDistance)
                {
                    penalty += (preferredDistance - distance) * weight;
                }
            }

            return penalty;
        }

        private float GetSpawnTerrainScore(Vector2Int cell)
        {
            if (!map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return -10f;
            }

            float baseScore = mapCell.Kind switch
            {
                CityMapCellKind.Meadow => 5.0f,
                CityMapCellKind.Grass => 3.0f,
                CityMapCellKind.Forest => 1.6f,
                CityMapCellKind.Dirt => 0.35f,
                _ => -10f
            };

            if (mapCell.Kind == CityMapCellKind.Meadow || mapCell.Kind == CityMapCellKind.Grass)
            {
                baseScore += CountForestNeighbors(cell) * 0.28f;
            }

            return baseScore;
        }

        private float GetWolfTerrainScore(Vector2Int cell)
        {
            if (!map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return -10f;
            }

            float baseScore = mapCell.Kind switch
            {
                CityMapCellKind.Forest => 5.4f,
                CityMapCellKind.Meadow => 3.2f,
                CityMapCellKind.Grass => 2.8f,
                CityMapCellKind.Dirt => 1.1f,
                _ => -10f
            };

            baseScore += CountForestNeighbors(cell) * 0.34f;
            baseScore += CountWalkableNeighbors(cell, 2) * 0.06f;
            return baseScore;
        }

        private float GetRabbitSpawnTerrainScore(Vector2Int cell)
        {
            if (!map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return -10f;
            }

            float baseScore = mapCell.Kind switch
            {
                CityMapCellKind.Meadow => 5.4f,
                CityMapCellKind.Grass => 4.1f,
                CityMapCellKind.Forest => 1.2f,
                CityMapCellKind.Dirt => 0.55f,
                _ => -10f
            };

            if (mapCell.Kind == CityMapCellKind.Meadow || mapCell.Kind == CityMapCellKind.Grass)
            {
                baseScore += CountForestNeighbors(cell) * 0.34f;
            }

            return baseScore;
        }

        private float GetFishSpawnTerrainScore(Vector2Int cell)
        {
            if (!IsFishSpawnCandidate(cell))
            {
                return -10f;
            }

            int waterNeighbors = CountWaterNeighbors(cell, 2, CityMapWaterKind.Lake);
            int shoreNeighbors = CountShoreNeighbors(cell, 2);
            float score = 1f + waterNeighbors * 0.34f + Mathf.Min(shoreNeighbors, 5) * 0.10f;
            if (waterNeighbors >= 12)
            {
                score += 2.0f;
            }
            else if (waterNeighbors >= 6)
            {
                score += 0.9f;
            }

            if (shoreNeighbors > 12)
            {
                score -= 0.75f;
            }

            return score;
        }

        private float GetBirdSpawnTerrainScore(StrategyBirdSpecies species, Vector2Int cell)
        {
            if (!map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return -10f;
            }

            return species switch
            {
                StrategyBirdSpecies.Duck => mapCell.Kind switch
                {
                    CityMapCellKind.Water => 4.4f + CountWaterNeighbors(cell, 2) * 0.28f + CountShoreNeighbors(cell, 2) * 0.08f,
                    CityMapCellKind.Shore => 2.8f + CountWaterNeighbors(cell, 2) * 0.32f,
                    _ => -10f
                },
                StrategyBirdSpecies.Crow => mapCell.Kind switch
                {
                    CityMapCellKind.Forest => 4.8f + CountForestNeighbors(cell) * 0.26f,
                    CityMapCellKind.Dirt => 3.1f + CountForestNeighbors(cell) * 0.16f,
                    CityMapCellKind.Grass => 1.8f + CountForestNeighbors(cell) * 0.12f,
                    CityMapCellKind.Meadow => 1.2f + CountForestNeighbors(cell) * 0.10f,
                    _ => -10f
                },
                _ => mapCell.Kind switch
                {
                    CityMapCellKind.Meadow => 5.2f + CountWalkableNeighbors(cell, 1) * 0.10f,
                    CityMapCellKind.Grass => 4.2f + CountWalkableNeighbors(cell, 1) * 0.08f,
                    CityMapCellKind.Dirt => 2.1f + CountWalkableNeighbors(cell, 1) * 0.05f,
                    CityMapCellKind.Shore => 1.4f + CountWaterNeighbors(cell, 1) * 0.08f,
                    _ => -10f
                }
            };
        }

        private int CountWaterNeighbors(Vector2Int cell, int radius)
        {
            return CountWaterNeighbors(cell, radius, null);
        }

        private int CountWaterNeighbors(Vector2Int cell, int radius, CityMapWaterKind? waterKind)
        {
            int count = 0;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    if (map.TryGetCell(cell.x + x, cell.y + y, out CityMapCell neighbor)
                        && neighbor.Kind == CityMapCellKind.Water
                        && (!waterKind.HasValue || neighbor.WaterKind == waterKind.Value))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private int CountShoreNeighbors(Vector2Int cell, int radius)
        {
            int count = 0;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    if (map.TryGetCell(cell.x + x, cell.y + y, out CityMapCell neighbor)
                        && neighbor.Kind == CityMapCellKind.Shore)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private int CountForestNeighbors(Vector2Int cell)
        {
            int count = 0;
            for (int y = -2; y <= 2; y++)
            {
                for (int x = -2; x <= 2; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    if (map.TryGetCell(cell.x + x, cell.y + y, out CityMapCell neighbor)
                        && neighbor.Kind == CityMapCellKind.Forest)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private Vector2 GetJitter(int x, int y, int salt)
        {
            float jitterX = Hash01(map.ActiveSeed, x, y, salt) - 0.5f;
            float jitterY = Hash01(map.ActiveSeed, x, y, salt + 17) - 0.5f;
            return new Vector2(jitterX, jitterY);
        }

        private bool CanWolfTargetResident(StrategyResidentAgent resident)
        {
            if (resident == null
                || !resident.IsAdult
                || resident.IsPendingRefugee
                || resident.IsFuneralDutyActive
                || resident.Activity == StrategyResidentAgent.ResidentActivity.StayingInsideHome
                || resident.Activity == StrategyResidentAgent.ResidentActivity.MourningCorpse
                || resident.Activity == StrategyResidentAgent.ResidentActivity.BuryingGrave
                || map == null
                || !map.TryWorldToCell(resident.transform.position, out Vector2Int cell))
            {
                return false;
            }

            return !IsWolfUnsafeSettlementCell(cell);
        }

        private StrategyWolfPack FindPackForWolf(StrategyWolfAgent wolf)
        {
            if (wolf == null)
            {
                return null;
            }

            RemoveMissingWolves();
            for (int i = 0; i < wolfPacks.Count; i++)
            {
                StrategyWolfPack pack = wolfPacks[i];
                if (pack == null)
                {
                    continue;
                }

                IReadOnlyList<StrategyWolfAgent> members = pack.Members;
                for (int memberIndex = 0; memberIndex < members.Count; memberIndex++)
                {
                    if (members[memberIndex] == wolf)
                    {
                        return pack;
                    }
                }
            }

            return null;
        }

        private float GetSettlementPressure(Vector2Int cell)
        {
            float pressure = 0f;
            if (hasCampCell)
            {
                float campDistance = Vector2Int.Distance(cell, campCell);
                if (campDistance < 5f)
                {
                    pressure += 4.5f;
                }
                else if (campDistance < 12f)
                {
                    pressure += Mathf.Lerp(2.4f, 0.35f, Mathf.InverseLerp(5f, 12f, campDistance));
                }
            }

            RefreshSettlementBuildingsIfNeeded();
            if (settlementBuildings != null)
            {
                for (int i = 0; i < settlementBuildings.Length; i++)
                {
                    StrategyPlacedBuilding building = settlementBuildings[i];
                    if (building == null)
                    {
                        continue;
                    }

                    Vector2 buildingCenter = new Vector2(
                        building.Origin.x + Mathf.Max(1, building.Footprint.x) * 0.5f,
                        building.Origin.y + Mathf.Max(1, building.Footprint.y) * 0.5f);
                    float distance = Vector2.Distance(new Vector2(cell.x, cell.y), buildingCenter);
                    if (distance < 4.5f)
                    {
                        pressure += 2.5f;
                    }
                    else if (distance < 9f)
                    {
                        pressure += Mathf.Lerp(1.1f, 0.15f, Mathf.InverseLerp(4.5f, 9f, distance));
                    }
                }
            }

            if (population != null)
            {
                IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
                for (int i = 0; i < residents.Count; i++)
                {
                    StrategyResidentAgent resident = residents[i];
                    if (resident == null
                        || resident.IsPendingRefugee
                        || !map.TryWorldToCell(resident.transform.position, out Vector2Int residentCell))
                    {
                        continue;
                    }

                    float distance = Vector2Int.Distance(cell, residentCell);
                    if (distance < 2.4f)
                    {
                        pressure += 0.45f;
                    }
                    else if (distance < 5f)
                    {
                        pressure += 0.16f;
                    }
                }
            }

            return pressure;
        }

        private void RefreshSettlementBuildingsIfNeeded()
        {
            if (Time.time < nextSettlementCacheRefreshTime)
            {
                return;
            }

            nextSettlementCacheRefreshTime = Time.time + WolfSettlementCacheInterval;
            settlementBuildings = Object.FindObjectsByType<StrategyPlacedBuilding>(FindObjectsInactive.Exclude);
            settlementConstructionSites = Object.FindObjectsByType<StrategyConstructionSite>(FindObjectsInactive.Exclude);
        }

        private void EnsureWildlifeRoot()
        {
            if (wildlifeRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("Wildlife");
            root.transform.SetParent(transform, false);
            wildlifeRoot = root.transform;
        }
    }
}
