using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        public void AssignFisherWorkplace(StrategyFisherHut hut)
        {
            if (hut == null
                || fisherWorkplace == hut
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
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
            fisherWorkplace = hut;
            fishingWorkCooldown = Random.Range(0.45f, 2.0f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentFisherWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("hutOrigin", hut.Origin));
        }

        public void ClearFisherWorkplace(StrategyFisherHut hut)
        {
            if (this == null)
            {
                return;
            }

            if (hut != null && fisherWorkplace != hut)
            {
                return;
            }

            StrategyFisherHut previousWorkplace = fisherWorkplace;
            CancelFisherWork(true);
            fisherWorkplace = null;
            StrategyDebugLogger.Info(
                "Population",
                "ResidentFisherWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("hutOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }

        public void AssignStorageWorkplace(StrategyStorageYard yard)
        {
            if (yard == null
                || storageWorkplace == yard
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
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
            CancelGranaryWork(true);
            storageWorkplace = yard;
            logisticsWorkCooldown = Random.Range(0.35f, 1.45f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentStorageWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("yardOrigin", yard.Origin));
        }

        public void ClearStorageWorkplace(StrategyStorageYard yard)
        {
            if (this == null)
            {
                return;
            }

            if (yard != null && storageWorkplace != yard)
            {
                return;
            }

            StrategyStorageYard previousWorkplace = storageWorkplace;
            CancelStorageWork(true);
            storageWorkplace = null;
            StrategyDebugLogger.Info(
                "Population",
                "ResidentStorageWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("yardOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }

        public void AssignGranaryWorkplace(StrategyGranary granary)
        {
            if (granary == null
                || granaryWorkplace == granary
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
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
            granaryWorkplace = granary;
            logisticsWorkCooldown = Random.Range(0.35f, 1.45f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentGranaryWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("granaryOrigin", granary.Origin));
        }

        public void ClearGranaryWorkplace(StrategyGranary granary)
        {
            if (this == null)
            {
                return;
            }

            if (granary != null && granaryWorkplace != granary)
            {
                return;
            }

            StrategyGranary previousWorkplace = granaryWorkplace;
            CancelGranaryWork(true);
            granaryWorkplace = null;
            StrategyDebugLogger.Info(
                "Population",
                "ResidentGranaryWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("granaryOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }

        public void AssignBuilderWorkplace(StrategyStorageYard yard)
        {
            if (yard == null
                || builderWorkplace == yard
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || storageWorkplace != null
                || granaryWorkplace != null
                || constructionSite != null
                || !CanAcceptWorkAssignment)
            {
                return;
            }

            CancelLumberWork();
            CancelStoneWork();
            CancelHunterWork(true);
            CancelStorageWork(true);
            CancelGranaryWork(true);
            builderWorkplace = yard;
            idleOrigin = yard.Origin;
            idleFootprint = new Vector2Int(3, 2);
            waitTimer = Random.Range(0.20f, 0.90f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentBuilderWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("yardOrigin", yard.Origin));
        }

        public void ClearBuilderWorkplace(StrategyStorageYard yard)
        {
            if (this == null)
            {
                return;
            }

            if (yard != null && builderWorkplace != yard)
            {
                return;
            }

            StrategyStorageYard previousWorkplace = builderWorkplace;
            ClearConstructionSite(null);
            builderWorkplace = null;
            if (home != null)
            {
                idleOrigin = home.Origin;
                idleFootprint = home.Footprint;
            }

            StrategyDebugLogger.Info(
                "Population",
                "ResidentBuilderWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("yardOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }
    }
}
