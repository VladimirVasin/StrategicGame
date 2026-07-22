using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyBattlePhase
    {
        Peaceful = 0,
        Active = 1,
        Securing = 2
    }

    public sealed class StrategyBattleThreatLease : IDisposable
    {
        private StrategyBattleLifecycleController controller;

        internal StrategyBattleThreatLease(
            StrategyBattleLifecycleController controller,
            object owner,
            string reason)
        {
            this.controller = controller;
            Owner = owner;
            Reason = reason;
        }

        public object Owner { get; private set; }
        public string Reason { get; }
        public bool IsReleased { get; private set; }

        public void Dispose()
        {
            if (IsReleased)
            {
                return;
            }

            StrategyBattleLifecycleController currentController = controller;
            if (currentController == null)
            {
                Invalidate();
                return;
            }

            currentController.ReleaseThreat(this);
        }

        internal void Invalidate()
        {
            controller = null;
            Owner = null;
            IsReleased = true;
        }
    }

    [DisallowMultipleComponent]
    public sealed class StrategyBattleLifecycleController : MonoBehaviour
    {
        public const float SecuringDurationSeconds = 2.5f;

        private readonly HashSet<StrategyBattleThreatLease> activeThreats = new();
        private bool isCleaningUp;

        public StrategyBattlePhase Phase { get; private set; } = StrategyBattlePhase.Peaceful;
        public int ActiveThreatCount => activeThreats.Count;
        public bool HasActiveThreats => activeThreats.Count > 0;
        public bool IsBattleInProgress => Phase != StrategyBattlePhase.Peaceful;
        public float SecuringSecondsRemaining { get; private set; }

        public event Action<StrategyBattlePhase, StrategyBattlePhase> PhaseChanged;

        public StrategyBattleThreatLease RegisterThreat(object owner, string reason)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (!isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Battle threats cannot be registered while the lifecycle controller is disabled.");
            }

            string normalizedReason = string.IsNullOrWhiteSpace(reason)
                ? "Unspecified threat"
                : reason.Trim();
            StrategyBattleThreatLease lease = new(this, owner, normalizedReason);
            activeThreats.Add(lease);
            SecuringSecondsRemaining = 0f;
            TransitionTo(StrategyBattlePhase.Active);
            return lease;
        }

        public void Advance(float scaledDeltaTime)
        {
            if (Phase != StrategyBattlePhase.Securing
                || scaledDeltaTime <= 0f
                || float.IsNaN(scaledDeltaTime))
            {
                return;
            }

            SecuringSecondsRemaining = Mathf.Max(
                0f,
                SecuringSecondsRemaining - scaledDeltaTime);
            if (SecuringSecondsRemaining <= 0f)
            {
                TransitionTo(StrategyBattlePhase.Peaceful);
            }
        }

        internal void ReleaseThreat(StrategyBattleThreatLease lease)
        {
            if (lease == null || lease.IsReleased)
            {
                return;
            }

            if (!activeThreats.Remove(lease))
            {
                lease.Invalidate();
                return;
            }

            lease.Invalidate();
            if (activeThreats.Count == 0 && Phase == StrategyBattlePhase.Active)
            {
                SecuringSecondsRemaining = SecuringDurationSeconds;
                TransitionTo(StrategyBattlePhase.Securing);
            }
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        private void OnDestroy()
        {
            CleanupForShutdown();
        }

        private void TransitionTo(StrategyBattlePhase nextPhase)
        {
            if (Phase == nextPhase)
            {
                return;
            }

            StrategyBattlePhase previousPhase = Phase;
            Phase = nextPhase;
            if (nextPhase == StrategyBattlePhase.Peaceful)
            {
                SecuringSecondsRemaining = 0f;
            }

            PhaseChanged?.Invoke(previousPhase, nextPhase);
        }

        internal void CleanupForShutdown()
        {
            if (isCleaningUp)
            {
                return;
            }

            isCleaningUp = true;
            foreach (StrategyBattleThreatLease lease in activeThreats)
            {
                lease.Invalidate();
            }

            activeThreats.Clear();
            Phase = StrategyBattlePhase.Peaceful;
            SecuringSecondsRemaining = 0f;
            isCleaningUp = false;
        }
    }
}
