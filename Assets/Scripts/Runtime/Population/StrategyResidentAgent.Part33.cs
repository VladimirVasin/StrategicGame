using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private Vector2Int activeFishingCell;
        private bool hasActiveFishingCell;

        private void StartCastingFishingLine()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (!IsFishingCastStillValid("start_casting", true))
            {
                ResetFisherWorkToIdle(true);
                return;
            }

            activity = ResidentActivity.CastingFishingLine;
            workFrame = 0;
            workFrameTimer = 0f;
            appliedWorkFrame = -1;
            usingWorkSprite = false;
            fishingLineCast = false;
            fishingWorkTimer = 0.72f;
            fishingBiteTimer = Random.Range(0.65f, 1.35f);
            FaceWorldPoint(activeFishTarget.transform.position);
            SetFishingLineVisible(true);
            StrategyDebugLogger.Info(
                "Fishing",
                "CastingStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("fishingCell", activeFishingCell),
                StrategyDebugLogger.F("fishWorld", activeFishTarget.transform.position),
                StrategyDebugLogger.F("hutOrigin", fisherWorkplace != null ? fisherWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateCastingFishingLine()
        {
            if (!IsFishingCastStillValid("casting", true))
            {
                ResetFisherWorkToIdle(true);
                return;
            }

            FaceWorldPoint(activeFishTarget.transform.position);
            AnimateFishingWork();
            SyncFishingLineRenderer();
            fishingWorkTimer -= Time.deltaTime;
            if (!fishingLineCast || fishingWorkTimer > 0f)
            {
                return;
            }

            activity = ResidentActivity.WaitingForFishBite;
            fishingBiteTimer = Random.Range(0.75f, 1.65f);
            StrategyDebugLogger.Info(
                "Fishing",
                "WaitingForBite",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("fishingCell", activeFishingCell),
                StrategyDebugLogger.F("fishWorld", activeFishTarget.transform.position));
        }

        private void UpdateWaitingForFishBite()
        {
            if (activeFishTarget == null || activeFishTarget.IsCaught)
            {
                ResetFisherWorkToIdle(false);
                return;
            }

            if (!activeFishTarget.IsHooked)
            {
                ResetFisherWorkToIdle(true);
                return;
            }

            if (!IsFishingCastStillValid("waiting_for_bite", true))
            {
                ResetFisherWorkToIdle(true);
                return;
            }

            FaceWorldPoint(activeFishTarget.transform.position);
            ApplyFishingFrame(7 + (Time.frameCount / 10) % 2);
            SetFishingLineVisible(true);
            fishingBiteTimer -= Time.deltaTime;
            if (fishingBiteTimer > 0f)
            {
                return;
            }

            activity = ResidentActivity.ReelingFish;
            workFrame = 8;
            workFrameTimer = 0f;
            appliedWorkFrame = -1;
            StrategyDebugLogger.Info(
                "Fishing",
                "ReelingStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("fishingCell", activeFishingCell),
                StrategyDebugLogger.F("fishWorld", activeFishTarget.transform.position));
        }

        private void UpdateReelingFish()
        {
            if (activeFishTarget == null || activeFishTarget.IsCaught)
            {
                ResetFisherWorkToIdle(false);
                return;
            }

            if (!activeFishTarget.IsHooked)
            {
                ResetFisherWorkToIdle(true);
                return;
            }

            if (!IsFishingCastStillValid("reeling", true))
            {
                ResetFisherWorkToIdle(true);
                return;
            }

            FaceWorldPoint(activeFishTarget.transform.position);
            SetFishingLineVisible(true);
            AnimateFishingWork();
        }

        private void StartCarryingFish(int amount)
        {
            if (amount <= 0)
            {
                ResetFisherWorkToIdle(false);
                return;
            }

            ClearFishingStandTracking();
            SetFishingLineVisible(false);
            if (fisherWorkplace == null
                || !fisherWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                if (fisherWorkplace != null)
                {
                    fisherWorkplace.AddFish(amount);
                }

                activeFishTarget = null;
                carriedFishAmount = 0;
                ResetFisherWorkToIdle(false);
                StrategyDebugLogger.Warn(
                    "Fishing",
                    "FishCarryFallback",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", "no_dropoff_path"),
                    StrategyDebugLogger.F("hutOrigin", fisherWorkplace != null ? fisherWorkplace.Origin : Vector2Int.zero));
                return;
            }

            carriedFishAmount = amount;
            activeFishTarget = null;
            activity = ResidentActivity.CarryingFish;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.12f);
            SetCarriedFishVisible(true);
            StrategyDebugLogger.Info(
                "Fishing",
                "FishCarryingStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedFishAmount),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("hutOrigin", fisherWorkplace != null ? fisherWorkplace.Origin : Vector2Int.zero));
        }

        private void StartDepositingFish()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingFish;
            fishingWorkTimer = Random.Range(FishingDepositSecondsMin, FishingDepositSecondsMax);
            if (fisherWorkplace != null)
            {
                FaceWorldPoint(fisherWorkplace.FootprintBounds.center);
            }

            StrategyDebugLogger.Info(
                "Fishing",
                "FishDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedFishAmount),
                StrategyDebugLogger.F("hutOrigin", fisherWorkplace != null ? fisherWorkplace.Origin : Vector2Int.zero));
        }

        private void UpdateDepositingFish()
        {
            fishingWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.2f);
            SetCarriedFishVisible(true);
            if (fishingWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedFishAmount;
            if (fisherWorkplace != null)
            {
                fisherWorkplace.AddFish(depositedAmount);
            }

            carriedFishAmount = 0;
            SetCarriedFishVisible(false);
            StrategyDebugLogger.Info(
                "Fishing",
                "FishDeposited",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("hutOrigin", fisherWorkplace != null ? fisherWorkplace.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("hutStock", fisherWorkplace != null ? fisherWorkplace.FishStored : -1));
            CompleteFisherDelivery();
        }

        private bool IsFishingCastStillValid(string phase, bool requireFishInCastRange)
        {
            if (fisherWorkplace == null
                || activeFishTarget == null
                || activeFishTarget.IsCaught
                || map == null
                || !map.TryWorldToCell(transform.position, out Vector2Int currentCell)
                || !fisherWorkplace.IsValidFishingStandCell(currentCell))
            {
                LogFishingCastInvalid(phase, "invalid_stand_cell");
                return false;
            }

            if (hasActiveFishingCell && currentCell != activeFishingCell)
            {
                LogFishingCastInvalid(phase, "left_fishing_cell");
                return false;
            }

            if (requireFishInCastRange
                && Vector2.Distance(transform.position, activeFishTarget.transform.position) > StrategyFisherHut.CastRange + 0.15f)
            {
                LogFishingCastInvalid(phase, "fish_out_of_cast_range");
                return false;
            }

            return true;
        }

        private void LogFishingCastInvalid(string phase, string reason)
        {
            StrategyDebugLogger.Warn(
                "Fishing",
                "FishingCastInvalid",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("phase", phase),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("fishingCell", hasActiveFishingCell ? activeFishingCell : Vector2Int.zero),
                StrategyDebugLogger.F("fishWorld", activeFishTarget != null ? activeFishTarget.transform.position : Vector3.zero),
                StrategyDebugLogger.F("hutOrigin", fisherWorkplace != null ? fisherWorkplace.Origin : Vector2Int.zero));
        }

        private void ClearFishingStandTracking()
        {
            hasActiveFishingCell = false;
            activeFishingCell = default;
        }
    }
}
