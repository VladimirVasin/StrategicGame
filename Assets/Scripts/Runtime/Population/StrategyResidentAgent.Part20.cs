using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private bool TryStartIronReturn(string reason)
        {
            if (carriedIronAmount <= 0)
            {
                return false;
            }

            if (!returnCarriedResourcesImmediately
                && StrategyStorageYard.TryFindNearestDropoff(transform.position, out StrategyStorageYard yard, out Vector2Int dropoffCell)
                && TryBuildPathTo(dropoffCell))
            {
                returnStorageYard = yard;
                returnGranary = null;
                activity = ResidentActivity.ReturningIronToStorage;
                hasTarget = true;
                waitTimer = Random.Range(0.02f, 0.12f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                SetCarriedIronVisible(true);
                UseIdleSprite();
                StrategyDebugLogger.Info(
                    "Logistics",
                    "CarriedResourceReturnStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Iron),
                    StrategyDebugLogger.F("amount", carriedIronAmount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin),
                    StrategyDebugLogger.F("dropoffCell", dropoffCell));
                return true;
            }

            return StoreCarriedIronImmediately(reason, "no_reachable_storage");
        }

        private bool CompleteIronResourceReturn(out int amount, out object resource, out Vector2Int storageOrigin)
        {
            amount = carriedIronAmount;
            resource = StrategyResourceType.Iron;
            storageOrigin = Vector2Int.zero;
            if (returnStorageYard == null)
            {
                if (!StoreCarriedIronImmediately("resource_return_completed", "target_missing"))
                {
                    ScheduleCarriedResourceReturnRetry();
                }

                return false;
            }

            storageOrigin = returnStorageYard.Origin;
            returnStorageYard.AddResource(StrategyResourceType.Iron, amount);
            carriedIronAmount = 0;
            SetCarriedIronVisible(false);
            return true;
        }

        private bool StoreCarriedIronImmediately(string reason, string fallbackReason)
        {
            int amount = carriedIronAmount;
            if (amount <= 0)
            {
                return false;
            }

            if (StrategyStorageYard.TryFindNearestStorageYard(transform.position, out StrategyStorageYard yard))
            {
                yard.AddResource(StrategyResourceType.Iron, amount);
                carriedIronAmount = 0;
                SetCarriedIronVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Info(
                    "Logistics",
                    "CarriedResourceStoredImmediately",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Iron),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", fallbackReason),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin));
                TryStartCarriedResourceReturn("remaining_carried_resource");
                return true;
            }

            if (map != null && map.TryWorldToCell(transform.position, out Vector2Int cell))
            {
                StrategyLooseCarriedResourcePile.Create(map, cell, transform.position, StrategyResourceType.Iron, amount);
                carriedIronAmount = 0;
                SetCarriedIronVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "CarriedResourceDroppedAsLoosePile",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Iron),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", "no_storage_yard"),
                    StrategyDebugLogger.F("origin", cell));
                TryStartCarriedResourceReturn("remaining_carried_resource");
                return true;
            }

            return false;
        }

        private void SetCarriedIronVisible(bool visible)
        {
            if (!visible || carriedIronAmount <= 0)
            {
                if (carriedIronRenderer != null)
                {
                    carriedIronRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureCarriedIronRenderer();
            if (carriedIronRenderer == null)
            {
                return;
            }

            carriedIronRenderer.gameObject.SetActive(true);
            SyncCarriedIronRenderer();
        }

        private void EnsureCarriedIronRenderer()
        {
            if (spriteRenderer == null || carriedIronRenderer != null)
            {
                return;
            }

            GameObject ironObject = new GameObject("Carried Iron");
            ironObject.transform.SetParent(transform, false);
            carriedIronRenderer = ironObject.AddComponent<SpriteRenderer>();
            carriedIronRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedIronSprite();
            carriedIronRenderer.color = Color.white;
            carriedIronRenderer.gameObject.SetActive(false);
        }

        private void SyncCarriedIronRenderer()
        {
            if (spriteRenderer == null || carriedIronRenderer == null)
            {
                return;
            }

            carriedIronRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedIronSprite();
            carriedIronRenderer.flipX = spriteRenderer.flipX;
            carriedIronRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            float side = spriteRenderer.flipX ? -0.13f : 0.13f;
            carriedIronRenderer.transform.localPosition = new Vector3(side, 0.39f, -0.02f);
            carriedIronRenderer.transform.localScale = Vector3.one;
        }

        private void DropLooseCarriedResourceOnDeath(Vector2Int cell, StrategyResourceType resource, int amount)
        {
            if (resource == StrategyResourceType.None || amount <= 0)
            {
                return;
            }

            StrategyLooseCarriedResourcePile pile = null;
            if (resource == StrategyResourceType.Dish
                && (carriedPreparedDishAmount > 0 || carriedPreparedDishLeftoverRations > 0f))
            {
                pile = StrategyLooseCarriedResourcePile.CreatePreparedDishes(
                    map,
                    cell,
                    transform.position,
                    carriedPreparedDishRecipeId,
                    carriedPreparedDishAmount,
                    carriedPreparedDishLeftoverRations);
            }

            pile ??= StrategyLooseCarriedResourcePile.Create(
                map,
                cell,
                transform.position,
                resource,
                amount);
            StrategyDebugLogger.Warn(
                "Logistics",
                "CarriedResourceDroppedOnDeath",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("origin", cell),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("exactPreparedDish", pile != null && pile.HasPreparedDishPayload),
                StrategyDebugLogger.F("reservation", "cleared"));
        }
    }
}
