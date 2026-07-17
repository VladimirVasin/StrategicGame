using System;
using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySaveSystem
    {
        internal static void CopyResidentPersonalInventoryEntriesForSave(
            List<StrategyResidentPersonalItemEntry> source,
            List<StrategyResidentItemSaveData> destination)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            source.Sort(CompareResidentPersonalItemEntries);
            destination.Clear();
            for (int i = 0; i < source.Count; i++)
            {
                StrategyResidentPersonalItemEntry entry = source[i];
                destination.Add(new StrategyResidentItemSaveData
                {
                    itemId = entry.ItemId,
                    quantity = entry.Quantity
                });
            }
        }

        internal static bool TryRestoreResidentPersonalInventory(
            StrategyResidentAgent resident,
            IReadOnlyList<StrategyResidentItemSaveData> savedItems,
            out StrategyResidentPersonalInventoryFailure failure)
        {
            if (resident == null || savedItems == null)
            {
                failure = StrategyResidentPersonalInventoryFailure.MissingEntries;
                return false;
            }

            List<StrategyResidentPersonalItemEntry> entries = new(savedItems.Count);
            for (int i = 0; i < savedItems.Count; i++)
            {
                StrategyResidentItemSaveData item = savedItems[i];
                if (item == null)
                {
                    failure = StrategyResidentPersonalInventoryFailure.MissingEntries;
                    return false;
                }

                entries.Add(new StrategyResidentPersonalItemEntry(item.itemId, item.quantity));
            }

            return resident.TryRestorePersonalItems(entries, out failure);
        }

        internal static int CountResidentPersonalItems(
            IReadOnlyList<StrategyResidentSaveData> residents)
        {
            int count = 0;
            for (int i = 0; i < residents.Count; i++)
            {
                count += residents[i]?.personalItems?.Count ?? 0;
            }

            return count;
        }

        private static int CompareResidentPersonalItemEntries(
            StrategyResidentPersonalItemEntry left,
            StrategyResidentPersonalItemEntry right)
        {
            return StringComparer.Ordinal.Compare(left.ItemId, right.ItemId);
        }
    }
}
