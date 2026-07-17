using System;
using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySaveSystem
    {
        private static bool ValidateScoutLodges(
            StrategySaveData data,
            out string reason)
        {
            Dictionary<string, int> buildingTools = new(StringComparer.Ordinal);
            for (int i = 0; i < data.buildings.Count; i++)
            {
                StrategyBuildingSaveData building = data.buildings[i];
                if (building != null && !string.IsNullOrWhiteSpace(building.stableId))
                {
                    buildingTools[building.stableId] = building.tool;
                }
            }

            Dictionary<int, int> residentLifeStages = new();
            for (int i = 0; i < data.residents.Count; i++)
            {
                StrategyResidentSaveData resident = data.residents[i];
                if (resident != null && resident.residentId > 0)
                {
                    residentLifeStages[resident.residentId] = resident.lifeStage;
                }
            }

            HashSet<string> lodgeIds = new(StringComparer.Ordinal);
            HashSet<int> assignedResidentIds = new();
            for (int i = 0; i < data.scoutLodges.Count; i++)
            {
                StrategyScoutLodgeSaveData lodge = data.scoutLodges[i];
                if (lodge == null
                    || string.IsNullOrWhiteSpace(lodge.lodgeStableId)
                    || !lodgeIds.Add(lodge.lodgeStableId)
                    || !buildingTools.TryGetValue(lodge.lodgeStableId, out int tool)
                    || tool != (int)StrategyBuildTool.ScoutLodge)
                {
                    reason = "invalid_scout_lodge_reference_" + i;
                    return false;
                }

                if (!Enum.IsDefined(typeof(StrategyScoutExpeditionState), lodge.expeditionState)
                    || lodge.residentId < 0
                    || lodge.residentId > 0
                    && (!residentLifeStages.TryGetValue(lodge.residentId, out int lifeStage)
                        || lifeStage != (int)StrategyResidentLifeStage.Adult
                        || !assignedResidentIds.Add(lodge.residentId)))
                {
                    reason = "invalid_scout_lodge_resident_or_state_" + i;
                    return false;
                }

                StrategyScoutExpeditionState state =
                    (StrategyScoutExpeditionState)lodge.expeditionState;
                bool active = state != StrategyScoutExpeditionState.Ready;
                float expectedDuration = active
                    ? StrategyScoutExpeditionPolicy.GetDurationSeconds(lodge.plannedDays)
                    : 0f;
                if (active && lodge.residentId <= 0
                    || lodge.plannedDays < 0
                    || lodge.plannedDays > StrategyScoutExpeditionPolicy.MaximumDays
                    || active && lodge.plannedDays < StrategyScoutExpeditionPolicy.MinimumDays
                    || !active && lodge.plannedDays != 0
                    || !IsFinite(lodge.startedElapsedSeconds)
                    || lodge.startedElapsedSeconds < 0f
                    || !IsFinite(lodge.endsElapsedSeconds)
                    || lodge.endsElapsedSeconds < lodge.startedElapsedSeconds
                    || active && Math.Abs(
                        lodge.endsElapsedSeconds
                            - lodge.startedElapsedSeconds
                            - expectedDuration) > 0.05f
                    || !active && (lodge.startedElapsedSeconds > 0.01f
                        || lodge.endsElapsedSeconds > 0.01f)
                    || !IsFinite(lodge.remainingFieldRations)
                    || lodge.remainingFieldRations < 0f
                    || lodge.remainingFieldRations > lodge.plannedDays + 0.01f
                    || state == StrategyScoutExpeditionState.Returning
                    && lodge.remainingFieldRations > 0.01f
                    || !IsFinite(lodge.provisionRationCredit)
                    || lodge.provisionRationCredit < 0f
                    || lodge.provisionRationCredit > 10f
                    || lodge.lastProvisionedDayIndex < -1
                    || !active && lodge.lastProvisionedDayIndex != -1)
                {
                    reason = "invalid_scout_lodge_mission_" + i;
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }
    }
}
