using System;
using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public sealed class StrategyResidentPersonalInventory
    {
        public const int SlotCapacity = 6;

        private readonly Dictionary<string, int> quantities =
            new(StringComparer.Ordinal);
        private readonly Func<bool> ownerEligibility;
        private readonly StrategyResidentItemCatalog catalog;

        public StrategyResidentPersonalInventory(
            StrategyResidentItemCatalog itemCatalog,
            Func<bool> canOwnerHoldItems = null)
        {
            catalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));
            ownerEligibility = canOwnerHoldItems ?? AlwaysEligible;
        }

        public event Action Changed;

        public StrategyResidentItemCatalog Catalog => catalog;
        public long Version { get; private set; }
        public int DistinctItemCount => quantities.Count;
        public int FreeSlotCount => SlotCapacity - quantities.Count;
        public bool IsOwnerEligible => ownerEligibility();

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

        public bool TryAddExact(string itemId, int quantity)
        {
            return TryAddExact(itemId, quantity, out _);
        }

        public bool CanAddExact(
            string itemId,
            int quantity,
            out StrategyResidentPersonalInventoryFailure failure)
        {
            if (quantity <= 0)
            {
                failure = StrategyResidentPersonalInventoryFailure.InvalidQuantity;
                return false;
            }

            if (!catalog.TryGet(itemId, out StrategyResidentItemDefinition definition))
            {
                failure = StrategyResidentPersonalInventoryFailure.UnknownItem;
                return false;
            }

            if (!IsOwnerEligible)
            {
                failure = StrategyResidentPersonalInventoryFailure.OwnerIneligible;
                return false;
            }

            int currentQuantity = GetQuantity(itemId);
            if (currentQuantity == 0 && quantities.Count >= SlotCapacity)
            {
                failure = StrategyResidentPersonalInventoryFailure.CapacityExceeded;
                return false;
            }

            if (quantity > definition.MaxStack - currentQuantity)
            {
                failure = StrategyResidentPersonalInventoryFailure.StackExceeded;
                return false;
            }

            failure = StrategyResidentPersonalInventoryFailure.None;
            return true;
        }

        public bool TryAddExact(
            string itemId,
            int quantity,
            out StrategyResidentPersonalInventoryFailure failure)
        {
            if (!CanAddExact(itemId, quantity, out failure))
            {
                return false;
            }

            int currentQuantity = GetQuantity(itemId);
            quantities[itemId] = currentQuantity + quantity;
            NotifyChanged();
            failure = StrategyResidentPersonalInventoryFailure.None;
            return true;
        }

        public bool TryRemoveExact(string itemId, int quantity)
        {
            return TryRemoveExact(itemId, quantity, out _);
        }

        public bool TryRemoveExact(
            string itemId,
            int quantity,
            out StrategyResidentPersonalInventoryFailure failure)
        {
            if (quantity <= 0)
            {
                failure = StrategyResidentPersonalInventoryFailure.InvalidQuantity;
                return false;
            }

            if (!IsOwnerEligible)
            {
                failure = StrategyResidentPersonalInventoryFailure.OwnerIneligible;
                return false;
            }

            if (itemId == null
                || !quantities.TryGetValue(itemId, out int currentQuantity)
                || currentQuantity < quantity)
            {
                failure = StrategyResidentPersonalInventoryFailure.InsufficientQuantity;
                return false;
            }

            int remainingQuantity = currentQuantity - quantity;
            if (remainingQuantity > 0)
            {
                quantities[itemId] = remainingQuantity;
            }
            else
            {
                quantities.Remove(itemId);
            }

            NotifyChanged();
            failure = StrategyResidentPersonalInventoryFailure.None;
            return true;
        }

        public StrategyResidentPersonalInventorySnapshot CaptureSnapshot()
        {
            List<StrategyResidentPersonalItemEntry> entries = new(quantities.Count);
            AppendOwnedItems(entries);
            return new StrategyResidentPersonalInventorySnapshot(Version, entries);
        }

        public void CopyOwnedItems(List<StrategyResidentPersonalItemEntry> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            AppendOwnedItems(destination);
        }

        public bool TryRestore(
            IReadOnlyList<StrategyResidentPersonalItemEntry> entries,
            out StrategyResidentPersonalInventoryFailure failure)
        {
            if (!TryBuildRestoredState(entries, out Dictionary<string, int> restored, out failure))
            {
                return false;
            }

            if (restored.Count > 0 && !IsOwnerEligible)
            {
                failure = StrategyResidentPersonalInventoryFailure.OwnerIneligible;
                return false;
            }

            if (StatesMatch(restored))
            {
                failure = StrategyResidentPersonalInventoryFailure.None;
                return true;
            }

            quantities.Clear();
            foreach (KeyValuePair<string, int> pair in restored)
            {
                quantities.Add(pair.Key, pair.Value);
            }

            NotifyChanged();
            failure = StrategyResidentPersonalInventoryFailure.None;
            return true;
        }

        private static bool AlwaysEligible()
        {
            return true;
        }

        private void AppendOwnedItems(List<StrategyResidentPersonalItemEntry> destination)
        {
            IReadOnlyList<StrategyResidentItemDefinition> definitions = catalog.Definitions;
            for (int index = 0; index < definitions.Count; index++)
            {
                string itemId = definitions[index].Id;
                if (quantities.TryGetValue(itemId, out int quantity))
                {
                    destination.Add(new StrategyResidentPersonalItemEntry(itemId, quantity));
                }
            }
        }

        private bool TryBuildRestoredState(
            IReadOnlyList<StrategyResidentPersonalItemEntry> entries,
            out Dictionary<string, int> restored,
            out StrategyResidentPersonalInventoryFailure failure)
        {
            restored = null;
            if (entries == null)
            {
                failure = StrategyResidentPersonalInventoryFailure.MissingEntries;
                return false;
            }

            if (entries.Count > SlotCapacity)
            {
                failure = StrategyResidentPersonalInventoryFailure.CapacityExceeded;
                return false;
            }

            Dictionary<string, int> candidate = new(StringComparer.Ordinal);
            for (int index = 0; index < entries.Count; index++)
            {
                StrategyResidentPersonalItemEntry entry = entries[index];
                if (!StrategyResidentItemDefinition.IsValidId(entry.ItemId))
                {
                    failure = StrategyResidentPersonalInventoryFailure.InvalidItemId;
                    return false;
                }

                if (!catalog.TryGet(entry.ItemId, out StrategyResidentItemDefinition definition))
                {
                    failure = StrategyResidentPersonalInventoryFailure.UnknownItem;
                    return false;
                }

                if (entry.Quantity <= 0)
                {
                    failure = StrategyResidentPersonalInventoryFailure.InvalidQuantity;
                    return false;
                }

                if (entry.Quantity > definition.MaxStack)
                {
                    failure = StrategyResidentPersonalInventoryFailure.StackExceeded;
                    return false;
                }

                if (!candidate.TryAdd(entry.ItemId, entry.Quantity))
                {
                    failure = StrategyResidentPersonalInventoryFailure.DuplicateItem;
                    return false;
                }
            }

            restored = candidate;
            failure = StrategyResidentPersonalInventoryFailure.None;
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
