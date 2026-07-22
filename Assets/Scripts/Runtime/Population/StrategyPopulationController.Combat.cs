using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationController
    {
        public bool TryKillResidentFromCombat(
            StrategyResidentAgent resident,
            in StrategyCombatDamage damage)
        {
            if (resident == null
                || resident.IsPendingRefugee
                || !residents.Contains(resident))
            {
                return false;
            }

            string residentName = resident.FullName;
            int residentId = resident.ResidentId;
            bool killed = HandleResidentDeath(
                resident,
                damage.Kind == StrategyCombatDamageKind.Bite
                    ? "wolf_attack"
                    : "combat_damage",
                1f,
                0f,
                1f,
                0,
                Vector2Int.zero);
            if (killed)
            {
                StrategyDebugLogger.Info(
                    "Combat",
                    "ResidentKilledInCombat",
                    StrategyDebugLogger.F("resident", residentName),
                    StrategyDebugLogger.F("residentId", residentId),
                    StrategyDebugLogger.F("damageKind", damage.Kind),
                    StrategyDebugLogger.F("hitWorld", damage.HitWorld));
            }

            return killed;
        }
    }
}
