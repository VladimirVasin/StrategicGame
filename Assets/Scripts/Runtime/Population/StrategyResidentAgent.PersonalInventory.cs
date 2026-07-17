using System;
using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private StrategyResidentPersonalInventory personalInventory;

        public bool CanOwnPersonalItems => IsAdult && !IsPendingRefugee && !deathRequested;
        public int PersonalItemCount => PersonalInventory.DistinctItemCount;
        public long PersonalInventoryVersion => PersonalInventory.Version;
        public StrategyResidentItemCatalog PersonalItemCatalog => PersonalInventory.Catalog;

        private StrategyResidentPersonalInventory PersonalInventory =>
            personalInventory ??= new StrategyResidentPersonalInventory(
                StrategyResidentItemCatalog.Production,
                IsEligiblePersonalItemOwner);

        public int GetPersonalItemQuantity(string itemId)
        {
            return PersonalInventory.GetQuantity(itemId);
        }

        public bool HasPersonalItem(string itemId)
        {
            return PersonalInventory.Contains(itemId);
        }

        public bool TryAddPersonalItem(
            string itemId,
            int quantity,
            out StrategyResidentPersonalInventoryFailure failure)
        {
            return PersonalInventory.TryAddExact(itemId, quantity, out failure);
        }

        public bool TryRemovePersonalItem(
            string itemId,
            int quantity,
            out StrategyResidentPersonalInventoryFailure failure)
        {
            return PersonalInventory.TryRemoveExact(itemId, quantity, out failure);
        }

        public void CopyPersonalItems(List<StrategyResidentPersonalItemEntry> destination)
        {
            PersonalInventory.CopyOwnedItems(destination);
        }

        internal void ConfigurePersonalItemCatalog(StrategyResidentItemCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (personalInventory != null && personalInventory.DistinctItemCount > 0)
            {
                throw new InvalidOperationException(
                    "The personal item catalog cannot change while the inventory contains items.");
            }

            personalInventory = new StrategyResidentPersonalInventory(
                catalog,
                IsEligiblePersonalItemOwner);
        }

        internal bool TryRestorePersonalItems(
            IReadOnlyList<StrategyResidentPersonalItemEntry> entries,
            out StrategyResidentPersonalInventoryFailure failure)
        {
            return PersonalInventory.TryRestore(entries, out failure);
        }

        private bool IsEligiblePersonalItemOwner()
        {
            return CanOwnPersonalItems;
        }
    }
}
