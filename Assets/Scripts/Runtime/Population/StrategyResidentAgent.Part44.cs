using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float ClayPitWorkSecondsMin = 6.2f;
        private const float ClayPitWorkSecondsMax = 10.4f;

        private StrategyClayPit clayPitWorkplace;
        private StrategyClayPit activeClayPit;
        private StrategyClayPit activeClaySource;
        private SpriteRenderer carriedClayRenderer;
        private float clayWorkCooldown;
        private float clayWorkTimer;
        private float clayPitWorkEffectTimer;
        private readonly List<Vector2Int> clayPitEntranceCandidates = new();

        public StrategyClayPit ClayPitWorkplace => clayPitWorkplace;

        public void AssignClayPitWorkplace(StrategyClayPit pit)
        {
            if (pit == null
                || clayPitWorkplace == pit
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || mineWorkplace != null
                || coalPitWorkplace != null
                || sawmillWorkplace != null
                || kilnWorkplace != null
                || forgeWorkplace != null
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
            CancelCoalPitWork();
            CancelSawmillWork(true);
            CancelKilnWork(true);
            CancelForgeWork(true);
            CancelStorageWork(true);
            CancelGranaryWork(true);
            clayPitWorkplace = pit;
            clayWorkCooldown = Random.Range(0.45f, 2.0f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentClayPitWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("pitOrigin", pit.Origin));
        }

        public void ClearClayPitWorkplace(StrategyClayPit pit)
        {
            if (this == null)
            {
                return;
            }

            if (pit != null && clayPitWorkplace != pit)
            {
                return;
            }

            StrategyClayPit previous = clayPitWorkplace;
            CancelClayPitWork();
            clayPitWorkplace = null;
            StrategyDebugLogger.Info(
                "Population",
                "ResidentClayPitWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("pitOrigin", previous != null ? previous.Origin : Vector2Int.zero));
        }

        private bool TryStartClayPitTask()
        {
            if (activity != ResidentActivity.Idle
                || clayPitWorkplace == null
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || mineWorkplace != null
                || coalPitWorkplace != null
                || sawmillWorkplace != null
                || kilnWorkplace != null
                || forgeWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || !CanWork
                || clayWorkCooldown > 0f
                || !clayPitWorkplace.HasStorageSpace)
            {
                return false;
            }

            if (!TryBuildPathToClayPitEntrance(clayPitWorkplace, out Vector2Int entranceCell, out int checkedCells))
            {
                clayWorkCooldown = Random.Range(2.0f, 4.0f);
                StrategyDebugLogger.Warn(
                    "Clay",
                    "ClayPitEntryRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("pitOrigin", clayPitWorkplace.Origin),
                    StrategyDebugLogger.F("entranceCell", entranceCell),
                    StrategyDebugLogger.F("checkedCells", checkedCells),
                    StrategyDebugLogger.F("reason", "no_entrance_path"));
                return false;
            }

            activeClayPit = clayPitWorkplace;
            activity = ResidentActivity.MovingToClayPit;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            StrategyDebugLogger.Info(
                "Clay",
                "ClayPitEntryStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("pitOrigin", activeClayPit.Origin),
                StrategyDebugLogger.F("entranceCell", entranceCell));
            return true;
        }

        private bool TryBuildPathToClayPitEntrance(StrategyClayPit pit, out Vector2Int entranceCell, out int checkedCells)
        {
            entranceCell = default;
            checkedCells = 0;
            clayPitEntranceCandidates.Clear();
            if (pit == null || !pit.TryCollectEntranceCells(clayPitEntranceCandidates))
            {
                return false;
            }

            SortClayPitEntranceCandidatesByDistance();
            entranceCell = clayPitEntranceCandidates[0];
            for (int i = 0; i < clayPitEntranceCandidates.Count; i++)
            {
                Vector2Int candidate = clayPitEntranceCandidates[i];
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

        private void SortClayPitEntranceCandidatesByDistance()
        {
            if (map == null || clayPitEntranceCandidates.Count <= 1)
            {
                return;
            }

            Vector3 residentWorld = transform.position;
            clayPitEntranceCandidates.Sort((left, right) =>
            {
                float leftDistance = (map.GetCellCenterWorld(left.x, left.y) - residentWorld).sqrMagnitude;
                float rightDistance = (map.GetCellCenterWorld(right.x, right.y) - residentWorld).sqrMagnitude;
                return leftDistance.CompareTo(rightDistance);
            });
        }

        private void StartDiggingClayInPit()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (activeClayPit == null)
            {
                ResetClayPitWorkToIdle();
                return;
            }

            activity = ResidentActivity.DiggingClayInPit;
            clayWorkTimer = GetUpgradedWorkDuration(ClayPitWorkSecondsMin, ClayPitWorkSecondsMax, activeClayPit);
            transform.position = activeClayPit.GetInteriorWorkWorld(this);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWorkSprite = false;
            appliedWorkFrame = -1;
            workFrame = Mathf.Abs(ResidentId) % StrategyResidentSpriteFactory.CoalMineFrameCount;
            workFrameTimer = Random.Range(0f, 0.7f);
            SetWorldPresenceVisible(true);
            FaceWorldPoint(activeClayPit.FootprintBounds.center + Vector3.right * 0.20f);
            ResetClayPitWorkEffectTimer(true);
            StrategyDebugLogger.Info(
                "Clay",
                "ClayPitWorkStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("pitOrigin", activeClayPit.Origin));
        }

        private void UpdateDiggingClayInPit()
        {
            if (activeClayPit == null || clayPitWorkplace == null || clayPitWorkplace != activeClayPit)
            {
                ResetClayPitWorkToIdle();
                return;
            }

            transform.position = activeClayPit.GetInteriorWorkWorld(this);
            FaceWorldPoint(activeClayPit.FootprintBounds.center);
            AnimateClayPitDiggingWork();
            UpdateClayPitWorkEffects();
            clayWorkTimer -= Time.deltaTime;
            if (clayWorkTimer > 0f)
            {
                return;
            }

            if (activeClayPit.TryMineClay(1, out int amount))
            {
                activeClayPit.AddClay(amount);
                StrategyDebugLogger.Info(
                    "Clay",
                    "ClayDugInPit",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("pitOrigin", activeClayPit.Origin),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("pitStock", activeClayPit.ClayStored));
                if (!StrategyDayNightCycleController.IsSettlementWorkTime)
                {
                    ResetClayPitWorkToIdle();
                    return;
                }

                clayWorkTimer = GetUpgradedWorkDuration(ClayPitWorkSecondsMin, ClayPitWorkSecondsMax, activeClayPit);
                return;
            }

            ResetClayPitWorkToIdle();
        }

        private void AnimateClayPitDiggingWork()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;
            if (!usingWorkSprite)
            {
                usingWorkSprite = true;
                workFrame = Mathf.Abs(ResidentId) % StrategyResidentSpriteFactory.CoalMineFrameCount;
                workFrameTimer = 0f;
                appliedWorkFrame = -1;
            }

            workFrameTimer += Time.deltaTime * CoalMineAnimationFrameRate;
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                workFrame = (workFrame + frameSteps) % StrategyResidentSpriteFactory.CoalMineFrameCount;
                workFrameTimer -= frameSteps;
            }

            Sprite sprite = StrategyResidentSpriteFactory.GetCoalMineSprite(gender, VisualVariant, workFrame);
            if (appliedWorkFrame != workFrame || spriteRenderer.sprite != sprite)
            {
                spriteRenderer.sprite = sprite;
                appliedWorkFrame = workFrame;
                usingWorkSprite = true;
                SyncReadabilityRenderers();
            }
        }

        private void ResetClayPitWorkToIdle()
        {
            activeClayPit = null;
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            clayWorkCooldown = Random.Range(2.0f, 4.5f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void CancelClayPitWork()
        {
            if (this == null)
            {
                return;
            }

            activeClayPit = null;
            if (activity == ResidentActivity.MovingToClayPit || activity == ResidentActivity.DiggingClayInPit)
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

        private bool TryStartStorageClayPickup()
        {
            if (storageWorkplace == null || !storageWorkplace.TryReserveClaySource(this, out StrategyClayPit source))
            {
                return false;
            }

            if (!TryBuildPathToBuildingAccess(source, out Vector2Int pickupCell))
            {
                source.ReleaseStoredClayReservation(this);
                if (WasLastPathBuildDeferred)
                {
                    logisticsWorkCooldown = Random.Range(0.18f, 0.38f);
                    return false;
                }

                logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                return false;
            }

            activeClaySource = source;
            activity = ResidentActivity.MovingToStorageClayPickup;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            return true;
        }

        private void StartPickingUpStorageClay()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (activeClaySource == null || storageWorkplace == null)
            {
                ResetStorageWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpStorageClay;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            FaceWorldPoint(activeClaySource.FootprintBounds.center);
        }

        private void UpdatePickingUpStorageClay()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if (activeClaySource == null
                || storageWorkplace == null
                || !TryBuildPathToBuildingAccess(storageWorkplace, out Vector2Int dropoffCell)
                || !activeClaySource.TryTakeReservedClay(this, out carriedClayAmount))
            {
                activeClaySource?.ReleaseStoredClayReservation(this);
                ResetStorageWorkToIdle();
                return;
            }

            activeClaySource = null;
            activity = ResidentActivity.CarryingClayToStorage;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedClayVisible(true);
        }

        private void StartDepositingStorageClay()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingStorageClay;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            if (storageWorkplace != null)
            {
                FaceWorldPoint(storageWorkplace.FootprintBounds.center);
            }
        }

        private void UpdateDepositingStorageClay()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.1f);
            SetCarriedClayVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedClayAmount;
            storageWorkplace?.AddResource(StrategyResourceType.Clay, depositedAmount);
            carriedClayAmount = 0;
            SetCarriedClayVisible(false);
            CompleteStorageDelivery();
        }
    }
}
