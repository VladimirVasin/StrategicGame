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
            else if (activePlanksSource != null)
            {
                activePlanksSource.ReleaseStoredPlanksReservation(this);
                activePlanksSource = null;
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
                || residentActivity == ResidentActivity.MovingToStoragePlanksPickup
                || residentActivity == ResidentActivity.PickingUpStoragePlanks
                || residentActivity == ResidentActivity.CarryingPlanksToStorage
                || residentActivity == ResidentActivity.DepositingStoragePlanks;
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
                || residentActivity == ResidentActivity.DepositingGranaryFish;
        }
    }
}
