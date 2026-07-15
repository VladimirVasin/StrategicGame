using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private StrategyLooseCarriedResourcePile activeLooseStorageResourceSource;

        private bool TryStartLooseStorageResourcePickup(
            StrategyResourceType resource,
            ResidentActivity movingActivity)
        {
            if (storageWorkplace == null
                || !StrategyLooseCarriedResourcePile.TryReserveNearestForStorage(
                    storageWorkplace,
                    this,
                    resource,
                    out StrategyLooseCarriedResourcePile source,
                    out Vector2Int pickupCell))
            {
                return false;
            }

            if (!TryBuildPathTo(pickupCell))
            {
                source.ReleaseReservation(this);
                logisticsWorkCooldown = WasLastPathBuildDeferred
                    ? Random.Range(0.18f, 0.38f)
                    : Random.Range(2.0f, 4.0f);
                return false;
            }

            activeLooseStorageResourceSource = source;
            activity = movingActivity;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            StrategyDebugLogger.Info(
                "Logistics",
                "LooseResourcePickupMoveStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("sourceOrigin", source.Origin),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("pickupCell", pickupCell),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
            return true;
        }

        private bool HasStoragePickupSource(Component regularSource, StrategyResourceType resource)
        {
            return regularSource != null
                || activeLooseStorageResourceSource != null
                    && activeLooseStorageResourceSource.Resource == resource;
        }

        private Bounds GetStoragePickupSourceBounds(Component regularSource)
        {
            if (activeLooseStorageResourceSource != null)
            {
                return activeLooseStorageResourceSource.FootprintBounds;
            }

            return regularSource switch
            {
                StrategyMine source => source.FootprintBounds,
                StrategyCoalPit source => source.FootprintBounds,
                StrategyClayPit source => source.FootprintBounds,
                IStrategyProductionLogisticsNode source => source.FootprintBounds,
                _ => new Bounds(transform.position, Vector3.one)
            };
        }

        private bool TryTakeStorageIronSource(out int amount, out Vector2Int sourceOrigin)
        {
            if (TryTakeLooseStorageResource(StrategyResourceType.Iron, out amount, out sourceOrigin))
            {
                return true;
            }

            sourceOrigin = activeIronSource != null ? activeIronSource.Origin : Vector2Int.zero;
            return activeIronSource != null && activeIronSource.TryTakeReservedIron(this, out amount);
        }

        private bool TryTakeStorageCoalSource(out int amount, out Vector2Int sourceOrigin)
        {
            if (TryTakeLooseStorageResource(StrategyResourceType.Coal, out amount, out sourceOrigin))
            {
                return true;
            }

            sourceOrigin = activeCoalSource != null ? activeCoalSource.Origin : Vector2Int.zero;
            return activeCoalSource != null && activeCoalSource.TryTakeReservedCoal(this, out amount);
        }

        private bool TryTakeStorageClaySource(out int amount, out Vector2Int sourceOrigin)
        {
            if (TryTakeLooseStorageResource(StrategyResourceType.Clay, out amount, out sourceOrigin))
            {
                return true;
            }

            sourceOrigin = activeClaySource != null ? activeClaySource.Origin : Vector2Int.zero;
            return activeClaySource != null && activeClaySource.TryTakeReservedClay(this, out amount);
        }

        private bool TryTakeStoragePotterySource(out int amount, out Vector2Int sourceOrigin)
        {
            if (TryTakeLooseStorageResource(StrategyResourceType.Pottery, out amount, out sourceOrigin))
            {
                return true;
            }

            sourceOrigin = activePotterySource != null ? activePotterySource.Origin : Vector2Int.zero;
            return activePotterySource != null
                && activePotterySource.TryTakeReservedOutput(StrategyResourceType.Pottery, this, out amount);
        }

        private bool TryTakeStorageToolsSource(out int amount, out Vector2Int sourceOrigin)
        {
            if (TryTakeLooseStorageResource(StrategyResourceType.Tools, out amount, out sourceOrigin))
            {
                return true;
            }

            sourceOrigin = activeToolsSource != null ? activeToolsSource.Origin : Vector2Int.zero;
            return activeToolsSource != null
                && activeToolsSource.TryTakeReservedOutput(StrategyResourceType.Tools, this, out amount);
        }

        private bool TryTakeLooseStorageResource(
            StrategyResourceType expectedResource,
            out int amount,
            out Vector2Int sourceOrigin)
        {
            amount = 0;
            sourceOrigin = Vector2Int.zero;
            StrategyLooseCarriedResourcePile source = activeLooseStorageResourceSource;
            if (source == null || source.Resource != expectedResource)
            {
                return false;
            }

            sourceOrigin = source.Origin;
            bool taken = source.TryTakeReserved(this, out StrategyResourceType resource, out amount)
                && resource == expectedResource
                && amount > 0;
            if (!taken)
            {
                source.ReleaseReservation(this);
            }

            activeLooseStorageResourceSource = null;
            return taken;
        }
    }
}
