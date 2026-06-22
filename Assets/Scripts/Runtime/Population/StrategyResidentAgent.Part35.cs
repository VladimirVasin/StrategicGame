using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private bool TryStartProductionInputDelivery()
        {
            if (storageWorkplace == null
                || !storageWorkplace.TryReserveProductionInputDelivery(
                    this,
                    out IStrategyProductionLogisticsNode target,
                    out StrategyResourceType resource,
                    out int amount))
            {
                return false;
            }

            if (!storageWorkplace.TryFindDropoffCell(out Vector2Int pickupCell) || !TryBuildPathTo(pickupCell))
            {
                target.ReleaseInputDeliveryReservation(resource, this);
                storageWorkplace.ReleaseProductionInputReservation(this, resource);
                logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "ProductionInputMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin),
                    StrategyDebugLogger.F("targetOrigin", target.Origin),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", "no_pickup_path"));
                return false;
            }

            activeProductionInputTarget = target;
            activeProductionInputResource = resource;
            activity = ResidentActivity.MovingToProductionInputPickup;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            return true;
        }

        private void StartPickingUpProductionInput()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (activeProductionInputTarget == null
                || activeProductionInputResource == StrategyResourceType.None
                || storageWorkplace == null)
            {
                ResetStorageWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpProductionInput;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            FaceWorldPoint(storageWorkplace.FootprintBounds.center);
        }

        private void UpdatePickingUpProductionInput()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if (activeProductionInputTarget == null
                || storageWorkplace == null
                || !activeProductionInputTarget.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell)
                || !storageWorkplace.TryTakeReservedProductionInput(
                    this,
                    activeProductionInputResource,
                    out int amount))
            {
                ResetProductionInputDeliveryToIdle(true);
                return;
            }

            SetCarriedProductionInputAmount(activeProductionInputResource, amount);
            activity = ResidentActivity.CarryingProductionInput;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedProductionInputVisible(activeProductionInputResource, true);
        }

        private void StartDepositingProductionInput()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingProductionInput;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            if (activeProductionInputTarget != null)
            {
                FaceWorldPoint(activeProductionInputTarget.FootprintBounds.center);
            }
        }

        private void UpdateDepositingProductionInput()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.1f);
            SetCarriedProductionInputVisible(activeProductionInputResource, true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int carriedAmount = GetCarriedProductionInputAmount(activeProductionInputResource);
            if (activeProductionInputTarget == null
                || carriedAmount <= 0
                || !activeProductionInputTarget.TryAcceptInputDelivery(
                    activeProductionInputResource,
                    this,
                    carriedAmount,
                    out int accepted))
            {
                ResetProductionInputDeliveryToIdle(true);
                return;
            }

            StrategyResourceType deliveredResource = activeProductionInputResource;
            int leftover = Mathf.Max(0, carriedAmount - accepted);
            SetCarriedProductionInputAmount(deliveredResource, leftover);
            SetCarriedProductionInputVisible(deliveredResource, leftover > 0);
            if (leftover > 0)
            {
                StoreCarriedProductionInputImmediately(deliveredResource, "production_input_overflow");
            }

            ClearProductionInputDelivery();
            CompleteStorageDelivery();
        }

        private void ResetProductionInputDeliveryToIdle(bool storeCarried)
        {
            activeProductionInputTarget?.ReleaseInputDeliveryReservation(activeProductionInputResource, this);
            storageWorkplace?.ReleaseProductionInputReservation(this, activeProductionInputResource);
            if (storeCarried && GetCarriedProductionInputAmount(activeProductionInputResource) > 0)
            {
                StoreCarriedProductionInputImmediately(activeProductionInputResource, "production_input_cancelled");
            }

            ClearProductionInputDelivery();
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            logisticsWorkCooldown = Random.Range(2.0f, 4.5f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void ClearProductionInputDelivery()
        {
            activeProductionInputTarget = null;
            activeProductionInputResource = StrategyResourceType.None;
        }

        private int GetCarriedProductionInputAmount(StrategyResourceType resource)
        {
            return resource switch
            {
                StrategyResourceType.Logs => carriedLogAmount,
                StrategyResourceType.Stone => carriedStoneAmount,
                StrategyResourceType.Iron => carriedIronAmount,
                StrategyResourceType.Coal => carriedCoalAmount,
                StrategyResourceType.Clay => carriedClayAmount,
                StrategyResourceType.Pottery => carriedPotteryAmount,
                StrategyResourceType.Planks => carriedPlanksAmount,
                StrategyResourceType.Tools => carriedToolsAmount,
                _ => 0
            };
        }

        private void SetCarriedProductionInputAmount(StrategyResourceType resource, int amount)
        {
            amount = Mathf.Max(0, amount);
            if (resource == StrategyResourceType.Logs)
            {
                carriedLogAmount = amount;
            }
            else if (resource == StrategyResourceType.Stone)
            {
                carriedStoneAmount = amount;
            }
            else if (resource == StrategyResourceType.Iron)
            {
                carriedIronAmount = amount;
            }
            else if (resource == StrategyResourceType.Coal)
            {
                carriedCoalAmount = amount;
            }
            else if (resource == StrategyResourceType.Clay)
            {
                carriedClayAmount = amount;
            }
            else if (resource == StrategyResourceType.Pottery)
            {
                carriedPotteryAmount = amount;
            }
            else if (resource == StrategyResourceType.Planks)
            {
                carriedPlanksAmount = amount;
            }
            else if (resource == StrategyResourceType.Tools)
            {
                carriedToolsAmount = amount;
            }
        }

        private void SetCarriedProductionInputVisible(StrategyResourceType resource, bool visible)
        {
            if (resource == StrategyResourceType.Logs)
            {
                SetCarriedLogsVisible(visible);
            }
            else if (resource == StrategyResourceType.Stone)
            {
                SetCarriedStoneVisible(visible);
            }
            else if (resource == StrategyResourceType.Iron)
            {
                SetCarriedIronVisible(visible);
            }
            else if (resource == StrategyResourceType.Coal)
            {
                SetCarriedCoalVisible(visible);
            }
            else if (resource == StrategyResourceType.Clay)
            {
                SetCarriedClayVisible(visible);
            }
            else if (resource == StrategyResourceType.Pottery)
            {
                SetCarriedPotteryVisible(visible);
            }
            else if (resource == StrategyResourceType.Planks)
            {
                SetCarriedPlanksVisible(visible);
            }
            else if (resource == StrategyResourceType.Tools)
            {
                SetCarriedToolsVisible(visible);
            }
        }

        private void StoreCarriedProductionInputImmediately(StrategyResourceType resource, string reason)
        {
            int amount = GetCarriedProductionInputAmount(resource);
            if (amount <= 0)
            {
                return;
            }

            if (storageWorkplace != null)
            {
                storageWorkplace.AddResource(resource, amount);
                SetCarriedProductionInputAmount(resource, 0);
                SetCarriedProductionInputVisible(resource, false);
                return;
            }

            if (resource == StrategyResourceType.Logs)
            {
                StoreCarriedMaterialImmediately(StrategyConstructionResourceKind.Logs, reason, "storage_missing");
            }
            else if (resource == StrategyResourceType.Stone)
            {
                StoreCarriedMaterialImmediately(StrategyConstructionResourceKind.Stone, reason, "storage_missing");
            }
            else if (resource == StrategyResourceType.Planks)
            {
                StoreCarriedPlanksImmediately(reason, "storage_missing");
            }
            else if (resource == StrategyResourceType.Iron)
            {
                StoreCarriedIronImmediately(reason, "storage_missing");
            }
            else if (resource == StrategyResourceType.Coal)
            {
                StoreCarriedCoalImmediately(reason, "storage_missing");
            }
            else if (resource == StrategyResourceType.Clay)
            {
                StoreCarriedClayImmediately(reason, "storage_missing");
            }
            else if (resource == StrategyResourceType.Pottery)
            {
                StoreCarriedPotteryImmediately(reason, "storage_missing");
            }
            else if (resource == StrategyResourceType.Tools)
            {
                StoreCarriedToolsImmediately(reason, "storage_missing");
            }
        }
    }
}
