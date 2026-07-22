using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyCombatFaction
    {
        None = 0,
        Settlement = 1,
        HostileWildlife = 2
    }

    public enum StrategyCombatDamageKind
    {
        Piercing = 0,
        Bite = 1
    }

    public readonly struct StrategyCombatDamage
    {
        public StrategyCombatDamage(
            object source,
            StrategyCombatFaction sourceFaction,
            int amount,
            StrategyCombatDamageKind kind,
            Vector3 hitWorld)
        {
            Source = source;
            SourceFaction = sourceFaction;
            Amount = amount;
            Kind = kind;
            HitWorld = hitWorld;
        }

        public object Source { get; }
        public StrategyCombatFaction SourceFaction { get; }
        public int Amount { get; }
        public StrategyCombatDamageKind Kind { get; }
        public Vector3 HitWorld { get; }
    }

    public readonly struct StrategyCombatDamageResult
    {
        public StrategyCombatDamageResult(
            bool applied,
            int previousHealth,
            int currentHealth,
            int maxHealth,
            bool becameDefeated,
            bool defeatPrevented = false)
        {
            Applied = applied;
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            BecameDefeated = becameDefeated;
            DefeatPrevented = defeatPrevented;
        }

        public bool Applied { get; }
        public int PreviousHealth { get; }
        public int CurrentHealth { get; }
        public int MaxHealth { get; }
        public bool BecameDefeated { get; }
        public bool DefeatPrevented { get; }

        public static StrategyCombatDamageResult Rejected(int currentHealth, int maxHealth)
        {
            return new StrategyCombatDamageResult(
                false,
                currentHealth,
                currentHealth,
                maxHealth,
                false);
        }
    }

    public interface IStrategyCombatant
    {
        StrategyCombatFaction CombatFaction { get; }
        bool IsCombatAlive { get; }
        bool CanBeCombatTargeted { get; }
        int CurrentCombatHealth { get; }
        int MaxCombatHealth { get; }
        Vector3 CombatWorldPosition { get; }

        bool TryGetCombatCell(out Vector2Int cell);
        StrategyCombatDamageResult ReceiveCombatDamage(in StrategyCombatDamage damage);
    }

    public static class StrategyCombatRules
    {
        public static bool AreHostile(
            StrategyCombatFaction sourceFaction,
            StrategyCombatFaction targetFaction)
        {
            return (sourceFaction == StrategyCombatFaction.Settlement
                    && targetFaction == StrategyCombatFaction.HostileWildlife)
                || (sourceFaction == StrategyCombatFaction.HostileWildlife
                    && targetFaction == StrategyCombatFaction.Settlement);
        }

        public static bool CanApplyDamage(
            in StrategyCombatDamage damage,
            IStrategyCombatant target)
        {
            if (target == null
                || target is Object unityTarget && unityTarget == null)
            {
                return false;
            }

            return target.IsCombatAlive
                && target.CanBeCombatTargeted
                && damage.Amount > 0
                && AreHostile(damage.SourceFaction, target.CombatFaction);
        }
    }
}
