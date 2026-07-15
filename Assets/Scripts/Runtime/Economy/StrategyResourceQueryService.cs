using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public static class StrategyResourceQueryService
    {
        private static readonly List<StrategyResourceStore> stores = new();

        public static int GetStored(
            StrategyResourceType resource,
            StrategyResourceStoreScope scopes = StrategyResourceStoreScope.All)
        {
            int total = 0;
            StrategyResourceStore.CopyActiveStores(stores);
            for (int i = 0; i < stores.Count; i++)
            {
                StrategyResourceStore store = stores[i];
                if ((store.Scope & scopes) != 0)
                {
                    total += store.GetStored(resource);
                }
            }

            return total;
        }

        public static int GetAvailable(
            StrategyResourceType resource,
            StrategyResourceStoreScope scopes = StrategyResourceStoreScope.All)
        {
            int total = 0;
            StrategyResourceStore.CopyActiveStores(stores);
            for (int i = 0; i < stores.Count; i++)
            {
                StrategyResourceStore store = stores[i];
                if ((store.Scope & scopes) != 0)
                {
                    total += store.GetAvailable(resource);
                }
            }

            return total;
        }

        public static float GetFoodRations(
            StrategyResourceStoreScope scopes = StrategyResourceStoreScope.All,
            bool availableOnly = true)
        {
            float total = 0f;
            StrategyResourceStore.CopyActiveStores(stores);
            for (int i = 0; i < stores.Count; i++)
            {
                StrategyResourceStore store = stores[i];
                if ((store.Scope & scopes) == 0)
                {
                    continue;
                }

                for (int resource = 1; resource <= (int)StrategyResourceType.Tools; resource++)
                {
                    StrategyResourceType type = (StrategyResourceType)resource;
                    float rationValue = StrategyFoodNutrition.GetRationValue(type);
                    if (rationValue > 0f)
                    {
                        if (type == StrategyResourceType.Dish
                            && store.Owner is StrategyLooseCarriedResourcePile exactDishPile
                            && exactDishPile.HasPreparedDishPayload)
                        {
                            continue;
                        }

                        int amount = availableOnly ? store.GetAvailable(type) : store.GetStored(type);
                        total += amount * rationValue;
                    }
                }

                if (store.Owner is StrategyHouseResourceStore house)
                {
                    total += house.GetPreparedDishRations();
                }
                else if (store.Owner is StrategyLooseCarriedResourcePile loosePile
                    && loosePile.HasPreparedDishPayload)
                {
                    total += loosePile.GetPreparedDishRations(availableOnly);
                }
            }

            return total;
        }

        public static StrategyConstructionResourceCost GetConstructionResources()
        {
            int logs = 0;
            int stone = 0;
            int planks = 0;
            StrategyResourceStore.CopyActiveStores(stores);
            for (int i = 0; i < stores.Count; i++)
            {
                switch (stores[i].Owner)
                {
                    case StrategyStorageYard yard:
                        logs += yard.AvailableConstructionLogs;
                        stone += yard.AvailableConstructionStone;
                        planks += yard.AvailableConstructionPlanks;
                        break;
                    case StrategyLooseConstructionResourcePile pile:
                        logs += pile.AvailableLogs;
                        stone += pile.AvailableStone;
                        planks += pile.AvailablePlanks;
                        break;
                    case StrategyLumberjackCamp camp:
                        logs += camp.AvailableConstructionLogs;
                        break;
                    case StrategyStonecutterCamp camp:
                        stone += camp.AvailableConstructionStone;
                        break;
                    case StrategySawmill sawmill:
                        planks += sawmill.AvailableConstructionPlanks;
                        break;
                    case StrategyStarterCaravanCart cart:
                        logs += cart.AvailableConstructionLogs;
                        stone += cart.AvailableConstructionStone;
                        planks += cart.AvailableConstructionPlanks;
                        break;
                }
            }

            return new StrategyConstructionResourceCost(logs, stone, planks);
        }
    }
}
