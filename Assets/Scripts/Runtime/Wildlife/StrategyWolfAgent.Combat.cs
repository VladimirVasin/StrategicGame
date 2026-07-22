using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWolfAgent : IStrategyCombatant
    {
        private const int WolfMaximumCombatHealth = 100;
        private const int WolfRetreatHealthThreshold = 20;
        private const int WolfBiteDamage = 20;
        private const float ForcedAttackRecoverySeconds = 0.9f;
        private const float CombatRetreatTimeoutSeconds = 12f;
        private const float CombatRetreatRetrySeconds = 0.75f;

        private StrategyCombatHealth combatHealth;
        private IStrategyCombatant forcedCombatTarget;
        private float combatAttackRecovery;
        private float combatRetreatRemaining;
        private float combatRetreatRetry;
        private bool isForcedCombatEncounter;
        private bool combatDepartureStarted;

        public StrategyCombatFaction CombatFaction => StrategyCombatFaction.HostileWildlife;
        public bool IsCombatAlive => EnsureCombatHealth().IsAlive;
        public bool CanBeCombatTargeted => IsCombatAlive
            && !combatDepartureStarted
            && state != StrategyWolfBehaviorState.Retreating;
        public int CurrentCombatHealth => EnsureCombatHealth().Current;
        public int MaxCombatHealth => EnsureCombatHealth().Maximum;
        public Vector3 CombatWorldPosition => transform.position;
        internal bool IsForcedCombatEncounter => isForcedCombatEncounter;

        public bool TryGetCombatCell(out Vector2Int cell)
        {
            return TryGetCurrentCell(out cell);
        }

        public StrategyCombatDamageResult ReceiveCombatDamage(in StrategyCombatDamage damage)
        {
            StrategyCombatHealth health = EnsureCombatHealth();
            if (!StrategyCombatRules.CanApplyDamage(damage, this))
            {
                return StrategyCombatDamageResult.Rejected(health.Current, health.Maximum);
            }

            StrategyCombatDamageResult result = health.ApplyDamage(damage.Amount);
            if (!result.Applied)
            {
                return result;
            }

            StrategyWorldEffectAnimator.Spawn(
                StrategyWorldEffectKind.Dust,
                damage.HitWorld,
                StrategyWorldSorting.ForPosition(damage.HitWorld, 5),
                Mathf.RoundToInt(Time.time * 41f) + result.CurrentHealth,
                0.62f);
            StrategyDebugLogger.Info(
                "Combat",
                "WolfDamaged",
                StrategyDebugLogger.F("wolf", GetEntityId()),
                StrategyDebugLogger.F("pack", PackId),
                StrategyDebugLogger.F("damage", damage.Amount),
                StrategyDebugLogger.F("kind", damage.Kind),
                StrategyDebugLogger.F("health", result.CurrentHealth),
                StrategyDebugLogger.F("maxHealth", result.MaxHealth));

            if (result.BecameDefeated)
            {
                DieFromCombat(damage);
            }
            else if (result.CurrentHealth <= WolfRetreatHealthThreshold)
            {
                StartCombatRetreat("low_health");
            }

            return result;
        }

        internal bool BeginForcedCombatEncounter(IStrategyCombatant target)
        {
            if (!CanUseForcedCombatTarget(target) || !CanBeCombatTargeted)
            {
                return false;
            }

            ReleaseTargets();
            forcedCombatTarget = target;
            isForcedCombatEncounter = true;
            combatAttackRecovery = 0f;
            combatDepartureStarted = false;
            path.Clear();
            pathIndex = 0;
            targetRefreshTimer = 0f;
            SetWolfState(StrategyWolfBehaviorState.Stalking, "forced_combat_target_acquired");
            StrategyDebugLogger.Info(
                "Combat",
                "WolfForcedEncounterStarted",
                StrategyDebugLogger.F("wolf", GetEntityId()),
                StrategyDebugLogger.F("pack", PackId),
                StrategyDebugLogger.F("target", GetForcedCombatTargetDebugName()));
            return true;
        }

        internal void DespawnCombatEncounterWolf(string reason)
        {
            if (combatDepartureStarted)
            {
                return;
            }

            combatDepartureStarted = true;
            attackResolved = true;
            ConsumeAnimalTarget();
            ReleaseTargets();
            path.Clear();
            pathIndex = 0;
            if (wildlife != null)
            {
                wildlife.DespawnWolf(this, pack, string.IsNullOrEmpty(reason) ? "encounter_reset" : reason);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private StrategyCombatHealth EnsureCombatHealth()
        {
            combatHealth ??= new StrategyCombatHealth(WolfMaximumCombatHealth);
            return combatHealth;
        }

        private void UpdateCombatRuntime(float elapsedSeconds)
        {
            EnsureCombatHealth();
            combatAttackRecovery = Mathf.Max(0f, combatAttackRecovery - elapsedSeconds);
            if (state != StrategyWolfBehaviorState.Retreating)
            {
                return;
            }

            combatRetreatRemaining -= elapsedSeconds;
            combatRetreatRetry -= elapsedSeconds;
        }

        private bool CanStartCombatAttack()
        {
            return !isForcedCombatEncounter || combatAttackRecovery <= 0f;
        }

        private bool TryResolveForcedCombatAttack()
        {
            if (!isForcedCombatEncounter)
            {
                return false;
            }

            if (!CanUseForcedCombatTarget(forcedCombatTarget))
            {
                StartCombatRetreat("combat_target_lost");
                return true;
            }

            StrategyCombatDamage bite = new(
                this,
                StrategyCombatFaction.HostileWildlife,
                WolfBiteDamage,
                StrategyCombatDamageKind.Bite,
                transform.position);
            StrategyCombatDamageResult result = forcedCombatTarget.ReceiveCombatDamage(bite);
            combatAttackRecovery = ForcedAttackRecoverySeconds;
            StrategyDebugLogger.Info(
                "Combat",
                "WolfBiteResolved",
                StrategyDebugLogger.F("wolf", GetEntityId()),
                StrategyDebugLogger.F("target", GetForcedCombatTargetDebugName()),
                StrategyDebugLogger.F("applied", result.Applied),
                StrategyDebugLogger.F("damage", WolfBiteDamage),
                StrategyDebugLogger.F("targetHealth", result.CurrentHealth),
                StrategyDebugLogger.F("targetDefeated", result.BecameDefeated));
            if (result.BecameDefeated || !CanUseForcedCombatTarget(forcedCombatTarget))
            {
                StartCombatRetreat("combat_target_defeated");
            }

            return true;
        }

        private bool TryContinueForcedCombatAfterAttack()
        {
            if (!isForcedCombatEncounter)
            {
                return false;
            }

            if (!CanUseForcedCombatTarget(forcedCombatTarget))
            {
                StartCombatRetreat("combat_target_lost_after_attack");
                return true;
            }

            SetWolfState(StrategyWolfBehaviorState.Chasing, "forced_combat_attack_recovery");
            targetRefreshTimer = 0f;
            return true;
        }

        private bool TryGetForcedCombatTargetWorld(out Vector3 world, out Vector2Int cell)
        {
            world = Vector3.zero;
            cell = default;
            if (!isForcedCombatEncounter || !CanUseForcedCombatTarget(forcedCombatTarget))
            {
                if (isForcedCombatEncounter)
                {
                    ClearForcedCombatTarget();
                }

                return false;
            }

            if (!forcedCombatTarget.TryGetCombatCell(out cell))
            {
                ClearForcedCombatTarget();
                return false;
            }

            world = forcedCombatTarget.CombatWorldPosition;
            return true;
        }

        private bool CanUseForcedCombatTarget(IStrategyCombatant target)
        {
            if (target == null
                || target is Object unityTarget && unityTarget == null
                || !target.IsCombatAlive
                || !target.CanBeCombatTargeted)
            {
                return false;
            }

            return StrategyCombatRules.AreHostile(CombatFaction, target.CombatFaction);
        }

        private void ClearForcedCombatTarget()
        {
            forcedCombatTarget = null;
            isForcedCombatEncounter = false;
        }

        private string GetForcedCombatTargetDebugName()
        {
            if (forcedCombatTarget is Component component && component != null)
            {
                return component.name;
            }

            return forcedCombatTarget != null ? forcedCombatTarget.GetType().Name : "none";
        }

        private bool CanUseForcedCombatDirectStep(Vector3 nextWorld)
        {
            if (!isForcedCombatEncounter || wildlife == null || map == null)
            {
                return true;
            }

            return map.TryWorldToCell(nextWorld, out Vector2Int nextCell)
                && wildlife.IsLandWildlifeTravelCell(nextCell, true);
        }

        private void StartCombatRetreat(string reason)
        {
            if (!IsCombatAlive || combatDepartureStarted || state == StrategyWolfBehaviorState.Retreating)
            {
                return;
            }

            attackResolved = true;
            ConsumeAnimalTarget();
            ReleaseTargets();
            path.Clear();
            pathIndex = 0;
            combatRetreatRemaining = CombatRetreatTimeoutSeconds;
            combatRetreatRetry = 0f;
            TryPrepareCombatRetreatPath();
            SetWolfState(StrategyWolfBehaviorState.Retreating, reason);
            StrategyDebugLogger.Info(
                "Combat",
                "WolfRetreatStarted",
                StrategyDebugLogger.F("wolf", GetEntityId()),
                StrategyDebugLogger.F("pack", PackId),
                StrategyDebugLogger.F("health", CurrentCombatHealth),
                StrategyDebugLogger.F("reason", reason));
        }

        private void UpdateCombatRetreating()
        {
            if (combatDepartureStarted)
            {
                return;
            }

            AnimateRunOrSwim();
            if (combatRetreatRemaining <= 0f)
            {
                CompleteCombatRetreat("retreat_timeout");
                return;
            }

            if (path.Count > 0 && pathIndex < path.Count)
            {
                if (MoveAlongPath(RunSpeed))
                {
                    CompleteCombatRetreat("safe_path_completed");
                }

                return;
            }

            if (combatRetreatRetry <= 0f)
            {
                combatRetreatRetry = CombatRetreatRetrySeconds;
                TryPrepareCombatRetreatPath();
            }
        }

        private bool TryPrepareCombatRetreatPath()
        {
            if (!TryStartRoaming(true))
            {
                SetWolfState(StrategyWolfBehaviorState.Retreating, "retreat_path_retry");
                return false;
            }

            SetWolfState(StrategyWolfBehaviorState.Retreating, "retreat_path_ready");
            return path.Count > 0;
        }

        private void CompleteCombatRetreat(string reason)
        {
            if (combatDepartureStarted)
            {
                return;
            }

            combatDepartureStarted = true;
            ReleaseTargets();
            StrategyDebugLogger.Info(
                "Combat",
                "WolfRetreated",
                StrategyDebugLogger.F("wolf", GetEntityId()),
                StrategyDebugLogger.F("pack", PackId),
                StrategyDebugLogger.F("health", CurrentCombatHealth),
                StrategyDebugLogger.F("reason", reason));
            if (wildlife != null)
            {
                wildlife.DespawnWolf(this, pack, reason);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void DieFromCombat(in StrategyCombatDamage damage)
        {
            if (combatDepartureStarted)
            {
                return;
            }

            combatDepartureStarted = true;
            attackResolved = true;
            ConsumeAnimalTarget();
            ReleaseTargets();
            path.Clear();
            pathIndex = 0;
            SetWolfState(StrategyWolfBehaviorState.Dead, "combat_health_depleted");
            CircleCollider2D clickCollider = GetComponent<CircleCollider2D>();
            if (clickCollider != null)
            {
                clickCollider.enabled = false;
            }

            StrategyDebugLogger.Info(
                "Combat",
                "WolfKilled",
                StrategyDebugLogger.F("wolf", GetEntityId()),
                StrategyDebugLogger.F("pack", PackId),
                StrategyDebugLogger.F("source", damage.Source),
                StrategyDebugLogger.F("kind", damage.Kind),
                StrategyDebugLogger.F("world", transform.position));
            if (wildlife != null)
            {
                wildlife.DespawnWolf(this, pack, "combat_killed");
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
