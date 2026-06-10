using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyKinshipUtility
    {
        public const int CloseRelativeDegree = 4;

        public static bool CanFormCouple(
            StrategyResidentAgent first,
            StrategyResidentAgent second,
            StrategyPopulationController population)
        {
            return first != null
                && second != null
                && first != second
                && first.IsAdult
                && second.IsAdult
                && first.Gender != second.Gender
                && !AreCloseRelatives(first, second, population);
        }

        public static bool AreCloseRelatives(
            StrategyResidentAgent first,
            StrategyResidentAgent second,
            StrategyPopulationController population)
        {
            int degree = GetKinshipDegree(first, second, population, CloseRelativeDegree);
            return degree > 0 && degree <= CloseRelativeDegree;
        }

        public static int GetKinshipDegree(
            StrategyResidentAgent first,
            StrategyResidentAgent second,
            StrategyPopulationController population,
            int maxDepth = CloseRelativeDegree)
        {
            if (first == null || second == null || population == null)
            {
                return -1;
            }

            if (first.ResidentId <= 0 || second.ResidentId <= 0)
            {
                return -1;
            }

            if (first.ResidentId == second.ResidentId)
            {
                return 0;
            }

            Queue<int> openIds = new();
            Queue<int> openDepths = new();
            HashSet<int> visited = new();
            openIds.Enqueue(first.ResidentId);
            openDepths.Enqueue(0);
            visited.Add(first.ResidentId);

            while (openIds.Count > 0)
            {
                int currentId = openIds.Dequeue();
                int depth = openDepths.Dequeue();
                if (depth >= maxDepth)
                {
                    continue;
                }

                if (!population.TryGetResidentById(currentId, out StrategyResidentAgent current) || current == null)
                {
                    continue;
                }

                TryVisit(current.FatherId, depth + 1, second.ResidentId, visited, openIds, openDepths, out int foundDegree);
                if (foundDegree >= 0)
                {
                    return foundDegree;
                }

                TryVisit(current.MotherId, depth + 1, second.ResidentId, visited, openIds, openDepths, out foundDegree);
                if (foundDegree >= 0)
                {
                    return foundDegree;
                }

                IReadOnlyList<int> childIds = current.ChildIds;
                for (int i = 0; i < childIds.Count; i++)
                {
                    TryVisit(childIds[i], depth + 1, second.ResidentId, visited, openIds, openDepths, out foundDegree);
                    if (foundDegree >= 0)
                    {
                        return foundDegree;
                    }
                }
            }

            return -1;
        }

        private static void TryVisit(
            int candidateId,
            int degree,
            int targetId,
            HashSet<int> visited,
            Queue<int> openIds,
            Queue<int> openDepths,
            out int foundDegree)
        {
            foundDegree = -1;
            if (candidateId <= 0 || !visited.Add(candidateId))
            {
                return;
            }

            if (candidateId == targetId)
            {
                foundDegree = degree;
                return;
            }

            openIds.Enqueue(candidateId);
            openDepths.Enqueue(degree);
        }
    }
}
