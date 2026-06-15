using System.Collections.Generic;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFamilyTreeHudController
    {
        internal void SetHoveredFamilyTreeMember(int residentId, bool hovered)
        {
            if (hovered)
            {
                hoveredResidentId = residentId;
                RefreshRelationshipLabels();
                return;
            }

            if (hoveredResidentId == residentId)
            {
                hoveredResidentId = 0;
                RefreshRelationshipLabels();
            }
        }

        private void RefreshRelationshipLabels()
        {
            recordsById.TryGetValue(hoveredResidentId, out StrategyResidentFamilyRecord focus);
            foreach (KeyValuePair<int, Text> pair in relationshipLabelsById)
            {
                Text label = pair.Value;
                if (label == null || focus == null || pair.Key == hoveredResidentId)
                {
                    SetRelationshipLabel(label, string.Empty);
                    continue;
                }

                string relation = recordsById.TryGetValue(pair.Key, out StrategyResidentFamilyRecord target)
                    ? GetRelationshipLabel(focus, target)
                    : string.Empty;
                SetRelationshipLabel(label, relation);
            }
        }

        private static void SetRelationshipLabel(Text label, string value)
        {
            if (label == null)
            {
                return;
            }

            label.text = value;
            label.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
        }

        private string GetRelationshipLabel(
            StrategyResidentFamilyRecord focus,
            StrategyResidentFamilyRecord target)
        {
            if (focus == null || target == null || focus.ResidentId == target.ResidentId)
            {
                return string.Empty;
            }

            if (target.ResidentId == focus.FatherId)
            {
                return "Father";
            }

            if (target.ResidentId == focus.MotherId)
            {
                return "Mother";
            }

            if (IsParentOf(focus, target))
            {
                return target.Gender == StrategyResidentGender.Male ? "Son" : "Daughter";
            }

            if (AreCoParents(focus, target))
            {
                return target.Gender == StrategyResidentGender.Male ? "Husband" : "Wife";
            }

            if (ShareKnownParent(focus, target))
            {
                return target.Gender == StrategyResidentGender.Male ? "Brother" : "Sister";
            }

            if (IsGrandparentOf(target, focus))
            {
                return target.Gender == StrategyResidentGender.Male ? "Grandfather" : "Grandmother";
            }

            if (IsGrandparentOf(focus, target))
            {
                return target.Gender == StrategyResidentGender.Male ? "Grandson" : "Granddaughter";
            }

            if (IsAuntOrUncleOf(target, focus))
            {
                return target.Gender == StrategyResidentGender.Male ? "Uncle" : "Aunt";
            }

            if (IsAuntOrUncleOf(focus, target))
            {
                return target.Gender == StrategyResidentGender.Male ? "Nephew" : "Niece";
            }

            if (AreCousins(focus, target))
            {
                return "Cousin";
            }

            return AreConnectedKin(focus.ResidentId, target.ResidentId) ? "Kin" : string.Empty;
        }

        private bool IsParentOf(
            StrategyResidentFamilyRecord parent,
            StrategyResidentFamilyRecord child)
        {
            return parent != null
                && child != null
                && (child.FatherId == parent.ResidentId || child.MotherId == parent.ResidentId);
        }

        private bool AreCoParents(
            StrategyResidentFamilyRecord first,
            StrategyResidentFamilyRecord second)
        {
            if (first == null || second == null || first.Gender == second.Gender)
            {
                return false;
            }

            foreach (StrategyResidentFamilyRecord record in allRecords)
            {
                if ((record.FatherId == first.ResidentId && record.MotherId == second.ResidentId)
                    || (record.FatherId == second.ResidentId && record.MotherId == first.ResidentId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ShareKnownParent(
            StrategyResidentFamilyRecord first,
            StrategyResidentFamilyRecord second)
        {
            return first != null
                && second != null
                && ((first.FatherId > 0 && first.FatherId == second.FatherId)
                    || (first.MotherId > 0 && first.MotherId == second.MotherId));
        }

        private bool IsGrandparentOf(
            StrategyResidentFamilyRecord elder,
            StrategyResidentFamilyRecord younger)
        {
            return elder != null
                && younger != null
                && (IsParentIdOf(elder.ResidentId, younger.FatherId)
                    || IsParentIdOf(elder.ResidentId, younger.MotherId));
        }

        private bool IsParentIdOf(int possibleParentId, int childParentId)
        {
            return childParentId > 0
                && recordsById.TryGetValue(childParentId, out StrategyResidentFamilyRecord parent)
                && (parent.FatherId == possibleParentId || parent.MotherId == possibleParentId);
        }

        private bool IsAuntOrUncleOf(
            StrategyResidentFamilyRecord elderSide,
            StrategyResidentFamilyRecord youngerSide)
        {
            return elderSide != null
                && youngerSide != null
                && ((youngerSide.FatherId > 0
                        && recordsById.TryGetValue(youngerSide.FatherId, out StrategyResidentFamilyRecord father)
                        && ShareKnownParent(elderSide, father))
                    || (youngerSide.MotherId > 0
                        && recordsById.TryGetValue(youngerSide.MotherId, out StrategyResidentFamilyRecord mother)
                        && ShareKnownParent(elderSide, mother)));
        }

        private bool AreCousins(
            StrategyResidentFamilyRecord first,
            StrategyResidentFamilyRecord second)
        {
            return first != null
                && second != null
                && ((first.FatherId > 0 && IsAuntOrUncleOfParent(first.FatherId, second))
                    || (first.MotherId > 0 && IsAuntOrUncleOfParent(first.MotherId, second)));
        }

        private bool IsAuntOrUncleOfParent(int parentId, StrategyResidentFamilyRecord cousinSide)
        {
            return recordsById.TryGetValue(parentId, out StrategyResidentFamilyRecord parent)
                && IsAuntOrUncleOf(parent, cousinSide);
        }

        private bool AreConnectedKin(int firstId, int secondId)
        {
            HashSet<int> visited = new();
            Queue<int> queue = new();
            queue.Enqueue(firstId);
            visited.Add(firstId);

            while (queue.Count > 0)
            {
                int currentId = queue.Dequeue();
                if (currentId == secondId)
                {
                    return true;
                }

                if (!recordsById.TryGetValue(currentId, out StrategyResidentFamilyRecord current))
                {
                    continue;
                }

                EnqueueKin(current.FatherId, visited, queue);
                EnqueueKin(current.MotherId, visited, queue);
                IReadOnlyList<int> childIds = current.ChildIds;
                for (int i = 0; i < childIds.Count; i++)
                {
                    EnqueueKin(childIds[i], visited, queue);
                }
            }

            return false;
        }

        private void EnqueueKin(int residentId, HashSet<int> visited, Queue<int> queue)
        {
            if (residentId <= 0 || visited.Contains(residentId) || !recordsById.ContainsKey(residentId))
            {
                return;
            }

            visited.Add(residentId);
            queue.Enqueue(residentId);
        }

        private void SortFamilyGroupsByRelationshipAffinity()
        {
            if (familyGroups.Count <= 2)
            {
                return;
            }

            List<FamilyTreeGroup> remaining = new(familyGroups);
            List<FamilyTreeGroup> ordered = new();
            FamilyTreeGroup current = PickMostConnectedGroup(remaining);
            ordered.Add(current);
            remaining.Remove(current);

            while (remaining.Count > 0)
            {
                FamilyTreeGroup next = PickClosestGroup(current, remaining);
                ordered.Add(next);
                remaining.Remove(next);
                current = next;
            }

            familyGroups.Clear();
            familyGroups.AddRange(ordered);
        }

        private FamilyTreeGroup PickMostConnectedGroup(List<FamilyTreeGroup> groups)
        {
            FamilyTreeGroup best = groups[0];
            int bestScore = GetTotalFamilyGroupAffinity(best, groups);
            for (int i = 1; i < groups.Count; i++)
            {
                int score = GetTotalFamilyGroupAffinity(groups[i], groups);
                if (score > bestScore
                    || (score == bestScore && CompareFamilyGroups(groups[i], best) < 0))
                {
                    best = groups[i];
                    bestScore = score;
                }
            }

            return best;
        }

        private FamilyTreeGroup PickClosestGroup(FamilyTreeGroup current, List<FamilyTreeGroup> groups)
        {
            FamilyTreeGroup best = groups[0];
            int bestScore = GetFamilyGroupAffinity(current, best);
            for (int i = 1; i < groups.Count; i++)
            {
                int score = GetFamilyGroupAffinity(current, groups[i]);
                if (score > bestScore
                    || (score == bestScore && CompareFamilyGroups(groups[i], best) < 0))
                {
                    best = groups[i];
                    bestScore = score;
                }
            }

            return best;
        }

        private int GetTotalFamilyGroupAffinity(FamilyTreeGroup group, List<FamilyTreeGroup> groups)
        {
            int total = 0;
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i] != group)
                {
                    total += GetFamilyGroupAffinity(group, groups[i]);
                }
            }

            return total;
        }

        private int GetFamilyGroupAffinity(FamilyTreeGroup first, FamilyTreeGroup second)
        {
            int score = 0;
            for (int i = 0; i < first.Records.Count; i++)
            {
                StrategyResidentFamilyRecord firstRecord = first.Records[i];
                for (int j = 0; j < second.Records.Count; j++)
                {
                    StrategyResidentFamilyRecord secondRecord = second.Records[j];
                    score += GetRecordAffinity(firstRecord, secondRecord);
                }
            }

            return score;
        }

        private int GetRecordAffinity(
            StrategyResidentFamilyRecord first,
            StrategyResidentFamilyRecord second)
        {
            if (first == null || second == null)
            {
                return 0;
            }

            if (IsParentOf(first, second) || IsParentOf(second, first))
            {
                return 8;
            }

            if (AreCoParents(first, second))
            {
                return 8;
            }

            if (ShareKnownParent(first, second))
            {
                return 5;
            }

            if (IsGrandparentOf(first, second) || IsGrandparentOf(second, first))
            {
                return 3;
            }

            if (IsAuntOrUncleOf(first, second) || IsAuntOrUncleOf(second, first))
            {
                return 2;
            }

            return AreCousins(first, second) ? 1 : 0;
        }
    }
}
