using UnityEngine;
using ResidentActivity = ProjectUnknown.Strategy.StrategyResidentAgent.ResidentActivity;

namespace ProjectUnknown.Strategy
{
    public enum StrategyResidentTaskKind
    {
        Rest,
        Migration,
        Household,
        Social,
        Forestry,
        Extraction,
        Production,
        Logistics,
        Construction,
        Hunting,
        Fishing,
        NightLighting,
        Funeral
    }

    public interface IStrategyResidentTask
    {
        StrategyResidentTaskKind Kind { get; }
        StrategyResidentAgent.ResidentActivity Activity { get; }
        bool IsWork { get; }
        bool IsLogistics { get; }
        int TransitionId { get; }
        float StartedAt { get; }
    }

    internal sealed class StrategyResidentTaskState : IStrategyResidentTask
    {
        public StrategyResidentTaskKind Kind { get; private set; } = StrategyResidentTaskKind.Rest;
        public StrategyResidentAgent.ResidentActivity Activity { get; private set; }
        public bool IsWork => Kind == StrategyResidentTaskKind.Forestry
            || Kind == StrategyResidentTaskKind.Extraction
            || Kind == StrategyResidentTaskKind.Production
            || Kind == StrategyResidentTaskKind.Logistics
            || Kind == StrategyResidentTaskKind.Construction
            || Kind == StrategyResidentTaskKind.Hunting
            || Kind == StrategyResidentTaskKind.Fishing;
        public bool IsLogistics => Kind == StrategyResidentTaskKind.Logistics;
        public int TransitionId { get; private set; }
        public float StartedAt { get; private set; }

        public void SetActivity(StrategyResidentAgent.ResidentActivity activity)
        {
            if (Activity == activity)
            {
                return;
            }

            StrategyResidentTaskKind nextKind = Classify(activity);
            Activity = activity;
            if (Kind != nextKind)
            {
                Kind = nextKind;
                TransitionId++;
                StartedAt = Time.time;
            }
        }

        public void Reset()
        {
            Kind = StrategyResidentTaskKind.Rest;
            Activity = StrategyResidentAgent.ResidentActivity.Idle;
            TransitionId++;
            StartedAt = Time.time;
        }

        private static StrategyResidentTaskKind Classify(ResidentActivity activity)
        {
            if (activity == ResidentActivity.ArrivingAsRefugee || activity == ResidentActivity.LeavingSettlement)
            {
                return StrategyResidentTaskKind.Migration;
            }

            if (activity is >= ResidentActivity.MovingToFuneral and <= ResidentActivity.WaitingAtFuneral)
            {
                return StrategyResidentTaskKind.Funeral;
            }

            if (activity is ResidentActivity.MovingToNightLight or ResidentActivity.LightingNightLight)
            {
                return StrategyResidentTaskKind.NightLighting;
            }

            if (activity is >= ResidentActivity.MovingToChildPlay and <= ResidentActivity.PlayingTag)
            {
                return StrategyResidentTaskKind.Social;
            }

            if (activity is >= ResidentActivity.MovingToTree and <= ResidentActivity.PlantingTree)
            {
                return StrategyResidentTaskKind.Forestry;
            }

            if (activity is >= ResidentActivity.MovingToStone and <= ResidentActivity.DiggingClayInPit)
            {
                return StrategyResidentTaskKind.Extraction;
            }

            if (activity is >= ResidentActivity.MovingToSawmill and <= ResidentActivity.ForgingTools)
            {
                return StrategyResidentTaskKind.Production;
            }

            if (activity is >= ResidentActivity.MovingToHuntingRange and <= ResidentActivity.DepositingGame)
            {
                return StrategyResidentTaskKind.Hunting;
            }

            if (activity is >= ResidentActivity.MovingToFishingSpot and <= ResidentActivity.DepositingFish)
            {
                return StrategyResidentTaskKind.Fishing;
            }

            if (activity is >= ResidentActivity.MovingToConstructionStorage and <= ResidentActivity.DepositingConstructionResource)
            {
                return StrategyResidentTaskKind.Logistics;
            }

            if (activity is ResidentActivity.MovingToConstructionSite or ResidentActivity.BuildingConstruction)
            {
                return StrategyResidentTaskKind.Construction;
            }

            if (IsStorageLogistics(activity)
                || activity is >= ResidentActivity.MovingToGranaryGamePickup and <= ResidentActivity.DepositingGranaryForage
                || activity is >= ResidentActivity.ReturningLogsToStorage and <= ResidentActivity.ReturningForageToGranary
                || activity is >= ResidentActivity.MovingToProductionInputPickup and <= ResidentActivity.DepositingProductionInput)
            {
                return StrategyResidentTaskKind.Logistics;
            }

            if (activity is >= ResidentActivity.MovingToGarden and <= ResidentActivity.DepositingForage
                || activity is >= ResidentActivity.MovingToHouseholdPotteryPickup and <= ResidentActivity.DepositingHouseholdLogs
                || activity is >= ResidentActivity.MovingToHouseholdFoodPickup and <= ResidentActivity.CookingHouseMeal)
            {
                return StrategyResidentTaskKind.Household;
            }

            return StrategyResidentTaskKind.Rest;
        }

        private static bool IsStorageLogistics(ResidentActivity activity)
        {
            return activity is >= ResidentActivity.MovingToStoragePickup and <= ResidentActivity.DepositingStorageLogs
                || activity is >= ResidentActivity.MovingToStorageStonePickup and <= ResidentActivity.DepositingStorageTools;
        }
    }
}
