using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyStartSiteFallbackLevel
    {
        None = 0,
        RelaxedSettlementSpace = 1,
        RelaxedWaterClearance = 2,
        LegacyCenterOut = 3,
        NoValidSite = 4
    }

    public readonly struct StrategyStartSiteDiagnostics
    {
        public StrategyStartSiteDiagnostics(
            StrategyStartSiteFallbackLevel fallbackLevel,
            string diagnosticCode,
            int acceptedCandidateCount,
            int score,
            int nearestWaterDistance,
            int nearestRiverDistance,
            int nearestLakeDistance,
            int connectedLandCellCount,
            float openLandRatio,
            float forestRatio,
            float buildableRatio)
        {
            FallbackLevel = fallbackLevel;
            DiagnosticCode = diagnosticCode ?? string.Empty;
            AcceptedCandidateCount = Mathf.Max(0, acceptedCandidateCount);
            Score = score;
            NearestWaterDistance = nearestWaterDistance;
            NearestRiverDistance = nearestRiverDistance;
            NearestLakeDistance = nearestLakeDistance;
            ConnectedLandCellCount = Mathf.Max(0, connectedLandCellCount);
            OpenLandRatio = Mathf.Clamp01(openLandRatio);
            ForestRatio = Mathf.Clamp01(forestRatio);
            BuildableRatio = Mathf.Clamp01(buildableRatio);
        }

        public StrategyStartSiteFallbackLevel FallbackLevel { get; }
        public string DiagnosticCode { get; }
        public int AcceptedCandidateCount { get; }
        public int Score { get; }
        public int NearestWaterDistance { get; }
        public int NearestRiverDistance { get; }
        public int NearestLakeDistance { get; }
        public int ConnectedLandCellCount { get; }
        public float OpenLandRatio { get; }
        public float ForestRatio { get; }
        public float BuildableRatio { get; }
        public bool UsedFallback => FallbackLevel != StrategyStartSiteFallbackLevel.None;
    }

    public sealed class StrategyStarterLayout
    {
        public const string PreferredDiagnostic = "preferred";
        public const string RelaxedSettlementDiagnostic = "relaxed_settlement_space";
        public const string RelaxedWaterDiagnostic = "relaxed_water_clearance";
        public const string LegacyDiagnostic = "legacy_center_out";
        public const string NoValidSiteDiagnostic = "no_valid_site";

        internal StrategyStarterLayout(
            bool isValid,
            int mapSeed,
            string profileId,
            int profileHash,
            Vector2Int campCell,
            bool hasCaravanReservation,
            Vector2Int caravanOrigin,
            StrategyStartSiteDiagnostics diagnostics)
        {
            IsValid = isValid;
            MapSeed = mapSeed;
            ProfileId = profileId ?? string.Empty;
            ProfileHash = profileHash;
            CampCell = campCell;
            HasCaravanReservation = hasCaravanReservation;
            CaravanOrigin = caravanOrigin;
            Diagnostics = diagnostics;
        }

        public bool IsValid { get; }
        public int MapSeed { get; }
        public string ProfileId { get; }
        public int ProfileHash { get; }
        public Vector2Int CampCell { get; }
        public bool HasCaravanReservation { get; }
        public Vector2Int CaravanOrigin { get; }
        public Vector2Int CaravanFootprint => StrategyStartSiteSelector.CaravanFootprint;
        public Vector2Int CaravanReservedFootprint => StrategyStartSiteSelector.CaravanReservedFootprint;
        public StrategyStartSiteDiagnostics Diagnostics { get; }
        public StrategyStartSiteFallbackLevel FallbackLevel => Diagnostics.FallbackLevel;

        internal static StrategyStarterLayout Failed(
            StrategyStartSiteMapSnapshot map,
            StrategyFoundingPreferences preferences)
        {
            Vector2Int center = new Vector2Int(map.Width / 2, map.Height / 2);
            StrategyStartSiteDiagnostics diagnostics = new StrategyStartSiteDiagnostics(
                StrategyStartSiteFallbackLevel.NoValidSite,
                NoValidSiteDiagnostic,
                0,
                0,
                -1,
                -1,
                -1,
                0,
                0f,
                0f,
                0f);
            return new StrategyStarterLayout(
                false,
                map.Seed,
                preferences.ProfileId,
                preferences.StableHash,
                center,
                false,
                default,
                diagnostics);
        }
    }
}
