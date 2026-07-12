using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFuneralController
    {

        private void UpdateBurial(FuneralProcess funeral)
        {
            funeral.Timer -= Time.deltaTime;
            float progress = 1f - Mathf.Clamp01(funeral.Timer / BurialSeconds);
            funeral.Corpse.SetBurialProgress(progress);
            if (funeral.Timer > 0f)
            {
                return;
            }

            if (!cemetery.TryCreateGrave(funeral.Snapshot, funeral.GraveCell))
            {
                return;
            }

            ReleaseReservedGrave(funeral);
            funeral.Corpse.CompleteBurial();
            EndFuneralDuties(funeral.Participants);
            EndFuneralDuties(funeral.Carriers);

            StrategyDebugLogger.Info(
                "Funeral",
                "FuneralCompleted",
                StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                StrategyDebugLogger.F("graveCell", funeral.GraveCell));
            funeral.Completed = true;
        }

        private void BeginBurial(FuneralProcess funeral, string reason)
        {
            if (!IsCorpseNearGrave(funeral))
            {
                StrategyDebugLogger.Warn(
                    "Funeral",
                    "BurialRejected",
                    StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                    StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                    StrategyDebugLogger.F("reason", "corpse_not_delivered"),
                    StrategyDebugLogger.F("corpseDistance", GetCorpseDistanceToGrave(funeral)),
                    StrategyDebugLogger.F("graveCell", funeral.GraveCell));
                funeral.Stage = FuneralStage.Procession;
                funeral.Timer = CarrierRetrySeconds;
                return;
            }

            funeral.Corpse.SetBurialWorld(funeral.GraveWorld);
            funeral.Corpse.StartBurial();
            funeral.Stage = FuneralStage.Burial;
            funeral.Timer = BurialSeconds;

            StartBurialPoses(funeral);
            StrategyWorldAudioDirector.PlayBurial(funeral.GraveWorld);

            StrategyDebugLogger.Info(
                "Funeral",
                "BurialStarted",
                StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("arrived", CountResidentsNear(funeral.ExpectedBurialAttendees, funeral.GraveWorld, ArrivalDistance)),
                StrategyDebugLogger.F("expectedBurialAttendees", funeral.ExpectedBurialAttendees.Count),
                StrategyDebugLogger.F("graveCell", funeral.GraveCell));
        }

        private void StartBurialPoses(FuneralProcess funeral)
        {
            bool serviceBurial = IsServiceBurial(funeral);
            for (int i = 0; i < funeral.ExpectedBurialAttendees.Count; i++)
            {
                StrategyResidentAgent attendee = funeral.ExpectedBurialAttendees[i];
                if (IsResidentNear(attendee, funeral.GraveWorld, ArrivalDistance))
                {
                    attendee.StartFuneralBurial(BurialSeconds + Random.Range(-0.35f, 0.35f), serviceBurial);
                }
            }
        }

        private void SelectCarriers(FuneralProcess funeral)
        {
            int requiredCarrierCount = GetRequiredCarrierCount(funeral);
            if (IsServiceBurial(funeral))
            {
                TryAddServiceCarrier(funeral);
                return;
            }

            for (int i = 0; i < funeral.Participants.Count && funeral.Carriers.Count < requiredCarrierCount; i++)
            {
                StrategyResidentAgent participant = funeral.Participants[i];
                if (participant != null && participant.CanWork && !funeral.Carriers.Contains(participant))
                {
                    funeral.Carriers.Add(participant);
                }
            }

            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count && funeral.Carriers.Count < requiredCarrierCount; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && resident.CanWork
                    && !resident.IsFuneralDutyActive
                    && !funeral.Carriers.Contains(resident)
                    && resident.ResidentId != funeral.Snapshot.ResidentId)
                {
                    funeral.Carriers.Add(resident);
                }
            }
        }

        private HashSet<Vector2Int> BuildReachableCellsFromCarriers(IReadOnlyList<StrategyResidentAgent> carriers)
        {
            HashSet<Vector2Int> reachable = new();
            if (map == null || carriers == null)
            {
                return reachable;
            }

            Queue<Vector2Int> open = new();
            for (int i = 0; i < carriers.Count; i++)
            {
                StrategyResidentAgent carrier = carriers[i];
                if (carrier == null || !map.TryWorldToCell(carrier.transform.position, out Vector2Int startCell))
                {
                    continue;
                }

                if (!map.IsCellWalkable(startCell)
                    && !TryFindNearbyWalkableCell(startCell, out startCell))
                {
                    continue;
                }

                if (reachable.Add(startCell))
                {
                    open.Enqueue(startCell);
                }
            }

            int visitLimit = Mathf.Max(256, map.Width * map.Height);
            while (open.Count > 0 && reachable.Count < visitLimit)
            {
                Vector2Int current = open.Dequeue();
                for (int i = 0; i < CardinalDirections.Length; i++)
                {
                    Vector2Int next = current + CardinalDirections[i];
                    if (reachable.Contains(next) || !map.IsCellWalkable(next))
                    {
                        continue;
                    }

                    reachable.Add(next);
                    open.Enqueue(next);
                }
            }

            return reachable;
        }

        private bool TryFindNearbyWalkableCell(Vector2Int origin, out Vector2Int cell)
        {
            for (int radius = 1; radius <= 4; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        Vector2Int candidate = origin + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate))
                        {
                            cell = candidate;
                            return true;
                        }
                    }
                }
            }

            cell = default;
            return false;
        }

        private void UpdateDraggedCorpsePosition(FuneralProcess funeral)
        {
            if (funeral.Carriers.Count <= 0)
            {
                return;
            }

            StrategyResidentAgent carrier = funeral.PrimaryCarrier;
            if (carrier == null)
            {
                carrier = GetFirstLiveCarrier(funeral.Carriers);
                funeral.PrimaryCarrier = carrier;
            }

            if (carrier == null)
            {
                return;
            }

            Vector3 anchor = carrier.transform.position;
            Vector3 awayFromGrave = anchor - funeral.GraveWorld;
            awayFromGrave.z = 0f;
            if (awayFromGrave.sqrMagnitude < 0.0001f)
            {
                awayFromGrave = anchor - funeral.Corpse.transform.position;
                awayFromGrave.z = 0f;
            }

            if (awayFromGrave.sqrMagnitude < 0.0001f)
            {
                awayFromGrave = Vector3.down;
            }

            Vector3 corpseWorld = anchor + awayFromGrave.normalized * CorpseDragDistance;
            funeral.Corpse.SetDraggedWorld(anchor, corpseWorld, CorpseMaxDragDistance);
        }

        private static StrategyResidentAgent GetFirstLiveCarrier(IReadOnlyList<StrategyResidentAgent> carriers)
        {
            if (carriers == null)
            {
                return null;
            }

            for (int i = 0; i < carriers.Count; i++)
            {
                if (carriers[i] != null)
                {
                    return carriers[i];
                }
            }

            return null;
        }

        private void CompleteFuneralWithoutGrave(FuneralProcess funeral)
        {
            ReleaseReservedGrave(funeral);
            funeral.Corpse.CompleteBurial();
            EndFuneralDuties(funeral.Participants);
            EndFuneralDuties(funeral.Carriers);
            funeral.Completed = true;
        }

        private void ReleaseReservedGrave(FuneralProcess funeral)
        {
            if (funeral == null || !funeral.HasReservedGrave || cemetery == null)
            {
                return;
            }

            cemetery.ReleaseGraveReservation(funeral.GraveCell);
            funeral.HasReservedGrave = false;
        }

        private static void FilterLiveResidents(List<StrategyResidentAgent> residents)
        {
            for (int i = residents.Count - 1; i >= 0; i--)
            {
                if (residents[i] == null)
                {
                    residents.RemoveAt(i);
                }
            }
        }

        private static bool AreResidentsNear(
            IReadOnlyList<StrategyResidentAgent> residents,
            Vector3 target,
            float distance)
        {
            if (residents == null || residents.Count <= 0)
            {
                return true;
            }

            float maxDistanceSqr = distance * distance;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null)
                {
                    continue;
                }

                if ((resident.transform.position - target).sqrMagnitude > maxDistanceSqr)
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountResidentsNear(
            IReadOnlyList<StrategyResidentAgent> residents,
            Vector3 target,
            float distance)
        {
            if (residents == null || residents.Count <= 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < residents.Count; i++)
            {
                if (IsResidentNear(residents[i], target, distance))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsResidentNear(StrategyResidentAgent resident, Vector3 target, float distance)
        {
            return resident != null
                && (resident.transform.position - target).sqrMagnitude <= distance * distance;
        }

        private static bool IsCorpseNearGrave(FuneralProcess funeral)
        {
            return funeral != null
                && funeral.Corpse != null
                && GetCorpseDistanceToGrave(funeral) <= CorpseBurialDistance;
        }

        private static float GetCorpseDistanceToGrave(FuneralProcess funeral)
        {
            if (funeral == null || funeral.Corpse == null)
            {
                return float.MaxValue;
            }

            return Vector3.Distance(funeral.Corpse.transform.position, funeral.GraveWorld);
        }

        private static void EndFuneralDuties(List<StrategyResidentAgent> residents)
        {
            for (int i = 0; i < residents.Count; i++)
            {
                residents[i]?.EndFuneralDuty();
            }
        }

        private static Vector3 GetRingOffset(int index, float radius)
        {
            float angle = 0.7f + index * 1.15f;
            return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.62f, 0f);
        }

        private bool TryGetGraveStandWorld(Vector2Int graveCell, int index, out Vector3 target)
        {
            if (map == null)
            {
                target = default;
                return false;
            }

            Vector2Int[] offsets =
            {
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(0, 1),
                new Vector2Int(-1, -1),
                new Vector2Int(1, -1),
                new Vector2Int(-1, 1),
                new Vector2Int(1, 1)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2Int candidate = graveCell + offsets[(index + i) % offsets.Length];
                if (map.IsCellWalkable(candidate))
                {
                    Vector3 world = map.GetCellCenterWorld(candidate.x, candidate.y);
                    Vector3 jitter = GetRingOffset(index, 0.18f);
                    target = new Vector3(world.x + jitter.x, world.y + jitter.y, -0.08f);
                    return true;
                }
            }

            for (int radius = 2; radius <= 3; radius++)
            {
                for (int i = 0; i < offsets.Length; i++)
                {
                    Vector2Int candidate = graveCell + offsets[(index + i) % offsets.Length] * radius;
                    if (!map.IsCellWalkable(candidate))
                    {
                        continue;
                    }

                    Vector3 world = map.GetCellCenterWorld(candidate.x, candidate.y);
                    target = new Vector3(world.x, world.y, -0.08f);
                    return true;
                }
            }

            target = default;
            return false;
        }

        private bool TryStartFuneralMoveAround(
            StrategyResidentAgent resident,
            Vector3 centerWorld,
            int preferredIndex,
            float radius,
            StrategyResidentAgent.ResidentActivity activity,
            bool silent = false)
        {
            if (resident == null)
            {
                return false;
            }

            float cellStep = map != null ? Mathf.Max(0.5f, map.CellSize) : 1f;
            for (int ring = 0; ring < 4; ring++)
            {
                float ringRadius = radius + ring * cellStep;
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    Vector3 target = centerWorld + GetRingOffset(preferredIndex + attempt, ringRadius);
                    if (resident.TryStartFuneralMove(target, activity, silent, false))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryStartFuneralMoveToGrave(
            StrategyResidentAgent resident,
            Vector2Int graveCell,
            int preferredIndex,
            StrategyResidentAgent.ResidentActivity activity,
            bool silent = false)
        {
            if (resident == null)
            {
                return false;
            }

            for (int attempt = 0; attempt < 8; attempt++)
            {
                if (TryGetGraveStandWorld(graveCell, preferredIndex + attempt, out Vector3 target)
                    && resident.TryStartFuneralMove(target, activity, silent, false))
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureCorpseRoot()
        {
            if (corpseRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("Corpses");
            root.transform.SetParent(transform, false);
            corpseRoot = root.transform;
        }
    }
}
