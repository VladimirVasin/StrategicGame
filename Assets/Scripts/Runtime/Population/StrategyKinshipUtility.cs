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

            Dictionary<int, int> firstAncestors = CollectAncestorDepths(first, population, maxDepth);
            Dictionary<int, int> secondAncestors = CollectAncestorDepths(second, population, maxDepth);
            if (secondAncestors.TryGetValue(first.ResidentId, out int secondToFirst))
            {
                return secondToFirst;
            }

            if (firstAncestors.TryGetValue(second.ResidentId, out int firstToSecond))
            {
                return firstToSecond;
            }

            int bestDegree = int.MaxValue;
            foreach (KeyValuePair<int, int> ancestor in firstAncestors)
            {
                if (!secondAncestors.TryGetValue(ancestor.Key, out int secondDepth))
                {
                    continue;
                }

                int degree = ancestor.Value + secondDepth;
                if (degree > 0 && degree <= maxDepth && degree < bestDegree)
                {
                    bestDegree = degree;
                }
            }

            return bestDegree == int.MaxValue ? -1 : bestDegree;
        }

        private static Dictionary<int, int> CollectAncestorDepths(
            StrategyResidentAgent resident,
            StrategyPopulationController population,
            int maxDepth)
        {
            Dictionary<int, int> ancestors = new();
            Queue<int> openIds = new();
            Queue<int> openDepths = new();
            EnqueueParent(resident.FatherId, 1, ancestors, openIds, openDepths, maxDepth);
            EnqueueParent(resident.MotherId, 1, ancestors, openIds, openDepths, maxDepth);

            while (openIds.Count > 0)
            {
                int currentId = openIds.Dequeue();
                int depth = openDepths.Dequeue();
                if (depth >= maxDepth)
                {
                    continue;
                }

                if (!population.TryGetFamilyRecord(currentId, out StrategyResidentFamilyRecord current) || current == null)
                {
                    continue;
                }

                EnqueueParent(current.FatherId, depth + 1, ancestors, openIds, openDepths, maxDepth);
                EnqueueParent(current.MotherId, depth + 1, ancestors, openIds, openDepths, maxDepth);
            }

            return ancestors;
        }

        private static void EnqueueParent(
            int candidateId,
            int degree,
            Dictionary<int, int> ancestors,
            Queue<int> openIds,
            Queue<int> openDepths,
            int maxDepth)
        {
            if (candidateId <= 0 || degree > maxDepth)
            {
                return;
            }

            if (ancestors.TryGetValue(candidateId, out int knownDegree) && knownDegree <= degree)
            {
                return;
            }

            ancestors[candidateId] = degree;
            openIds.Enqueue(candidateId);
            openDepths.Enqueue(degree);
        }
    }
}
