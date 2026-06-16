using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float MineWorkSecondsMin = 4.8f;
        private const float MineWorkSecondsMax = 7.6f;

        public void AssignMineWorkplace(StrategyMine mine)
        {
            if (mine == null
                || mineWorkplace == mine
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
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
            CancelStorageWork(true);
            CancelGranaryWork(true);
            mineWorkplace = mine;
            mineWorkCooldown = Random.Range(0.45f, 2.0f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentMineWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("mineOrigin", mine.Origin));
        }

        public void ClearMineWorkplace(StrategyMine mine)
        {
            if (this == null)
            {
                return;
            }

            if (mine != null && mineWorkplace != mine)
            {
                return;
            }

            StrategyMine previousWorkplace = mineWorkplace;
            CancelMineWork();
            mineWorkplace = null;
            StrategyDebugLogger.Info(
                "Population",
                "ResidentMineWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("mineOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }

        private bool TryStartMineTask()
        {
            if (activity != ResidentActivity.Idle
                || mineWorkplace == null
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || !CanWork
                || mineWorkCooldown > 0f)
            {
                return false;
            }

            if (!mineWorkplace.TryReserveIronDeposit(this, out StrategyIronDeposit deposit))
            {
                mineWorkCooldown = Random.Range(3.0f, 6.0f);
                return false;
            }

            if (!mineWorkplace.TryFindEntranceCell(out Vector2Int entranceCell)
                || !TryBuildPathTo(entranceCell))
            {
                deposit.Release(this);
                mineWorkCooldown = Random.Range(2.0f, 4.0f);
                StrategyDebugLogger.Warn(
                    "Mining",
                    "MineEntryRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("mineOrigin", mineWorkplace.Origin),
                    StrategyDebugLogger.F("reason", "no_entrance_path"));
                return false;
            }

            activeMine = mineWorkplace;
            activeIronDeposit = deposit;
            activity = ResidentActivity.MovingToMine;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            StrategyDebugLogger.Info(
                "Mining",
                "MineEntryStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("mineOrigin", activeMine.Origin),
                StrategyDebugLogger.F("depositCell", deposit.Cell),
                StrategyDebugLogger.F("entranceCell", entranceCell));
            return true;
        }

        private void StartMiningUnderground()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (activeMine == null || !EnsureActiveIronDeposit())
            {
                ResetMineWorkToIdle();
                return;
            }

            activity = ResidentActivity.MiningUnderground;
            mineWorkTimer = Random.Range(MineWorkSecondsMin, MineWorkSecondsMax);
            transform.position = GetMineInteriorWorld();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            SetCarriedIronVisible(false);
            SetWorldPresenceVisible(false);
            hiddenUnderground = true;
            ResetMineWorkEffectTimer(true);
            footstepAudio?.ResetStepPhase();
            StrategyDebugLogger.Info(
                "Mining",
                "MinerWentUnderground",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("mineOrigin", activeMine.Origin));
        }

        private void UpdateMiningUnderground()
        {
            if (activeMine == null || mineWorkplace == null || mineWorkplace != activeMine)
            {
                ResetMineWorkToIdle();
                return;
            }

            transform.position = GetMineInteriorWorld();
            footstepAudio?.ResetStepPhase();
            UpdateMineUndergroundEffects();
            if (!hiddenUnderground)
            {
                SetWorldPresenceVisible(false);
                hiddenUnderground = true;
            }

            if (!EnsureActiveIronDeposit())
            {
                ResetMineWorkToIdle();
                return;
            }

            if (!activeMine.HasStorageSpace)
            {
                ResetMineWorkToIdle();
                return;
            }

            mineWorkTimer -= Time.deltaTime;
            if (mineWorkTimer > 0f)
            {
                return;
            }

            if (activeIronDeposit.TryMine(this, 1, out int amount))
            {
                activeMine.AddIron(amount);
                StrategyDebugLogger.Info(
                    "Mining",
                    "IronMinedUnderground",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("mineOrigin", activeMine.Origin),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("mineStock", activeMine.IronStored));
            }

            if (activeIronDeposit == null || activeIronDeposit.IsDepleted)
            {
                activeIronDeposit = null;
            }

            mineWorkTimer = Random.Range(MineWorkSecondsMin, MineWorkSecondsMax);
        }

        private bool EnsureActiveIronDeposit()
        {
            if (activeIronDeposit != null && !activeIronDeposit.IsDepleted)
            {
                return true;
            }

            if (activeIronDeposit != null)
            {
                activeIronDeposit.Release(this);
                activeIronDeposit = null;
            }

            return activeMine != null && activeMine.TryReserveIronDeposit(this, out activeIronDeposit);
        }

        private Vector3 GetMineInteriorWorld()
        {
            StrategyMine mine = activeMine != null ? activeMine : mineWorkplace;
            if (mine == null)
            {
                return transform.position;
            }

            Bounds bounds = mine.FootprintBounds;
            return new Vector3(bounds.center.x, bounds.center.y, -0.09f);
        }

        private void ExitUndergroundAtMineEntrance()
        {
            StrategyMine mine = activeMine != null ? activeMine : mineWorkplace;
            if (mine != null
                && map != null
                && mine.TryFindEntranceCell(out Vector2Int entranceCell))
            {
                Vector3 world = map.GetCellCenterWorld(entranceCell.x, entranceCell.y);
                transform.position = new Vector3(world.x, world.y, transform.position.z);
            }

            hiddenUnderground = false;
            SetWorldPresenceVisible(true);
        }

        private void ResetMineWorkToIdle()
        {
            if (activeIronDeposit != null)
            {
                activeIronDeposit.Release(this);
            }

            if (hiddenUnderground)
            {
                ExitUndergroundAtMineEntrance();
            }

            activeIronDeposit = null;
            activeMine = null;
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            mineWorkCooldown = Random.Range(2.0f, 4.5f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void CancelMineWork()
        {
            if (this == null)
            {
                return;
            }

            if (activeIronDeposit != null)
            {
                activeIronDeposit.Release(this);
            }

            activeIronDeposit = null;
            activeMine = null;
            if (activity == ResidentActivity.MovingToMine || activity == ResidentActivity.MiningUnderground)
            {
                if (hiddenUnderground)
                {
                    ExitUndergroundAtMineEntrance();
                }

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

        private bool TryStartStorageIronPickup()
        {
            if (storageWorkplace == null || !storageWorkplace.TryReserveIronSource(this, out StrategyMine source))
            {
                return false;
            }

            if (!source.TryFindDropoffCell(out Vector2Int pickupCell) || !TryBuildPathTo(pickupCell))
            {
                source.ReleaseStoredIronReservation(this);
                logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "PickupMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", source.Origin),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Iron),
                    StrategyDebugLogger.F("reason", "no_pickup_path"));
                return false;
            }

            activeIronSource = source;
            activity = ResidentActivity.MovingToStorageIronPickup;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            StrategyDebugLogger.Info(
                "Logistics",
                "PickupMoveStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("sourceOrigin", source.Origin),
                StrategyDebugLogger.F("resource", StrategyResourceType.Iron),
                StrategyDebugLogger.F("pickupCell", pickupCell),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
            return true;
        }

        private void StartPickingUpStorageIron()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (activeIronSource == null || storageWorkplace == null)
            {
                ResetStorageWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpStorageIron;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            FaceWorldPoint(activeIronSource.FootprintBounds.center);
        }

        private void UpdatePickingUpStorageIron()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if (activeIronSource == null
                || storageWorkplace == null
                || !storageWorkplace.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell)
                || !activeIronSource.TryTakeReservedIron(this, out carriedIronAmount))
            {
                activeIronSource?.ReleaseStoredIronReservation(this);
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "IronPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "pickup_failed"));
                ResetStorageWorkToIdle();
                return;
            }

            Vector2Int sourceOrigin = activeIronSource.Origin;
            activeIronSource = null;
            activity = ResidentActivity.CarryingIronToStorage;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedIronVisible(true);
            StrategyDebugLogger.Info(
                "Logistics",
                "IronPickedUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedIronAmount),
                StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
        }

        private void StartDepositingStorageIron()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingStorageIron;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            if (storageWorkplace != null)
            {
                FaceWorldPoint(storageWorkplace.FootprintBounds.center);
            }
        }

        private void UpdateDepositingStorageIron()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.1f);
            SetCarriedIronVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedIronAmount;
            storageWorkplace?.AddResource(StrategyResourceType.Iron, depositedAmount);
            carriedIronAmount = 0;
            SetCarriedIronVisible(false);
            StrategyDebugLogger.Info(
                "Logistics",
                "IronDelivered",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("yardOrigin", storageWorkplace != null ? storageWorkplace.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("yardStock", storageWorkplace != null ? storageWorkplace.IronStored : -1));
            CompleteStorageDelivery();
        }
    }
}
