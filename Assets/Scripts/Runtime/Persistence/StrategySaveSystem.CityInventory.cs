using System;
using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySaveSystem
    {
        private void CaptureCityInventory(StrategySaveData save)
        {
            List<StrategyCityInventoryEntry> entries = new();
            cityInventory.CopyOwnedItems(entries);
            CopyCityInventoryEntriesForSave(entries, save.cityItems);
        }

        internal static void CopyCityInventoryEntriesForSave(
            List<StrategyCityInventoryEntry> source,
            List<StrategyCityItemSaveData> destination)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            source.Sort(CompareCityInventoryEntries);
            destination.Clear();
            for (int i = 0; i < source.Count; i++)
            {
                StrategyCityInventoryEntry entry = source[i];
                destination.Add(new StrategyCityItemSaveData
                {
                    itemId = entry.ItemId,
                    quantity = entry.Quantity
                });
            }
        }

        internal static bool TryRestoreCityInventory(
            StrategyCityInventory inventory,
            IReadOnlyList<StrategyCityItemSaveData> savedItems,
            out StrategyCityInventoryRestoreFailure failure)
        {
            if (inventory == null || savedItems == null)
            {
                failure = StrategyCityInventoryRestoreFailure.MissingEntries;
                return false;
            }

            List<StrategyCityInventoryEntry> entries = new(savedItems.Count);
            for (int i = 0; i < savedItems.Count; i++)
            {
                StrategyCityItemSaveData item = savedItems[i];
                if (item == null)
                {
                    failure = StrategyCityInventoryRestoreFailure.MissingEntries;
                    return false;
                }

                entries.Add(new StrategyCityInventoryEntry(item.itemId, item.quantity));
            }

            return inventory.TryRestore(entries, out failure);
        }

        private static int CompareCityInventoryEntries(
            StrategyCityInventoryEntry left,
            StrategyCityInventoryEntry right)
        {
            return StringComparer.Ordinal.Compare(left.ItemId, right.ItemId);
        }
    }
}
