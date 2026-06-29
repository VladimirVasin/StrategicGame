using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySawmill
    {
        private static readonly List<StrategySawmill> constructionSawmillQuery = new();
        private static Vector3 constructionSawmillSortWorld;
        private readonly Dictionary<object, int> constructionPlankReservations = new();
        private readonly Dictionary<StrategyResidentAgent, ConstructionPickupReservation> constructionPickupReservations = new();

        public int AvailableConstructionPlanks => Mathf.Max(0, planksStored - CountReservations(constructionPlankReservations));

        private sealed class ConstructionPickupReservation
        {
            public object Owner;
            public int Amount;
        }

        public static int GetTotalAvailableConstructionPlanks()
        {
            int total = 0;
            List<StrategySawmill> sawmills = GetSawmillsSortedByDistance(Vector3.zero);
            for (int i = 0; i < sawmills.Count; i++)
            {
                if (sawmills[i] != null)
                {
                    total += sawmills[i].AvailableConstructionPlanks;
                }
            }

            return total;
        }

        public static int ReserveConstructionPlanks(object owner, int requested, Vector3 nearWorld)
        {
            if (owner == null || requested <= 0)
            {
                return 0;
            }

            int remaining = requested;
            List<StrategySawmill> sawmills = GetSawmillsSortedByDistance(nearWorld);
            for (int i = 0; i < sawmills.Count && remaining > 0; i++)
            {
                remaining -= sawmills[i] != null ? sawmills[i].ReserveConstructionPlanks(owner, remaining) : 0;
            }

            return requested - remaining;
        }

        public static int SpendAvailableConstructionPlanks(int requested, Vector3 nearWorld)
        {
            if (requested <= 0)
            {
                return 0;
            }

            int remaining = requested;
            List<StrategySawmill> sawmills = GetSawmillsSortedByDistance(nearWorld);
            for (int i = 0; i < sawmills.Count && remaining > 0; i++)
            {
                remaining -= sawmills[i] != null ? sawmills[i].SpendAvailableConstructionPlanks(remaining) : 0;
            }

            return requested - remaining;
        }

        public static void ReleaseConstructionReservations(object owner)
        {
            if (owner == null)
            {
                return;
            }

            List<StrategySawmill> sawmills = GetSawmillsSortedByDistance(Vector3.zero);
            for (int i = 0; i < sawmills.Count; i++)
            {
                sawmills[i]?.ReleaseConstructionReservation(owner);
            }
        }

        public static bool TryFindConstructionPickup(
            object owner,
            Vector3 nearWorld,
            int maxAmount,
            out StrategySawmill sawmill,
            out Vector2Int pickupCell,
            out int amount)
        {
            sawmill = null;
            pickupCell = default;
            amount = 0;
            if (owner == null || maxAmount <= 0)
            {
                return false;
            }

            List<StrategySawmill> sawmills = GetSawmillsSortedByDistance(nearWorld);
            for (int i = 0; i < sawmills.Count; i++)
            {
                StrategySawmill candidate = sawmills[i];
                int available = candidate != null ? candidate.GetAvailableConstructionReservationAmount(owner) : 0;
                if (available <= 0 || !candidate.TryFindDropoffCell(out pickupCell))
                {
                    continue;
                }

                sawmill = candidate;
                amount = Mathf.Min(maxAmount, available);
                return true;
            }

            return false;
        }

        public bool TryReserveConstructionPickup(
            object owner,
            StrategyResidentAgent builder,
            StrategyConstructionResourceKind kind,
            int amount)
        {
            if (owner == null || builder == null || kind != StrategyConstructionResourceKind.Planks || amount <= 0)
            {
                return false;
            }

            ReleaseConstructionPickupReservation(builder);
            if (GetAvailableConstructionReservationAmount(owner) < amount)
            {
                return false;
            }

            constructionPickupReservations[builder] = new ConstructionPickupReservation
            {
                Owner = owner,
                Amount = amount
            };
            return true;
        }

        public void ReleaseConstructionPickupReservation(StrategyResidentAgent builder)
        {
            if (builder != null)
            {
                constructionPickupReservations.Remove(builder);
            }
        }

        public bool TryTakeReservedConstructionResource(
            object owner,
            StrategyResidentAgent builder,
            StrategyConstructionResourceKind kind,
            int maxAmount,
            out int amount)
        {
            amount = 0;
            if (owner == null
                || builder == null
                || kind != StrategyConstructionResourceKind.Planks
                || maxAmount <= 0
                || !constructionPickupReservations.TryGetValue(builder, out ConstructionPickupReservation pickup)
                || pickup == null
                || !ReferenceEquals(pickup.Owner, owner)
                || pickup.Amount <= 0)
            {
                return false;
            }

            if (!constructionPlankReservations.TryGetValue(owner, out int reserved)
                || reserved <= 0
                || planksStored <= 0)
            {
                constructionPickupReservations.Remove(builder);
                return false;
            }

            amount = Mathf.Min(maxAmount, pickup.Amount, reserved, planksStored);
            planksStored -= amount;
            reserved -= amount;
            pickup.Amount -= amount;
            if (reserved <= 0)
            {
                constructionPlankReservations.Remove(owner);
            }
            else
            {
                constructionPlankReservations[owner] = reserved;
            }

            if (pickup.Amount <= 0)
            {
                constructionPickupReservations.Remove(builder);
            }

            UpdateStockVisual();
            return amount > 0;
        }

        private int ReserveConstructionPlanks(object owner, int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableConstructionPlanks);
            if (owner == null || amount <= 0)
            {
                return 0;
            }

            ClearStoredPlanksReservationForConstruction();
            AddReservation(constructionPlankReservations, owner, amount);
            return amount;
        }

        private int SpendAvailableConstructionPlanks(int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableConstructionPlanks);
            if (amount <= 0)
            {
                return 0;
            }

            ClearStoredPlanksReservationForConstruction();
            planksStored -= amount;
            UpdateStockVisual();
            return amount;
        }

        private int GetAvailableConstructionReservationAmount(object owner)
        {
            if (owner == null
                || !constructionPlankReservations.TryGetValue(owner, out int amount)
                || amount <= 0)
            {
                return 0;
            }

            return Mathf.Max(0, amount - CountPickupReservations(owner));
        }

        private int CountPickupReservations(object owner)
        {
            int total = 0;
            foreach (KeyValuePair<StrategyResidentAgent, ConstructionPickupReservation> pair in constructionPickupReservations)
            {
                if (pair.Key != null
                    && pair.Value != null
                    && ReferenceEquals(pair.Value.Owner, owner)
                    && pair.Value.Amount > 0)
                {
                    total += pair.Value.Amount;
                }
            }

            return total;
        }

        private void ReleaseConstructionReservation(object owner)
        {
            constructionPlankReservations.Remove(owner);
            List<StrategyResidentAgent> buildersToRelease = new();
            foreach (KeyValuePair<StrategyResidentAgent, ConstructionPickupReservation> pair in constructionPickupReservations)
            {
                if (pair.Value != null && ReferenceEquals(pair.Value.Owner, owner))
                {
                    buildersToRelease.Add(pair.Key);
                }
            }

            for (int i = 0; i < buildersToRelease.Count; i++)
            {
                constructionPickupReservations.Remove(buildersToRelease[i]);
            }
        }

        private void ClearStoredPlanksReservationForConstruction()
        {
            planksReservationOwner = null;
            reservedPlanks = 0;
        }

        private static List<StrategySawmill> GetSawmillsSortedByDistance(Vector3 nearWorld)
        {
            StrategyPlacedBuilding.CopyActiveComponents(constructionSawmillQuery);
            constructionSawmillSortWorld = nearWorld;
            constructionSawmillQuery.Sort(CompareSawmillsByDistance);
            return constructionSawmillQuery;
        }

        private static int CompareSawmillsByDistance(StrategySawmill left, StrategySawmill right)
        {
            if (left == right)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            float leftDistance = (left.FootprintBounds.center - constructionSawmillSortWorld).sqrMagnitude;
            float rightDistance = (right.FootprintBounds.center - constructionSawmillSortWorld).sqrMagnitude;
            return leftDistance.CompareTo(rightDistance);
        }

        private static void AddReservation(Dictionary<object, int> reservations, object owner, int amount)
        {
            if (reservations.TryGetValue(owner, out int current))
            {
                reservations[owner] = current + amount;
            }
            else
            {
                reservations.Add(owner, amount);
            }
        }

        private static int CountReservations(Dictionary<object, int> reservations)
        {
            int total = 0;
            foreach (KeyValuePair<object, int> pair in reservations)
            {
                if (pair.Key != null && pair.Value > 0)
                {
                    total += pair.Value;
                }
            }

            return total;
        }
    }
}
