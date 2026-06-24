using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private void StartGatheringForage()
        {
            if (activeForageNode == null
                || (activeForagerCamp == null && home == null)
                || !activeForageNode.IsReservedBy(this))
            {
                ResetForageWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.GatheringForage;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            forageWorkTimer = Random.Range(ForageGatherSecondsMin, ForageGatherSecondsMax);
            FaceWorldPoint(activeForageNode.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Forage",
                "ResidentForageGathering",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", activeForageNode.ResourceType),
                StrategyDebugLogger.F("nodeCell", activeForageNode.Cell));
        }

        private void UpdateGatheringForage()
        {
            forageWorkTimer -= Time.deltaTime;
            AnimateForageWork(
                activeForageNode != null ? activeForageNode.ResourceType : StrategyResourceType.Berries,
                false);
            if (activeForageNode != null)
            {
                FaceWorldPoint(activeForageNode.FootprintBounds.center);
            }

            if (forageWorkTimer > 0f)
            {
                return;
            }

            if (activeForageNode == null
                || !activeForageNode.TryGather(this, out StrategyResourceType resource, out int amount))
            {
                ResetForageWorkToIdle(false);
                return;
            }

            carriedForageResource = resource;
            carriedForageAmount = IsAdult ? amount : Mathf.Min(1, amount);
            activeForageNode = null;
            SetCarriedForageVisible(true);

            if (carriedForageAmount <= 0 || !TryBuildPathToForageDropoff())
            {
                ResetForageWorkToIdle(true);
                return;
            }

            activity = ResidentActivity.CarryingForage;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.12f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            SetCarriedForageVisible(true);
            StrategyDebugLogger.Info(
                "Forage",
                "ResidentForageCarrying",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", carriedForageResource),
                StrategyDebugLogger.F("amount", carriedForageAmount),
                StrategyDebugLogger.F("campOrigin", activeForagerCamp != null ? activeForagerCamp.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero));
        }

        private void StartPickingUpLooseForage()
        {
            if (activeLooseForageSource == null
                || home == null
                || !activeLooseForageSource.IsReservedBy(this))
            {
                ResetForageWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.PickingUpLooseForage;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            forageWorkTimer = Random.Range(0.35f, 0.75f);
            FaceWorldPoint(activeLooseForageSource.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Forage",
                "ResidentLooseForagePickup",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", activeLooseForageSource.Resource),
                StrategyDebugLogger.F("pileOrigin", activeLooseForageSource.Origin));
        }

        private void UpdatePickingUpLooseForage()
        {
            forageWorkTimer -= Time.deltaTime;
            AnimateForageWork(
                activeLooseForageSource != null ? activeLooseForageSource.Resource : StrategyResourceType.Berries,
                true);
            if (activeLooseForageSource != null)
            {
                FaceWorldPoint(activeLooseForageSource.FootprintBounds.center);
            }

            if (forageWorkTimer > 0f)
            {
                return;
            }

            if (activeLooseForageSource == null
                || !activeLooseForageSource.TryTakeReserved(this, out StrategyResourceType resource, out int amount))
            {
                ResetForageWorkToIdle(false);
                return;
            }

            carriedForageResource = resource;
            carriedForageAmount = amount;
            activeLooseForageSource = null;
            SetCarriedForageVisible(true);

            if (carriedForageAmount <= 0 || !TryBuildPathToHomeDropoff())
            {
                ResetForageWorkToIdle(true);
                return;
            }

            activity = ResidentActivity.CarryingForage;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.12f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            SetCarriedForageVisible(true);
            StrategyDebugLogger.Info(
                "Forage",
                "ResidentLooseForageCarrying",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", carriedForageResource),
                StrategyDebugLogger.F("amount", carriedForageAmount),
                StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero));
        }

        private void StartDepositingForage()
        {
            bool hasCampDropoff = activeForagerCamp != null;
            if ((!hasCampDropoff && (home == null || home.Resources == null)) || carriedForageAmount <= 0)
            {
                ResetForageWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.DepositingForage;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            forageWorkTimer = Random.Range(ForageDepositSecondsMin, ForageDepositSecondsMax);
            FaceWorldPoint(hasCampDropoff ? activeForagerCamp.FootprintBounds.center : home.FootprintBounds.center);
            SetCarriedForageVisible(true);
        }

        private void UpdateDepositingForage()
        {
            forageWorkTimer -= Time.deltaTime;
            AnimateForageWork(carriedForageResource, true);
            SetCarriedForageVisible(true);
            if (forageWorkTimer > 0f)
            {
                return;
            }

            StrategyResourceType depositedResource = carriedForageResource;
            int depositedAmount = carriedForageAmount;
            if (activeForagerCamp != null && depositedAmount > 0)
            {
                activeForagerCamp.AddForage(depositedResource, depositedAmount);
                StrategyDebugLogger.Info(
                    "ForagerCamp",
                    "ForageDelivered",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", depositedResource),
                    StrategyDebugLogger.F("amount", depositedAmount),
                    StrategyDebugLogger.F("campOrigin", activeForagerCamp.Origin));
                activeForagerCamp = null;
                foragerWorkCooldown = Random.Range(2.0f, 4.5f);
            }
            else if (home != null && home.Resources != null && depositedAmount > 0)
            {
                home.Resources.AddResource(depositedResource, depositedAmount);
                StrategyDebugLogger.Info(
                    "Forage",
                    "ResidentForageDeposited",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", depositedResource),
                    StrategyDebugLogger.F("amount", depositedAmount),
                    StrategyDebugLogger.F("homeOrigin", home.Origin));
            }

            carriedForageResource = StrategyResourceType.None;
            carriedForageAmount = 0;
            activeForagerCamp = null;
            SetCarriedForageVisible(false);
            activity = GetRestingActivity();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.45f, 1.05f);
        }

        private bool TryBuildPathToHomeDropoff()
        {
            if (map == null || home == null || !TryFindHomeExitWorld(out Vector3 exitWorld))
            {
                return false;
            }

            return map.TryWorldToCell(exitWorld, out Vector2Int dropoffCell)
                && TryBuildPathTo(dropoffCell);
        }

        private bool TryBuildPathToForageDropoff()
        {
            if (activeForagerCamp != null)
            {
                return activeForagerCamp.TryFindDropoffCell(out Vector2Int campDropoffCell)
                    && TryBuildPathTo(campDropoffCell);
            }

            return TryBuildPathToHomeDropoff();
        }

        private void CancelForageWork(bool storeCarried)
        {
            if (!IsForagingActivity(activity)
                && activeForageNode == null
                && activeLooseForageSource == null
                && carriedForageAmount <= 0)
            {
                return;
            }

            ResetForageWorkToIdle(storeCarried);
        }

        private void ResetForageWorkToIdle(bool storeCarried)
        {
            if (activeForageNode != null)
            {
                activeForageNode.Release(this);
                activeForageNode = null;
            }

            if (activeLooseForageSource != null)
            {
                activeLooseForageSource.ReleaseReservation(this);
                activeLooseForageSource = null;
            }

            if (storeCarried && carriedForageAmount > 0 && activeForagerCamp != null)
            {
                activeForagerCamp.AddForage(carriedForageResource, carriedForageAmount);
                StrategyDebugLogger.Info(
                    "ForagerCamp",
                    "ForageStoredOnCancel",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", carriedForageResource),
                    StrategyDebugLogger.F("amount", carriedForageAmount),
                    StrategyDebugLogger.F("campOrigin", activeForagerCamp.Origin));
                foragerWorkCooldown = Random.Range(2.0f, 4.5f);
            }
            else if (storeCarried && carriedForageAmount > 0 && home != null && home.Resources != null)
            {
                home.Resources.AddResource(carriedForageResource, carriedForageAmount);
                StrategyDebugLogger.Info(
                    "Forage",
                    "ResidentForageStoredOnCancel",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", carriedForageResource),
                    StrategyDebugLogger.F("amount", carriedForageAmount),
                    StrategyDebugLogger.F("homeOrigin", home.Origin));
            }

            activeForagerCamp = null;
            carriedForageResource = StrategyResourceType.None;
            carriedForageAmount = 0;
            SetCarriedForageVisible(false);
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.30f, 0.85f);
        }

        private int GetCarriedHouseholdFoodAmount()
        {
            return carriedHouseholdFoodResource switch
            {
                StrategyResourceType.Game => carriedGameAmount,
                StrategyResourceType.Fish => carriedFishAmount,
                StrategyResourceType.Berries or StrategyResourceType.Roots or StrategyResourceType.Mushrooms => carriedForageAmount,
                _ => 0
            };
        }

        private void ClearCarriedHouseholdFood()
        {
            if (carriedHouseholdFoodResource == StrategyResourceType.Game)
            {
                carriedGameAmount = 0;
            }
            else if (carriedHouseholdFoodResource == StrategyResourceType.Fish)
            {
                carriedFishAmount = 0;
            }
            else if (IsForageFood(carriedHouseholdFoodResource))
            {
                carriedForageAmount = 0;
                carriedForageResource = StrategyResourceType.None;
            }

            carriedHouseholdFoodResource = StrategyResourceType.None;
        }

        private void SetCarriedHouseholdFoodVisible(bool visible)
        {
            if (carriedHouseholdFoodResource == StrategyResourceType.Game)
            {
                SetCarriedGameVisible(visible);
                SetCarriedFishVisible(false);
            }
            else if (carriedHouseholdFoodResource == StrategyResourceType.Fish)
            {
                SetCarriedFishVisible(visible);
                SetCarriedGameVisible(false);
                SetCarriedForageVisible(false);
            }
            else if (IsForageFood(carriedHouseholdFoodResource))
            {
                SetCarriedForageVisible(visible);
                SetCarriedGameVisible(false);
                SetCarriedFishVisible(false);
            }
            else
            {
                SetCarriedGameVisible(false);
                SetCarriedFishVisible(false);
                SetCarriedForageVisible(false);
            }
        }
    }
}
