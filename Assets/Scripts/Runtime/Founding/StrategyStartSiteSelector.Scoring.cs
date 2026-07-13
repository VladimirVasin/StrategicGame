using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyStartSiteSelector
    {
        private static int ScoreCandidate(
            StrategyStartSiteFeatureMap features,
            StrategyFoundingPreferences preferences,
            Candidate candidate)
        {
            int openPercent = GetPercent(candidate.Open, candidate.Area);
            int forestPercent = GetPercent(candidate.Forest, candidate.Area);
            int meadowPercent = GetPercent(candidate.Meadow, candidate.Area);
            int grassPercent = GetPercent(candidate.Grass, candidate.Area);
            int dirtPercent = GetPercent(candidate.Dirt, candidate.Area);
            int buildablePercent = GetPercent(candidate.Buildable, candidate.Area);
            int waterDistance = candidate.WaterDistance >= features.MissingSourceDistance
                ? 30
                : candidate.WaterDistance;

            int score = buildablePercent * 6
                + openPercent * 4
                + Math.Min(20, candidate.ResidentSpawnCells) * 12
                + Math.Min(800, candidate.ConnectedLand) / 4
                + Math.Min(20, candidate.EdgeDistance) * 8
                + Math.Min(12, waterDistance) * 5;

            score += ScoreWaterPreference(features, preferences, candidate);
            score += ScoreLandscapePreference(
                preferences,
                openPercent,
                forestPercent,
                meadowPercent,
                grassPercent,
                dirtPercent);
            score += ScoreLivelihoodPreference(
                features,
                preferences,
                candidate,
                forestPercent,
                meadowPercent,
                grassPercent,
                dirtPercent);
            score += ScorePriorityPreference(
                features,
                preferences,
                candidate,
                openPercent,
                forestPercent,
                meadowPercent,
                grassPercent,
                dirtPercent,
                buildablePercent);
            return score;
        }

        private static int ScoreWaterPreference(
            StrategyStartSiteFeatureMap features,
            StrategyFoundingPreferences preferences,
            Candidate candidate)
        {
            return preferences.WaterChoiceId switch
            {
                StrategyFoundingChoiceIds.WaterRiver => ProximityBandScore(
                    candidate.RiverDistance,
                    features.MissingSourceDistance) * 6,
                StrategyFoundingChoiceIds.WaterLake => ProximityBandScore(
                    candidate.LakeDistance,
                    features.MissingSourceDistance) * 6,
                StrategyFoundingChoiceIds.WaterInland => Math.Min(
                    30,
                    Math.Max(
                        0,
                        (candidate.WaterDistance >= features.MissingSourceDistance
                            ? 36
                            : candidate.WaterDistance) - SafeWaterClearance)) * 18,
                _ => Math.Min(
                    12,
                    Math.Max(0, candidate.WaterDistance - SafeWaterClearance)) * 4
            };
        }

        private static int ScoreLandscapePreference(
            StrategyFoundingPreferences preferences,
            int openPercent,
            int forestPercent,
            int meadowPercent,
            int grassPercent,
            int dirtPercent)
        {
            int forestEdge = GetForestEdgeScore(forestPercent);
            int diversity = CountTerrainKinds(
                forestPercent,
                meadowPercent,
                grassPercent,
                dirtPercent);
            return preferences.LandscapeChoiceId switch
            {
                StrategyFoundingChoiceIds.LandscapeForestEdge => forestEdge * 5
                    + Math.Min(68, openPercent) * 2,
                StrategyFoundingChoiceIds.LandscapeOpenMeadow => openPercent * 7
                    + meadowPercent,
                _ => ClampScore(
                        100 - Math.Abs(forestPercent - 22) * 2 - Math.Abs(meadowPercent - 28),
                        0,
                        100) * 5
                    + diversity * 20
            };
        }

        private static int ScoreLivelihoodPreference(
            StrategyStartSiteFeatureMap features,
            StrategyFoundingPreferences preferences,
            Candidate candidate,
            int forestPercent,
            int meadowPercent,
            int grassPercent,
            int dirtPercent)
        {
            int diversity = CountTerrainKinds(
                forestPercent,
                meadowPercent,
                grassPercent,
                dirtPercent);
            return preferences.LivelihoodChoiceId switch
            {
                StrategyFoundingChoiceIds.LivelihoodHunting => meadowPercent * 5
                    + grassPercent * 2
                    + GetForestEdgeScore(forestPercent) * 3,
                StrategyFoundingChoiceIds.LivelihoodFishing => ProximityBandScore(
                    candidate.WaterDistance,
                    features.MissingSourceDistance) * 5,
                StrategyFoundingChoiceIds.LivelihoodForaging => forestPercent * 4
                    + meadowPercent * 4
                    + grassPercent,
                _ => diversity * 30 + forestPercent + meadowPercent
            };
        }

        private static int ScorePriorityPreference(
            StrategyStartSiteFeatureMap features,
            StrategyFoundingPreferences preferences,
            Candidate candidate,
            int openPercent,
            int forestPercent,
            int meadowPercent,
            int grassPercent,
            int dirtPercent,
            int buildablePercent)
        {
            int diversity = CountTerrainKinds(
                forestPercent,
                meadowPercent,
                grassPercent,
                dirtPercent);
            int nearWaterBonus = candidate.WaterDistance < features.MissingSourceDistance
                && candidate.WaterDistance <= 18
                ? 15
                : 0;
            int resourceOpportunity = Math.Min(
                100,
                forestPercent * 2 + meadowPercent + dirtPercent * 2 + nearWaterBonus);
            return preferences.PriorityChoiceId switch
            {
                StrategyFoundingChoiceIds.PriorityConstruction => buildablePercent * 5
                    + openPercent * 3,
                StrategyFoundingChoiceIds.PriorityResources => resourceOpportunity * 6
                    + diversity * 25,
                _ => buildablePercent * 2 + diversity * 20
            };
        }

        private static int ProximityBandScore(int distance, int missingSourceDistance)
        {
            if (distance >= missingSourceDistance)
            {
                return 0;
            }

            if (distance < SafeWaterClearance)
            {
                return Math.Max(0, 100 - (SafeWaterClearance - distance) * 20);
            }

            if (distance <= PreferredWaterDistance)
            {
                return 100 - (PreferredWaterDistance - distance) * 5;
            }

            return Math.Max(0, 100 - (distance - PreferredWaterDistance) * 7);
        }

        private static int GetForestEdgeScore(int forestPercent)
        {
            return ClampScore(100 - Math.Abs(forestPercent - 32) * 3, 0, 100);
        }

        private static int CountTerrainKinds(
            int forestPercent,
            int meadowPercent,
            int grassPercent,
            int dirtPercent)
        {
            int count = forestPercent >= 5 ? 1 : 0;
            count += meadowPercent >= 5 ? 1 : 0;
            count += grassPercent >= 5 ? 1 : 0;
            count += dirtPercent >= 5 ? 1 : 0;
            return count;
        }

        private static int GetPercent(int count, int total)
        {
            return total > 0 ? count * 100 / total : 0;
        }

        private static int ClampScore(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static bool TryFindCaravanOrigin(
            StrategyStartSiteMapSnapshot map,
            Vector2Int camp,
            out Vector2Int origin)
        {
            for (int i = 0; i < caravanOffsets.Length; i++)
            {
                Vector2Int candidate = ClampCaravanOrigin(map, camp + caravanOffsets[i]);
                if (IsValidCaravanOrigin(map, camp, candidate))
                {
                    origin = candidate;
                    return true;
                }
            }

            for (int radius = 2; radius <= 7; radius++)
            {
                for (int offsetY = -radius; offsetY <= radius; offsetY++)
                {
                    for (int offsetX = -radius; offsetX <= radius; offsetX++)
                    {
                        if (Math.Abs(offsetX) != radius && Math.Abs(offsetY) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = ClampCaravanOrigin(
                            map,
                            camp + new Vector2Int(offsetX, offsetY));
                        if (IsValidCaravanOrigin(map, camp, candidate))
                        {
                            origin = candidate;
                            return true;
                        }
                    }
                }
            }

            origin = default;
            return false;
        }

        private static Vector2Int ClampCaravanOrigin(
            StrategyStartSiteMapSnapshot map,
            Vector2Int origin)
        {
            Vector2Int footprint = CaravanReservedFootprint;
            return new Vector2Int(
                Math.Max(0, Math.Min(map.Width - footprint.x, origin.x)),
                Math.Max(0, Math.Min(map.Height - footprint.y, origin.y)));
        }

        private static bool IsValidCaravanOrigin(
            StrategyStartSiteMapSnapshot map,
            Vector2Int camp,
            Vector2Int origin)
        {
            Vector2Int footprint = CaravanReservedFootprint;
            if (Contains(origin, footprint, camp))
            {
                return false;
            }

            for (int y = 0; y < footprint.y; y++)
            {
                for (int x = 0; x < footprint.x; x++)
                {
                    if (!map.TryGetCell(origin.x + x, origin.y + y, out StrategyStartSiteCell cell)
                        || !cell.IsDryLand
                        || !cell.IsWalkable
                        || !cell.IsBuildable)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static int CountResidentSpawnCells(
            StrategyStartSiteMapSnapshot map,
            Vector2Int camp,
            Vector2Int caravanOrigin,
            bool hasCaravan = true)
        {
            int count = 0;
            for (int y = -ResidentSpawnRadius; y <= ResidentSpawnRadius; y++)
            {
                for (int x = -ResidentSpawnRadius; x <= ResidentSpawnRadius; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    Vector2Int cellPosition = camp + new Vector2Int(x, y);
                    if (hasCaravan && Contains(caravanOrigin, CaravanReservedFootprint, cellPosition))
                    {
                        continue;
                    }

                    if (map.TryGetCell(cellPosition.x, cellPosition.y, out StrategyStartSiteCell cell)
                        && cell.IsDryLand
                        && cell.IsWalkable)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static bool Contains(Vector2Int origin, Vector2Int footprint, Vector2Int cell)
        {
            return cell.x >= origin.x
                && cell.x < origin.x + footprint.x
                && cell.y >= origin.y
                && cell.y < origin.y + footprint.y;
        }

        private static int GetEdgeDistance(StrategyStartSiteMapSnapshot map, int x, int y)
        {
            return Math.Min(Math.Min(x, map.Width - 1 - x), Math.Min(y, map.Height - 1 - y));
        }

        private static uint StableTieHash(int seed, int profileHash, int x, int y)
        {
            unchecked
            {
                uint hash = (uint)Math.Max(1, seed);
                hash ^= (uint)profileHash * 0x9e3779b9u;
                hash ^= (uint)x * 0x85ebca6bu;
                hash ^= (uint)y * 0xc2b2ae35u;
                hash ^= hash >> 16;
                hash *= 0x7feb352du;
                hash ^= hash >> 15;
                hash *= 0x846ca68bu;
                hash ^= hash >> 16;
                return hash;
            }
        }
    }
}
