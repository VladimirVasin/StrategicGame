using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectUnknown.Strategy
{
    public readonly struct StrategyCityInventoryEntry : IEquatable<StrategyCityInventoryEntry>
    {
        public StrategyCityInventoryEntry(string itemId, int quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }

        public string ItemId { get; }
        public int Quantity { get; }

        public bool Equals(StrategyCityInventoryEntry other)
        {
            return string.Equals(ItemId, other.ItemId, StringComparison.Ordinal)
                && Quantity == other.Quantity;
        }

        public override bool Equals(object obj)
        {
            return obj is StrategyCityInventoryEntry other && Equals(other);
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

    public sealed class StrategyCityInventorySnapshot
    {
        private readonly ReadOnlyCollection<StrategyCityInventoryEntry> entries;

        internal StrategyCityInventorySnapshot(
            long version,
            IList<StrategyCityInventoryEntry> sourceEntries)
        {
            Version = version;
            StrategyCityInventoryEntry[] copy = new StrategyCityInventoryEntry[sourceEntries.Count];
            sourceEntries.CopyTo(copy, 0);
            entries = Array.AsReadOnly(copy);
        }

        public long Version { get; }
        public IReadOnlyList<StrategyCityInventoryEntry> Entries => entries;
        public int Count => entries.Count;
    }

    public enum StrategyCityInventoryRestoreFailure
    {
        None,
        MissingEntries,
        InvalidItemId,
        UnknownItem,
        DuplicateItem,
        InvalidQuantity
    }
}
