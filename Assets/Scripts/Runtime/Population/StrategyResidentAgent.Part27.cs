using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private void CompleteCarriedResourceReturn()
        {
            ResidentActivity completedActivity = activity;
            int amount = 0;
            object resource = StrategyConstructionResourceKind.None;
            Vector2Int storageOrigin = Vector2Int.zero;

            if (completedActivity == ResidentActivity.ReturningLogsToStorage)
            {
                amount = carriedLogAmount;
                resource = StrategyConstructionResourceKind.Logs;
                if (returnStorageYard == null)
                {
                    if (!StoreCarriedMaterialImmediately(
                        StrategyConstructionResourceKind.Logs,
                        "resource_return_completed",
                        "target_missing"))
                    {
                        ScheduleCarriedResourceReturnRetry();
                    }

                    return;
                }
                else
                {
                    storageOrigin = returnStorageYard.Origin;
                    StoreReturnedMaterialAtYard(returnStorageYard, StrategyConstructionResourceKind.Logs, amount);
                }

                carriedLogAmount = 0;
                SetCarriedLogsVisible(false);
            }
            else if (completedActivity == ResidentActivity.ReturningStoneToStorage)
            {
                amount = carriedStoneAmount;
                resource = StrategyConstructionResourceKind.Stone;
                if (returnStorageYard == null)
                {
                    if (!StoreCarriedMaterialImmediately(
                        StrategyConstructionResourceKind.Stone,
                        "resource_return_completed",
                        "target_missing"))
                    {
                        ScheduleCarriedResourceReturnRetry();
                    }

                    return;
                }
                else
                {
                    storageOrigin = returnStorageYard.Origin;
                    StoreReturnedMaterialAtYard(returnStorageYard, StrategyConstructionResourceKind.Stone, amount);
                }

                carriedStoneAmount = 0;
                SetCarriedStoneVisible(false);
            }
            else if (completedActivity == ResidentActivity.ReturningIronToStorage)
            {
                if (!CompleteIronResourceReturn(out amount, out resource, out storageOrigin))
                {
                    return;
                }
            }
            else if (completedActivity == ResidentActivity.ReturningGameToGranary)
            {
                amount = carriedGameAmount;
                resource = StrategyResourceType.Game;
                if (returnGranary == null)
                {
                    if (!StoreCarriedFoodImmediately(
                        StrategyResourceType.Game,
                        "resource_return_completed",
                        "target_missing"))
                    {
                        ScheduleCarriedResourceReturnRetry();
                    }

                    return;
                }
                else
                {
                    storageOrigin = returnGranary.Origin;
                    returnGranary.AddGame(amount);
                }

                carriedGameAmount = 0;
                SetCarriedGameVisible(false);
            }
            else if (completedActivity == ResidentActivity.ReturningFishToGranary)
            {
                amount = carriedFishAmount;
                resource = StrategyResourceType.Fish;
                if (returnGranary == null)
                {
                    if (!StoreCarriedFoodImmediately(
                        StrategyResourceType.Fish,
                        "resource_return_completed",
                        "target_missing"))
                    {
                        ScheduleCarriedResourceReturnRetry();
                    }

                    return;
                }
                else
                {
                    storageOrigin = returnGranary.Origin;
                    returnGranary.AddFish(amount);
                }

                carriedFishAmount = 0;
                SetCarriedFishVisible(false);
                SetFishingLineVisible(false);
            }

            returnStorageYard = null;
            returnGranary = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();

            if (amount <= 0)
            {
                ClearEmptyCarriedResourceReturn("completed_without_resource");
                return;
            }

            StrategyDebugLogger.Info(
                "Logistics",
                "CarriedResourceReturned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("storageOrigin", storageOrigin));

            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.25f, 0.70f);

            if (HasAnyCarriedResource() && TryStartCarriedResourceReturn("remaining_carried_resource"))
            {
                return;
            }
        }
    }
}
