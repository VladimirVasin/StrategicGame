using System;
using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySaveSystem
    {
        private static bool ValidateResidentPersonalItems(
            StrategyResidentSaveData resident,
            int residentIndex,
            out string reason)
        {
            if (resident.personalItems == null)
            {
                reason = "missing_resident_personal_items_" + residentIndex;
                return false;
            }

            if (resident.personalItems.Count > MaxSaveResidentItemsPerResident)
            {
                reason = "resident_personal_item_limit_exceeded_" + residentIndex;
                return false;
            }

            if (resident.lifeStage != (int)StrategyResidentLifeStage.Adult
                && resident.personalItems.Count > 0)
            {
                reason = "ineligible_resident_personal_items_" + residentIndex;
                return false;
            }

            HashSet<string> itemIds = new(StringComparer.Ordinal);
            for (int itemIndex = 0; itemIndex < resident.personalItems.Count; itemIndex++)
            {
                StrategyResidentItemSaveData item = resident.personalItems[itemIndex];
                if (item == null)
                {
                    reason = "null_resident_personal_item_"
                        + residentIndex
                        + "_"
                        + itemIndex;
                    return false;
                }

                if (!StrategyResidentItemDefinition.IsValidId(item.itemId))
                {
                    reason = "invalid_resident_personal_item_id_"
                        + residentIndex
                        + "_"
                        + itemIndex;
                    return false;
                }

                if (!itemIds.Add(item.itemId))
                {
                    reason = "duplicate_resident_personal_item_id_"
                        + residentIndex
                        + "_"
                        + itemIndex;
                    return false;
                }

                if (item.quantity <= 0 || item.quantity > MaxSaveResidentItemQuantity)
                {
                    reason = "invalid_resident_personal_item_quantity_"
                        + residentIndex
                        + "_"
                        + itemIndex;
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        internal static bool ValidateResidentItemsAgainstCatalog(
            IReadOnlyList<StrategyResidentSaveData> residents,
            StrategyResidentItemCatalog catalog,
            out string reason)
        {
            if (catalog == null)
            {
                reason = "missing_resident_item_catalog";
                return false;
            }

            for (int residentIndex = 0; residentIndex < residents.Count; residentIndex++)
            {
                IReadOnlyList<StrategyResidentItemSaveData> items = residents[residentIndex].personalItems;
                for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
                {
                    StrategyResidentItemSaveData item = items[itemIndex];
                    if (!catalog.TryGet(item.itemId, out StrategyResidentItemDefinition definition))
                    {
                        reason = "unknown_resident_personal_item_"
                            + residentIndex
                            + "_"
                            + itemIndex;
                        return false;
                    }

                    if (item.quantity > definition.MaxStack)
                    {
                        reason = "resident_personal_item_stack_exceeded_"
                            + residentIndex
                            + "_"
                            + itemIndex;
                        return false;
                    }
                }
            }

            reason = string.Empty;
            return true;
        }
    }
}
