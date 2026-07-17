using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public readonly struct StrategyFirstNightRatShotPlan
    {
        public StrategyFirstNightRatShotPlan(
            StrategyResidentAgent resident,
            Vector2Int residentCell,
            Vector2Int startCell,
            Vector2Int passCell,
            Vector2Int endCell,
            Vector3 residentWorld,
            Vector3 startWorld,
            Vector3 passWorld,
            Vector3 endWorld,
            Vector3 focusCenter,
            float focusOrthographicSize,
            int ratVariant,
            bool requiresResidentStaging,
            bool usedFallbackCorridor)
        {
            Resident = resident;
            ResidentCell = residentCell;
            StartCell = startCell;
            PassCell = passCell;
            EndCell = endCell;
            ResidentWorld = residentWorld;
            StartWorld = startWorld;
            PassWorld = passWorld;
            EndWorld = endWorld;
            FocusCenter = focusCenter;
            FocusOrthographicSize = focusOrthographicSize;
            RatVariant = ratVariant;
            RequiresResidentStaging = requiresResidentStaging;
            UsedFallbackCorridor = usedFallbackCorridor;
        }

        public StrategyResidentAgent Resident { get; }
        public Vector2Int ResidentCell { get; }
        public Vector2Int StartCell { get; }
        public Vector2Int PassCell { get; }
        public Vector2Int EndCell { get; }
        public Vector3 ResidentWorld { get; }
        public Vector3 StartWorld { get; }
        public Vector3 PassWorld { get; }
        public Vector3 EndWorld { get; }
        public Vector3 FocusCenter { get; }
        public float FocusOrthographicSize { get; }
        public int RatVariant { get; }
        public bool RequiresResidentStaging { get; }
        public bool UsedFallbackCorridor { get; }
    }

    public static class StrategyFirstNightRatShotPlanner
    {
        public const float LetterboxAspect = 2.39f;
        public const float MinimumFocusSize = 4.8f;
        public const float MaximumFocusSize = 7.5f;

        private const int MaximumFallbackRadius = 6;
        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.left,
            Vector2Int.down
        };

        public static bool TryCreate(
            StrategyPopulationController population,
            CityMapController map,
            float viewportAspect,
            out StrategyFirstNightRatShotPlan plan)
        {
            plan = default;
            if (population == null || map == null)
            {
                return false;
            }

            Vector2Int campCell = population.TryGetCampCell(out Vector2Int camp)
                ? camp
                : new Vector2Int(map.Width / 2, map.Height / 2);
            return TryCreate(
                population.Residents,
                campCell,
                map.ActiveSeed,
                map.CellSize,
                viewportAspect,
                resident => resident != null
                    && resident.IsAdult
                    && resident.CanParticipateInCinematic,
                resident => resident != null
                    && resident.IsAdult
                    && resident.CanBeTemporarilyRevealedForCinematic,
                resident => map.TryWorldToCell(resident.transform.position, out Vector2Int cell)
                    ? (Vector2Int?)cell
                    : null,
                resident => resident.transform.position,
                resident => resident.ResidentId,
                map.IsCellWalkable,
                cell => map.GetCellCenterWorld(cell.x, cell.y),
                out plan);
        }

        internal static bool TryCreate(
            IReadOnlyList<StrategyResidentAgent> residents,
            Vector2Int campCell,
            int seed,
            float cellSize,
            float viewportAspect,
            Func<StrategyResidentAgent, bool> canParticipate,
            Func<StrategyResidentAgent, bool> canReveal,
            Func<StrategyResidentAgent, Vector2Int?> resolveCell,
            Func<StrategyResidentAgent, Vector3> resolveWorld,
            Func<StrategyResidentAgent, int> resolveStableId,
            Func<Vector2Int, bool> isWalkable,
            Func<Vector2Int, Vector3> getWorld,
            out StrategyFirstNightRatShotPlan plan)
        {
            plan = default;
            if (residents == null
                || canParticipate == null
                || canReveal == null
                || resolveCell == null
                || resolveWorld == null
                || resolveStableId == null
                || isWalkable == null
                || getWorld == null)
            {
                return false;
            }

            List<Candidate> candidates = new();
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null)
                {
                    continue;
                }

                Vector2Int? cell = resolveCell(resident);
                int stableId = resolveStableId(resident);
                if (canParticipate(resident))
                {
                    candidates.Add(new Candidate(resident, cell, stableId, 0, campCell, seed));
                }

                if (canReveal(resident))
                {
                    candidates.Add(new Candidate(resident, cell, stableId, 1, campCell, seed));
                }
            }

            candidates.Sort(Candidate.Compare);
            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate candidate = candidates[i];
                Vector2Int anchor = candidate.Tier == 0 && candidate.Cell.HasValue
                    ? candidate.Cell.Value
                    : campCell;
                int searchRadius = candidate.Tier == 0 ? 1 : MaximumFallbackRadius;
                int corridorSeed = StableHash(seed, candidate.StableId, anchor.x, anchor.y);
                if (!TryFindCorridor(
                        anchor,
                        searchRadius,
                        corridorSeed,
                        isWalkable,
                        out Corridor corridor))
                {
                    continue;
                }

                bool stageResident = candidate.Tier != 0;
                Vector2Int residentCell = stageResident || !candidate.Cell.HasValue
                    ? corridor.Pass
                    : candidate.Cell.Value;
                Vector3 passWorld = getWorld(corridor.Pass);
                Vector3 residentWorld = stageResident
                    ? OffsetResidentFromPath(passWorld, corridor.Direction, cellSize, corridorSeed)
                    : resolveWorld(candidate.Resident);
                float ratZ = residentWorld.z - 0.002f;
                Vector3 startWorld = WithZ(getWorld(corridor.Start), ratZ);
                passWorld = WithZ(passWorld, ratZ);
                Vector3 endWorld = WithZ(getWorld(corridor.End), ratZ);
                ComputeFocus(
                    residentWorld,
                    startWorld,
                    passWorld,
                    endWorld,
                    cellSize,
                    viewportAspect,
                    out Vector3 focusCenter,
                    out float focusSize);
                int ratVariant = PositiveModulo(
                    StableHash(seed, candidate.StableId, corridor.Start.x, corridor.End.y),
                    StrategyCinematicRatSpriteFactory.VariantCount);
                plan = new StrategyFirstNightRatShotPlan(
                    candidate.Resident,
                    residentCell,
                    corridor.Start,
                    corridor.Pass,
                    corridor.End,
                    residentWorld,
                    startWorld,
                    passWorld,
                    endWorld,
                    focusCenter,
                    focusSize,
                    ratVariant,
                    stageResident,
                    stageResident || corridor.Pass != anchor);
                return true;
            }

            return false;
        }

        private static bool TryFindCorridor(
            Vector2Int anchor,
            int searchRadius,
            int seed,
            Func<Vector2Int, bool> isWalkable,
            out Corridor corridor)
        {
            int directionStart = PositiveModulo(seed, Directions.Length);
            int lengthStart = PositiveModulo(seed / Directions.Length, 3);
            for (int radius = 0; radius <= searchRadius; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (radius > 0 && Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int pass = anchor + new Vector2Int(x, y);
                        for (int directionOffset = 0; directionOffset < Directions.Length; directionOffset++)
                        {
                            Vector2Int direction = Directions[(directionStart + directionOffset) % Directions.Length];
                            for (int lengthOffset = 0; lengthOffset < 3; lengthOffset++)
                            {
                                int length = 4 + (lengthStart + lengthOffset) % 3;
                                int startDistance = length / 2;
                                int endDistance = length - startDistance;
                                Vector2Int start = pass - direction * startDistance;
                                Vector2Int end = pass + direction * endDistance;
                                if (IsWalkableLine(start, direction, length, isWalkable))
                                {
                                    corridor = new Corridor(start, pass, end, direction);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            corridor = default;
            return false;
        }

        private static bool IsWalkableLine(
            Vector2Int start,
            Vector2Int direction,
            int length,
            Func<Vector2Int, bool> isWalkable)
        {
            for (int step = 0; step <= length; step++)
            {
                if (!isWalkable(start + direction * step))
                {
                    return false;
                }
            }

            return true;
        }

        private static void ComputeFocus(
            Vector3 resident,
            Vector3 start,
            Vector3 pass,
            Vector3 end,
            float cellSize,
            float viewportAspect,
            out Vector3 center,
            out float orthographicSize)
        {
            float padding = Mathf.Max(0.5f, cellSize);
            float minX = Mathf.Min(resident.x, start.x, pass.x, end.x) - padding;
            float maxX = Mathf.Max(resident.x, start.x, pass.x, end.x) + padding;
            float minY = Mathf.Min(resident.y, start.y, pass.y, end.y) - padding * 0.8f;
            float maxY = Mathf.Max(resident.y, start.y, pass.y, end.y) + padding * 1.8f;
            center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, resident.z);

            float safeAspect = Mathf.Max(0.5f, viewportAspect);
            float apertureHeightFraction = Mathf.Min(1f, safeAspect / LetterboxAspect);
            float compositionAspect = Mathf.Min(safeAspect, LetterboxAspect);
            float widthSize = (maxX - minX) / (2f * compositionAspect);
            float heightSize = (maxY - minY) / (2f * apertureHeightFraction);
            orthographicSize = Mathf.Clamp(
                Mathf.Max(widthSize, heightSize),
                MinimumFocusSize,
                MaximumFocusSize);
        }

        private static Vector3 OffsetResidentFromPath(
            Vector3 passWorld,
            Vector2Int direction,
            float cellSize,
            int seed)
        {
            Vector2Int perpendicular = new(-direction.y, direction.x);
            float side = (seed & 1) == 0 ? 1f : -1f;
            Vector3 offset = new(
                perpendicular.x * cellSize * 0.32f * side,
                perpendicular.y * cellSize * 0.32f * side,
                0f);
            return passWorld + offset;
        }

        private static Vector3 WithZ(Vector3 value, float z)
        {
            value.z = z;
            return value;
        }

        private static int StableHash(int a, int b, int c, int d)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + a;
                hash = hash * 31 + b;
                hash = hash * 31 + c;
                return hash * 31 + d;
            }
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int remainder = value % modulus;
            return remainder < 0 ? remainder + modulus : remainder;
        }

        private readonly struct Corridor
        {
            public Corridor(Vector2Int start, Vector2Int pass, Vector2Int end, Vector2Int direction)
            {
                Start = start;
                Pass = pass;
                End = end;
                Direction = direction;
            }

            public Vector2Int Start { get; }
            public Vector2Int Pass { get; }
            public Vector2Int End { get; }
            public Vector2Int Direction { get; }
        }

        private readonly struct Candidate
        {
            public Candidate(
                StrategyResidentAgent resident,
                Vector2Int? cell,
                int stableId,
                int tier,
                Vector2Int camp,
                int seed)
            {
                Resident = resident;
                Cell = cell;
                StableId = stableId;
                Tier = tier;
                long x = cell.HasValue ? cell.Value.x - (long)camp.x : long.MaxValue / 4;
                long y = cell.HasValue ? cell.Value.y - (long)camp.y : 0;
                DistanceToCamp = cell.HasValue ? x * x + y * y : long.MaxValue;
                TieBreak = unchecked((uint)StableHash(seed, stableId, cell?.x ?? 0, cell?.y ?? 0));
            }

            public StrategyResidentAgent Resident { get; }
            public Vector2Int? Cell { get; }
            public int StableId { get; }
            public int Tier { get; }
            private long DistanceToCamp { get; }
            private uint TieBreak { get; }

            public static int Compare(Candidate left, Candidate right)
            {
                int result = left.Tier.CompareTo(right.Tier);
                if (result == 0)
                {
                    result = left.DistanceToCamp.CompareTo(right.DistanceToCamp);
                }

                if (result == 0)
                {
                    result = left.TieBreak.CompareTo(right.TieBreak);
                }

                return result == 0 ? left.StableId.CompareTo(right.StableId) : result;
            }
        }
    }
}
