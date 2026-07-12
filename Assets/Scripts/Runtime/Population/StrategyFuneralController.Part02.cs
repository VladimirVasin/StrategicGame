using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFuneralController
    {
        private const int ServiceCarrierNearestPool = 4;

        private void RecallFamilyForFuneral(FuneralProcess funeral, string reason)
        {
            if (funeral == null || funeral.Corpse == null || population == null)
            {
                return;
            }

            List<StrategyResidentAgent> participants = population.CollectFuneralParticipants(funeral.Snapshot, MaxFamilyParticipants);
            funeral.Participants.Clear();

            Vector3 corpseWorld = funeral.Corpse.transform.position;
            int started = 0;
            for (int i = 0; i < participants.Count; i++)
            {
                StrategyResidentAgent participant = participants[i];
                if (participant == null
                    || participant.IsFuneralDutyActive
                    || participant.ResidentId == funeral.Snapshot.ResidentId)
                {
                    continue;
                }

                if (TryStartFuneralMoveAround(
                    participant,
                    corpseWorld,
                    i,
                    0.76f,
                    StrategyResidentAgent.ResidentActivity.MovingToFuneral))
                {
                    funeral.Participants.Add(participant);
                    started++;
                }
                else
                {
                    StrategyDebugLogger.Warn(
                        "Funeral",
                        "FamilyFuneralPathRejected",
                        StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                        StrategyDebugLogger.F("participant", participant.FullName),
                        StrategyDebugLogger.F("corpseWorld", corpseWorld));
                }
            }

            funeral.Dispatched = true;
            StrategyDebugLogger.Info(
                "Funeral",
                "FamilyRecalledForFuneral",
                StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("participants", participants.Count),
                StrategyDebugLogger.F("reachableParticipants", funeral.Participants.Count),
                StrategyDebugLogger.F("started", started));
        }

        private void PrepareServiceBurial(FuneralProcess funeral, string reason)
        {
            funeral.Stage = FuneralStage.Mourning;
            funeral.Timer = 0f;
            StrategyDebugLogger.Info(
                "Funeral",
                "ServiceBurialPrepared",
                StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                StrategyDebugLogger.F("reason", reason));
        }

        private bool IsServiceBurial(FuneralProcess funeral)
        {
            return funeral != null && funeral.Participants.Count <= 0;
        }

        private void AssignNightFuneralTorchBearer(FuneralProcess funeral)
        {
            if (funeral == null
                || (!funeral.StartedAtNight && !IsNightFuneralTorchTime())
                || funeral.ExpectedBurialAttendees.Count <= 0)
            {
                return;
            }

            StrategyResidentAgent torchBearer = SelectFuneralTorchBearer(funeral);
            if (torchBearer == null)
            {
                StrategyDebugLogger.Warn(
                    "Funeral",
                    "FuneralTorchBearerUnavailable",
                    StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                    StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                    StrategyDebugLogger.F("expectedBurialAttendees", funeral.ExpectedBurialAttendees.Count));
                return;
            }

            funeral.TorchBearer = torchBearer;
            bool torchLit = torchBearer.SetFuneralNightTorchActive(true);
            StrategyDebugLogger.Info(
                "Funeral",
                "FuneralTorchBearerAssigned",
                StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                StrategyDebugLogger.F("torchBearer", torchBearer.FullName),
                StrategyDebugLogger.F("torchBearerId", torchBearer.ResidentId),
                StrategyDebugLogger.F("torchLit", torchLit),
                StrategyDebugLogger.F("startedAtNight", funeral.StartedAtNight),
                StrategyDebugLogger.F("phase", StrategyDayNightCycleController.CurrentCalendarSnapshot.PhaseLabel));
        }

        private static StrategyResidentAgent SelectFuneralTorchBearer(FuneralProcess funeral)
        {
            StrategyResidentAgent fallback = null;
            for (int i = 0; i < funeral.ExpectedBurialAttendees.Count; i++)
            {
                StrategyResidentAgent attendee = funeral.ExpectedBurialAttendees[i];
                if (attendee == null)
                {
                    continue;
                }

                fallback ??= attendee;
                if (attendee.IsAdult && !funeral.Carriers.Contains(attendee))
                {
                    return attendee;
                }
            }

            for (int i = 0; i < funeral.ExpectedBurialAttendees.Count; i++)
            {
                StrategyResidentAgent attendee = funeral.ExpectedBurialAttendees[i];
                if (attendee != null && attendee.IsAdult)
                {
                    return attendee;
                }
            }

            return fallback;
        }

        private static bool IsNightFuneralTorchTime()
        {
            return StrategyDayNightCycleController.CurrentCalendarSnapshot.Phase
                == StrategyTimeOfDayPhase.Night;
        }

        private int GetRequiredCarrierCount(FuneralProcess funeral)
        {
            return IsServiceBurial(funeral) ? 1 : RequiredCarrierCount;
        }

        private bool TryAddServiceCarrier(FuneralProcess funeral)
        {
            if (TryFindServiceCarrier(funeral, out StrategyResidentAgent carrier, out int poolCount))
            {
                funeral.Carriers.Add(carrier);
                StrategyDebugLogger.Info(
                    "Funeral",
                    "ServiceBurialCarrierSelected",
                    StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                    StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId),
                    StrategyDebugLogger.F("carrier", carrier.FullName),
                    StrategyDebugLogger.F("carrierId", carrier.ResidentId),
                    StrategyDebugLogger.F("poolCount", poolCount));
                return true;
            }

            StrategyDebugLogger.Warn(
                "Funeral",
                "ServiceBurialCarrierUnavailable",
                StrategyDebugLogger.F("resident", funeral.Snapshot.FullName),
                StrategyDebugLogger.F("residentId", funeral.Snapshot.ResidentId));
            return false;
        }

        private bool TryFindServiceCarrier(
            FuneralProcess funeral,
            out StrategyResidentAgent carrier,
            out int poolCount)
        {
            carrier = null;
            poolCount = 0;
            if (funeral == null || population == null)
            {
                return false;
            }

            List<StrategyResidentAgent> nearest = new();
            List<float> distances = new();
            Vector3 corpseWorld = funeral.Corpse != null
                ? funeral.Corpse.transform.position
                : funeral.Snapshot.DeathWorld;
            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent candidate = residents[i];
                if (!IsServiceCarrierCandidate(candidate, funeral.Snapshot.ResidentId))
                {
                    continue;
                }

                InsertNearestServiceCarrier(
                    nearest,
                    distances,
                    candidate,
                    (candidate.transform.position - corpseWorld).sqrMagnitude);
            }

            poolCount = nearest.Count;
            if (nearest.Count <= 0)
            {
                return false;
            }

            carrier = nearest[Random.Range(0, nearest.Count)];
            return carrier != null;
        }

        private static bool IsServiceCarrierCandidate(StrategyResidentAgent resident, int deceasedId)
        {
            return resident != null
                && resident.CanWork
                && !resident.IsFuneralDutyActive
                && resident.ResidentId != deceasedId;
        }

        private static void InsertNearestServiceCarrier(
            List<StrategyResidentAgent> nearest,
            List<float> distances,
            StrategyResidentAgent candidate,
            float distance)
        {
            int index = distances.Count;
            for (int i = 0; i < distances.Count; i++)
            {
                if (distance < distances[i])
                {
                    index = i;
                    break;
                }
            }

            nearest.Insert(index, candidate);
            distances.Insert(index, distance);
            if (nearest.Count > ServiceCarrierNearestPool)
            {
                nearest.RemoveAt(nearest.Count - 1);
                distances.RemoveAt(distances.Count - 1);
            }
        }
    }
}
