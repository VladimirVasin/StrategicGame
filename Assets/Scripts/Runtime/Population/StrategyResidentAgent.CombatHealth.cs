using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent : IStrategyCombatant
    {
        private const int ResidentMaximumCombatHealth = 100;
        private const int ResidentDailyCombatRecovery = 20;

        private readonly StrategyCombatHealth combatHealth =
            new StrategyCombatHealth(ResidentMaximumCombatHealth);
        private int lastCombatRecoveryDayIndex = -1;

        public StrategyCombatFaction CombatFaction => StrategyCombatFaction.Settlement;
        public bool IsCombatAlive => !deathRequested && combatHealth.IsAlive;
        public bool CanBeCombatTargeted => IsCombatAlive
            && !IsPendingRefugee
            && !hiddenInsideHome
            && !hiddenUnderground
            && !IsOnScoutExpedition;
        public int CurrentCombatHealth => combatHealth.Current;
        public int MaxCombatHealth => combatHealth.Maximum;
        public int LastCombatRecoveryDayIndex => lastCombatRecoveryDayIndex;
        public Vector3 CombatWorldPosition => transform.position;
        public int CombatWoundSeverity => combatHealth.Current > 75
            ? 0
            : combatHealth.Current > 50
                ? 1
                : combatHealth.Current > 25
                    ? 2
                    : 3;

        private float CombatMovementSpeedMultiplier => CombatWoundSeverity switch
        {
            0 => 1f,
            1 => 0.92f,
            2 => 0.82f,
            _ => 0.70f
        };

        public bool TryGetCombatCell(out Vector2Int cell)
        {
            cell = default;
            return map != null && map.TryWorldToCell(transform.position, out cell);
        }

        public StrategyCombatDamageResult ReceiveCombatDamage(in StrategyCombatDamage damage)
        {
            if (!StrategyCombatRules.CanApplyDamage(damage, this))
            {
                return StrategyCombatDamageResult.Rejected(
                    combatHealth.Current,
                    combatHealth.Maximum);
            }

            StrategyCombatDamageResult result = combatHealth.ApplyDamage(damage.Amount);
            StrategyWorldEffectAnimator.Spawn(
                StrategyWorldEffectKind.Dust,
                damage.HitWorld,
                StrategyWorldSorting.ForPosition(damage.HitWorld, 5),
                residentId * 31 + result.CurrentHealth,
                0.72f);
            StrategyDebugLogger.Info(
                "Combat",
                "ResidentDamaged",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("damage", damage.Amount),
                StrategyDebugLogger.F("kind", damage.Kind),
                StrategyDebugLogger.F("health", result.CurrentHealth),
                StrategyDebugLogger.F("maximum", result.MaxHealth));

            if (!result.BecameDefeated)
            {
                return result;
            }

            bool killed = population != null
                && population.TryKillResidentFromCombat(this, damage);
            if (killed)
            {
                activeCombatTarget = null;
                return result;
            }

            combatHealth.TryRestore(1);
            return new StrategyCombatDamageResult(
                true,
                result.PreviousHealth,
                combatHealth.Current,
                combatHealth.Maximum,
                false,
                true);
        }

        public void RestoreCombatState(int restoredHealth, int restoredRecoveryDayIndex)
        {
            combatHealth.TryRestore(Mathf.Clamp(restoredHealth, 1, combatHealth.Maximum));
            lastCombatRecoveryDayIndex = Mathf.Max(-1, restoredRecoveryDayIndex);
        }

        private void UpdateCombatRecovery()
        {
            StrategyCalendarSnapshot calendar = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            if (calendar.Phase != StrategyTimeOfDayPhase.Dawn
                || calendar.DayIndex == lastCombatRecoveryDayIndex
                || !combatHealth.IsAlive)
            {
                return;
            }

            lastCombatRecoveryDayIndex = calendar.DayIndex;
            int restored = Mathf.Min(
                combatHealth.Maximum,
                combatHealth.Current + ResidentDailyCombatRecovery);
            if (restored == combatHealth.Current)
            {
                return;
            }

            combatHealth.TryRestore(restored);
            StrategyDebugLogger.Info(
                "Combat",
                "ResidentCombatHealthRecovered",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("health", combatHealth.Current),
                StrategyDebugLogger.F("day", calendar.DisplayDay));
        }
    }
}
