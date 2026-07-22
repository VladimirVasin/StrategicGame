using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFuneralController
    {
        private int MoveResidentsOffGraveCell(FuneralProcess funeral)
        {
            if (funeral == null || population == null || map == null)
            {
                return 0;
            }

            int moved = 0;
            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null
                    || !map.TryWorldToCell(resident.transform.position, out Vector2Int residentCell)
                    || residentCell != funeral.GraveCell
                    || !TryFindGraveClearanceCell(funeral.GraveCell, out Vector2Int targetCell))
                {
                    continue;
                }

                Vector3 targetWorld = map.GetCellCenterWorld(targetCell.x, targetCell.y);
                if (resident.TryStartFuneralMove(
                    targetWorld,
                    StrategyResidentAgent.ResidentActivity.MovingToBurial,
                    true,
                    false))
                {
                    moved++;
                }
            }

            return moved;
        }

        private bool TryFindGraveClearanceCell(Vector2Int graveCell, out Vector2Int targetCell)
        {
            for (int radius = 1; radius <= 2; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = graveCell + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate) && !IsResidentOnCell(candidate))
                        {
                            targetCell = candidate;
                            return true;
                        }
                    }
                }
            }

            targetCell = default;
            return false;
        }

        private bool IsResidentOnCell(Vector2Int cell)
        {
            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && map.TryWorldToCell(resident.transform.position, out Vector2Int residentCell)
                    && residentCell == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureNightTorchBearer(FuneralProcess funeral)
        {
            if (funeral == null
                || funeral.TorchBearer != null
                || !IsNightFuneralTorchTime()
                || funeral.ExpectedBurialAttendees.Count <= 0
                || funeral.Stage < FuneralStage.Procession
                || Time.time < funeral.NextTorchAssignmentTime)
            {
                return;
            }

            funeral.NextTorchAssignmentTime = Time.time + TorchAssignmentRetrySeconds;
            AssignNightFuneralTorchBearer(funeral);
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
    }
}
