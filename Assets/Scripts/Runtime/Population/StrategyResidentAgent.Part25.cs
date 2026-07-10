using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private IStrategyProductionLogisticsNode activePlanksSource;
        private StrategyLooseConstructionResourcePile activeLoosePlanksSource;
        private SpriteRenderer carriedPlanksRenderer;

        private bool TryStartStoragePlanksPickup()
        {
            if (storageWorkplace == null)
            {
                return false;
            }

            if (storageWorkplace.TryReservePlanksSource(this, out IStrategyProductionLogisticsNode source))
            {
                if (!source.TryFindDropoffCell(out Vector2Int pickupCell) || !TryBuildPathTo(pickupCell))
                {
                    source.ReleaseOutputPickupReservation(StrategyResourceType.Planks, this);
                    logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                    StrategyDebugLogger.Warn(
                        "Logistics",
                        "PickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("sourceOrigin", source.Origin),
                        StrategyDebugLogger.F("resource", StrategyResourceType.Planks),
                        StrategyDebugLogger.F("reason", "no_pickup_path"));
                    return false;
                }

                activePlanksSource = source;
                activity = ResidentActivity.MovingToStoragePlanksPickup;
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.20f);
                return true;
            }

            return TryStartLooseStoragePlanksPickup();
        }

        private bool TryStartLooseStoragePlanksPickup()
        {
            if (!StrategyLooseConstructionResourcePile.TryReserveNearestForStorage(
                    storageWorkplace,
                    this,
                    StrategyConstructionResourceKind.Planks,
                    out StrategyLooseConstructionResourcePile source))
            {
                return false;
            }

            if (!source.TryFindPickupCell(out Vector2Int pickupCell) || !TryBuildPathTo(pickupCell))
            {
                source.ReleaseStorageReservation(this);
                logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                return false;
            }

            activeLoosePlanksSource = source;
            activity = ResidentActivity.MovingToStoragePlanksPickup;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            return true;
        }

        private void StartPickingUpStoragePlanks()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if ((activePlanksSource == null && activeLoosePlanksSource == null) || storageWorkplace == null)
            {
                ResetStorageWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpStoragePlanks;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            FaceWorldPoint(activePlanksSource != null
                ? activePlanksSource.FootprintBounds.center
                : activeLoosePlanksSource.FootprintBounds.center);
        }

        private void UpdatePickingUpStoragePlanks()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if ((activePlanksSource == null && activeLoosePlanksSource == null)
                || storageWorkplace == null
                || !storageWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell)
                || !TryTakeReservedStoragePlanks())
            {
                activePlanksSource?.ReleaseOutputPickupReservation(StrategyResourceType.Planks, this);
                activeLoosePlanksSource?.ReleaseStorageReservation(this);
                ResetStorageWorkToIdle();
                return;
            }

            activePlanksSource = null;
            activeLoosePlanksSource = null;
            activity = ResidentActivity.CarryingPlanksToStorage;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedPlanksVisible(true);
        }

        private bool TryTakeReservedStoragePlanks()
        {
            if (activeLoosePlanksSource != null)
            {
                return activeLoosePlanksSource.TryTakeReservedForStorage(
                    this,
                    StrategyConstructionResourceKind.Planks,
                    out carriedPlanksAmount);
            }

            return activePlanksSource != null
                && activePlanksSource.TryTakeReservedOutput(StrategyResourceType.Planks, this, out carriedPlanksAmount);
        }

        private void StartDepositingStoragePlanks()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingStoragePlanks;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            if (storageWorkplace != null)
            {
                FaceWorldPoint(storageWorkplace.FootprintBounds.center);
            }
        }

        private void UpdateDepositingStoragePlanks()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.1f);
            SetCarriedPlanksVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedPlanksAmount;
            storageWorkplace?.AddResource(StrategyResourceType.Planks, depositedAmount);
            carriedPlanksAmount = 0;
            SetCarriedPlanksVisible(false);
            CompleteStorageDelivery();
        }

        private bool TryStartPlanksReturn(string reason, bool restartCurrentReturn = false)
        {
            if (carriedPlanksAmount <= 0)
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
                activity = ResidentActivity.ReturningPlanksToStorage;
                hasTarget = true;
                waitTimer = Random.Range(0.02f, 0.12f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                SetCarriedPlanksVisible(true);
                UseIdleSprite();
                StrategyDebugLogger.Info(
                    "Logistics",
                    "CarriedResourceReturnStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Planks),
                    StrategyDebugLogger.F("amount", carriedPlanksAmount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin),
                    StrategyDebugLogger.F("dropoffCell", dropoffCell));
                return true;
            }

            return StoreCarriedPlanksImmediately(reason, "no_reachable_storage");
        }

        private void CompletePlanksResourceReturn()
        {
            int amount = carriedPlanksAmount;
            if (returnStorageYard == null)
            {
                if (!StoreCarriedPlanksImmediately("resource_return_completed", "target_missing"))
                {
                    SchedulePlanksResourceReturnRetry();
                }

                return;
            }

            Vector2Int storageOrigin = returnStorageYard.Origin;
            StoreReturnedMaterialAtYard(returnStorageYard, StrategyConstructionResourceKind.Planks, amount);
            carriedPlanksAmount = 0;
            SetCarriedPlanksVisible(false);
            returnStorageYard = null;
            returnGranary = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            StrategyDebugLogger.Info(
                "Logistics",
                "CarriedResourceReturned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", StrategyResourceType.Planks),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("storageOrigin", storageOrigin));
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.25f, 0.70f);
            TryStartStorageCarriedReturn("remaining_carried_resource");
        }

        private bool StoreCarriedPlanksImmediately(string reason, string fallbackReason)
        {
            int amount = carriedPlanksAmount;
            if (amount <= 0)
            {
                return false;
            }

            if (StrategyStorageYard.TryFindNearestStorageYard(transform.position, out StrategyStorageYard yard))
            {
                StoreReturnedMaterialAtYard(yard, StrategyConstructionResourceKind.Planks, amount);
                carriedPlanksAmount = 0;
                SetCarriedPlanksVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Info(
                    "Logistics",
                    "CarriedResourceStoredImmediately",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Planks),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", fallbackReason),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin));
                TryStartStorageCarriedReturn("remaining_carried_resource");
                return true;
            }

            if (map != null && map.TryWorldToCell(transform.position, out Vector2Int cell))
            {
                StrategyLooseConstructionResourcePile pile = StrategyLooseConstructionResourcePile.Create(
                    map,
                    cell,
                    transform.position,
                    0,
                    0,
                    amount);
                RestoreReturnedMaterialReservationOnPile(pile, StrategyConstructionResourceKind.Planks, amount);
                carriedPlanksAmount = 0;
                SetCarriedPlanksVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "CarriedResourceDroppedAsLoosePile",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Planks),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", "no_storage_yard"),
                    StrategyDebugLogger.F("origin", cell));
                TryStartStorageCarriedReturn("remaining_carried_resource");
                return true;
            }

            return false;
        }

        private void SchedulePlanksResourceReturnRetry()
        {
            if (carriedPlanksAmount <= 0)
            {
                ClearEmptyCarriedResourceReturn("planks_retry_without_resource");
                return;
            }

            returnStorageYard = null;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.ReturningPlanksToStorage;
            waitTimer = Random.Range(0.65f, 1.35f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
        }

        private void SetCarriedPlanksVisible(bool visible)
        {
            if (!visible || carriedPlanksAmount <= 0)
            {
                if (carriedPlanksRenderer != null)
                {
                    carriedPlanksRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureCarriedPlanksRenderer();
            if (carriedPlanksRenderer == null)
            {
                return;
            }

            carriedPlanksRenderer.gameObject.SetActive(true);
            SyncCarriedPlanksRenderer();
        }

        private void EnsureCarriedPlanksRenderer()
        {
            if (spriteRenderer == null || carriedPlanksRenderer != null)
            {
                return;
            }

            GameObject planksObject = new GameObject("Carried Planks");
            planksObject.transform.SetParent(transform, false);
            carriedPlanksRenderer = planksObject.AddComponent<SpriteRenderer>();
            carriedPlanksRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedPlanksSprite();
            carriedPlanksRenderer.color = Color.white;
            carriedPlanksRenderer.gameObject.SetActive(false);
        }

        private void SyncCarriedPlanksRenderer()
        {
            if (spriteRenderer == null || carriedPlanksRenderer == null)
            {
                return;
            }

            carriedPlanksRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedPlanksSprite();
            carriedPlanksRenderer.flipX = spriteRenderer.flipX;
            carriedPlanksRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            float side = spriteRenderer.flipX ? -0.13f : 0.13f;
            carriedPlanksRenderer.transform.localPosition = new Vector3(side, 0.42f, -0.02f);
            carriedPlanksRenderer.transform.localScale = Vector3.one;
        }
    }
}
