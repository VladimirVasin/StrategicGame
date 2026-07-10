using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private IStrategyProductionLogisticsNode activeToolsSource;
        private SpriteRenderer carriedToolsRenderer;

        private bool TryStartStorageToolsPickup()
        {
            if (storageWorkplace == null || !storageWorkplace.TryReserveToolsSource(this, out IStrategyProductionLogisticsNode source))
            {
                return false;
            }

            if (!source.TryFindDropoffCell(out Vector2Int pickupCell) || !TryBuildPathTo(pickupCell))
            {
                source.ReleaseOutputPickupReservation(StrategyResourceType.Tools, this);
                logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "PickupMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", source.Origin),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Tools),
                    StrategyDebugLogger.F("reason", "no_pickup_path"));
                return false;
            }

            activeToolsSource = source;
            activity = ResidentActivity.MovingToStorageToolsPickup;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            return true;
        }

        private void StartPickingUpStorageTools()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (activeToolsSource == null || storageWorkplace == null)
            {
                ResetStorageWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpStorageTools;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            FaceWorldPoint(activeToolsSource.FootprintBounds.center);
        }

        private void UpdatePickingUpStorageTools()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if (activeToolsSource == null
                || storageWorkplace == null
                || !storageWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell)
                || !activeToolsSource.TryTakeReservedOutput(StrategyResourceType.Tools, this, out carriedToolsAmount))
            {
                activeToolsSource?.ReleaseOutputPickupReservation(StrategyResourceType.Tools, this);
                ResetStorageWorkToIdle();
                return;
            }

            activeToolsSource = null;
            activity = ResidentActivity.CarryingToolsToStorage;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedToolsVisible(true);
        }

        private void StartDepositingStorageTools()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingStorageTools;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            if (storageWorkplace != null)
            {
                FaceWorldPoint(storageWorkplace.FootprintBounds.center);
            }
        }

        private void UpdateDepositingStorageTools()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.1f);
            SetCarriedToolsVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedToolsAmount;
            storageWorkplace?.AddResource(StrategyResourceType.Tools, depositedAmount);
            carriedToolsAmount = 0;
            SetCarriedToolsVisible(false);
            CompleteStorageDelivery();
        }

        private bool TryStartToolsReturn(string reason, bool restartCurrentReturn = false)
        {
            if (carriedToolsAmount <= 0)
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
                activity = ResidentActivity.ReturningToolsToStorage;
                hasTarget = true;
                waitTimer = Random.Range(0.02f, 0.12f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                SetCarriedToolsVisible(true);
                UseIdleSprite();
                StrategyDebugLogger.Info(
                    "Logistics",
                    "CarriedResourceReturnStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Tools),
                    StrategyDebugLogger.F("amount", carriedToolsAmount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin),
                    StrategyDebugLogger.F("dropoffCell", dropoffCell));
                return true;
            }

            return StoreCarriedToolsImmediately(reason, "no_reachable_storage");
        }

        private void CompleteToolsResourceReturn()
        {
            int amount = carriedToolsAmount;
            if (returnStorageYard == null)
            {
                if (!StoreCarriedToolsImmediately("resource_return_completed", "target_missing"))
                {
                    ScheduleToolsResourceReturnRetry();
                }

                return;
            }

            Vector2Int storageOrigin = returnStorageYard.Origin;
            returnStorageYard.AddResource(StrategyResourceType.Tools, amount);
            carriedToolsAmount = 0;
            SetCarriedToolsVisible(false);
            returnStorageYard = null;
            returnGranary = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            StrategyDebugLogger.Info(
                "Logistics",
                "CarriedResourceReturned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", StrategyResourceType.Tools),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("storageOrigin", storageOrigin));
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.25f, 0.70f);
            TryStartStorageCarriedReturn("remaining_carried_resource");
        }

        private bool StoreCarriedToolsImmediately(string reason, string fallbackReason)
        {
            int amount = carriedToolsAmount;
            if (amount <= 0)
            {
                return false;
            }

            if (StrategyStorageYard.TryFindNearestStorageYard(transform.position, out StrategyStorageYard yard))
            {
                yard.AddResource(StrategyResourceType.Tools, amount);
                carriedToolsAmount = 0;
                SetCarriedToolsVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                TryStartStorageCarriedReturn("remaining_carried_resource");
                return true;
            }

            if (map != null && map.TryWorldToCell(transform.position, out Vector2Int cell))
            {
                StrategyLooseCarriedResourcePile.Create(map, cell, transform.position, StrategyResourceType.Tools, amount);
                carriedToolsAmount = 0;
                SetCarriedToolsVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "CarriedResourceDroppedAsLoosePile",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Tools),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", fallbackReason),
                    StrategyDebugLogger.F("origin", cell));
                TryStartStorageCarriedReturn("remaining_carried_resource");
                return true;
            }

            return false;
        }

        private void ScheduleToolsResourceReturnRetry()
        {
            if (carriedToolsAmount <= 0)
            {
                ClearEmptyCarriedResourceReturn("tools_retry_without_resource");
                return;
            }

            returnStorageYard = null;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.ReturningToolsToStorage;
            waitTimer = Random.Range(0.65f, 1.35f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
        }

        private void SetCarriedToolsVisible(bool visible)
        {
            if (!visible || carriedToolsAmount <= 0)
            {
                if (carriedToolsRenderer != null)
                {
                    carriedToolsRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureCarriedToolsRenderer();
            if (carriedToolsRenderer == null)
            {
                return;
            }

            carriedToolsRenderer.gameObject.SetActive(true);
            SyncCarriedToolsRenderer();
        }

        private void EnsureCarriedToolsRenderer()
        {
            if (spriteRenderer == null || carriedToolsRenderer != null)
            {
                return;
            }

            GameObject toolsObject = new GameObject("Carried Tools");
            toolsObject.transform.SetParent(transform, false);
            carriedToolsRenderer = toolsObject.AddComponent<SpriteRenderer>();
            carriedToolsRenderer.sprite = StrategyResourceIconFactory.GetSprite(StrategyResourceType.Tools);
            carriedToolsRenderer.color = Color.white;
            carriedToolsRenderer.gameObject.SetActive(false);
        }

        private void SyncCarriedToolsRenderer()
        {
            if (spriteRenderer == null || carriedToolsRenderer == null)
            {
                return;
            }

            carriedToolsRenderer.sprite = StrategyResourceIconFactory.GetSprite(StrategyResourceType.Tools);
            carriedToolsRenderer.flipX = spriteRenderer.flipX;
            carriedToolsRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            float side = spriteRenderer.flipX ? -0.13f : 0.13f;
            carriedToolsRenderer.transform.localPosition = new Vector3(side, 0.41f, -0.02f);
            carriedToolsRenderer.transform.localScale = Vector3.one * 0.84f;
        }
    }
}
