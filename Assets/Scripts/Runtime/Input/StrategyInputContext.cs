using System;
using System.Collections.Generic;
using System.Threading;

namespace ProjectUnknown.Strategy
{
    [Flags]
    public enum StrategyInputChannel
    {
        None = 0,
        Global = 1 << 0,
        Camera = 1 << 1,
        Gameplay = 1 << 2,
        Build = 1 << 3,
        Debug = 1 << 4,
        All = Global | Camera | Gameplay | Build | Debug
    }

    public enum StrategyCancelMode
    {
        None,
        Close,
        Swallow
    }

    public sealed class StrategyInputContextHandle : IDisposable
    {
        private StrategyInputContextState state;
        private readonly long contextId;

        internal StrategyInputContextHandle(
            StrategyInputContextState contextState,
            long id,
            object owner)
        {
            state = contextState;
            contextId = id;
            Owner = owner;
        }

        public object Owner { get; }
        public bool IsDisposed => state == null || !state.Contains(contextId);

        public void Dispose()
        {
            StrategyInputContextState previous = Interlocked.Exchange(ref state, null);
            previous?.Release(contextId);
        }
    }

    internal sealed class StrategyInputContextState
    {
        private readonly List<Entry> entries = new();
        private long nextContextId;
        private StrategyInputChannel blockedChannels;
        private object cancelOwner;
        private StrategyCancelMode cancelMode;
        private object secondaryPointerOwner;

        public int Count => entries.Count;
        public StrategyInputChannel BlockedChannels => blockedChannels;
        public object CancelOwner => cancelOwner;
        public StrategyCancelMode CancelMode => cancelMode;
        public object SecondaryPointerOwner => secondaryPointerOwner;

        public StrategyInputContextHandle Push(
            object owner,
            StrategyInputChannel blocked,
            StrategyCancelMode requestedCancelMode,
            bool ownsSecondaryPointer)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            long id = ++nextContextId;
            entries.Add(new Entry(id, owner, blocked, requestedCancelMode, ownsSecondaryPointer));
            Recalculate();
            return new StrategyInputContextHandle(this, id, owner);
        }

        public bool IsBlocked(StrategyInputChannel channel)
        {
            return channel != StrategyInputChannel.None
                && (blockedChannels & channel) != StrategyInputChannel.None;
        }

        public bool IsTopCancelOwner(object owner)
        {
            return owner != null && ReferenceEquals(cancelOwner, owner);
        }

        public bool IsSecondaryPointerOwnedBy(object owner)
        {
            return owner != null && ReferenceEquals(secondaryPointerOwner, owner);
        }

        public int ReleaseOwner(object owner)
        {
            if (owner == null)
            {
                return 0;
            }

            int released = 0;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(entries[i].Owner, owner))
                {
                    continue;
                }

                entries.RemoveAt(i);
                released++;
            }

            if (released > 0)
            {
                Recalculate();
            }

            return released;
        }

        public void Clear()
        {
            entries.Clear();
            Recalculate();
        }

        internal bool Release(long id)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].Id != id)
                {
                    continue;
                }

                entries.RemoveAt(i);
                Recalculate();
                return true;
            }

            return false;
        }

        internal bool Contains(long id)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        private void Recalculate()
        {
            blockedChannels = StrategyInputChannel.None;
            cancelOwner = null;
            cancelMode = StrategyCancelMode.None;
            secondaryPointerOwner = null;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                blockedChannels |= entry.BlockedChannels;
                if (entry.CancelMode != StrategyCancelMode.None)
                {
                    cancelOwner = entry.Owner;
                    cancelMode = entry.CancelMode;
                }

                if (entry.OwnsSecondaryPointer)
                {
                    secondaryPointerOwner = entry.Owner;
                }
            }
        }

        private readonly struct Entry
        {
            public Entry(
                long id,
                object owner,
                StrategyInputChannel blockedChannels,
                StrategyCancelMode contextCancelMode,
                bool ownsSecondaryPointer)
            {
                Id = id;
                Owner = owner;
                BlockedChannels = blockedChannels;
                CancelMode = contextCancelMode;
                OwnsSecondaryPointer = ownsSecondaryPointer;
            }

            public long Id { get; }
            public object Owner { get; }
            public StrategyInputChannel BlockedChannels { get; }
            public StrategyCancelMode CancelMode { get; }
            public bool OwnsSecondaryPointer { get; }
        }
    }
}
