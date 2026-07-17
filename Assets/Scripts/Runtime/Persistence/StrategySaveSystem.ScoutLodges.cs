using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySaveSystem
    {
        private void CaptureScoutLodges(StrategySaveData save)
        {
            if (save == null || placement == null)
            {
                return;
            }

            for (int i = 0; i < placement.PlacedBuildings.Count; i++)
            {
                StrategyPlacedBuilding building = placement.PlacedBuildings[i];
                StrategyScoutLodge lodge = building != null
                    ? building.GetComponent<StrategyScoutLodge>()
                    : null;
                if (lodge == null || string.IsNullOrWhiteSpace(building.StableId))
                {
                    continue;
                }

                int residentId = lodge.TryGetWorker(0, out StrategyResidentAgent worker)
                    ? worker.ResidentId
                    : 0;
                save.scoutLodges.Add(new StrategyScoutLodgeSaveData
                {
                    lodgeStableId = building.StableId,
                    residentId = residentId,
                    expeditionState = (int)lodge.ExpeditionState,
                    plannedDays = lodge.PlannedExpeditionDays,
                    startedElapsedSeconds = lodge.ExpeditionStartedElapsedSeconds,
                    endsElapsedSeconds = lodge.ExpeditionEndsElapsedSeconds,
                    remainingFieldRations = lodge.RemainingFieldRations,
                    provisionRationCredit = lodge.ProvisionRationCredit,
                    lastProvisionedDayIndex = lodge.LastProvisionedDayIndex
                });
            }
        }

        private void RestoreScoutLodges(
            IReadOnlyList<StrategyScoutLodgeSaveData> savedLodges,
            IReadOnlyDictionary<string, StrategyPlacedBuilding> buildingsById)
        {
            if (savedLodges == null || buildingsById == null || population == null)
            {
                return;
            }

            for (int i = 0; i < savedLodges.Count; i++)
            {
                StrategyScoutLodgeSaveData saved = savedLodges[i];
                if (saved == null
                    || !buildingsById.TryGetValue(
                        saved.lodgeStableId,
                        out StrategyPlacedBuilding building))
                {
                    continue;
                }

                StrategyScoutLodge lodge = building.GetComponent<StrategyScoutLodge>();
                if (lodge == null)
                {
                    continue;
                }

                StrategyResidentAgent resident = null;
                if (saved.residentId > 0)
                {
                    population.TryGetResidentById(saved.residentId, out resident);
                }

                if (!lodge.RestorePersistentState(
                    resident,
                    (StrategyScoutExpeditionState)saved.expeditionState,
                    saved.plannedDays,
                    saved.startedElapsedSeconds,
                    saved.endsElapsedSeconds,
                    saved.remainingFieldRations,
                    saved.provisionRationCredit,
                    saved.lastProvisionedDayIndex))
                {
                    StrategyDebugLogger.Warn(
                        "Save",
                        "ScoutLodgeRestoreSkipped",
                        StrategyDebugLogger.F("lodgeId", saved.lodgeStableId),
                        StrategyDebugLogger.F("residentId", saved.residentId));
                }
            }
        }
    }
}
