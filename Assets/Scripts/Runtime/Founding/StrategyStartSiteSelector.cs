using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyStartSiteSelector
    {
        public const int SafeWaterClearance = 6;
        public const int PreferredWaterDistance = 8;
        public const int LocalFeatureRadius = 6;
        public const int ResidentSpawnRadius = 3;

        private static readonly Vector2Int[] caravanOffsets =
        {
            new Vector2Int(2, -1),
            new Vector2Int(-4, -1),
            new Vector2Int(-1, 2),
            new Vector2Int(-1, -4),
            new Vector2Int(3, 1),
            new Vector2Int(-5, 1),
            new Vector2Int(1, 3),
            new Vector2Int(1, -5)
        };

        public static Vector2Int CaravanFootprint => new Vector2Int(3, 2);
        public static Vector2Int CaravanReservedFootprint => new Vector2Int(3, 3);

        public static StrategyStarterLayout Select(
            StrategyStartSiteMapSnapshot map,
            StrategyFoundingPreferences preferences = null)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            preferences ??= StrategyFoundingPreferences.Balanced;
            StrategyStartSiteFeatureMap features = new StrategyStartSiteFeatureMap(map);
            int strictEdgeMargin = Math.Min(8, Math.Max(1, Math.Min(map.Width, map.Height) / 6));
            int strictComponent = Math.Min(384, Math.Max(24, map.CellCount / 20));

            SelectionPolicy strict = new SelectionPolicy(
                SafeWaterClearance,
                strictEdgeMargin,
                12,
                62,
                44,
                strictComponent,
                true,
                StrategyStartSiteFallbackLevel.None,
                StrategyStarterLayout.PreferredDiagnostic);
            if (TrySelectScored(features, preferences, strict, out Candidate selected, out int accepted))
            {
                return CreateLayout(features, preferences, selected, accepted, strict);
            }

            SelectionPolicy relaxedSpace = new SelectionPolicy(
                SafeWaterClearance,
                Math.Min(2, strictEdgeMargin),
                8,
                38,
                22,
                Math.Min(96, Math.Max(16, map.CellCount / 60)),
                false,
                StrategyStartSiteFallbackLevel.RelaxedSettlementSpace,
                StrategyStarterLayout.RelaxedSettlementDiagnostic);
            if (TrySelectScored(features, preferences, relaxedSpace, out selected, out accepted))
            {
                return CreateLayout(features, preferences, selected, accepted, relaxedSpace);
            }

            SelectionPolicy relaxedWater = new SelectionPolicy(
                2,
                1,
                6,
                25,
                0,
                Math.Min(48, Math.Max(8, map.CellCount / 100)),
                false,
                StrategyStartSiteFallbackLevel.RelaxedWaterClearance,
                StrategyStarterLayout.RelaxedWaterDiagnostic);
            if (TrySelectScored(features, preferences, relaxedWater, out selected, out accepted))
            {
                return CreateLayout(features, preferences, selected, accepted, relaxedWater);
            }

            if (TrySelectLegacyCenterOut(features, preferences, out selected, out accepted))
            {
                SelectionPolicy legacy = new SelectionPolicy(
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    false,
                    StrategyStartSiteFallbackLevel.LegacyCenterOut,
                    StrategyStarterLayout.LegacyDiagnostic);
                return CreateLayout(features, preferences, selected, accepted, legacy);
            }

            return StrategyStarterLayout.Failed(map, preferences);
        }

        private static bool TrySelectScored(
            StrategyStartSiteFeatureMap features,
            StrategyFoundingPreferences preferences,
            SelectionPolicy policy,
            out Candidate selected,
            out int acceptedCount)
        {
            selected = default;
            acceptedCount = 0;
            bool found = false;
            for (int y = 0; y < features.Map.Height; y++)
            {
                for (int x = 0; x < features.Map.Width; x++)
                {
                    if (!TryCreateCandidate(features, x, y, policy, out Candidate candidate))
                    {
                        continue;
                    }

                    acceptedCount++;
                    candidate.Score = ScoreCandidate(features, preferences, candidate);
                    candidate.TieHash = StableTieHash(
                        features.Map.Seed,
                        preferences.StableHash,
                        x,
                        y);
                    if (!found
                        || candidate.Score > selected.Score
                        || (candidate.Score == selected.Score && candidate.TieHash < selected.TieHash))
                    {
                        selected = candidate;
                        found = true;
                    }
                }
            }

            return found;
        }

        private static bool TryCreateCandidate(
            StrategyStartSiteFeatureMap features,
            int x,
            int y,
            SelectionPolicy policy,
            out Candidate candidate)
        {
            candidate = default;
            StrategyStartSiteCell cell = features.Map.GetCell(x, y);
            if (!cell.IsDryLand
                || !cell.IsWalkable
                || !cell.IsBuildable
                || (policy.RequireOpenCampCell && !cell.IsOpenLand))
            {
                return false;
            }

            int edgeDistance = GetEdgeDistance(features.Map, x, y);
            int waterDistance = features.GetNearestWaterDistance(x, y);
            int connectedLand = features.GetConnectedLandSize(x, y);
            if (edgeDistance < policy.EdgeMargin
                || waterDistance < policy.MinimumWaterDistance
                || connectedLand < policy.MinimumConnectedLand)
            {
                return false;
            }

            int area = features.GetAreaCellCount(x, y, LocalFeatureRadius);
            int buildable = features.CountBuildable(x, y, LocalFeatureRadius);
            int open = features.CountOpen(x, y, LocalFeatureRadius);
            if (buildable * 100 < area * policy.MinimumBuildablePercent
                || open * 100 < area * policy.MinimumOpenPercent
                || !TryFindCaravanOrigin(features.Map, new Vector2Int(x, y), out Vector2Int caravanOrigin))
            {
                return false;
            }

            int residentSpawnCells = CountResidentSpawnCells(
                features.Map,
                new Vector2Int(x, y),
                caravanOrigin);
            if (residentSpawnCells < policy.MinimumResidentSpawnCells)
            {
                return false;
            }

            candidate = CreateCandidate(
                features,
                new Vector2Int(x, y),
                caravanOrigin,
                true,
                residentSpawnCells,
                area,
                open,
                buildable,
                connectedLand,
                edgeDistance);
            return true;
        }

        private static bool TrySelectLegacyCenterOut(
            StrategyStartSiteFeatureMap features,
            StrategyFoundingPreferences preferences,
            out Candidate selected,
            out int acceptedCount)
        {
            Vector2Int center = new Vector2Int(features.Map.Width / 2, features.Map.Height / 2);
            int maxRadius = Math.Max(features.Map.Width, features.Map.Height);
            for (int pass = 0; pass < 3; pass++)
            {
                bool requireOpen = pass == 0;
                bool requireWaterClearance = pass < 2;
                for (int radius = 0; radius <= maxRadius; radius++)
                {
                    selected = default;
                    acceptedCount = 0;
                    bool found = false;
                    for (int offsetY = -radius; offsetY <= radius; offsetY++)
                    {
                        for (int offsetX = -radius; offsetX <= radius; offsetX++)
                        {
                            if (radius > 0
                                && Math.Abs(offsetX) != radius
                                && Math.Abs(offsetY) != radius)
                            {
                                continue;
                            }

                            int x = center.x + offsetX;
                            int y = center.y + offsetY;
                            if (!features.Map.IsInside(x, y))
                            {
                                continue;
                            }

                            StrategyStartSiteCell cell = features.Map.GetCell(x, y);
                            if (!cell.IsDryLand
                                || !cell.IsWalkable
                                || (requireOpen && !cell.IsOpenLand)
                                || (requireWaterClearance
                                    && features.GetNearestWaterDistance(x, y) < SafeWaterClearance))
                            {
                                continue;
                            }

                            Vector2Int camp = new Vector2Int(x, y);
                            bool hasCaravan = TryFindCaravanOrigin(
                                features.Map,
                                camp,
                                out Vector2Int caravanOrigin);
                            int area = features.GetAreaCellCount(x, y, LocalFeatureRadius);
                            int open = features.CountOpen(x, y, LocalFeatureRadius);
                            int buildable = features.CountBuildable(x, y, LocalFeatureRadius);
                            Candidate candidate = CreateCandidate(
                                features,
                                camp,
                                caravanOrigin,
                                hasCaravan,
                                CountResidentSpawnCells(
                                    features.Map,
                                    camp,
                                    hasCaravan ? caravanOrigin : default,
                                    hasCaravan),
                                area,
                                open,
                                buildable,
                                features.GetConnectedLandSize(x, y),
                                GetEdgeDistance(features.Map, x, y));
                            candidate.Score = ScoreCandidate(features, preferences, candidate);
                            candidate.TieHash = StableTieHash(
                                features.Map.Seed,
                                preferences.StableHash,
                                x,
                                y);
                            acceptedCount++;
                            if (!found || candidate.TieHash < selected.TieHash)
                            {
                                selected = candidate;
                                found = true;
                            }
                        }
                    }

                    if (found)
                    {
                        return true;
                    }
                }
            }

            selected = default;
            acceptedCount = 0;
            return false;
        }

        private static Candidate CreateCandidate(
            StrategyStartSiteFeatureMap features,
            Vector2Int camp,
            Vector2Int caravanOrigin,
            bool hasCaravan,
            int residentSpawnCells,
            int area,
            int open,
            int buildable,
            int connectedLand,
            int edgeDistance)
        {
            return new Candidate
            {
                Cell = camp,
                CaravanOrigin = caravanOrigin,
                HasCaravan = hasCaravan,
                ResidentSpawnCells = residentSpawnCells,
                Area = area,
                Forest = features.CountForest(camp.x, camp.y, LocalFeatureRadius),
                Meadow = features.CountMeadow(camp.x, camp.y, LocalFeatureRadius),
                Grass = features.CountGrass(camp.x, camp.y, LocalFeatureRadius),
                Dirt = features.CountDirt(camp.x, camp.y, LocalFeatureRadius),
                Open = open,
                Buildable = buildable,
                ConnectedLand = connectedLand,
                EdgeDistance = edgeDistance,
                WaterDistance = features.GetNearestWaterDistance(camp.x, camp.y),
                RiverDistance = features.GetNearestRiverDistance(camp.x, camp.y),
                LakeDistance = features.GetNearestLakeDistance(camp.x, camp.y)
            };
        }

        private static StrategyStarterLayout CreateLayout(
            StrategyStartSiteFeatureMap features,
            StrategyFoundingPreferences preferences,
            Candidate candidate,
            int acceptedCount,
            SelectionPolicy policy)
        {
            float inverseArea = candidate.Area > 0 ? 1f / candidate.Area : 0f;
            StrategyStartSiteDiagnostics diagnostics = new StrategyStartSiteDiagnostics(
                policy.FallbackLevel,
                policy.DiagnosticCode,
                acceptedCount,
                candidate.Score,
                StrategyStartSiteFeatureMap.GetDiagnosticDistance(
                    candidate.WaterDistance,
                    features.MissingSourceDistance),
                StrategyStartSiteFeatureMap.GetDiagnosticDistance(
                    candidate.RiverDistance,
                    features.MissingSourceDistance),
                StrategyStartSiteFeatureMap.GetDiagnosticDistance(
                    candidate.LakeDistance,
                    features.MissingSourceDistance),
                candidate.ConnectedLand,
                candidate.Open * inverseArea,
                candidate.Forest * inverseArea,
                candidate.Buildable * inverseArea);
            return new StrategyStarterLayout(
                true,
                features.Map.Seed,
                preferences.ProfileId,
                preferences.StableHash,
                candidate.Cell,
                candidate.HasCaravan,
                candidate.CaravanOrigin,
                diagnostics);
        }

        private readonly struct SelectionPolicy
        {
            public SelectionPolicy(
                int minimumWaterDistance,
                int edgeMargin,
                int minimumResidentSpawnCells,
                int minimumBuildablePercent,
                int minimumOpenPercent,
                int minimumConnectedLand,
                bool requireOpenCampCell,
                StrategyStartSiteFallbackLevel fallbackLevel,
                string diagnosticCode)
            {
                MinimumWaterDistance = minimumWaterDistance;
                EdgeMargin = edgeMargin;
                MinimumResidentSpawnCells = minimumResidentSpawnCells;
                MinimumBuildablePercent = minimumBuildablePercent;
                MinimumOpenPercent = minimumOpenPercent;
                MinimumConnectedLand = minimumConnectedLand;
                RequireOpenCampCell = requireOpenCampCell;
                FallbackLevel = fallbackLevel;
                DiagnosticCode = diagnosticCode;
            }

            public int MinimumWaterDistance { get; }
            public int EdgeMargin { get; }
            public int MinimumResidentSpawnCells { get; }
            public int MinimumBuildablePercent { get; }
            public int MinimumOpenPercent { get; }
            public int MinimumConnectedLand { get; }
            public bool RequireOpenCampCell { get; }
            public StrategyStartSiteFallbackLevel FallbackLevel { get; }
            public string DiagnosticCode { get; }
        }

        private struct Candidate
        {
            public Vector2Int Cell;
            public Vector2Int CaravanOrigin;
            public bool HasCaravan;
            public int ResidentSpawnCells;
            public int Area;
            public int Forest;
            public int Meadow;
            public int Grass;
            public int Dirt;
            public int Open;
            public int Buildable;
            public int ConnectedLand;
            public int EdgeDistance;
            public int WaterDistance;
            public int RiverDistance;
            public int LakeDistance;
            public int Score;
            public uint TieHash;
        }
    }
}
