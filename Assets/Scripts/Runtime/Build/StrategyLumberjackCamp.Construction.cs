using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyLumberjackCamp
    {
        private static readonly List<StrategyLumberjackCamp> constructionCampQuery = new();
        private static Vector3 constructionCampSortWorld;
        private readonly Dictionary<object, int> constructionLogReservations = new();
        private readonly Dictionary<StrategyResidentAgent, ConstructionPickupReservation> constructionPickupReservations = new();

        public int AvailableConstructionLogs => Mathf.Max(0, logsStored - CountReservations(constructionLogReservations));

        private sealed class ConstructionPickupReservation
        {
            public object Owner;
            public int Amount;
        }

        public static int GetTotalAvailableConstructionLogs()
        {
            int total = 0;
            List<StrategyLumberjackCamp> camps = GetCampsSortedByDistance(Vector3.zero);
            for (int i = 0; i < camps.Count; i++)
            {
                if (camps[i] != null)
                {
                    total += camps[i].AvailableConstructionLogs;
                }
            }

            return total;
        }

        public static int ReserveConstructionLogs(object owner, int requested, Vector3 nearWorld)
        {
            if (owner == null || requested <= 0)
            {
                return 0;
            }

            int remaining = requested;
            List<StrategyLumberjackCamp> camps = GetCampsSortedByDistance(nearWorld);
            for (int i = 0; i < camps.Count && remaining > 0; i++)
            {
                remaining -= camps[i] != null ? camps[i].ReserveConstructionLogs(owner, remaining) : 0;
            }

            return requested - remaining;
        }

        public static int SpendAvailableConstructionLogs(int requested, Vector3 nearWorld)
        {
            if (requested <= 0)
            {
                return 0;
            }

            int remaining = requested;
            List<StrategyLumberjackCamp> camps = GetCampsSortedByDistance(nearWorld);
            for (int i = 0; i < camps.Count && remaining > 0; i++)
            {
                remaining -= camps[i] != null ? camps[i].SpendAvailableConstructionLogs(remaining) : 0;
            }

            return requested - remaining;
        }

        public static void ReleaseConstructionReservations(object owner)
        {
            if (owner == null)
            {
                return;
            }

            List<StrategyLumberjackCamp> camps = GetCampsSortedByDistance(Vector3.zero);
            for (int i = 0; i < camps.Count; i++)
            {
                camps[i]?.ReleaseConstructionReservation(owner);
            }
        }

        public static bool TryFindConstructionPickup(
            object owner,
            Vector3 nearWorld,
            int maxAmount,
            out StrategyLumberjackCamp camp,
            out Vector2Int pickupCell,
            out int amount)
        {
            camp = null;
            pickupCell = default;
            amount = 0;
            if (owner == null || maxAmount <= 0)
            {
                return false;
            }

            List<StrategyLumberjackCamp> camps = GetCampsSortedByDistance(nearWorld);
            for (int i = 0; i < camps.Count; i++)
            {
                StrategyLumberjackCamp candidate = camps[i];
                int available = candidate != null ? candidate.GetAvailableConstructionReservationAmount(owner) : 0;
                if (available <= 0 || !candidate.TryFindDropoffCell(out pickupCell))
                {
                    continue;
                }

                camp = candidate;
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
            if (owner == null || builder == null || kind != StrategyConstructionResourceKind.Logs || amount <= 0)
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
                || kind != StrategyConstructionResourceKind.Logs
                || maxAmount <= 0
                || !constructionPickupReservations.TryGetValue(builder, out ConstructionPickupReservation pickup)
                || pickup == null
                || !ReferenceEquals(pickup.Owner, owner)
                || pickup.Amount <= 0)
            {
                return false;
            }

            if (!constructionLogReservations.TryGetValue(owner, out int reserved)
                || reserved <= 0
                || logsStored <= 0)
            {
                constructionPickupReservations.Remove(builder);
                return false;
            }

            amount = Mathf.Min(maxAmount, pickup.Amount, reserved, logsStored);
            logsStored -= amount;
            reserved -= amount;
            pickup.Amount -= amount;
            if (reserved <= 0)
            {
                constructionLogReservations.Remove(owner);
            }
            else
            {
                constructionLogReservations[owner] = reserved;
            }

            if (pickup.Amount <= 0)
            {
                constructionPickupReservations.Remove(builder);
            }

            UpdateStockVisual();
            return amount > 0;
        }

        private int ReserveConstructionLogs(object owner, int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableConstructionLogs);
            if (owner == null || amount <= 0)
            {
                return 0;
            }

            ClearStoredLogsReservationForConstruction();
            AddReservation(constructionLogReservations, owner, amount);
            return amount;
        }

        private int SpendAvailableConstructionLogs(int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableConstructionLogs);
            if (amount <= 0)
            {
                return 0;
            }

            ClearStoredLogsReservationForConstruction();
            logsStored -= amount;
            UpdateStockVisual();
            return amount;
        }

        private int GetAvailableConstructionReservationAmount(object owner)
        {
            if (owner == null
                || !constructionLogReservations.TryGetValue(owner, out int amount)
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
            constructionLogReservations.Remove(owner);
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

        private void ClearStoredLogsReservationForConstruction()
        {
            logsReservationOwner = null;
            reservedLogs = 0;
        }

        private static List<StrategyLumberjackCamp> GetCampsSortedByDistance(Vector3 nearWorld)
        {
            StrategyPlacedBuilding.CopyActiveComponents(constructionCampQuery);
            constructionCampSortWorld = nearWorld;
            constructionCampQuery.Sort(CompareCampsByDistance);
            return constructionCampQuery;
        }

        private static int CompareCampsByDistance(StrategyLumberjackCamp left, StrategyLumberjackCamp right)
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

            float leftDistance = (left.FootprintBounds.center - constructionCampSortWorld).sqrMagnitude;
            float rightDistance = (right.FootprintBounds.center - constructionCampSortWorld).sqrMagnitude;
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
