using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private bool HasStorageHaulerRole => storageWorkplace != null || granaryWorkplace != null || settlementHaulerRole;
        private bool HasBuilderWorkRole => builderWorkplace != null || settlementBuilderRole;

        public bool AssignSettlementHaulerRole()
        {
            if (!CanAcceptWorkAssignment || HasWorkplace || HasConstructionAssignment)
            {
                return false;
            }

            settlementHaulerRole = true;
            logisticsWorkCooldown = Random.Range(0.35f, 1.45f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentSettlementHaulerAssigned",
                StrategyDebugLogger.F("resident", FullName));
            return true;
        }

        public void ClearSettlementHaulerRole(bool allowCarriedResourceReturn = true)
        {
            if (!settlementHaulerRole)
            {
                return;
            }

            StrategyStorageYard previousStorage = storageWorkplace;
            ClearConstructionSite(null, allowCarriedResourceReturn);
            CancelStorageWork(allowCarriedResourceReturn);
            CancelGranaryWork(allowCarriedResourceReturn);
            settlementHaulerRole = false;
            storageWorkplace = null;
            ResetSettlementRoleIdleOrigin();
            StrategyDebugLogger.Info(
                "Population",
                "ResidentSettlementHaulerCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("yardOrigin", previousStorage != null ? previousStorage.Origin : Vector2Int.zero));
        }

        public bool AssignSettlementBuilderRole()
        {
            if (!CanAcceptWorkAssignment || HasWorkplace || HasConstructionAssignment)
            {
                return false;
            }

            settlementBuilderRole = true;
            waitTimer = Random.Range(0.20f, 0.90f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentSettlementBuilderAssigned",
                StrategyDebugLogger.F("resident", FullName));
            return true;
        }

        public void ClearSettlementBuilderRole(bool allowCarriedResourceReturn = true)
        {
            if (!settlementBuilderRole)
            {
                return;
            }

            ClearConstructionSite(null, allowCarriedResourceReturn);
            settlementBuilderRole = false;
            ResetSettlementRoleIdleOrigin();
            StrategyDebugLogger.Info(
                "Population",
                "ResidentSettlementBuilderCleared",
                StrategyDebugLogger.F("resident", FullName));
        }

        public bool TryReleaseExternalWorkAssignment()
        {
            if (!CanAcceptWorkAssignment || IsHouseholder || HasConstructionAssignment)
            {
                return false;
            }

            StrategyLumberjackCamp previousLumber = workplace;
            StrategyStonecutterCamp previousStone = stoneWorkplace;
            StrategyHunterCamp previousHunter = hunterWorkplace;
            StrategyFisherHut previousFisher = fisherWorkplace;
            StrategyForagerCamp previousForager = foragerWorkplace;
            StrategyMine previousMine = mineWorkplace;
            StrategyCoalPit previousCoal = coalPitWorkplace;
            StrategyClayPit previousClay = clayPitWorkplace;
            StrategySawmill previousSawmill = sawmillWorkplace;
            StrategyKiln previousKiln = kilnWorkplace;
            StrategyForge previousForge = forgeWorkplace;
            StrategyStorageYard previousStorage = storageWorkplace;
            StrategyStorageYard previousBuilder = builderWorkplace;
            StrategyGranary previousGranary = granaryWorkplace;
            StrategyScoutLodge previousScout = scoutWorkplace;

            previousLumber?.UnassignWorker(this);
            previousStone?.UnassignWorker(this);
            previousHunter?.UnassignWorker(this);
            previousFisher?.UnassignWorker(this);
            previousForager?.UnassignWorker(this);
            previousMine?.UnassignWorker(this);
            previousCoal?.UnassignWorker(this);
            previousClay?.UnassignWorker(this);
            previousSawmill?.UnassignWorker(this);
            previousKiln?.UnassignWorker(this);
            previousForge?.UnassignWorker(this);
            previousStorage?.UnassignWorker(this);
            previousBuilder?.UnassignBuilder(this);
            previousGranary?.UnassignWorker(this);
            previousScout?.UnassignWorker(this);
            ClearSettlementHaulerRole();
            ClearSettlementBuilderRole();

            bool released = !HasExternalWorkplace;
            StrategyDebugLogger.Info(
                "Population",
                released ? "ResidentWorkReleasedForReassignment" : "ResidentWorkReleaseFailed",
                StrategyDebugLogger.F("resident", FullName));
            return released;
        }

        private bool TrySelectStorageHaulerYard()
        {
            if (storageWorkplace != null)
            {
                if (!settlementHaulerRole || CanReachBuildingForReservation(storageWorkplace))
                {
                    return true;
                }

                storageWorkplace = null;
            }

            return settlementHaulerRole
                && StrategyStorageYard.TryFindNearestReachableStorageYard(
                    transform.position,
                    this,
                    out storageWorkplace);
        }

        private bool CanStartHaulerConstructionDelivery()
        {
            return constructionSite == null && HasStorageHaulerRole;
        }

        private void ResetSettlementRoleIdleOrigin()
        {
            if (home != null)
            {
                idleOrigin = home.Origin;
                idleFootprint = home.Footprint;
            }
        }
    }
}
