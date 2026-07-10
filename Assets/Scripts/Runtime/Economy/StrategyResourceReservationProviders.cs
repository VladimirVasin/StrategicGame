using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard : IStrategyResourceReservationProvider
    {
        public int GetReservedResourceAmount(StrategyResourceType resource)
        {
            return resource switch
            {
                StrategyResourceType.Logs => CountReservations(constructionLogReservations)
                    + reservedLogisticsLogs
                    + CountProductionInputReservations(resource)
                    + CountHouseholdLogReservations(),
                StrategyResourceType.Stone => CountReservations(constructionStoneReservations)
                    + CountProductionInputReservations(resource),
                StrategyResourceType.Planks => CountReservations(constructionPlankReservations)
                    + CountProductionInputReservations(resource),
                StrategyResourceType.Pottery => CountProductionInputReservations(resource)
                    + CountHouseholdPotteryReservations(),
                StrategyResourceType.Iron or StrategyResourceType.Coal or StrategyResourceType.Clay or StrategyResourceType.Tools
                    => CountProductionInputReservations(resource),
                _ => 0
            };
        }
    }

    public sealed partial class StrategyStarterCaravanCart : IStrategyResourceReservationProvider
    {
        public int GetReservedResourceAmount(StrategyResourceType resource)
        {
            int householdFood = 0;
            foreach (HouseholdFoodReservation reservation in householdFoodReservations.Values)
            {
                if (reservation != null && reservation.Resource == resource)
                {
                    householdFood += reservation.Amount;
                }
            }

            return resource switch
            {
                StrategyResourceType.Logs => CountReservations(constructionLogReservations)
                    + CountReservations(householdLogReservations),
                StrategyResourceType.Stone => CountReservations(constructionStoneReservations),
                StrategyResourceType.Planks => CountReservations(constructionPlankReservations),
                _ => householdFood
            };
        }
    }

    public sealed partial class StrategyGranary : IStrategyResourceReservationProvider
    {
        public int GetReservedResourceAmount(StrategyResourceType resource)
        {
            return householdFoodReservedResource == resource
                ? householdFoodReservedAmount
                : 0;
        }
    }

    public sealed partial class StrategyLooseConstructionResourcePile : IStrategyResourceReservationProvider
    {
        public int GetReservedResourceAmount(StrategyResourceType resource)
        {
            return resource switch
            {
                StrategyResourceType.Logs => CountReservations(logReservations),
                StrategyResourceType.Stone => CountReservations(stoneReservations),
                StrategyResourceType.Planks => CountReservations(plankReservations),
                _ => 0
            };
        }
    }

    public sealed partial class StrategyLumberjackCamp : IStrategyResourceReservationProvider
    {
        public int GetReservedResourceAmount(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Logs
                ? CountReservations(constructionLogReservations) + reservedLogs
                : 0;
        }
    }

    public sealed partial class StrategyStonecutterCamp : IStrategyResourceReservationProvider
    {
        public int GetReservedResourceAmount(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Stone
                ? CountReservations(constructionStoneReservations) + reservedStone
                : 0;
        }
    }

    public sealed partial class StrategySawmill : IStrategyResourceReservationProvider
    {
        public int GetReservedResourceAmount(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Planks
                ? CountReservations(constructionPlankReservations) + reservedPlanks
                : 0;
        }
    }

    public sealed partial class StrategyMine : IStrategyResourceReservationProvider
    {
        public int GetReservedResourceAmount(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Iron ? reservedIron : 0;
        }
    }

    public sealed partial class StrategyCoalPit : IStrategyResourceReservationProvider
    {
        public int GetReservedResourceAmount(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Coal ? reservedCoal : 0;
        }
    }

    public sealed partial class StrategyClayPit : IStrategyResourceReservationProvider
    {
        public int GetReservedResourceAmount(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Clay ? reservedClay : 0;
        }
    }

    public sealed partial class StrategyKiln : IStrategyResourceReservationProvider
    {
        public int GetReservedResourceAmount(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Pottery ? reservedPottery : 0;
        }
    }

    public sealed partial class StrategyForge : IStrategyResourceReservationProvider
    {
        public int GetReservedResourceAmount(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Tools ? reservedTools : 0;
        }
    }

    public sealed partial class StrategyHunterCamp : IStrategyResourceReservationProvider
    {
        public int GetReservedResourceAmount(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Game ? reservedGame : 0;
        }
    }

    public sealed partial class StrategyFisherHut : IStrategyResourceReservationProvider
    {
        public int GetReservedResourceAmount(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Fish ? reservedFish : 0;
        }
    }

    public sealed partial class StrategyChickenCoop : IStrategyResourceReservationProvider
    {
        public int GetReservedResourceAmount(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Eggs ? reservedEggs : 0;
        }
    }

    public sealed partial class StrategyForagerCamp : IStrategyResourceReservationProvider
    {
        public int GetReservedResourceAmount(StrategyResourceType resource)
        {
            return forageReservedResource == resource ? forageReservedAmount : 0;
        }
    }
}
