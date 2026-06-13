using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyFuneralController : MonoBehaviour
    {
        private const int MaxFamilyParticipants = 24;
        private const int RequiredCarrierCount = 2;
        private const float GatherTimeoutSeconds = 32f;
        private const float ProcessionTimeoutSeconds = 58f;
        private const float BurialGatherTimeoutSeconds = 34f;
        private const float MourningSeconds = 5.2f;
        private const float BurialSeconds = 4.5f;
        private const float ArrivalDistance = 1.35f;
        private const float CorpseDragDistance = 0.72f;
        private const float CorpseMaxDragDistance = 0.96f;
        private const float CarrierRetrySeconds = 6.0f;
        private static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        private readonly Queue<FuneralProcess> queuedFunerals = new();
        private CityMapController map;
        private StrategyPopulationController population;
        private StrategyCemeteryController cemetery;
        private Transform corpseRoot;
        private FuneralProcess activeFuneral;

        private enum FuneralStage
        {
            WaitingForCorpse,
            GatheringFamily,
            Mourning,
            Procession,
            GatheringAtGrave,
            Burial
        }

        private sealed class FuneralProcess
        {
            public StrategyResidentDeathSnapshot Snapshot;
            public StrategyCorpse Corpse;
            public FuneralStage Stage;
            public readonly List<StrategyResidentAgent> Participants = new();
            public readonly List<StrategyResidentAgent> Carriers = new();
            public readonly List<StrategyResidentAgent> ExpectedBurialAttendees = new();
            public StrategyResidentAgent PrimaryCarrier;
            public Vector2Int GraveCell;
            public Vector3 GraveWorld;
            public float Timer;
            public bool Dispatched;
        }

        public void Configure(CityMapController mapController, StrategyPopulationController populationController)
        {
            map = mapController;
            population = populationController;
            cemetery = GetComponent<StrategyCemeteryController>();
            if (cemetery == null)
            {
                cemetery = gameObject.AddComponent<StrategyCemeteryController>();
            }

            cemetery.Configure(map, population);
            EnsureCorpseRoot();
        }

        public void NotifyResidentDeath(StrategyResidentDeathSnapshot snapshot)
        {
            EnsureCorpseRoot();
            GameObject corpseObject = new GameObject("Corpse - " + snapshot.FullName);
            corpseObject.transform.SetParent(corpseRoot, false);
            corpseObject.transform.position = new Vector3(snapshot.DeathWorld.x, snapshot.DeathWorld.y, -0.09f);

            SpriteRenderer renderer = corpseObject.AddComponent<SpriteRenderer>();
            StrategyCorpse corpse = corpseObject.AddComponent<StrategyCorpse>();
            corpse.Configure(snapshot, renderer);

            FuneralProcess process = new FuneralProcess
            {
                Snapshot = snapshot,
                Corpse = corpse,
                Stage = FuneralStage.WaitingForCorpse
            };
            RecallFamilyForFuneral(process, "death");
            queuedFunerals.Enqueue(process);

            StrategyDebugLogger.Info(
                "Funeral",
                "FuneralQueued",
                StrategyDebugLogger.F("resident", snapshot.FullName),
                StrategyDebugLogger.F("residentId", snapshot.ResidentId),
                StrategyDebugLogger.F("queued", queuedFunerals.Count));
        }

        private void Update()
        {
            if (activeFuneral == null)
            {
                if (queuedFunerals.Count <= 0)
                {
                    return;
                }

                activeFuneral = queuedFunerals.Dequeue();
                StrategyDebugLogger.Info(
                    "Funeral",
                    "FuneralStarted",
                    StrategyDebugLogger.F("resident", activeFuneral.Snapshot.FullName),
                    StrategyDebugLogger.F("residentId", activeFuneral.Snapshot.ResidentId));
            }

            UpdateActiveFuneral(activeFuneral);
        }

        private void UpdateActiveFuneral(FuneralProcess funeral)
        {
            if (funeral == null || funeral.Corpse == null)
            {
                activeFuneral = null;
                return;
            }

            FilterLiveResidents(funeral.Participants);
            FilterLiveResidents(funeral.Carriers);
            FilterLiveResidents(funeral.ExpectedBurialAttendees);

            switch (funeral.Stage)
            {
                case FuneralStage.WaitingForCorpse:
                    UpdateWaitingForCorpse(funeral);
                    break;
                case FuneralStage.GatheringFamily:
                    UpdateGatheringFamily(funeral);
                    break;
                case FuneralStage.Mourning:
                    UpdateMourning(funeral);
                    break;
                case FuneralStage.Procession:
                    UpdateProcession(funeral);
                    break;
                case FuneralStage.GatheringAtGrave:
                    UpdateGatheringAtGrave(funeral);
                    break;
                case FuneralStage.Burial:
                    UpdateBurial(funeral);
                    break;
            }
        }

        private void UpdateWaitingForCorpse(FuneralProcess funeral)
        {
            if (!funeral.Corpse.IsDeathComplete)
            {
                return;
            }

            funeral.Stage = FuneralStage.GatheringFamily;
            funeral.Timer = GatherTimeoutSeconds;
        }

        private void UpdateGatheringFamily(FuneralProcess funeral)
        {
            if (!funeral.Dispatched)
            {
                RecallFamilyForFuneral(funeral, "gathering_retry");
            }

            funeral.Timer -= Time.deltaTime;
            if (funeral.Participants.Count <= 0
                || funeral.Timer <= 0f
                || AreResidentsNear(funeral.Participants, funeral.Corpse.transform.position, ArrivalDistance))
            {
                funeral.Stage = FuneralStage.Mourning;
                funeral.Timer = MourningSeconds;
                for (int i = 0; i < funeral.Participants.Count; i++)
                {
                    funeral.Participants[i]?.StartFuneralMourning(MourningSeconds + Random.Range(-0.45f, 0.45f));
                }

                StrategyDebugLogger.Info(
                    "Funeral",
                    "MourningStarted",
                    StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                    StrategyDebugLogger.F("participants", funeral.Participants.Count));
            }
        }

        private void RecallFamilyForFuneral(FuneralProcess funeral, string reason)
        {
            if (funeral == null || funeral.Corpse == null || population == null)
            {
                return;
            }

            funeral.Participants.Clear();
            funeral.Participants.AddRange(population.CollectFuneralParticipants(funeral.Snapshot, MaxFamilyParticipants));

            Vector3 corpseWorld = funeral.Corpse.transform.position;
            int started = 0;
            for (int i = 0; i < funeral.Participants.Count; i++)
            {
                StrategyResidentAgent participant = funeral.Participants[i];
                if (participant == null)
                {
                    continue;
                }

                Vector3 target = corpseWorld + GetRingOffset(i, 0.76f);
                if (participant.TryStartFuneralMove(target, StrategyResidentAgent.ResidentActivity.MovingToFuneral))
                {
                    started++;
                }
            }

            funeral.Dispatched = true;
            StrategyDebugLogger.Info(
                "Funeral",
                "FamilyRecalledForFuneral",
                StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("participants", funeral.Participants.Count),
                StrategyDebugLogger.F("started", started));
        }

        private void UpdateMourning(FuneralProcess funeral)
        {
            funeral.Timer -= Time.deltaTime;
            if (funeral.Timer > 0f)
            {
                return;
            }

            funeral.Carriers.Clear();
            SelectCarriers(funeral);
            if (funeral.Carriers.Count <= 0)
            {
                funeral.Timer = CarrierRetrySeconds;
                StrategyDebugLogger.Warn(
                    "Funeral",
                    "FuneralProcessionDelayed",
                    StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                    StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                    StrategyDebugLogger.F("reason", "no_carriers"));
                return;
            }

            HashSet<Vector2Int> reachableCells = BuildReachableCellsFromCarriers(funeral.Carriers);
            if (!cemetery.TryReserveGraveCell(reachableCells, RequiredCarrierCount, out Vector2Int graveCell, out Vector3 graveWorld))
            {
                StrategyDebugLogger.Warn(
                    "Funeral",
                    "GraveReservationFailed",
                    StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                    StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                    StrategyDebugLogger.F("reachableCells", reachableCells.Count));
                CompleteFuneralWithoutGrave(funeral);
                return;
            }

            funeral.GraveCell = graveCell;
            funeral.GraveWorld = new Vector3(graveWorld.x, graveWorld.y, -0.09f);
            DispatchProcession(funeral);
        }

        private void DispatchProcession(FuneralProcess funeral)
        {
            if (funeral.Carriers.Count <= 0)
            {
                funeral.Stage = FuneralStage.Mourning;
                funeral.Timer = CarrierRetrySeconds;
                StrategyDebugLogger.Warn(
                    "Funeral",
                    "FuneralProcessionDelayed",
                    StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                    StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                    StrategyDebugLogger.F("reason", "no_carriers_at_dispatch"));
                return;
            }

            List<StrategyResidentAgent> startedCarriers = new();
            funeral.ExpectedBurialAttendees.Clear();
            funeral.PrimaryCarrier = null;
            for (int i = 0; i < funeral.Carriers.Count; i++)
            {
                StrategyResidentAgent carrier = funeral.Carriers[i];
                if (carrier == null)
                {
                    continue;
                }

                Vector3 target = GetGraveStandWorld(funeral.GraveCell, i);
                if (carrier.TryStartFuneralMove(target, StrategyResidentAgent.ResidentActivity.CarryingCorpseToCemetery))
                {
                    startedCarriers.Add(carrier);
                    funeral.ExpectedBurialAttendees.Add(carrier);
                    funeral.PrimaryCarrier ??= carrier;
                }
                else
                {
                    StrategyDebugLogger.Warn(
                        "Funeral",
                        "CarrierPathRejected",
                        StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                        StrategyDebugLogger.F("carrier", carrier.FullName),
                        StrategyDebugLogger.F("graveCell", funeral.GraveCell));
                }
            }

            funeral.Carriers.Clear();
            funeral.Carriers.AddRange(startedCarriers);
            if (funeral.Carriers.Count <= 0)
            {
                funeral.Stage = FuneralStage.Mourning;
                funeral.Timer = CarrierRetrySeconds;
                StrategyDebugLogger.Warn(
                    "Funeral",
                    "FuneralProcessionDelayed",
                    StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                    StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                    StrategyDebugLogger.F("reason", "no_reachable_carrier_path"));
                return;
            }

            int mournerIndex = 0;
            for (int i = 0; i < funeral.Participants.Count; i++)
            {
                StrategyResidentAgent participant = funeral.Participants[i];
                if (participant == null || funeral.Carriers.Contains(participant))
                {
                    continue;
                }

                Vector3 target = GetGraveStandWorld(funeral.GraveCell, mournerIndex + RequiredCarrierCount);
                if (participant.TryStartFuneralMove(target, StrategyResidentAgent.ResidentActivity.MovingToBurial))
                {
                    funeral.ExpectedBurialAttendees.Add(participant);
                }
                else
                {
                    StrategyDebugLogger.Warn(
                        "Funeral",
                        "BurialParticipantPathRejected",
                        StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                        StrategyDebugLogger.F("participant", participant.FullName),
                        StrategyDebugLogger.F("graveCell", funeral.GraveCell));
                }

                mournerIndex++;
            }

            funeral.Stage = FuneralStage.Procession;
            funeral.Timer = ProcessionTimeoutSeconds;
            StrategyDebugLogger.Info(
                "Funeral",
                "FuneralProcessionStarted",
                StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                StrategyDebugLogger.F("carriers", funeral.Carriers.Count),
                StrategyDebugLogger.F("expectedBurialAttendees", funeral.ExpectedBurialAttendees.Count),
                StrategyDebugLogger.F("graveCell", funeral.GraveCell));
        }

        private void UpdateProcession(FuneralProcess funeral)
        {
            if (funeral.Carriers.Count <= 0)
            {
                funeral.Stage = FuneralStage.Mourning;
                funeral.Timer = CarrierRetrySeconds;
                StrategyDebugLogger.Warn(
                    "Funeral",
                    "FuneralProcessionPaused",
                    StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                    StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                    StrategyDebugLogger.F("reason", "no_live_carriers"));
                return;
            }

            funeral.Timer -= Time.deltaTime;
            UpdateDraggedCorpsePosition(funeral);

            bool carriersArrived = AreResidentsNear(funeral.Carriers, funeral.GraveWorld, ArrivalDistance);

            if (!carriersArrived && funeral.Timer > 0f)
            {
                return;
            }

            funeral.Stage = FuneralStage.GatheringAtGrave;
            funeral.Timer = BurialGatherTimeoutSeconds;
            UpdateDraggedCorpsePosition(funeral);

            StrategyDebugLogger.Info(
                "Funeral",
                "BurialGatherStarted",
                StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                StrategyDebugLogger.F("expectedBurialAttendees", funeral.ExpectedBurialAttendees.Count),
                StrategyDebugLogger.F("graveCell", funeral.GraveCell));
        }

        private void UpdateGatheringAtGrave(FuneralProcess funeral)
        {
            funeral.Timer -= Time.deltaTime;
            UpdateDraggedCorpsePosition(funeral);

            int arrived = CountResidentsNear(funeral.ExpectedBurialAttendees, funeral.GraveWorld, ArrivalDistance);
            bool allArrived = funeral.ExpectedBurialAttendees.Count <= 0
                || arrived >= funeral.ExpectedBurialAttendees.Count;
            if (!allArrived && funeral.Timer > 0f)
            {
                return;
            }

            BeginBurial(funeral, allArrived ? "family_gathered" : "gather_timeout");
        }

        private void BeginBurial(FuneralProcess funeral, string reason)
        {
            funeral.Corpse.SetBurialWorld(funeral.GraveWorld);
            funeral.Corpse.StartBurial();
            funeral.Stage = FuneralStage.Burial;
            funeral.Timer = BurialSeconds;

            StartBurialPoses(funeral);

            StrategyDebugLogger.Info(
                "Funeral",
                "BurialStarted",
                StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("arrived", CountResidentsNear(funeral.ExpectedBurialAttendees, funeral.GraveWorld, ArrivalDistance)),
                StrategyDebugLogger.F("expectedBurialAttendees", funeral.ExpectedBurialAttendees.Count),
                StrategyDebugLogger.F("graveCell", funeral.GraveCell));
        }

        private void UpdateBurial(FuneralProcess funeral)
        {
            funeral.Timer -= Time.deltaTime;
            float progress = 1f - Mathf.Clamp01(funeral.Timer / BurialSeconds);
            funeral.Corpse.SetBurialProgress(progress);
            if (funeral.Timer > 0f)
            {
                return;
            }

            cemetery.CreateGrave(funeral.Snapshot, funeral.GraveCell);
            funeral.Corpse.CompleteBurial();
            EndFuneralDuties(funeral.Participants);
            EndFuneralDuties(funeral.Carriers);

            StrategyDebugLogger.Info(
                "Funeral",
                "FuneralCompleted",
                StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                StrategyDebugLogger.F("graveCell", funeral.GraveCell));
            activeFuneral = null;
        }

        private void StartBurialPoses(FuneralProcess funeral)
        {
            for (int i = 0; i < funeral.ExpectedBurialAttendees.Count; i++)
            {
                StrategyResidentAgent attendee = funeral.ExpectedBurialAttendees[i];
                if (IsResidentNear(attendee, funeral.GraveWorld, ArrivalDistance))
                {
                    attendee.StartFuneralBurial(BurialSeconds + Random.Range(-0.35f, 0.35f));
                }
            }
        }

        private void SelectCarriers(FuneralProcess funeral)
        {
            for (int i = 0; i < funeral.Participants.Count && funeral.Carriers.Count < RequiredCarrierCount; i++)
            {
                StrategyResidentAgent participant = funeral.Participants[i];
                if (participant != null && participant.CanWork && !funeral.Carriers.Contains(participant))
                {
                    funeral.Carriers.Add(participant);
                }
            }

            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count && funeral.Carriers.Count < RequiredCarrierCount; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && resident.CanWork
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
            funeral.Corpse.CompleteBurial();
            EndFuneralDuties(funeral.Participants);
            EndFuneralDuties(funeral.Carriers);
            activeFuneral = null;
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

        private Vector3 GetGraveStandWorld(Vector2Int graveCell, int index)
        {
            if (map == null)
            {
                return Vector3.zero;
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
                    return new Vector3(world.x + jitter.x, world.y + jitter.y, -0.08f);
                }
            }

            Vector3 fallback = map.GetCellCenterWorld(graveCell.x, graveCell.y) + GetRingOffset(index, 1.15f);
            return new Vector3(fallback.x, fallback.y, -0.08f);
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
