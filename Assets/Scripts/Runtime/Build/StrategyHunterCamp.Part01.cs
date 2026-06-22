using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyHunterCamp
    {
        private readonly Dictionary<IStrategyHuntTarget, float> rejectedHuntTargetUntil = new();
        private readonly List<IStrategyHuntTarget> rejectedHuntTargetCleanup = new();

        public bool CanHuntDeer => StrategyProductionBuildingUpgradeCatalog.HasInstalledUpgrade(
            this,
            StrategyProductionBuildingUpgradeType.DeerHuntingKit);

        public bool TryReserveHuntTarget(object owner, out IStrategyHuntTarget target)
        {
            target = null;
            if (wildlife == null)
            {
                wildlife = StrategyWildlifeController.Active;
            }

            PruneRejectedRabbitTargets();
            PruneRejectedHuntTargets();
            return HasStorageSpace
                && wildlife != null
                && wildlife.TryReserveHuntTarget(Origin, WorkRadius, CanHuntDeer, owner, out target, IsHuntTargetAllowed);
        }

        public void RegisterRejectedHuntTarget(IStrategyHuntTarget target, Vector2Int cell, string reason)
        {
            if (target is StrategyRabbitAgent rabbit)
            {
                RegisterRejectedRabbitTarget(rabbit, cell, reason);
                return;
            }

            if (target == null)
            {
                return;
            }

            rejectedHuntTargetUntil[target] = Time.time + RejectedRabbitTargetCooldownSeconds;
            if (Time.time < nextRejectedRabbitTargetLogTime)
            {
                return;
            }

            nextRejectedRabbitTargetLogTime = Time.time + RejectedRabbitTargetLogCooldownSeconds;
            StrategyDebugLogger.Info(
                "HunterCamp",
                "HuntTargetTemporarilyRejected",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("kind", target.HuntTargetKind),
                StrategyDebugLogger.F("targetCell", cell),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("cooldownSeconds", RejectedRabbitTargetCooldownSeconds));
        }

        private bool IsHuntTargetAllowed(IStrategyHuntTarget target)
        {
            if (target is StrategyRabbitAgent rabbit)
            {
                return IsRabbitTargetAllowed(rabbit);
            }

            float now = Time.time;
            return target != null
                && (!rejectedHuntTargetUntil.TryGetValue(target, out float rejectedUntil) || rejectedUntil <= now);
        }

        private void PruneRejectedHuntTargets()
        {
            float now = Time.time;
            rejectedHuntTargetCleanup.Clear();
            foreach (KeyValuePair<IStrategyHuntTarget, float> pair in rejectedHuntTargetUntil)
            {
                if (pair.Key == null || pair.Value <= now)
                {
                    rejectedHuntTargetCleanup.Add(pair.Key);
                }
            }

            for (int i = 0; i < rejectedHuntTargetCleanup.Count; i++)
            {
                rejectedHuntTargetUntil.Remove(rejectedHuntTargetCleanup[i]);
            }
        }
    }
}
