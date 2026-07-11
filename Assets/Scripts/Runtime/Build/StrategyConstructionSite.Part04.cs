using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyConstructionSite
    {
        public int ReservedBuildHitCount => buildWorkReservations.Count;
        public int UnreservedBuildHitCount => Mathf.Max(0, BuildableHitLimit - buildHits - ReservedBuildHitCount);

        public bool HasReservedBuildWork(StrategyResidentAgent builder)
        {
            return builder != null && buildWorkReservations.Contains(builder);
        }

        public bool TryReserveBuildWork(StrategyResidentAgent builder)
        {
            if (completed
                || buildHitsRequired <= 0
                || builder == null
                || !builders.Contains(builder))
            {
                return false;
            }

            if (HasReservedBuildWork(builder))
            {
                return true;
            }

            if (UnreservedBuildHitCount <= 0)
            {
                return false;
            }

            buildWorkReservations.Add(builder);
            return true;
        }

        public void ReleaseBuildWorkReservation(StrategyResidentAgent builder)
        {
            if (builder != null)
            {
                buildWorkReservations.Remove(builder);
            }
        }

        public bool ConsumeReservedBuildWork(StrategyResidentAgent builder, Vector3 hitWorld)
        {
            if (!HasReservedBuildWork(builder))
            {
                return false;
            }

            buildWorkReservations.Remove(builder);
            if (completed || builder == null || !builders.Contains(builder) || buildHits >= BuildableHitLimit)
            {
                return false;
            }

            buildHits = Mathf.Min(buildHits + 1, BuildableHitLimit);
            UpdateVisuals();
            PlayBuildHitEffect(hitWorld);
            if (buildHits == 1 || buildHits % 5 == 0 || buildHits >= buildHitsRequired)
            {
                StrategyDebugLogger.Info(
                    "Construction",
                    "BuildHit",
                    StrategyDebugLogger.F("tool", tool),
                    StrategyDebugLogger.F("origin", origin),
                    StrategyDebugLogger.F("builder", builder.FullName),
                    StrategyDebugLogger.F("progress", Progress),
                    StrategyDebugLogger.F("buildableProgressLimit", BuildableProgressLimit),
                    StrategyDebugLogger.F("buildHits", buildHits),
                    StrategyDebugLogger.F("buildableHitLimit", BuildableHitLimit),
                    StrategyDebugLogger.F("reservedBuildHits", ReservedBuildHitCount),
                    StrategyDebugLogger.F("unreservedBuildHits", UnreservedBuildHitCount));
            }

            if (buildHits >= buildHitsRequired)
            {
                CompleteConstruction();
            }

            return true;
        }

        private void ClearBuildWorkReservations()
        {
            buildWorkReservations.Clear();
        }
    }
}
