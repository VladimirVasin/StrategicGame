using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyDeerAgent
    {
        private const int HuntGameYield = 4;
        private const int HuntButcherHitsRequired = 6;

        private object huntReservationOwner;
        private int huntButcherHits;
        private bool isCarcass;

        public bool IsCarcass => isCarcass;
        public bool IsHuntReserved => huntReservationOwner != null;
        public bool CanBeHunted => IsAdult && isAlive && !isCarcass && huntReservationOwner == null && predatorReservationOwner == null;
        public string HuntTargetKind => "Deer";
        public Vector3 HuntWorldPosition => transform.position;

        public bool TryReserveForHunt(object owner)
        {
            if (owner == null)
            {
                return false;
            }

            if (huntReservationOwner == owner)
            {
                return isAlive || isCarcass;
            }

            if (!CanBeHunted)
            {
                return false;
            }

            huntReservationOwner = owner;
            hasTarget = false;
            hasThreat = false;
            path.Clear();
            pathIndex = 0;
            stateTimer = Random.Range(0.9f, 1.6f);
            SetState(StrategyDeerBehaviorState.Alert, true, false);
            StrategyDebugLogger.Info(
                "Wildlife",
                "DeerHuntReserved",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("herd", herdId),
                StrategyDebugLogger.F("world", transform.position));
            return true;
        }

        public void ReleaseHuntReservation(object owner)
        {
            if (owner == null || huntReservationOwner != owner)
            {
                return;
            }

            huntReservationOwner = null;
            if (isAlive)
            {
                StartFleeing(transform.position + Vector3.left, true);
            }

            StrategyDebugLogger.Info(
                "Wildlife",
                "DeerHuntReservationReleased",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("herd", herdId),
                StrategyDebugLogger.F("world", transform.position));
        }

        public bool ReactToHuntMiss(object owner, Vector3 threatWorld)
        {
            if (owner == null || huntReservationOwner != owner || !isAlive || isCarcass)
            {
                return false;
            }

            huntReservationOwner = null;
            hasTarget = false;
            hasThreat = true;
            path.Clear();
            pathIndex = 0;
            lastThreatWorld = threatWorld;
            StartFleeing(threatWorld, true);
            StrategyDebugLogger.Info(
                "Wildlife",
                "DeerHuntMissFlee",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("herd", herdId),
                StrategyDebugLogger.F("world", transform.position),
                StrategyDebugLogger.F("threatWorld", threatWorld));
            return true;
        }

        public bool ReceiveArrowHit(object owner, Vector3 hitWorld)
        {
            if (owner == null || huntReservationOwner != owner || !isAlive || isCarcass)
            {
                return false;
            }

            isAlive = false;
            isCarcass = true;
            huntButcherHits = 0;
            predatorReservationOwner = null;
            hasTarget = false;
            hasThreat = false;
            path.Clear();
            pathIndex = 0;
            lastThreatWorld = hitWorld;
            state = StrategyDeerBehaviorState.Resting;
            transform.localRotation = Quaternion.Euler(0f, 0f, spriteRenderer != null && spriteRenderer.flipX ? 11f : -11f);
            SetAnimatedScale(1.04f, 0.72f);
            ApplySprite(StrategyDeerSpritePose.Rest, 0);
            StrategyDebugLogger.Info(
                "Wildlife",
                "DeerHit",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("herd", herdId),
                StrategyDebugLogger.F("world", transform.position),
                StrategyDebugLogger.F("hitWorld", hitWorld));
            return true;
        }

        public bool ReceiveButcherHit(object owner, Vector3 hitWorld, out int gameAmount)
        {
            gameAmount = 0;
            if (owner == null || huntReservationOwner != owner || isAlive || !isCarcass)
            {
                return false;
            }

            huntButcherHits++;
            FaceWorldPoint(hitWorld);
            StrategyDebugLogger.Info(
                "Wildlife",
                "DeerButcherHit",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("herd", herdId),
                StrategyDebugLogger.F("hit", huntButcherHits),
                StrategyDebugLogger.F("required", HuntButcherHitsRequired),
                StrategyDebugLogger.F("world", transform.position));
            if (huntButcherHits < HuntButcherHitsRequired)
            {
                return false;
            }

            gameAmount = HuntGameYield;
            huntReservationOwner = null;
            StrategyDebugLogger.Info(
                "Wildlife",
                "DeerButchered",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("herd", herdId),
                StrategyDebugLogger.F("yield", gameAmount),
                StrategyDebugLogger.F("world", transform.position));
            Destroy(gameObject);
            return true;
        }
    }
}
