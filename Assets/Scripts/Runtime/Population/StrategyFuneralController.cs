using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyFuneralController : MonoBehaviour
    {
        private const int MaxFamilyParticipants = 24;
        private const int RequiredCarrierCount = 2;
        private const float GatherTimeoutSeconds = 32f;
        private const float ProcessionTimeoutSeconds = 58f;
        private const float BurialGatherTimeoutSeconds = 34f;
        private const float MourningSeconds = 5.2f;
        private const float BurialSeconds = 4.5f;
        private const float ArrivalDistance = 1.35f;
        private const float CorpseBurialDistance = 2.25f;
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
            if (funeral.Participants.Count <= 0)
            {
                PrepareServiceBurial(funeral, "no_family_participants");
                return;
            }

            if (funeral.Timer <= 0f
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
            if (!cemetery.TryReserveGraveCell(reachableCells, GetRequiredCarrierCount(funeral), out Vector2Int graveCell, out Vector3 graveWorld))
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
            bool serviceBurial = IsServiceBurial(funeral);
            int requiredCarrierCount = GetRequiredCarrierCount(funeral);
            for (int i = 0; i < funeral.Carriers.Count; i++)
            {
                StrategyResidentAgent carrier = funeral.Carriers[i];
                if (carrier == null)
                {
                    continue;
                }

                if (TryStartFuneralMoveToGrave(
                    carrier,
                    funeral.GraveCell,
                    i,
                    StrategyResidentAgent.ResidentActivity.CarryingCorpseToCemetery,
                    serviceBurial))
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

                if (TryStartFuneralMoveToGrave(
                    participant,
                    funeral.GraveCell,
                    mournerIndex + requiredCarrierCount,
                    StrategyResidentAgent.ResidentActivity.MovingToBurial))
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
                StrategyDebugLogger.F("serviceBurial", serviceBurial),
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

            if (!carriersArrived || !IsCorpseNearGrave(funeral))
            {
                StrategyDebugLogger.Warn(
                    "Funeral",
                    "FuneralProcessionDeliveryFailed",
                    StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                    StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                    StrategyDebugLogger.F("carriersArrived", carriersArrived),
                    StrategyDebugLogger.F("corpseDistance", GetCorpseDistanceToGrave(funeral)),
                    StrategyDebugLogger.F("graveCell", funeral.GraveCell));

                funeral.Stage = FuneralStage.Mourning;
                funeral.Timer = CarrierRetrySeconds;
                funeral.Dispatched = false;
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

            if (!IsCorpseNearGrave(funeral))
            {
                StrategyDebugLogger.Warn(
                    "Funeral",
                    "BurialDelayed",
                    StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                    StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                    StrategyDebugLogger.F("reason", "corpse_not_delivered"),
                    StrategyDebugLogger.F("corpseDistance", GetCorpseDistanceToGrave(funeral)),
                    StrategyDebugLogger.F("graveCell", funeral.GraveCell));

                funeral.Stage = FuneralStage.Procession;
                funeral.Timer = CarrierRetrySeconds;
                return;
            }

            BeginBurial(funeral, allArrived ? "family_gathered" : "gather_timeout");
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

            StrategyDebugLogger.Info(
                "Funeral",
                "BurialStarted",
                StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("arrived", CountResidentsNear(funeral.ExpectedBurialAttendees, funeral.GraveWorld, ArrivalDistance)),
                StrategyDebugLogger.F("expectedBurialAttendees", funeral.ExpectedBurialAttendees.Count),
                StrategyDebugLogger.F("graveCell", funeral.GraveCell));
        }
    }
}
