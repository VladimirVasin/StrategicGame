using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard
    {
        public void ReturnReservedConstructionResource(
            object owner,
            StrategyConstructionResourceKind kind,
            int amount)
        {
            if (owner == null || amount <= 0 || kind == StrategyConstructionResourceKind.None)
            {
                return;
            }

            if (kind == StrategyConstructionResourceKind.Logs)
            {
                logsStored += amount;
                AddReservation(constructionLogReservations, owner, amount);
            }
            else if (kind == StrategyConstructionResourceKind.Stone)
            {
                stoneStored += amount;
                AddReservation(constructionStoneReservations, owner, amount);
            }
            else if (kind == StrategyConstructionResourceKind.Planks)
            {
                planksStored += amount;
                AddReservation(constructionPlankReservations, owner, amount);
            }

            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "StorageYard",
                "ConstructionResourceReturnedToReservation",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("owner", owner),
                StrategyDebugLogger.F("resource", kind),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("logsStock", logsStored),
                StrategyDebugLogger.F("stoneStock", stoneStored),
                StrategyDebugLogger.F("planksStock", planksStored),
                StrategyDebugLogger.F("ownerUnclaimed", GetAvailableReservationAmount(owner, kind)));
        }

        public bool CanAssignNextAvailableWorker()
        {
            if (population == null)
            {
                return false;
            }

            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && resident.CanAcceptWorkAssignment
                    && !resident.HasWorkplace
                    && !resident.HasConstructionAssignment
                    && !workers.Contains(resident)
                    && !builders.Contains(resident))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAssignNextAvailableWorker(out StrategyResidentAgent assigned)
        {
            assigned = null;
            if (population == null)
            {
                return false;
            }

            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            List<StrategyResidentAgent> candidates = new();
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && resident.CanAcceptWorkAssignment
                    && !resident.HasWorkplace
                    && !resident.HasConstructionAssignment
                    && !workers.Contains(resident)
                    && !builders.Contains(resident))
                {
                    candidates.Add(resident);
                }
            }

            if (candidates.Count <= 0)
            {
                return false;
            }

            assigned = candidates[Random.Range(0, candidates.Count)];
            return AssignWorker(assigned);
        }

        public bool AssignWorker(StrategyResidentAgent resident)
        {
            if (resident == null
                || workers.Contains(resident)
                || builders.Contains(resident)
                || !resident.CanAcceptWorkAssignment
                || resident.HasWorkplace
                || resident.HasConstructionAssignment)
            {
                return false;
            }

            workers.Add(resident);
            resident.AssignStorageWorkplace(this);
            StrategyDebugLogger.Info(
                "StorageYard",
                "WorkerAssigned",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("worker", resident.FullName),
                StrategyDebugLogger.F("workerCount", workers.Count));
            return true;
        }

        public void UnassignWorkerAt(int index)
        {
            if (index < 0 || index >= workers.Count)
            {
                return;
            }

            StrategyResidentAgent worker = workers[index];
            workers.RemoveAt(index);
            if (worker != null)
            {
                StrategyDebugLogger.Info(
                    "StorageYard",
                    "WorkerUnassigned",
                    StrategyDebugLogger.F("yardOrigin", Origin),
                    StrategyDebugLogger.F("worker", worker.FullName),
                    StrategyDebugLogger.F("workerCount", workers.Count));
                worker.ClearStorageWorkplace(this);
            }
        }

        public void UnassignWorker(StrategyResidentAgent worker)
        {
            int index = workers.IndexOf(worker);
            if (index >= 0)
            {
                UnassignWorkerAt(index);
            }
        }

        public bool TryGetWorker(int index, out StrategyResidentAgent worker)
        {
            worker = index >= 0 && index < workers.Count ? workers[index] : null;
            return worker != null;
        }

        public bool CanAssignNextAvailableBuilder()
        {
            if (population == null)
            {
                return false;
            }

            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && resident.CanAcceptWorkAssignment
                    && !resident.HasWorkplace
                    && !resident.HasConstructionAssignment
                    && !workers.Contains(resident)
                    && !builders.Contains(resident))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAssignNextAvailableBuilder(out StrategyResidentAgent assigned)
        {
            assigned = null;
            if (population == null)
            {
                return false;
            }

            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            List<StrategyResidentAgent> candidates = new();
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && resident.CanAcceptWorkAssignment
                    && !resident.HasWorkplace
                    && !resident.HasConstructionAssignment
                    && !workers.Contains(resident)
                    && !builders.Contains(resident))
                {
                    candidates.Add(resident);
                }
            }

            if (candidates.Count <= 0)
            {
                return false;
            }

            assigned = candidates[Random.Range(0, candidates.Count)];
            bool assignedToYard = AssignBuilder(assigned);
            if (assignedToYard)
            {
                TryDispatchBuilder(assigned);
            }

            return assignedToYard;
        }

        public bool AssignBuilder(StrategyResidentAgent resident)
        {
            if (resident == null
                || workers.Contains(resident)
                || builders.Contains(resident)
                || !resident.CanAcceptWorkAssignment
                || resident.HasWorkplace
                || resident.HasConstructionAssignment)
            {
                return false;
            }

            builders.Add(resident);
            resident.AssignBuilderWorkplace(this);
            StrategyDebugLogger.Info(
                "StorageYard",
                "BuilderAssigned",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("builder", resident.FullName),
                StrategyDebugLogger.F("builderCount", builders.Count));
            return true;
        }

        public void UnassignBuilderAt(int index)
        {
            if (index < 0 || index >= builders.Count)
            {
                return;
            }

            StrategyResidentAgent builder = builders[index];
            builders.RemoveAt(index);
            if (builder != null)
            {
                StrategyDebugLogger.Info(
                    "StorageYard",
                    "BuilderUnassigned",
                    StrategyDebugLogger.F("yardOrigin", Origin),
                    StrategyDebugLogger.F("builder", builder.FullName),
                    StrategyDebugLogger.F("builderCount", builders.Count));
                builder.ClearBuilderWorkplace(this);
            }
        }

        public void UnassignBuilder(StrategyResidentAgent builder)
        {
            int index = builders.IndexOf(builder);
            if (index >= 0)
            {
                UnassignBuilderAt(index);
            }
        }

        public bool TryGetBuilder(int index, out StrategyResidentAgent builder)
        {
            builder = index >= 0 && index < builders.Count ? builders[index] : null;
            return builder != null;
        }

        public bool TryReserveLogSource(object owner, out StrategyLumberjackCamp source)
        {
            source = null;
            if (owner == null)
            {
                return false;
            }

            StrategyLumberjackCamp[] camps = Object.FindObjectsByType<StrategyLumberjackCamp>();
            StrategyLumberjackCamp bestCamp = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < camps.Length; i++)
            {
                StrategyLumberjackCamp camp = camps[i];
                if (camp == null || camp.AvailableLogs <= 0)
                {
                    continue;
                }

                float distance = (camp.FootprintBounds.center - FootprintBounds.center).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCamp = camp;
                }
            }

            if (bestCamp == null || !bestCamp.TryReserveStoredLogs(owner, out _))
            {
                return false;
            }

            source = bestCamp;
            return true;
        }

        public bool TryReserveStoneSource(object owner, out StrategyStonecutterCamp source)
        {
            source = null;
            if (owner == null)
            {
                return false;
            }

            StrategyStonecutterCamp[] camps = Object.FindObjectsByType<StrategyStonecutterCamp>();
            StrategyStonecutterCamp bestCamp = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < camps.Length; i++)
            {
                StrategyStonecutterCamp camp = camps[i];
                if (camp == null || camp.AvailableStone <= 0)
                {
                    continue;
                }

                float distance = (camp.FootprintBounds.center - FootprintBounds.center).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCamp = camp;
                }
            }

            if (bestCamp == null || !bestCamp.TryReserveStoredStone(owner, out _))
            {
                return false;
            }

            source = bestCamp;
            return true;
        }

        public bool ShouldPrioritizeStonePickup()
        {
            if (!HasAvailableStoneLogisticsSource())
            {
                return false;
            }

            if (!HasAvailableLogLogisticsSource())
            {
                return true;
            }

            if (HasWaitingConstructionNeeding(StrategyConstructionResourceKind.Stone)
                && AvailableConstructionStone <= 0)
            {
                return true;
            }

            return stoneStored < logsStored;
        }

        public bool TryFindDropoffCell(out Vector2Int cell)
        {
            cell = default;
            if (map == null || building == null)
            {
                return false;
            }

            for (int radius = 1; radius <= 3; radius++)
            {
                List<Vector2Int> candidates = new();
                for (int y = -radius; y < building.Footprint.y + radius; y++)
                {
                    for (int x = -radius; x < building.Footprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == building.Footprint.x + radius - 1
                            || y == building.Footprint.y + radius - 1;
                        if (!isEdge)
                        {
                            continue;
                        }

                        Vector2Int candidate = building.Origin + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    cell = candidates[Random.Range(0, candidates.Count)];
                    return true;
                }
            }

            return false;
        }

        public void AddLogs(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            logsStored += amount;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "StorageYard",
                "LogsStored",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", logsStored));
        }

        public void AddStone(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            stoneStored += amount;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "StorageYard",
                "ResourceStored",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("resource", StrategyResourceType.Stone),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", stoneStored));
        }

        private int ReserveConstructionLogs(object owner, int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableConstructionLogs);
            if (amount <= 0)
            {
                return 0;
            }

            AddReservation(constructionLogReservations, owner, amount);
            return amount;
        }

        private int ReserveConstructionStone(object owner, int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableConstructionStone);
            if (amount <= 0)
            {
                return 0;
            }

            AddReservation(constructionStoneReservations, owner, amount);
            return amount;
        }

        private int ReserveConstructionPlanks(object owner, int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableConstructionPlanks);
            if (amount <= 0)
            {
                return 0;
            }

            AddReservation(constructionPlankReservations, owner, amount);
            return amount;
        }
    }
}
