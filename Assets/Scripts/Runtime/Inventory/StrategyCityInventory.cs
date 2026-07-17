using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyCityInventory : MonoBehaviour
    {
        private readonly Dictionary<string, int> quantities =
            new(StringComparer.Ordinal);

        private StrategyCityItemCatalog catalog = StrategyCityItemCatalog.Production;

        public event Action Changed;

        public StrategyCityItemCatalog Catalog => catalog;
        public long Version { get; private set; }
        public int DistinctItemCount => quantities.Count;

        public long TotalQuantity
        {
            get
            {
                long total = 0;
                foreach (int quantity in quantities.Values)
                {
                    total += quantity;
                }

                return total;
            }
        }

        public void Configure(StrategyCityItemCatalog itemCatalog)
        {
            if (itemCatalog == null)
            {
                throw new ArgumentNullException(nameof(itemCatalog));
            }

            if (ReferenceEquals(catalog, itemCatalog))
            {
                return;
            }

            if (quantities.Count > 0)
            {
                throw new InvalidOperationException("The catalog cannot change while the inventory contains items.");
            }

            catalog = itemCatalog;
        }

        public int GetQuantity(string itemId)
        {
            return itemId != null && quantities.TryGetValue(itemId, out int quantity)
                ? quantity
                : 0;
        }

        public bool Contains(string itemId)
        {
            return GetQuantity(itemId) > 0;
        }

        public bool TryAdd(string itemId, int requestedQuantity)
        {
            return TryAdd(itemId, requestedQuantity, out _);
        }

        public bool TryAdd(string itemId, int requestedQuantity, out int addedQuantity)
        {
            addedQuantity = 0;
            if (requestedQuantity <= 0
                || !catalog.TryGet(itemId, out StrategyCityItemDefinition definition))
            {
                return false;
            }

            int currentQuantity = GetQuantity(itemId);
            addedQuantity = Math.Min(requestedQuantity, definition.MaxStack - currentQuantity);
            if (addedQuantity <= 0)
            {
                addedQuantity = 0;
                return false;
            }

            quantities[itemId] = currentQuantity + addedQuantity;
            NotifyChanged();
            return true;
        }

        public bool TryRemove(string itemId, int requestedQuantity)
        {
            return TryRemove(itemId, requestedQuantity, out _);
        }

        public bool TryRemove(string itemId, int requestedQuantity, out int removedQuantity)
        {
            removedQuantity = 0;
            if (requestedQuantity <= 0
                || itemId == null
                || !quantities.TryGetValue(itemId, out int currentQuantity))
            {
                return false;
            }

            removedQuantity = Math.Min(requestedQuantity, currentQuantity);
            int remainingQuantity = currentQuantity - removedQuantity;
            if (remainingQuantity > 0)
            {
                quantities[itemId] = remainingQuantity;
            }
            else
            {
                quantities.Remove(itemId);
            }

            NotifyChanged();
            return true;
        }

        public StrategyCityInventorySnapshot CaptureSnapshot()
        {
            List<StrategyCityInventoryEntry> entries = new(quantities.Count);
            AppendOwnedItems(entries);
            return new StrategyCityInventorySnapshot(Version, entries);
        }

        public void CopyOwnedItems(List<StrategyCityInventoryEntry> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            AppendOwnedItems(destination);
        }

        public bool Restore(IReadOnlyList<StrategyCityInventoryEntry> entries)
        {
            return TryRestore(entries, out _);
        }

        public bool TryRestore(
            IReadOnlyList<StrategyCityInventoryEntry> entries,
            out StrategyCityInventoryRestoreFailure failure)
        {
            if (!TryBuildRestoredState(entries, out Dictionary<string, int> restored, out failure))
            {
                return false;
            }

            if (StatesMatch(restored))
            {
                return true;
            }

            quantities.Clear();
            foreach (KeyValuePair<string, int> pair in restored)
            {
                quantities.Add(pair.Key, pair.Value);
            }

            NotifyChanged();
            return true;
        }

        private void AppendOwnedItems(List<StrategyCityInventoryEntry> destination)
        {
            IReadOnlyList<StrategyCityItemDefinition> definitions = catalog.Definitions;
            for (int index = 0; index < definitions.Count; index++)
            {
                string itemId = definitions[index].Id;
                if (quantities.TryGetValue(itemId, out int quantity))
                {
                    destination.Add(new StrategyCityInventoryEntry(itemId, quantity));
                }
            }
        }

        private bool TryBuildRestoredState(
            IReadOnlyList<StrategyCityInventoryEntry> entries,
            out Dictionary<string, int> restored,
            out StrategyCityInventoryRestoreFailure failure)
        {
            restored = null;
            if (entries == null)
            {
                failure = StrategyCityInventoryRestoreFailure.MissingEntries;
                return false;
            }

            Dictionary<string, int> candidate = new(StringComparer.Ordinal);
            for (int index = 0; index < entries.Count; index++)
            {
                StrategyCityInventoryEntry entry = entries[index];
                if (!StrategyCityItemDefinition.IsValidId(entry.ItemId))
                {
                    failure = StrategyCityInventoryRestoreFailure.InvalidItemId;
                    return false;
                }

                if (!catalog.TryGet(entry.ItemId, out StrategyCityItemDefinition definition))
                {
                    failure = StrategyCityInventoryRestoreFailure.UnknownItem;
                    return false;
                }

                if (entry.Quantity <= 0 || entry.Quantity > definition.MaxStack)
                {
                    failure = StrategyCityInventoryRestoreFailure.InvalidQuantity;
                    return false;
                }

                if (!candidate.TryAdd(entry.ItemId, entry.Quantity))
                {
                    failure = StrategyCityInventoryRestoreFailure.DuplicateItem;
                    return false;
                }
            }

            restored = candidate;
            failure = StrategyCityInventoryRestoreFailure.None;
            return true;
        }

        private bool StatesMatch(IReadOnlyDictionary<string, int> restored)
        {
            if (quantities.Count != restored.Count)
            {
                return false;
            }

            foreach (KeyValuePair<string, int> pair in restored)
            {
                if (!quantities.TryGetValue(pair.Key, out int quantity) || quantity != pair.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private void NotifyChanged()
        {
            Version++;
            Changed?.Invoke();
        }
    }
}
