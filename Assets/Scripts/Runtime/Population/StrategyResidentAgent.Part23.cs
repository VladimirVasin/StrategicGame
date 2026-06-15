using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private bool TryStartStorageCoalPickup()
        {
            if (storageWorkplace == null || !storageWorkplace.TryReserveCoalSource(this, out StrategyCoalPit source))
            {
                return false;
            }

            if (!source.TryFindDropoffCell(out Vector2Int pickupCell) || !TryBuildPathTo(pickupCell))
            {
                source.ReleaseStoredCoalReservation(this);
                logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "PickupMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", source.Origin),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Coal),
                    StrategyDebugLogger.F("reason", "no_pickup_path"));
                return false;
            }

            activeCoalSource = source;
            activity = ResidentActivity.MovingToStorageCoalPickup;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            return true;
        }

        private void StartPickingUpStorageCoal()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (activeCoalSource == null || storageWorkplace == null)
            {
                ResetStorageWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpStorageCoal;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            FaceWorldPoint(activeCoalSource.FootprintBounds.center);
        }

        private void UpdatePickingUpStorageCoal()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if (activeCoalSource == null
                || storageWorkplace == null
                || !storageWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell)
                || !activeCoalSource.TryTakeReservedCoal(this, out carriedCoalAmount))
            {
                activeCoalSource?.ReleaseStoredCoalReservation(this);
                ResetStorageWorkToIdle();
                return;
            }

            activeCoalSource = null;
            activity = ResidentActivity.CarryingCoalToStorage;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedCoalVisible(true);
        }

        private void StartDepositingStorageCoal()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingStorageCoal;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            if (storageWorkplace != null)
            {
                FaceWorldPoint(storageWorkplace.FootprintBounds.center);
            }
        }

        private void UpdateDepositingStorageCoal()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.1f);
            SetCarriedCoalVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedCoalAmount;
            storageWorkplace?.AddResource(StrategyResourceType.Coal, depositedAmount);
            carriedCoalAmount = 0;
            SetCarriedCoalVisible(false);
            CompleteStorageDelivery();
        }

        private bool TryStartCoalReturn(string reason, bool restartCurrentReturn = false)
        {
            if (carriedCoalAmount <= 0)
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
                activity = ResidentActivity.ReturningCoalToStorage;
                hasTarget = true;
                waitTimer = Random.Range(0.02f, 0.12f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                SetCarriedCoalVisible(true);
                UseIdleSprite();
                StrategyDebugLogger.Info(
                    "Logistics",
                    "CarriedResourceReturnStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Coal),
                    StrategyDebugLogger.F("amount", carriedCoalAmount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin),
                    StrategyDebugLogger.F("dropoffCell", dropoffCell));
                return true;
            }

            return StoreCarriedCoalImmediately(reason, "no_reachable_storage");
        }

        private void CompleteCoalResourceReturn()
        {
            int amount = carriedCoalAmount;
            Vector2Int storageOrigin = Vector2Int.zero;
            if (returnStorageYard == null)
            {
                if (!StoreCarriedCoalImmediately("resource_return_completed", "target_missing"))
                {
                    ScheduleCoalResourceReturnRetry();
                }

                return;
            }

            storageOrigin = returnStorageYard.Origin;
            returnStorageYard.AddResource(StrategyResourceType.Coal, amount);
            carriedCoalAmount = 0;
            SetCarriedCoalVisible(false);
            returnStorageYard = null;
            returnGranary = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            StrategyDebugLogger.Info(
                "Logistics",
                "CarriedResourceReturned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", StrategyResourceType.Coal),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("storageOrigin", storageOrigin));
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.25f, 0.70f);
            TryStartStorageCarriedReturn("remaining_carried_resource");
        }

        private bool StoreCarriedCoalImmediately(string reason, string fallbackReason)
        {
            int amount = carriedCoalAmount;
            if (amount <= 0)
            {
                return false;
            }

            if (StrategyStorageYard.TryFindNearestStorageYard(transform.position, out StrategyStorageYard yard))
            {
                yard.AddResource(StrategyResourceType.Coal, amount);
                carriedCoalAmount = 0;
                SetCarriedCoalVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Info(
                    "Logistics",
                    "CarriedResourceStoredImmediately",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Coal),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", fallbackReason),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin));
                TryStartStorageCarriedReturn("remaining_carried_resource");
                return true;
            }

            if (map != null && map.TryWorldToCell(transform.position, out Vector2Int cell))
            {
                StrategyLooseCarriedResourcePile.Create(map, cell, transform.position, StrategyResourceType.Coal, amount);
                carriedCoalAmount = 0;
                SetCarriedCoalVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "CarriedResourceDroppedAsLoosePile",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Coal),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", "no_storage_yard"),
                    StrategyDebugLogger.F("origin", cell));
                TryStartStorageCarriedReturn("remaining_carried_resource");
                return true;
            }

            return false;
        }

        private void ScheduleCoalResourceReturnRetry()
        {
            if (carriedCoalAmount <= 0)
            {
                ClearEmptyCarriedResourceReturn("coal_retry_without_resource");
                return;
            }

            returnStorageYard = null;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.ReturningCoalToStorage;
            waitTimer = Random.Range(0.65f, 1.35f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
        }

        private bool TryStartStorageCarriedReturn(string reason)
        {
            return TryStartCarriedResourceReturn(reason) || TryStartCoalReturn(reason);
        }

        private void ReleaseActiveStorageWorkReservations()
        {
            activeLogSource?.ReleaseStoredLogsReservation(this);
            activeStoneSource?.ReleaseStoredStoneReservation(this);
            activeIronSource?.ReleaseStoredIronReservation(this);
            activeCoalSource?.ReleaseStoredCoalReservation(this);
            activeLooseLogSource?.ReleaseStorageReservation(this);
            activeLooseStoneSource?.ReleaseStorageReservation(this);
        }

        private void ClearActiveStorageSources()
        {
            activeLogSource = null;
            activeStoneSource = null;
            activeIronSource = null;
            activeCoalSource = null;
            activeLooseLogSource = null;
            activeLooseStoneSource = null;
        }

        private void SetCarriedCoalVisible(bool visible)
        {
            if (!visible || carriedCoalAmount <= 0)
            {
                if (carriedCoalRenderer != null)
                {
                    carriedCoalRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureCarriedCoalRenderer();
            if (carriedCoalRenderer == null)
            {
                return;
            }

            carriedCoalRenderer.gameObject.SetActive(true);
            SyncCarriedCoalRenderer();
        }

        private void EnsureCarriedCoalRenderer()
        {
            if (spriteRenderer == null || carriedCoalRenderer != null)
            {
                return;
            }

            GameObject coalObject = new GameObject("Carried Coal");
            coalObject.transform.SetParent(transform, false);
            carriedCoalRenderer = coalObject.AddComponent<SpriteRenderer>();
            carriedCoalRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedCoalSprite();
            carriedCoalRenderer.color = Color.white;
            carriedCoalRenderer.gameObject.SetActive(false);
        }

        private void SyncCarriedCoalRenderer()
        {
            if (spriteRenderer == null || carriedCoalRenderer == null)
            {
                return;
            }

            carriedCoalRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedCoalSprite();
            carriedCoalRenderer.flipX = spriteRenderer.flipX;
            carriedCoalRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            float side = spriteRenderer.flipX ? -0.13f : 0.13f;
            carriedCoalRenderer.transform.localPosition = new Vector3(side, 0.38f, -0.02f);
            carriedCoalRenderer.transform.localScale = Vector3.one;
        }
    }
}
