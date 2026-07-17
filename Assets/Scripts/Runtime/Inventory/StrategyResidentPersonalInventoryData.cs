using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectUnknown.Strategy
{
    public readonly struct StrategyResidentPersonalItemEntry : IEquatable<StrategyResidentPersonalItemEntry>
    {
        public StrategyResidentPersonalItemEntry(string itemId, int quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }

        public string ItemId { get; }
        public int Quantity { get; }

        public bool Equals(StrategyResidentPersonalItemEntry other)
        {
            return string.Equals(ItemId, other.ItemId, StringComparison.Ordinal)
                && Quantity == other.Quantity;
        }

        public override bool Equals(object obj)
        {
            return obj is StrategyResidentPersonalItemEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((ItemId != null ? StringComparer.Ordinal.GetHashCode(ItemId) : 0) * 397)
                    ^ Quantity;
            }
        }
    }

    public sealed class StrategyResidentPersonalInventorySnapshot
    {
        private readonly ReadOnlyCollection<StrategyResidentPersonalItemEntry> entries;

        internal StrategyResidentPersonalInventorySnapshot(
            long version,
            IList<StrategyResidentPersonalItemEntry> sourceEntries)
        {
            Version = version;
            StrategyResidentPersonalItemEntry[] copy =
                new StrategyResidentPersonalItemEntry[sourceEntries.Count];
            sourceEntries.CopyTo(copy, 0);
            entries = Array.AsReadOnly(copy);
        }

        public long Version { get; }
        public IReadOnlyList<StrategyResidentPersonalItemEntry> Entries => entries;
        public int Count => entries.Count;
    }

    public enum StrategyResidentPersonalInventoryFailure
    {
        None,
        OwnerIneligible,
        MissingEntries,
        InvalidItemId,
        UnknownItem,
        DuplicateItem,
        InvalidQuantity,
        CapacityExceeded,
        StackExceeded,
        InsufficientQuantity
    }
}
