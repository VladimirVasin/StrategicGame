using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationController
    {
        private readonly HashSet<int> adoptedOrphanIds = new();

        private bool TryAdoptOrphanedChildren()
        {
            bool adoptedAny = false;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent child = residents[i];
                if (!IsAdoptableOrphan(child)
                    || adoptedOrphanIds.Contains(child.ResidentId)
                    || !TryFindAdoptionHouse(child, out StrategyPlacedBuilding house))
                {
                    continue;
                }

                if (!CompleteAdoption(child, house))
                {
                    continue;
                }

                adoptedAny = true;
            }

            return adoptedAny;
        }

        private bool IsAdoptableOrphan(StrategyResidentAgent child)
        {
            if (child == null
                || child.IsAdult
                || child.IsPendingRefugee
                || child.ResidentId <= 0)
            {
                return false;
            }

            bool fatherAlive = child.FatherId > 0 && TryGetResidentById(child.FatherId, out _);
            bool motherAlive = child.MotherId > 0 && TryGetResidentById(child.MotherId, out _);
            return !fatherAlive && !motherAlive;
        }

        private bool TryFindAdoptionHouse(StrategyResidentAgent child, out StrategyPlacedBuilding house)
        {
            house = null;
            int bestPriority = int.MaxValue;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < houses.Count; i++)
            {
                StrategyPlacedBuilding candidate = houses[i];
                if (!CanHouseAdoptChild(candidate, child, out int priority))
                {
                    continue;
                }

                float distance = (candidate.FootprintBounds.center - child.transform.position).sqrMagnitude;
                if (house == null
                    || priority < bestPriority
                    || (priority == bestPriority && distance < bestDistance))
                {
                    house = candidate;
                    bestPriority = priority;
                    bestDistance = distance;
                }
            }

            return house != null;
        }

        private bool CanHouseAdoptChild(StrategyPlacedBuilding house, StrategyResidentAgent child, out int priority)
        {
            priority = int.MaxValue;
            if (house == null
                || house.Tool != StrategyBuildTool.House
                || !house.CanAcceptResident(child)
                || IsHouseBlockedByFoodShortage(house))
            {
                return false;
            }

            bool hasAdult = false;
            bool hasRelative = false;
            IReadOnlyList<StrategyResidentAgent> occupants = house.Residents;
            for (int i = 0; i < occupants.Count; i++)
            {
                StrategyResidentAgent occupant = occupants[i];
                if (occupant == null || occupant == child || !occupant.IsAdult)
                {
                    continue;
                }

                hasAdult = true;
                int degree = StrategyKinshipUtility.GetKinshipDegree(
                    child,
                    occupant,
                    this,
                    StrategyKinshipUtility.CloseRelativeDegree);
                hasRelative |= degree > 0;
            }

            if (!hasAdult)
            {
                return false;
            }

            priority = house == child.Home ? 0 : hasRelative ? 1 : 2;
            return true;
        }

        private bool CompleteAdoption(StrategyResidentAgent child, StrategyPlacedBuilding house)
        {
            StrategyPlacedBuilding previousHome = child.Home;
            if (previousHome != house && !MoveResidentToHouse(child, house))
            {
                return false;
            }

            adoptedOrphanIds.Add(child.ResidentId);
            ConfigureHousehold(house);
            if (previousHome != null && previousHome != house)
            {
                ConfigureHousehold(previousHome);
            }

            string household = house.Householder != null
                ? house.Householder.FamilyName
                : "household";
            StrategyEventLogHudController.Notify(
                "Adopted: " + child.FullName + " joined " + household,
                new Color(0.76f, 0.68f, 0.95f));
            StrategyDebugLogger.Info(
                "Population",
                "ChildAdopted",
                StrategyDebugLogger.F("child", child.FullName),
                StrategyDebugLogger.F("childId", child.ResidentId),
                StrategyDebugLogger.F("fromHome", previousHome != null ? previousHome.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("toHome", house.Origin),
                StrategyDebugLogger.F("household", household));
            return true;
        }
    }
}
