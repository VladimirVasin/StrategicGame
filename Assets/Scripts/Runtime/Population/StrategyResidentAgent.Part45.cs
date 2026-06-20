using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private bool TryStartClayReturn(string reason, bool restartCurrentReturn = false)
        {
            if (carriedClayAmount <= 0)
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
                activity = ResidentActivity.ReturningClayToStorage;
                hasTarget = true;
                waitTimer = Random.Range(0.02f, 0.12f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                SetCarriedClayVisible(true);
                UseIdleSprite();
                StrategyDebugLogger.Info(
                    "Logistics",
                    "CarriedResourceReturnStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Clay),
                    StrategyDebugLogger.F("amount", carriedClayAmount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin),
                    StrategyDebugLogger.F("dropoffCell", dropoffCell));
                return true;
            }

            return StoreCarriedClayImmediately(reason, "no_reachable_storage");
        }

        private bool CompleteClayResourceReturn(out int amount, out object resource, out Vector2Int storageOrigin)
        {
            amount = carriedClayAmount;
            resource = StrategyResourceType.Clay;
            storageOrigin = Vector2Int.zero;
            if (returnStorageYard == null)
            {
                if (!StoreCarriedClayImmediately("resource_return_completed", "target_missing"))
                {
                    ScheduleClayResourceReturnRetry();
                }

                return false;
            }

            storageOrigin = returnStorageYard.Origin;
            returnStorageYard.AddResource(StrategyResourceType.Clay, amount);
            carriedClayAmount = 0;
            SetCarriedClayVisible(false);
            return true;
        }

        private bool StoreCarriedClayImmediately(string reason, string fallbackReason)
        {
            int amount = carriedClayAmount;
            if (amount <= 0)
            {
                return false;
            }

            if (StrategyStorageYard.TryFindNearestStorageYard(transform.position, out StrategyStorageYard yard))
            {
                yard.AddResource(StrategyResourceType.Clay, amount);
                carriedClayAmount = 0;
                SetCarriedClayVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Info(
                    "Logistics",
                    "CarriedResourceStoredImmediately",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Clay),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", fallbackReason),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin));
                TryStartStorageCarriedReturn("remaining_carried_resource");
                return true;
            }

            if (map != null && map.TryWorldToCell(transform.position, out Vector2Int cell))
            {
                StrategyLooseCarriedResourcePile.Create(map, cell, transform.position, StrategyResourceType.Clay, amount);
                carriedClayAmount = 0;
                SetCarriedClayVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "CarriedResourceDroppedAsLoosePile",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Clay),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", "no_storage_yard"),
                    StrategyDebugLogger.F("origin", cell));
                TryStartStorageCarriedReturn("remaining_carried_resource");
                return true;
            }

            return false;
        }

        private void ScheduleClayResourceReturnRetry()
        {
            if (carriedClayAmount <= 0)
            {
                ClearEmptyCarriedResourceReturn("clay_retry_without_resource");
                return;
            }

            returnStorageYard = null;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.ReturningClayToStorage;
            waitTimer = Random.Range(0.65f, 1.35f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
        }

        private void ResetClayPitWorkEffectTimer(bool immediate)
        {
            clayPitWorkEffectTimer = immediate ? 0.05f : Random.Range(0.46f, 0.86f);
        }

        private void UpdateClayPitWorkEffects()
        {
            if (activeClayPit == null)
            {
                return;
            }

            clayPitWorkEffectTimer -= Time.deltaTime;
            if (clayPitWorkEffectTimer > 0f)
            {
                return;
            }

            activeClayPit.PlayDiggingWorkEffect(this, ResidentId + Mathf.RoundToInt(Time.time * 10f));
            ResetClayPitWorkEffectTimer(false);
        }

        private void SetCarriedClayVisible(bool visible)
        {
            if (!visible || carriedClayAmount <= 0)
            {
                if (carriedClayRenderer != null)
                {
                    carriedClayRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureCarriedClayRenderer();
            if (carriedClayRenderer == null)
            {
                return;
            }

            carriedClayRenderer.gameObject.SetActive(true);
            SyncCarriedClayRenderer();
        }

        private void EnsureCarriedClayRenderer()
        {
            if (spriteRenderer == null || carriedClayRenderer != null)
            {
                return;
            }

            GameObject clayObject = new GameObject("Carried Clay");
            clayObject.transform.SetParent(transform, false);
            carriedClayRenderer = clayObject.AddComponent<SpriteRenderer>();
            carriedClayRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedClaySprite();
            carriedClayRenderer.color = Color.white;
            carriedClayRenderer.gameObject.SetActive(false);
        }

        private void SyncCarriedClayRenderer()
        {
            if (spriteRenderer == null || carriedClayRenderer == null)
            {
                return;
            }

            carriedClayRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedClaySprite();
            carriedClayRenderer.flipX = spriteRenderer.flipX;
            carriedClayRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            float side = spriteRenderer.flipX ? -0.13f : 0.13f;
            carriedClayRenderer.transform.localPosition = new Vector3(side, 0.38f, -0.02f);
            carriedClayRenderer.transform.localScale = Vector3.one;
        }
    }
}
