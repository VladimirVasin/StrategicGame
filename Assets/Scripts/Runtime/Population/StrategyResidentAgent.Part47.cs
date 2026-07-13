using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private IStrategyProductionLogisticsNode activePotterySource;
        private SpriteRenderer carriedPotteryRenderer;

        private bool TryStartStoragePotteryPickup()
        {
            if (storageWorkplace == null || !storageWorkplace.TryReservePotterySource(this, out IStrategyProductionLogisticsNode source))
            {
                return false;
            }

            if (source is not Component sourceComponent
                || !TryBuildPathToBuildingAccess(sourceComponent, out Vector2Int pickupCell))
            {
                source.ReleaseOutputPickupReservation(StrategyResourceType.Pottery, this);
                if (WasLastPathBuildDeferred)
                {
                    logisticsWorkCooldown = Random.Range(0.18f, 0.38f);
                    return false;
                }

                logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "PickupMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", source.Origin),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Pottery),
                    StrategyDebugLogger.F("reason", "no_pickup_path"));
                return false;
            }

            activePotterySource = source;
            activity = ResidentActivity.MovingToStoragePotteryPickup;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            return true;
        }

        private void StartPickingUpStoragePottery()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (activePotterySource == null || storageWorkplace == null)
            {
                ResetStorageWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpStoragePottery;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            FaceWorldPoint(activePotterySource.FootprintBounds.center);
        }

        private void UpdatePickingUpStoragePottery()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if (activePotterySource == null
                || storageWorkplace == null
                || !TryBuildPathToBuildingAccess(storageWorkplace, out Vector2Int dropoffCell)
                || !activePotterySource.TryTakeReservedOutput(StrategyResourceType.Pottery, this, out carriedPotteryAmount))
            {
                activePotterySource?.ReleaseOutputPickupReservation(StrategyResourceType.Pottery, this);
                ResetStorageWorkToIdle();
                return;
            }

            activePotterySource = null;
            activity = ResidentActivity.CarryingPotteryToStorage;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedPotteryVisible(true);
        }

        private void StartDepositingStoragePottery()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingStoragePottery;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            if (storageWorkplace != null)
            {
                FaceWorldPoint(storageWorkplace.FootprintBounds.center);
            }
        }

        private void UpdateDepositingStoragePottery()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.1f);
            SetCarriedPotteryVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedPotteryAmount;
            storageWorkplace?.AddResource(StrategyResourceType.Pottery, depositedAmount);
            carriedPotteryAmount = 0;
            SetCarriedPotteryVisible(false);
            CompleteStorageDelivery();
        }

        private bool TryStartPotteryReturn(string reason, bool restartCurrentReturn = false)
        {
            if (carriedPotteryAmount <= 0)
            {
                return false;
            }

            if (restartCurrentReturn)
            {
                returnStorageYard = null;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
            }

            if (!returnCarriedResourcesImmediately
                && StrategyStorageYard.TryFindNearestDropoff(transform.position, out StrategyStorageYard yard, out Vector2Int dropoffCell)
                && TryBuildPathTo(dropoffCell))
            {
                returnStorageYard = yard;
                returnGranary = null;
                activity = ResidentActivity.ReturningPotteryToStorage;
                hasTarget = true;
                waitTimer = Random.Range(0.02f, 0.12f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                SetCarriedPotteryVisible(true);
                UseIdleSprite();
                StrategyDebugLogger.Info(
                    "Logistics",
                    "CarriedResourceReturnStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Pottery),
                    StrategyDebugLogger.F("amount", carriedPotteryAmount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin),
                    StrategyDebugLogger.F("dropoffCell", dropoffCell));
                return true;
            }

            return StoreCarriedPotteryImmediately(reason, "no_reachable_storage");
        }

        private void CompletePotteryResourceReturn()
        {
            int amount = carriedPotteryAmount;
            if (returnStorageYard == null)
            {
                if (!StoreCarriedPotteryImmediately("resource_return_completed", "target_missing"))
                {
                    SchedulePotteryResourceReturnRetry();
                }

                return;
            }

            Vector2Int storageOrigin = returnStorageYard.Origin;
            returnStorageYard.AddResource(StrategyResourceType.Pottery, amount);
            carriedPotteryAmount = 0;
            SetCarriedPotteryVisible(false);
            returnStorageYard = null;
            returnGranary = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            StrategyDebugLogger.Info(
                "Logistics",
                "CarriedResourceReturned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", StrategyResourceType.Pottery),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("storageOrigin", storageOrigin));
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.25f, 0.70f);
            TryStartStorageCarriedReturn("remaining_carried_resource");
        }

        private bool StoreCarriedPotteryImmediately(string reason, string fallbackReason)
        {
            int amount = carriedPotteryAmount;
            if (amount <= 0)
            {
                return false;
            }

            if (StrategyStorageYard.TryFindNearestStorageYard(transform.position, out StrategyStorageYard yard))
            {
                yard.AddResource(StrategyResourceType.Pottery, amount);
                carriedPotteryAmount = 0;
                SetCarriedPotteryVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                TryStartStorageCarriedReturn("remaining_carried_resource");
                return true;
            }

            if (map != null && map.TryWorldToCell(transform.position, out Vector2Int cell))
            {
                StrategyLooseCarriedResourcePile.Create(map, cell, transform.position, StrategyResourceType.Pottery, amount);
                carriedPotteryAmount = 0;
                SetCarriedPotteryVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "CarriedResourceDroppedAsLoosePile",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Pottery),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", fallbackReason),
                    StrategyDebugLogger.F("origin", cell));
                TryStartStorageCarriedReturn("remaining_carried_resource");
                return true;
            }

            return false;
        }

        private void SchedulePotteryResourceReturnRetry()
        {
            if (carriedPotteryAmount <= 0)
            {
                ClearEmptyCarriedResourceReturn("pottery_retry_without_resource");
                return;
            }

            returnStorageYard = null;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.ReturningPotteryToStorage;
            waitTimer = Random.Range(0.65f, 1.35f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
        }

        private bool TryRetryStorageProductionResourceReturn()
        {
            if (activity == ResidentActivity.ReturningCoalToStorage)
            {
                if (!TryStartCoalReturn("resource_return_retry", true))
                {
                    ScheduleCoalResourceReturnRetry();
                }

                return true;
            }

            if (activity == ResidentActivity.ReturningClayToStorage)
            {
                if (!TryStartClayReturn("resource_return_retry", true))
                {
                    ScheduleClayResourceReturnRetry();
                }

                return true;
            }

            if (activity == ResidentActivity.ReturningPlanksToStorage)
            {
                if (!TryStartPlanksReturn("resource_return_retry", true))
                {
                    SchedulePlanksResourceReturnRetry();
                }

                return true;
            }

            if (activity == ResidentActivity.ReturningToolsToStorage)
            {
                if (!TryStartToolsReturn("resource_return_retry", true))
                {
                    ScheduleToolsResourceReturnRetry();
                }

                return true;
            }

            if (activity != ResidentActivity.ReturningPotteryToStorage)
            {
                return false;
            }

            if (!TryStartPotteryReturn("resource_return_retry", true))
            {
                SchedulePotteryResourceReturnRetry();
            }

            return true;
        }

        private void SetCarriedPotteryVisible(bool visible)
        {
            if (!visible || carriedPotteryAmount <= 0)
            {
                if (carriedPotteryRenderer != null)
                {
                    carriedPotteryRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureCarriedPotteryRenderer();
            if (carriedPotteryRenderer == null)
            {
                return;
            }

            carriedPotteryRenderer.gameObject.SetActive(true);
            SyncCarriedPotteryRenderer();
        }

        private void EnsureCarriedPotteryRenderer()
        {
            if (spriteRenderer == null || carriedPotteryRenderer != null)
            {
                return;
            }

            GameObject potteryObject = new GameObject("Carried Pottery");
            potteryObject.transform.SetParent(transform, false);
            carriedPotteryRenderer = potteryObject.AddComponent<SpriteRenderer>();
            carriedPotteryRenderer.sprite = StrategyResourceIconFactory.GetSprite(StrategyResourceType.Pottery);
            carriedPotteryRenderer.color = Color.white;
            carriedPotteryRenderer.gameObject.SetActive(false);
        }

        private void SyncCarriedPotteryRenderer()
        {
            if (spriteRenderer == null || carriedPotteryRenderer == null)
            {
                return;
            }

            carriedPotteryRenderer.sprite = StrategyResourceIconFactory.GetSprite(StrategyResourceType.Pottery);
            carriedPotteryRenderer.flipX = spriteRenderer.flipX;
            carriedPotteryRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            float side = spriteRenderer.flipX ? -0.13f : 0.13f;
            carriedPotteryRenderer.transform.localPosition = new Vector3(side, 0.41f, -0.02f);
            carriedPotteryRenderer.transform.localScale = Vector3.one * 0.82f;
        }
    }
}
