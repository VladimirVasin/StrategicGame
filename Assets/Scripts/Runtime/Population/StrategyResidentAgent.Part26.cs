namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private void CancelStorageWork(bool storeCarriedLogs)
        {
            if (this == null)
            {
                return;
            }

            if (IsStorageWorkActivity(activity))
            {
                ResetStorageWorkToIdle(storeCarriedLogs);
                return;
            }

            if (IsGranaryWorkActivity(activity))
            {
                ResetGranaryWorkToIdle(true);
                return;
            }

            if (activeLogSource != null)
            {
                activeLogSource.ReleaseStoredLogsReservation(this);
                activeLogSource = null;
            }
            else if (activeStoneSource != null)
            {
                activeStoneSource.ReleaseStoredStoneReservation(this);
                activeStoneSource = null;
            }
            else if (activeIronSource != null)
            {
                activeIronSource.ReleaseStoredIronReservation(this);
                activeIronSource = null;
            }
            else if (activeCoalSource != null)
            {
                activeCoalSource.ReleaseStoredCoalReservation(this);
                activeCoalSource = null;
            }
            else if (activeClaySource != null)
            {
                activeClaySource.ReleaseStoredClayReservation(this);
                activeClaySource = null;
            }
            else if (activePlanksSource != null)
            {
                activePlanksSource.ReleaseOutputPickupReservation(StrategyResourceType.Planks, this);
                activePlanksSource = null;
            }
            else if (activePotterySource != null)
            {
                activePotterySource.ReleaseOutputPickupReservation(StrategyResourceType.Pottery, this);
                activePotterySource = null;
            }
            else if (activeToolsSource != null)
            {
                activeToolsSource.ReleaseOutputPickupReservation(StrategyResourceType.Tools, this);
                activeToolsSource = null;
            }
            else if (activeProductionInputTarget != null)
            {
                activeProductionInputTarget.ReleaseInputDeliveryReservation(activeProductionInputResource, this);
                storageWorkplace?.ReleaseProductionInputReservation(this, activeProductionInputResource);
                ClearProductionInputDelivery();
            }
            else if (activeLoosePlanksSource != null)
            {
                activeLoosePlanksSource.ReleaseStorageReservation(this);
                activeLoosePlanksSource = null;
            }
        }

        private static bool IsStorageWorkActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MovingToStoragePickup
                || residentActivity == ResidentActivity.PickingUpStorageLogs
                || residentActivity == ResidentActivity.CarryingLogsToStorage
                || residentActivity == ResidentActivity.DepositingStorageLogs
                || residentActivity == ResidentActivity.MovingToStorageStonePickup
                || residentActivity == ResidentActivity.PickingUpStorageStone
                || residentActivity == ResidentActivity.CarryingStoneToStorage
                || residentActivity == ResidentActivity.DepositingStorageStone
                || residentActivity == ResidentActivity.MovingToStorageIronPickup
                || residentActivity == ResidentActivity.PickingUpStorageIron
                || residentActivity == ResidentActivity.CarryingIronToStorage
                || residentActivity == ResidentActivity.DepositingStorageIron
                || residentActivity == ResidentActivity.MovingToStorageCoalPickup
                || residentActivity == ResidentActivity.PickingUpStorageCoal
                || residentActivity == ResidentActivity.CarryingCoalToStorage
                || residentActivity == ResidentActivity.DepositingStorageCoal
                || residentActivity == ResidentActivity.MovingToStorageClayPickup
                || residentActivity == ResidentActivity.PickingUpStorageClay
                || residentActivity == ResidentActivity.CarryingClayToStorage
                || residentActivity == ResidentActivity.DepositingStorageClay
                || residentActivity == ResidentActivity.MovingToStoragePlanksPickup
                || residentActivity == ResidentActivity.PickingUpStoragePlanks
                || residentActivity == ResidentActivity.CarryingPlanksToStorage
                || residentActivity == ResidentActivity.DepositingStoragePlanks
                || residentActivity == ResidentActivity.MovingToStoragePotteryPickup
                || residentActivity == ResidentActivity.PickingUpStoragePottery
                || residentActivity == ResidentActivity.CarryingPotteryToStorage
                || residentActivity == ResidentActivity.DepositingStoragePottery
                || residentActivity == ResidentActivity.MovingToStorageToolsPickup
                || residentActivity == ResidentActivity.PickingUpStorageTools
                || residentActivity == ResidentActivity.CarryingToolsToStorage
                || residentActivity == ResidentActivity.DepositingStorageTools
                || residentActivity == ResidentActivity.MovingToProductionInputPickup
                || residentActivity == ResidentActivity.PickingUpProductionInput
                || residentActivity == ResidentActivity.CarryingProductionInput
                || residentActivity == ResidentActivity.DepositingProductionInput;
        }

        private static bool IsGranaryWorkActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MovingToGranaryGamePickup
                || residentActivity == ResidentActivity.PickingUpGranaryGame
                || residentActivity == ResidentActivity.CarryingGameToGranary
                || residentActivity == ResidentActivity.DepositingGranaryGame
                || residentActivity == ResidentActivity.MovingToGranaryFishPickup
                || residentActivity == ResidentActivity.PickingUpGranaryFish
                || residentActivity == ResidentActivity.CarryingFishToGranary
                || residentActivity == ResidentActivity.DepositingGranaryFish
                || residentActivity == ResidentActivity.MovingToGranaryForagePickup
                || residentActivity == ResidentActivity.PickingUpGranaryForage
                || residentActivity == ResidentActivity.CarryingForageToGranary
                || residentActivity == ResidentActivity.DepositingGranaryForage;
        }
    }
}
