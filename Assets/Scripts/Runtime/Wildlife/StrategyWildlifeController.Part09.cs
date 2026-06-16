using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {
        public bool TryReserveWolfPrey(
            StrategyWolfAgent wolf,
            Vector2Int center,
            out StrategyRabbitAgent rabbit,
            out StrategyDeerAgent deerTarget)
        {
            rabbit = null;
            deerTarget = null;
            if (wolf == null || map == null || IsWolfUnsafeSettlementCell(center))
            {
                return false;
            }

            RemoveMissingRabbits();
            RemoveMissingDeer();
            float radiusSqr = WolfHuntRadius * WolfHuntRadius;
            int rabbitCount = CountLivingRabbitsForWolfControl();
            int rabbitSurplus = rabbitCount - WolfRabbitControlThreshold
                - CountHunterReservedRabbits()
                - CountPredatorReservedRabbits();
            if (rabbitSurplus > 0 && TryFindWolfRabbitPrey(wolf, center, radiusSqr, out rabbit))
            {
                return true;
            }

            int deerCount = CountLivingDeerForWolfControl();
            int deerSurplus = deerCount - WolfDeerControlThreshold
                - CountPredatorReservedDeer();
            if (wolf.PackMemberCount < 2)
            {
                LogWolfPreySearchSkipped(center, rabbitCount, rabbitSurplus, deerCount, deerSurplus);
                return false;
            }

            bool found = deerSurplus > 0 && TryFindWolfDeerPrey(wolf, center, radiusSqr, out deerTarget);
            if (!found)
            {
                LogWolfPreySearchSkipped(center, rabbitCount, rabbitSurplus, deerCount, deerSurplus);
            }

            return found;
        }

        private bool TryFindWolfRabbitPrey(
            StrategyWolfAgent wolf,
            Vector2Int center,
            float radiusSqr,
            out StrategyRabbitAgent rabbit)
        {
            rabbit = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < rabbits.Count; i++)
            {
                StrategyRabbitAgent candidate = rabbits[i];
                if (candidate == null
                    || !candidate.CanBeWolfPrey
                    || !candidate.TryGetCurrentCell(out Vector2Int cell)
                    || !IsLandWildlifeTravelCell(cell))
                {
                    continue;
                }

                float sqr = (cell - center).sqrMagnitude;
                if (sqr > radiusSqr || sqr >= bestScore)
                {
                    continue;
                }

                bestScore = sqr;
                rabbit = candidate;
            }

            return rabbit != null && rabbit.TryReserveForPredator(wolf);
        }

        private bool TryFindWolfDeerPrey(
            StrategyWolfAgent wolf,
            Vector2Int center,
            float radiusSqr,
            out StrategyDeerAgent deerTarget)
        {
            deerTarget = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < deer.Count; i++)
            {
                StrategyDeerAgent candidate = deer[i];
                if (candidate == null
                    || !candidate.CanBeWolfPrey
                    || !candidate.TryGetCurrentCell(out Vector2Int cell)
                    || !IsLandWildlifeTravelCell(cell))
                {
                    continue;
                }

                float sqr = (cell - center).sqrMagnitude;
                if (sqr > radiusSqr || sqr >= bestScore)
                {
                    continue;
                }

                bestScore = sqr;
                deerTarget = candidate;
            }

            return deerTarget != null && deerTarget.TryReserveForPredator(wolf);
        }

        private int CountLivingRabbitsForWolfControl()
        {
            int count = 0;
            for (int i = 0; i < rabbits.Count; i++)
            {
                StrategyRabbitAgent rabbit = rabbits[i];
                if (rabbit != null && rabbit.IsAlive && !rabbit.IsCarcass)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountLivingDeerForWolfControl()
        {
            int count = 0;
            for (int i = 0; i < deer.Count; i++)
            {
                StrategyDeerAgent agent = deer[i];
                if (agent != null && agent.IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountPredatorReservedRabbits()
        {
            int count = 0;
            for (int i = 0; i < rabbits.Count; i++)
            {
                StrategyRabbitAgent rabbit = rabbits[i];
                if (rabbit != null && rabbit.IsPredatorReserved)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountHunterReservedRabbits()
        {
            int count = 0;
            for (int i = 0; i < rabbits.Count; i++)
            {
                StrategyRabbitAgent rabbit = rabbits[i];
                if (rabbit != null && rabbit.IsHuntReserved)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountPredatorReservedDeer()
        {
            int count = 0;
            for (int i = 0; i < deer.Count; i++)
            {
                StrategyDeerAgent agent = deer[i];
                if (agent != null && agent.IsPredatorReserved)
                {
                    count++;
                }
            }

            return count;
        }

        private int GetPreferredWolfRiverSide(int pack)
        {
            if (map == null || riverRouteCells.Count <= 0)
            {
                return 0;
            }

            return pack % 2 == 0 ? -1 : 1;
        }

        private bool TryFindWolfPackCenter(
            int pack,
            HashSet<Vector2Int> usedCells,
            int preferredRiverSide,
            out Vector2Int cell)
        {
            if (TryFindWolfPackCenterOnRiverSide(pack, usedCells, preferredRiverSide, out cell))
            {
                return true;
            }

            if (preferredRiverSide == 0
                || !TryFindWolfPackCenterOnRiverSide(pack, usedCells, 0, out cell))
            {
                return false;
            }

            StrategyDebugLogger.Info(
                "Wildlife",
                "WolfPackRiverSideFallback",
                StrategyDebugLogger.F("pack", pack),
                StrategyDebugLogger.F("preferredSide", preferredRiverSide),
                StrategyDebugLogger.F("center", cell));
            return true;
        }

        private bool TryFindWolfPackCenterOnRiverSide(
            int pack,
            HashSet<Vector2Int> usedCells,
            int preferredRiverSide,
            out Vector2Int cell)
        {
            cell = default;
            float bestScore = float.MinValue;
            bool found = false;
            for (int attempt = 0; attempt < SpawnSearchAttempts; attempt++)
            {
                int x = Hash(map.ActiveSeed, pack, attempt, 1051, 1091) % map.Width;
                int y = Hash(map.ActiveSeed, pack, attempt, 1123, 1151) % map.Height;
                Vector2Int candidate = new Vector2Int(x, y);
                if (usedCells.Contains(candidate)
                    || !IsWolfPackCenterCandidate(candidate)
                    || !IsPreferredWolfRiverSide(candidate, preferredRiverSide))
                {
                    continue;
                }

                float score = GetWolfPackCenterScore(pack, candidate, usedCells);
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                cell = candidate;
                found = true;
            }

            return found;
        }

        private bool IsPreferredWolfRiverSide(Vector2Int cell, int preferredRiverSide)
        {
            if (preferredRiverSide == 0)
            {
                return true;
            }

            return TryGetRiverSide(cell, out int side) && side == preferredRiverSide;
        }

        private float GetWolfPackCenterScore(int pack, Vector2Int candidate, HashSet<Vector2Int> usedCells)
        {
            float campScore = hasCampCell
                ? Mathf.Clamp(Vector2Int.Distance(candidate, campCell) - WolfCampAvoidRadius, 0f, 28f) * 0.16f
                : 0f;
            return GetWolfTerrainScore(candidate)
                + CountWalkableNeighbors(candidate, 3) * 0.24f
                + campScore
                - GetSettlementPressure(candidate) * 4.2f
                - GetUsedCellSpacingPenalty(candidate, usedCells, 14, 0.40f)
                + Hash01(map.ActiveSeed, candidate.x, candidate.y, pack + 607) * 0.25f;
        }

        private bool TryGetRiverSide(Vector2Int cell, out int side)
        {
            side = 0;
            if (map == null || riverRouteCells.Count <= 0)
            {
                return false;
            }

            Vector2Int flow = map.RiverFlowDirection;
            bool horizontalRiver = Mathf.Abs(flow.x) >= Mathf.Abs(flow.y);
            int bestDistanceSqr = int.MaxValue;
            int bestDelta = 0;
            for (int i = 0; i < riverRouteCells.Count; i++)
            {
                Vector2Int riverCell = riverRouteCells[i];
                int dx = cell.x - riverCell.x;
                int dy = cell.y - riverCell.y;
                int sqr = (dx * dx) + (dy * dy);
                if (sqr >= bestDistanceSqr)
                {
                    continue;
                }

                bestDistanceSqr = sqr;
                bestDelta = horizontalRiver ? dy : dx;
            }

            if (bestDelta == 0)
            {
                return false;
            }

            side = bestDelta > 0 ? 1 : -1;
            return true;
        }
    }
}
