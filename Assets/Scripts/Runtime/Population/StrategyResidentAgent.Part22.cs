using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float CoalPitWorkSecondsMin = 4.2f;
        private const float CoalPitWorkSecondsMax = 6.8f;

        private StrategyCoalPit coalPitWorkplace;
        private StrategyCoalPit activeCoalPit;
        private StrategyCoalPit activeCoalSource;
        private SpriteRenderer carriedCoalRenderer;
        private int carriedCoalAmount;
        private float coalWorkCooldown;
        private float coalWorkTimer;
        private readonly List<Vector2Int> coalPitEntranceCandidates = new();

        public StrategyCoalPit CoalPitWorkplace => coalPitWorkplace;

        public void AssignCoalPitWorkplace(StrategyCoalPit pit)
        {
            if (pit == null
                || coalPitWorkplace == pit
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || mineWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || constructionSite != null
                || !CanAcceptWorkAssignment)
            {
                return;
            }

            CancelLumberWork();
            CancelStoneWork();
            CancelHunterWork(true);
            CancelFisherWork(true);
            CancelMineWork();
            CancelStorageWork(true);
            CancelGranaryWork(true);
            coalPitWorkplace = pit;
            coalWorkCooldown = Random.Range(0.45f, 2.0f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentCoalPitWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("pitOrigin", pit.Origin));
        }

        public void ClearCoalPitWorkplace(StrategyCoalPit pit)
        {
            if (this == null)
            {
                return;
            }

            if (pit != null && coalPitWorkplace != pit)
            {
                return;
            }

            StrategyCoalPit previousWorkplace = coalPitWorkplace;
            CancelCoalPitWork();
            coalPitWorkplace = null;
            StrategyDebugLogger.Info(
                "Population",
                "ResidentCoalPitWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("pitOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }

        private bool TryStartCoalPitTask()
        {
            if (activity != ResidentActivity.Idle
                || coalPitWorkplace == null
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || mineWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || !CanWork
                || coalWorkCooldown > 0f
                || !coalPitWorkplace.HasStorageSpace)
            {
                return false;
            }

            if (!TryBuildPathToCoalPitEntrance(coalPitWorkplace, out Vector2Int entranceCell, out int checkedEntranceCells))
            {
                Vector2Int startCell = Vector2Int.zero;
                bool hasStartCell = map != null && map.TryWorldToCell(transform.position, out startCell);
                coalWorkCooldown = Random.Range(2.0f, 4.0f);
                StrategyDebugLogger.Warn(
                    "Coal",
                    "CoalPitEntryRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("pitOrigin", coalPitWorkplace.Origin),
                    StrategyDebugLogger.F("startCell", hasStartCell ? startCell : Vector2Int.zero),
                    StrategyDebugLogger.F("startWalkable", hasStartCell && map != null && map.IsCellWalkable(startCell)),
                    StrategyDebugLogger.F("entranceCell", entranceCell),
                    StrategyDebugLogger.F("entranceWalkable", map != null && map.IsCellWalkable(entranceCell)),
                    StrategyDebugLogger.F("checkedCells", checkedEntranceCells),
                    StrategyDebugLogger.F("reason", "no_entrance_path"));
                return false;
            }

            activeCoalPit = coalPitWorkplace;
            activity = ResidentActivity.MovingToCoalPit;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            StrategyDebugLogger.Info(
                "Coal",
                "CoalPitEntryStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("pitOrigin", activeCoalPit.Origin),
                StrategyDebugLogger.F("entranceCell", entranceCell));
            return true;
        }

        private bool TryBuildPathToCoalPitEntrance(
            StrategyCoalPit pit,
            out Vector2Int entranceCell,
            out int checkedCells)
        {
            entranceCell = default;
            checkedCells = 0;
            coalPitEntranceCandidates.Clear();
            if (pit == null || !pit.TryCollectEntranceCells(coalPitEntranceCandidates))
            {
                return false;
            }

            SortCoalPitEntranceCandidatesByDistance();
            entranceCell = coalPitEntranceCandidates[0];
            for (int i = 0; i < coalPitEntranceCandidates.Count; i++)
            {
                Vector2Int candidate = coalPitEntranceCandidates[i];
                checkedCells++;
                if (TryBuildPathTo(candidate))
                {
                    entranceCell = candidate;
                    return true;
                }

                path.Clear();
                pathIndex = 0;
            }

            return false;
        }

        private void SortCoalPitEntranceCandidatesByDistance()
        {
            if (map == null || coalPitEntranceCandidates.Count <= 1)
            {
                return;
            }

            Vector3 residentWorld = transform.position;
            coalPitEntranceCandidates.Sort((left, right) =>
            {
                float leftDistance = (map.GetCellCenterWorld(left.x, left.y) - residentWorld).sqrMagnitude;
                float rightDistance = (map.GetCellCenterWorld(right.x, right.y) - residentWorld).sqrMagnitude;
                return leftDistance.CompareTo(rightDistance);
            });
        }

        private void StartMiningCoalInPit()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (activeCoalPit == null)
            {
                ResetCoalPitWorkToIdle();
                return;
            }

            activity = ResidentActivity.MiningCoalInPit;
            coalWorkTimer = Random.Range(CoalPitWorkSecondsMin, CoalPitWorkSecondsMax);
            transform.position = activeCoalPit.GetInteriorWorkWorld();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            SetWorldPresenceVisible(true);
            FaceWorldPoint(activeCoalPit.FootprintBounds.center + Vector3.right * 0.25f);
            StrategyDebugLogger.Info(
                "Coal",
                "CoalPitWorkStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("pitOrigin", activeCoalPit.Origin));
        }

        private void UpdateMiningCoalInPit()
        {
            if (activeCoalPit == null || coalPitWorkplace == null || coalPitWorkplace != activeCoalPit)
            {
                ResetCoalPitWorkToIdle();
                return;
            }

            transform.position = activeCoalPit.GetInteriorWorkWorld();
            AnimateLumberWork(10.6f, 4.1f);
            coalWorkTimer -= Time.deltaTime;
            if (coalWorkTimer > 0f)
            {
                return;
            }

            if (activeCoalPit.TryMineCoal(1, out int amount))
            {
                activeCoalPit.AddCoal(amount);
                StrategyDebugLogger.Info(
                    "Coal",
                    "CoalMinedInPit",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("pitOrigin", activeCoalPit.Origin),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("pitStock", activeCoalPit.CoalStored));
                coalWorkTimer = Random.Range(CoalPitWorkSecondsMin, CoalPitWorkSecondsMax);
                return;
            }

            ResetCoalPitWorkToIdle();
        }

        private void ResetCoalPitWorkToIdle()
        {
            activeCoalPit = null;
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            coalWorkCooldown = Random.Range(2.0f, 4.5f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void CancelCoalPitWork()
        {
            if (this == null)
            {
                return;
            }

            activeCoalPit = null;
            if (activity == ResidentActivity.MovingToCoalPit || activity == ResidentActivity.MiningCoalInPit)
            {
                activity = ResidentActivity.Idle;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Random.Range(0.25f, 0.75f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                UseIdleSprite();
            }
        }
    }
}
