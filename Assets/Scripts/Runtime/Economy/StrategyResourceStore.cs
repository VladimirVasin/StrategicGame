using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ProjectUnknown.Strategy
{
    [Flags]
    public enum StrategyResourceStoreScope
    {
        None = 0,
        Settlement = 1,
        Production = 2,
        Household = 4,
        TemporarySettlement = 8,
        Loose = 16,
        All = Settlement | Production | Household | TemporarySettlement | Loose
    }

    public enum StrategyResourceReservationChannel
    {
        General,
        Construction,
        Logistics,
        ProductionInput,
        Household,
        Trade
    }

    public interface IStrategyResourceStoreOwner
    {
        StrategyResourceStore ResourceStore { get; }
    }

    public interface IStrategyResourceReservationProvider
    {
        int GetReservedResourceAmount(StrategyResourceType resource);
    }

    public sealed class StrategyResourceStore
    {
        private static readonly List<StrategyResourceStore> activeStores = new();
        private static readonly int ResourceCount = Enum.GetValues(typeof(StrategyResourceType)).Length;

        private readonly int[] amounts = new int[ResourceCount];
        private readonly Dictionary<ReservationKey, int> reservations = new();
        private object owner;
        private int capacity;

        public object Owner => owner;
        public StrategyResourceStoreScope Scope { get; private set; }
        public bool AcceptsNewResources { get; private set; } = true;
        public int Capacity => capacity;
        public int Version { get; private set; }

        public int TotalStored
        {
            get
            {
                int total = 0;
                for (int i = 1; i < amounts.Length; i++)
                {
                    total += Math.Max(0, amounts[i]);
                }

                return total;
            }
        }

        public void Bind(
            object storeOwner,
            StrategyResourceStoreScope scope,
            int totalCapacity = 0,
            bool acceptsNewResources = true)
        {
            owner = storeOwner;
            Scope = scope;
            capacity = Math.Max(0, totalCapacity);
            AcceptsNewResources = acceptsNewResources;
            if (!activeStores.Contains(this))
            {
                activeStores.Add(this);
            }
        }

        public ref int GetAmountRef(StrategyResourceType resource)
        {
            int index = NormalizeResourceIndex(resource);
            return ref amounts[index];
        }

        public int GetStored(StrategyResourceType resource)
        {
            int index = NormalizeResourceIndex(resource);
            return index == 0 ? 0 : Math.Max(0, amounts[index]);
        }

        public int GetReserved(StrategyResourceType resource)
        {
            int total = 0;
            foreach (KeyValuePair<ReservationKey, int> pair in reservations)
            {
                if (pair.Key.Resource == resource)
                {
                    total += Math.Max(0, pair.Value);
                }
            }

            if (owner is IStrategyResourceReservationProvider provider)
            {
                total += Math.Max(0, provider.GetReservedResourceAmount(resource));
            }

            return total;
        }

        public int GetAvailable(StrategyResourceType resource)
        {
            return Math.Max(0, GetStored(resource) - GetReserved(resource));
        }

        public int Add(StrategyResourceType resource, int amount)
        {
            if (!AcceptsNewResources || resource == StrategyResourceType.None || amount <= 0)
            {
                return 0;
            }

            int accepted = capacity <= 0
                ? amount
                : Math.Min(amount, Math.Max(0, capacity - TotalStored));
            if (accepted <= 0)
            {
                return 0;
            }

            amounts[(int)resource] += accepted;
            Version++;
            return accepted;
        }

        public int Take(StrategyResourceType resource, int amount)
        {
            if (resource == StrategyResourceType.None || amount <= 0)
            {
                return 0;
            }

            int taken = Math.Min(GetAvailable(resource), amount);
            if (taken > 0)
            {
                amounts[(int)resource] -= taken;
                Version++;
            }

            return taken;
        }

        public int[] CaptureAmounts()
        {
            int[] snapshot = new int[amounts.Length];
            Array.Copy(amounts, snapshot, amounts.Length);
            return snapshot;
        }

        public void RestoreAmounts(IReadOnlyList<int> snapshot)
        {
            Array.Clear(amounts, 0, amounts.Length);
            if (snapshot != null)
            {
                int count = Math.Min(amounts.Length, snapshot.Count);
                for (int i = 1; i < count; i++)
                {
                    amounts[i] = Math.Max(0, snapshot[i]);
                }
            }

            reservations.Clear();
            Version++;
        }

        public bool TryReserve(
            object reservationOwner,
            StrategyResourceType resource,
            int amount,
            StrategyResourceReservationChannel channel,
            out int reservedAmount)
        {
            reservedAmount = 0;
            if (reservationOwner == null || resource == StrategyResourceType.None || amount <= 0)
            {
                return false;
            }

            ReservationKey key = new(reservationOwner, resource, channel);
            if (reservations.TryGetValue(key, out int existing) && existing > 0)
            {
                reservedAmount = existing;
                return true;
            }

            reservedAmount = Math.Min(amount, GetAvailable(resource));
            if (reservedAmount <= 0)
            {
                return false;
            }

            reservations[key] = reservedAmount;
            Version++;
            return true;
        }

        public int TakeReserved(
            object reservationOwner,
            StrategyResourceType resource,
            int maxAmount,
            StrategyResourceReservationChannel channel)
        {
            if (reservationOwner == null || maxAmount <= 0)
            {
                return 0;
            }

            ReservationKey key = new(reservationOwner, resource, channel);
            if (!reservations.TryGetValue(key, out int reserved) || reserved <= 0)
            {
                return 0;
            }

            int taken = Math.Min(Math.Min(reserved, GetStored(resource)), maxAmount);
            int remaining = reserved - taken;
            if (remaining > 0)
            {
                reservations[key] = remaining;
            }
            else
            {
                reservations.Remove(key);
            }

            if (taken > 0)
            {
                amounts[(int)resource] -= taken;
            }

            Version++;
            return taken;
        }

        public void Release(
            object reservationOwner,
            StrategyResourceType resource,
            StrategyResourceReservationChannel channel)
        {
            if (reservationOwner != null
                && reservations.Remove(new ReservationKey(reservationOwner, resource, channel)))
            {
                Version++;
            }
        }

        public void ReleaseAll(object reservationOwner)
        {
            if (reservationOwner == null || reservations.Count == 0)
            {
                return;
            }

            List<ReservationKey> remove = new();
            foreach (ReservationKey key in reservations.Keys)
            {
                if (ReferenceEquals(key.Owner, reservationOwner))
                {
                    remove.Add(key);
                }
            }

            for (int i = 0; i < remove.Count; i++)
            {
                reservations.Remove(remove[i]);
            }

            if (remove.Count > 0)
            {
                Version++;
            }
        }

        public static void CopyActiveStores(List<StrategyResourceStore> target)
        {
            target.Clear();
            for (int i = activeStores.Count - 1; i >= 0; i--)
            {
                StrategyResourceStore store = activeStores[i];
                if (store == null || !IsOwnerAlive(store.owner))
                {
                    activeStores.RemoveAt(i);
                    continue;
                }

                target.Add(store);
            }
        }

        private static bool IsOwnerAlive(object candidate)
        {
            return candidate switch
            {
                null => false,
                UnityEngine.Object unityObject => unityObject != null,
                _ => true
            };
        }

        private static int NormalizeResourceIndex(StrategyResourceType resource)
        {
            int index = (int)resource;
            return index >= 0 && index < ResourceCount ? index : 0;
        }

        private readonly struct ReservationKey : IEquatable<ReservationKey>
        {
            public ReservationKey(
                object owner,
                StrategyResourceType resource,
                StrategyResourceReservationChannel channel)
            {
                Owner = owner;
                Resource = resource;
                Channel = channel;
            }

            public object Owner { get; }
            public StrategyResourceType Resource { get; }
            public StrategyResourceReservationChannel Channel { get; }

            public bool Equals(ReservationKey other)
            {
                return ReferenceEquals(Owner, other.Owner)
                    && Resource == other.Resource
                    && Channel == other.Channel;
            }

            public override bool Equals(object obj)
            {
                return obj is ReservationKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = Owner != null ? RuntimeHelpers.GetHashCode(Owner) : 0;
                    hash = hash * 397 ^ (int)Resource;
                    hash = hash * 397 ^ (int)Channel;
                    return hash;
                }
            }
        }
    }
}
