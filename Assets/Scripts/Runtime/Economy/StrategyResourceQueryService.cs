using System;
using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public sealed class StrategyResourceSnapshot
    {
        private static readonly int ResourceCount = Enum.GetValues(typeof(StrategyResourceType)).Length;

        private readonly int[] storedAmounts = new int[ResourceCount];
        private readonly int[] availableAmounts = new int[ResourceCount];

        public int GetStored(StrategyResourceType resource)
        {
            int index = NormalizeResourceIndex(resource);
            return index == 0 ? 0 : storedAmounts[index];
        }

        public int GetAvailable(StrategyResourceType resource)
        {
            int index = NormalizeResourceIndex(resource);
            return index == 0 ? 0 : availableAmounts[index];
        }

        internal void Clear()
        {
            Array.Clear(storedAmounts, 0, storedAmounts.Length);
            Array.Clear(availableAmounts, 0, availableAmounts.Length);
        }

        internal void Add(StrategyResourceType resource, int stored, int available)
        {
            int index = NormalizeResourceIndex(resource);
            if (index == 0)
            {
                return;
            }

            storedAmounts[index] += Math.Max(0, stored);
            availableAmounts[index] += Math.Max(0, available);
        }

        private static int NormalizeResourceIndex(StrategyResourceType resource)
        {
            int index = (int)resource;
            return index >= 0 && index < ResourceCount ? index : 0;
        }
    }

    public static class StrategyResourceQueryService
    {
        private static readonly List<StrategyResourceStore> stores = new();

        public static void PopulateSnapshot(
            StrategyResourceSnapshot snapshot,
            StrategyResourceStoreScope scopes = StrategyResourceStoreScope.All)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            snapshot.Clear();
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
                    if (type == StrategyResourceType.Dish
                        && store.Owner is StrategyHouseResourceStore house)
                    {
                        int preparedDishes = house.GetPreparedDishAmount();
                        snapshot.Add(type, preparedDishes, preparedDishes);
                        continue;
                    }

                    if (type == StrategyResourceType.Dish
                        && store.Owner is StrategyLooseCarriedResourcePile loosePile
                        && loosePile.HasPreparedDishPayload)
                    {
                        int preparedDishes = loosePile.PreparedDishAmount;
                        int availableDishes = Math.Min(
                            preparedDishes,
                            store.GetAvailable(StrategyResourceType.Dish));
                        snapshot.Add(type, preparedDishes, availableDishes);
                        continue;
                    }

                    snapshot.Add(type, store.GetStored(type), store.GetAvailable(type));
                }
            }
        }

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
