using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationController
    {
        private readonly Dictionary<int, int> unsettledRefugeeFamilyByResidentId = new();
        private readonly Dictionary<int, List<int>> unsettledRefugeeFamilyMembersByGroup = new();
        private int nextUnsettledRefugeeFamilyGroupId = 1;
        private bool movingUnsettledRefugeeFamily;

        private void TrackUnsettledRefugeeFamily(IReadOnlyList<StrategyResidentAgent> family)
        {
            if (family == null || family.Count <= 0)
            {
                return;
            }

            int groupId = nextUnsettledRefugeeFamilyGroupId++;
            List<int> memberIds = new();
            for (int i = 0; i < family.Count; i++)
            {
                StrategyResidentAgent resident = family[i];
                if (resident == null || resident.ResidentId <= 0)
                {
                    continue;
                }

                memberIds.Add(resident.ResidentId);
                unsettledRefugeeFamilyByResidentId[resident.ResidentId] = groupId;
            }

            if (memberIds.Count <= 0)
            {
                for (int i = 0; i < memberIds.Count; i++)
                {
                    unsettledRefugeeFamilyByResidentId.Remove(memberIds[i]);
                }

                return;
            }

            unsettledRefugeeFamilyMembersByGroup[groupId] = memberIds;
            StrategyDebugLogger.Info(
                "Population",
                "UnsettledRefugeeFamilyTracked",
                StrategyDebugLogger.F("groupId", groupId),
                StrategyDebugLogger.F("members", memberIds.Count));
        }

        private bool IsInUnsettledRefugeeFamily(StrategyResidentAgent resident)
        {
            return resident != null
                && resident.ResidentId > 0
                && unsettledRefugeeFamilyByResidentId.ContainsKey(resident.ResidentId);
        }

        private bool TryFindUnsettledFamilyForHouse(
            StrategyPlacedBuilding destinationHouse,
            out List<StrategyResidentAgent> family)
        {
            family = null;
            if (destinationHouse == null
                || destinationHouse.Tool != StrategyBuildTool.House
                || destinationHouse.ResidentCount > 0)
            {
                return false;
            }

            List<int> groupIds = new(unsettledRefugeeFamilyMembersByGroup.Keys);
            for (int i = 0; i < groupIds.Count; i++)
            {
                if (!TryBuildLiveUnsettledFamily(groupIds[i], out List<StrategyResidentAgent> candidateFamily))
                {
                    continue;
                }

                bool allHomeless = true;
                for (int memberIndex = 0; memberIndex < candidateFamily.Count; memberIndex++)
                {
                    if (candidateFamily[memberIndex].Home != null)
                    {
                        allHomeless = false;
                        break;
                    }
                }

                if (allHomeless && candidateFamily.Count <= destinationHouse.ResidentCapacity)
                {
                    family = candidateFamily;
                    return true;
                }
            }

            return false;
        }

        private bool TryBuildLiveUnsettledFamily(
            int groupId,
            out List<StrategyResidentAgent> family)
        {
            family = null;
            if (!unsettledRefugeeFamilyMembersByGroup.TryGetValue(groupId, out List<int> memberIds))
            {
                return false;
            }

            List<StrategyResidentAgent> members = new();
            StrategyPlacedBuilding settledHome = null;
            bool mixedHomes = false;
            for (int i = 0; i < memberIds.Count; i++)
            {
                if (!TryGetResidentById(memberIds[i], out StrategyResidentAgent resident)
                    || resident == null
                    || resident.IsPendingRefugee)
                {
                    continue;
                }

                if (resident.Home != null)
                {
                    if (settledHome == null)
                    {
                        settledHome = resident.Home;
                    }
                    else if (settledHome != resident.Home)
                    {
                        mixedHomes = true;
                    }
                }

                members.Add(resident);
            }

            if (members.Count <= 0)
            {
                ClearUnsettledRefugeeFamily(groupId, "members_missing");
                return false;
            }

            bool allSettledTogether = settledHome != null && !mixedHomes;
            for (int i = 0; i < members.Count && allSettledTogether; i++)
            {
                allSettledTogether &= members[i].Home == settledHome;
            }

            if (allSettledTogether)
            {
                ClearUnsettledRefugeeFamily(groupId, "settled_together");
                return false;
            }

            family = members;
            return true;
        }

        private bool CanMoveResidentAndHomelessDependentsToHouse(
            StrategyResidentAgent resident,
            StrategyPlacedBuilding destinationHouse)
        {
            if (!CanMoveResidentToHouse(resident, destinationHouse))
            {
                return false;
            }

            if (unsettledRefugeeFamilyByResidentId.TryGetValue(resident.ResidentId, out int groupId)
                && TryBuildLiveUnsettledFamily(groupId, out List<StrategyResidentAgent> family))
            {
                if (destinationHouse.ResidentCount > 0 && CountAdultMembers(family) > 1)
                {
                    return false;
                }

                int requiredSlots = CountUnsettledFamilyMembersNeedingMove(family, destinationHouse);
                return requiredSlots <= destinationHouse.ResidentCapacity - destinationHouse.ResidentCount;
            }

            int dependentSlots = CountHomelessDependents(resident);
            return 1 + dependentSlots <= destinationHouse.ResidentCapacity - destinationHouse.ResidentCount;
        }

        private void TryMoveUnsettledFamilyToHouse(
            StrategyResidentAgent resident,
            StrategyPlacedBuilding house,
            string reason)
        {
            if (movingUnsettledRefugeeFamily
                || resident == null
                || house == null
                || !unsettledRefugeeFamilyByResidentId.TryGetValue(resident.ResidentId, out int groupId)
                || !TryBuildLiveUnsettledFamily(groupId, out List<StrategyResidentAgent> family))
            {
                return;
            }

            int missingSlots = CountUnsettledFamilyMembersNeedingMove(family, house);
            if (missingSlots > house.ResidentCapacity - house.ResidentCount)
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "UnsettledRefugeeFamilyMoveBlocked",
                    StrategyDebugLogger.F("groupId", groupId),
                    StrategyDebugLogger.F("houseOrigin", house.Origin),
                    StrategyDebugLogger.F("missingSlots", missingSlots),
                    StrategyDebugLogger.F("freeSlots", house.ResidentCapacity - house.ResidentCount),
                    StrategyDebugLogger.F("reason", "capacity"));
                return;
            }

            int moved = 0;
            movingUnsettledRefugeeFamily = true;
            for (int i = 0; i < family.Count; i++)
            {
                StrategyResidentAgent member = family[i];
                if (member == null || member.Home == house || member.Home != null)
                {
                    continue;
                }

                if (MoveResidentToHouse(member, house))
                {
                    moved++;
                }
            }

            movingUnsettledRefugeeFamily = false;
            if (moved > 0)
            {
                StrategyDebugLogger.Info(
                    "Population",
                    "UnsettledRefugeeFamilyMovedToHouse",
                    StrategyDebugLogger.F("groupId", groupId),
                    StrategyDebugLogger.F("houseOrigin", house.Origin),
                    StrategyDebugLogger.F("moved", moved),
                    StrategyDebugLogger.F("reason", reason));
            }

            TryClearUnsettledRefugeeFamilyIfSettled(groupId, house);
        }

        private void TryClearUnsettledRefugeeFamilyIfSettled(
            StrategyResidentAgent resident,
            StrategyPlacedBuilding house)
        {
            if (resident == null
                || !unsettledRefugeeFamilyByResidentId.TryGetValue(resident.ResidentId, out int groupId))
            {
                return;
            }

            TryClearUnsettledRefugeeFamilyIfSettled(groupId, house);
        }

        private void TryClearUnsettledRefugeeFamilyIfSettled(
            int groupId,
            StrategyPlacedBuilding house)
        {
            if (house == null
                || !unsettledRefugeeFamilyMembersByGroup.TryGetValue(groupId, out List<int> memberIds))
            {
                return;
            }

            for (int i = 0; i < memberIds.Count; i++)
            {
                if (TryGetResidentById(memberIds[i], out StrategyResidentAgent member)
                    && member != null
                    && member.Home != house)
                {
                    return;
                }
            }

            ClearUnsettledRefugeeFamily(groupId, "settled");
        }

        private void ClearUnsettledRefugeeFamily(int groupId, string reason)
        {
            if (!unsettledRefugeeFamilyMembersByGroup.TryGetValue(groupId, out List<int> memberIds))
            {
                return;
            }

            for (int i = 0; i < memberIds.Count; i++)
            {
                unsettledRefugeeFamilyByResidentId.Remove(memberIds[i]);
            }

            unsettledRefugeeFamilyMembersByGroup.Remove(groupId);
            StrategyDebugLogger.Info(
                "Population",
                "UnsettledRefugeeFamilyCleared",
                StrategyDebugLogger.F("groupId", groupId),
                StrategyDebugLogger.F("reason", reason));
        }

        private bool TryRejoinHomelessChildrenWithParents(string reason)
        {
            bool movedAny = false;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent child = residents[i];
                if (child == null
                    || child.IsAdult
                    || child.IsPendingRefugee
                    || child.Home != null)
                {
                    continue;
                }

                if (!TryFindParentHomeForHomelessChild(child, out StrategyPlacedBuilding parentHome))
                {
                    continue;
                }

                if (MoveResidentToHouse(child, parentHome))
                {
                    movedAny = true;
                    StrategyDebugLogger.Info(
                        "Population",
                        "HomelessChildRejoinedParentHome",
                        StrategyDebugLogger.F("child", child.FullName),
                        StrategyDebugLogger.F("childId", child.ResidentId),
                        StrategyDebugLogger.F("homeOrigin", parentHome.Origin),
                        StrategyDebugLogger.F("reason", reason));
                }
            }

            return movedAny;
        }

        private bool TryFindParentHomeForHomelessChild(
            StrategyResidentAgent child,
            out StrategyPlacedBuilding parentHome)
        {
            parentHome = null;
            if (child == null || child.Home != null)
            {
                return false;
            }

            return TryGetParentHome(child.FatherId, child, out parentHome)
                || TryGetParentHome(child.MotherId, child, out parentHome);
        }

        private bool TryGetParentHome(
            int parentId,
            StrategyResidentAgent child,
            out StrategyPlacedBuilding parentHome)
        {
            parentHome = null;
            if (parentId <= 0
                || !TryGetResidentById(parentId, out StrategyResidentAgent parent)
                || parent == null
                || parent.Home == null
                || parent.Home.Tool != StrategyBuildTool.House
                || IsHouseBlockedByFoodShortage(parent.Home)
                || !parent.Home.CanAcceptResident(child))
            {
                return false;
            }

            parentHome = parent.Home;
            return true;
        }

        private static int CountUnsettledFamilyMembersNeedingMove(
            IReadOnlyList<StrategyResidentAgent> family,
            StrategyPlacedBuilding house)
        {
            int count = 0;
            for (int i = 0; i < family.Count; i++)
            {
                StrategyResidentAgent member = family[i];
                if (member != null && member.Home == null && member.Home != house)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountAdultMembers(IReadOnlyList<StrategyResidentAgent> family)
        {
            int count = 0;
            for (int i = 0; i < family.Count; i++)
            {
                if (family[i] != null && family[i].IsAdult)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountHomelessDependents(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent candidate = residents[i];
                if (IsHomelessDependentOfResident(candidate, resident))
                {
                    count++;
                }
            }

            return count;
        }

        private void TryMoveHomelessDependentsToHouse(
            StrategyResidentAgent resident,
            StrategyPlacedBuilding house,
            string reason)
        {
            if (resident == null || house == null || house.Tool != StrategyBuildTool.House)
            {
                return;
            }

            List<StrategyResidentAgent> dependents = new();
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent candidate = residents[i];
                if (IsHomelessDependentOfResident(candidate, resident))
                {
                    dependents.Add(candidate);
                }
            }

            int moved = 0;
            for (int i = 0; i < dependents.Count; i++)
            {
                StrategyResidentAgent dependent = dependents[i];
                if (!house.HasFreeResidentSlot)
                {
                    break;
                }

                if (MoveResidentToHouse(dependent, house))
                {
                    moved++;
                }
            }

            if (moved > 0)
            {
                StrategyDebugLogger.Info(
                    "Population",
                    "HomelessDependentsMovedToHouse",
                    StrategyDebugLogger.F("resident", resident.FullName),
                    StrategyDebugLogger.F("residentId", resident.ResidentId),
                    StrategyDebugLogger.F("houseOrigin", house.Origin),
                    StrategyDebugLogger.F("moved", moved),
                    StrategyDebugLogger.F("remaining", dependents.Count - moved),
                    StrategyDebugLogger.F("reason", reason));
            }
        }

        private static bool IsHomelessDependentOfResident(
            StrategyResidentAgent candidate,
            StrategyResidentAgent resident)
        {
            return candidate != null
                && resident != null
                && candidate != resident
                && !candidate.IsAdult
                && !candidate.IsPendingRefugee
                && candidate.Home == null
                && (candidate.FatherId == resident.ResidentId || candidate.MotherId == resident.ResidentId);
        }
    }
}
