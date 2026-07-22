using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {
        private const int CombatEncounterWolfPackIdStart = 10000;
        private const int CombatEncounterWolfSpawnMinRadius = 2;
        private const int CombatEncounterWolfSpawnMaxRadius = 12;
        private const int CombatEncounterWolfSpawnSearchBand = 4;

        private readonly HashSet<StrategyWolfAgent> combatEncounterWolves = new();
        private readonly Dictionary<StrategyWolfAgent, StrategyBattleThreatLease>
            combatEncounterThreats = new();
        private StrategyBattleLifecycleController battleLifecycle;
        private int nextRuntimeWolfPackId = CombatEncounterWolfPackIdStart;

        public void ConfigureBattleLifecycle(
            StrategyBattleLifecycleController lifecycleController)
        {
            if (battleLifecycle == lifecycleController)
            {
                return;
            }

            List<StrategyWolfAgent> activeThreatOwners =
                new(combatEncounterThreats.Keys);
            ReleaseAllCombatEncounterThreats();
            battleLifecycle = lifecycleController;
            for (int i = 0; i < activeThreatOwners.Count; i++)
            {
                RegisterCombatEncounterThreat(activeThreatOwners[i]);
            }
        }

        public bool TrySpawnCombatEncounterWolf(
            StrategyResidentAgent target,
            int preferredDistance,
            out StrategyWolfAgent wolf)
        {
            wolf = null;
            if (target == null
                || map == null
                || !map.TryWorldToCell(target.transform.position, out Vector2Int targetCell)
                || !TryFindCombatEncounterWolfSpawnCell(
                    targetCell,
                    preferredDistance,
                    out Vector2Int spawnCell))
            {
                StrategyDebugLogger.Warn(
                    "Combat",
                    "CombatEncounterWolfSpawnRejected",
                    StrategyDebugLogger.F("hasTarget", target != null),
                    StrategyDebugLogger.F("hasMap", map != null));
                return false;
            }

            EnsureWildlifeRoot();
            int packId = AllocateRuntimeWolfPackId();
            StrategyWolfPack pack = new(packId, spawnCell, WolfHomeRadius);
            wolfPacks.Add(pack);
            wolf = SpawnWolf(pack, spawnCell, spawnCell, 0);
            if (wolf == null)
            {
                wolfPacks.Remove(pack);
                return false;
            }

            combatEncounterWolves.Add(wolf);
            StrategyDebugLogger.Info(
                "Combat",
                "CombatEncounterWolfSpawned",
                StrategyDebugLogger.F("wolf", wolf.GetEntityId()),
                StrategyDebugLogger.F("pack", packId),
                StrategyDebugLogger.F("spawnCell", spawnCell),
                StrategyDebugLogger.F("targetCell", targetCell));
            return true;
        }

        public bool TryBeginCombatEncounter(
            StrategyWolfAgent wolf,
            IStrategyCombatant target)
        {
            if (wolf == null
                || !combatEncounterWolves.Contains(wolf)
                || !CanUseCombatEncounterTarget(target))
            {
                return false;
            }

            if (!wolf.BeginForcedCombatEncounter(target))
            {
                return false;
            }

            RegisterCombatEncounterThreat(wolf);
            return true;
        }

        public void DespawnCombatEncounterWolf(StrategyWolfAgent wolf)
        {
            if (wolf != null && combatEncounterWolves.Contains(wolf))
            {
                wolf.DespawnCombatEncounterWolf("encounter_despawn");
            }
        }

        public int ResetCombatEncounters()
        {
            if (combatEncounterWolves.Count <= 0)
            {
                return 0;
            }

            List<StrategyWolfAgent> active = new(combatEncounterWolves);
            int removed = 0;
            for (int i = 0; i < active.Count; i++)
            {
                StrategyWolfAgent wolf = active[i];
                if (wolf == null)
                {
                    continue;
                }

                wolf.DespawnCombatEncounterWolf("encounter_reset");
                removed++;
            }

            return removed;
        }

        internal void DespawnWolf(
            StrategyWolfAgent wolf,
            StrategyWolfPack pack,
            string reason)
        {
            if (wolf == null)
            {
                return;
            }

            NotifyWolfRemoved(wolf, pack);
            StrategyDebugLogger.Info(
                "Wildlife",
                "WolfDespawned",
                StrategyDebugLogger.F("wolf", wolf.GetEntityId()),
                StrategyDebugLogger.F("pack", pack != null ? pack.PackId : -1),
                StrategyDebugLogger.F("reason", reason));
            Destroy(wolf.gameObject);
        }

        internal void NotifyWolfRemoved(
            StrategyWolfAgent wolf,
            StrategyWolfPack pack)
        {
            if (ReferenceEquals(wolf, null))
            {
                return;
            }

            ReleaseCombatEncounterThreat(wolf);
            combatEncounterWolves.Remove(wolf);
            wolves.Remove(wolf);
            if (pack == null)
            {
                return;
            }

            pack.RemoveMember(wolf);
            if (pack.MemberCount > 0)
            {
                return;
            }

            wolfPacks.Remove(pack);
            wolfMigrations.Remove(pack.PackId);
        }

        private bool IsWolfCombatEncounterPack(StrategyWolfPack pack)
        {
            if (pack == null)
            {
                return false;
            }

            IReadOnlyList<StrategyWolfAgent> members = pack.Members;
            for (int i = 0; i < members.Count; i++)
            {
                StrategyWolfAgent member = members[i];
                if (member != null && combatEncounterWolves.Contains(member))
                {
                    return true;
                }
            }

            return false;
        }

        internal void RegisterCombatEncounterThreat(StrategyWolfAgent wolf)
        {
            if (wolf == null || battleLifecycle == null)
            {
                return;
            }

            ReleaseCombatEncounterThreat(wolf);
            combatEncounterThreats[wolf] = battleLifecycle.RegisterThreat(
                wolf,
                "Wolf combat encounter");
        }

        private void ReleaseCombatEncounterThreat(StrategyWolfAgent wolf)
        {
            if (ReferenceEquals(wolf, null)
                || !combatEncounterThreats.TryGetValue(
                    wolf,
                    out StrategyBattleThreatLease lease))
            {
                return;
            }

            combatEncounterThreats.Remove(wolf);
            lease?.Dispose();
        }

        private void ReleaseAllCombatEncounterThreats()
        {
            if (combatEncounterThreats.Count <= 0)
            {
                return;
            }

            List<StrategyBattleThreatLease> leases = new(combatEncounterThreats.Values);
            combatEncounterThreats.Clear();
            for (int i = 0; i < leases.Count; i++)
            {
                leases[i]?.Dispose();
            }
        }

        private bool CanUseCombatEncounterTarget(IStrategyCombatant target)
        {
            if (target == null
                || target is Object unityTarget && unityTarget == null
                || !target.IsCombatAlive
                || !target.CanBeCombatTargeted)
            {
                return false;
            }

            return StrategyCombatRules.AreHostile(
                StrategyCombatFaction.HostileWildlife,
                target.CombatFaction);
        }

        private bool TryFindCombatEncounterWolfSpawnCell(
            Vector2Int targetCell,
            int preferredDistance,
            out Vector2Int spawnCell)
        {
            spawnCell = default;
            int preferredRadius = Mathf.Clamp(
                preferredDistance,
                CombatEncounterWolfSpawnMinRadius,
                CombatEncounterWolfSpawnMaxRadius);
            for (int offset = 0; offset <= CombatEncounterWolfSpawnSearchBand; offset++)
            {
                if (TryFindCombatEncounterWolfSpawnCellAtRadius(
                    targetCell,
                    preferredRadius + offset,
                    out spawnCell))
                {
                    return true;
                }

                if (offset > 0
                    && TryFindCombatEncounterWolfSpawnCellAtRadius(
                        targetCell,
                        preferredRadius - offset,
                        out spawnCell))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryFindCombatEncounterWolfSpawnCellAtRadius(
            Vector2Int targetCell,
            int radius,
            out Vector2Int spawnCell)
        {
            spawnCell = default;
            if (radius < CombatEncounterWolfSpawnMinRadius
                || radius > CombatEncounterWolfSpawnMaxRadius)
            {
                return false;
            }

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
                    {
                        continue;
                    }

                    Vector2Int candidate = targetCell + new Vector2Int(x, y);
                    if (!IsCombatEncounterWolfSpawnCell(candidate))
                    {
                        continue;
                    }

                    spawnCell = candidate;
                    return true;
                }
            }

            return false;
        }

        private bool IsCombatEncounterWolfSpawnCell(Vector2Int cell)
        {
            if (map == null
                || !StrategyWildlifeRiverCrossing.IsLandCell(map, cell)
                || !map.IsCellWalkable(cell))
            {
                return false;
            }

            for (int i = 0; i < wolves.Count; i++)
            {
                StrategyWolfAgent wolf = wolves[i];
                if (wolf != null
                    && wolf.TryGetCombatCell(out Vector2Int wolfCell)
                    && (wolfCell - cell).sqrMagnitude <= 2f)
                {
                    return false;
                }
            }

            return true;
        }

        private int AllocateRuntimeWolfPackId()
        {
            while (HasWolfPackId(nextRuntimeWolfPackId))
            {
                nextRuntimeWolfPackId++;
            }

            return nextRuntimeWolfPackId++;
        }

        private bool HasWolfPackId(int packId)
        {
            for (int i = 0; i < wolfPacks.Count; i++)
            {
                if (wolfPacks[i] != null && wolfPacks[i].PackId == packId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
