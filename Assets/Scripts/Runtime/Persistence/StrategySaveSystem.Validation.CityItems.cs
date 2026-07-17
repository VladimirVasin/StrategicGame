using System;
using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySaveSystem
    {
        private static bool ValidateCityItems(
            IReadOnlyList<StrategyCityItemSaveData> items,
            out string reason)
        {
            HashSet<string> itemIds = new(StringComparer.Ordinal);
            for (int i = 0; i < items.Count; i++)
            {
                StrategyCityItemSaveData item = items[i];
                if (item == null)
                {
                    reason = "null_city_item_" + i;
                    return false;
                }

                if (!StrategyCityItemDefinition.IsValidId(item.itemId))
                {
                    reason = "invalid_city_item_id_" + i;
                    return false;
                }

                if (!itemIds.Add(item.itemId))
                {
                    reason = "duplicate_city_item_id_" + i;
                    return false;
                }

                if (item.quantity <= 0 || item.quantity > MaxSaveCityItemQuantity)
                {
                    reason = "invalid_city_item_quantity_" + i;
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        internal static bool ValidateCityItemsAgainstCatalog(
            IReadOnlyList<StrategyCityItemSaveData> items,
            StrategyCityItemCatalog catalog,
            out string reason)
        {
            if (catalog == null)
            {
                reason = "missing_city_item_catalog";
                return false;
            }

            for (int i = 0; i < items.Count; i++)
            {
                StrategyCityItemSaveData item = items[i];
                if (!catalog.TryGet(item.itemId, out StrategyCityItemDefinition definition))
                {
                    reason = "unknown_city_item_" + i;
                    return false;
                }

                if (item.quantity > definition.MaxStack)
                {
                    reason = "city_item_stack_exceeded_" + i;
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }
    }
}
