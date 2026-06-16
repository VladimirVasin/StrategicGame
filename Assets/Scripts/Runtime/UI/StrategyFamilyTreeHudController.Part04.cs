using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFamilyTreeHudController
    {
        private const float FamilyCardMargin = 24f;
        private const float FamilyBlockSpacing = 54f;
        private const float ConnectionThickness = 2f;

        private sealed class FamilyLayoutBlock
        {
            public readonly List<StrategyResidentFamilyRecord> Members = new();
            public float ParentCenterX;
            public int SortSeed;

            public float Width => Members.Count * CardWidth + Mathf.Max(0, Members.Count - 1) * CardSpacing;
        }

        private readonly struct FamilyParentKey : System.IEquatable<FamilyParentKey>
        {
            public FamilyParentKey(int fatherId, int motherId)
            {
                FatherId = fatherId;
                MotherId = motherId;
            }

            public int FatherId { get; }
            public int MotherId { get; }

            public bool Equals(FamilyParentKey other)
            {
                return FatherId == other.FatherId && MotherId == other.MotherId;
            }

            public override bool Equals(object obj)
            {
                return obj is FamilyParentKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (FatherId * 397) ^ MotherId;
                }
            }
        }

        private float CalculateFamilySectionWidth(int maxRowCount)
        {
            float width = 48f + maxRowCount * CardWidth + Mathf.Max(0, maxRowCount - 1) * CardSpacing;
            foreach (KeyValuePair<int, List<StrategyResidentFamilyRecord>> pair in rowsByGeneration)
            {
                List<FamilyLayoutBlock> blocks = BuildFamilyLayoutBlocks(pair.Value, false);
                width = Mathf.Max(width, FamilyCardMargin * 2f + CalculateBlockRowWidth(blocks));
            }

            return Mathf.Max(FamilyColumnWidth, width);
        }

        private int GetMaxLayoutGeneration()
        {
            int maxGeneration = 0;
            foreach (int generation in rowsByGeneration.Keys)
            {
                maxGeneration = Mathf.Max(maxGeneration, generation);
            }

            return maxGeneration;
        }

        private void PositionGenerationCards(int generation, float sectionWidth)
        {
            if (!rowsByGeneration.TryGetValue(generation, out List<StrategyResidentFamilyRecord> row)
                || row.Count <= 0)
            {
                return;
            }

            List<FamilyLayoutBlock> blocks = BuildFamilyLayoutBlocks(row, generation > 0);
            if (generation > 0)
            {
                blocks.Sort(CompareLayoutBlocks);
                PositionBlocksNearParents(blocks, generation, sectionWidth);
                return;
            }

            PositionBlocksCentered(blocks, generation, sectionWidth);
        }

        private List<FamilyLayoutBlock> BuildFamilyLayoutBlocks(
            List<StrategyResidentFamilyRecord> row,
            bool useParentCenters)
        {
            List<FamilyLayoutBlock> blocks = new();
            bool[] used = new bool[row.Count];
            for (int i = 0; i < row.Count; i++)
            {
                if (used[i])
                {
                    continue;
                }

                int partnerIndex = FindBestCoParentIndex(row, used, i);
                FamilyLayoutBlock block = new()
                {
                    SortSeed = i
                };
                if (partnerIndex >= 0)
                {
                    AddOrderedCoParents(block, row[i], row[partnerIndex]);
                    used[partnerIndex] = true;
                }
                else
                {
                    block.Members.Add(row[i]);
                }

                used[i] = true;
                block.ParentCenterX = useParentCenters ? GetBlockParentCenterX(block, i) : i;
                blocks.Add(block);
            }

            return blocks;
        }

        private int FindBestCoParentIndex(
            List<StrategyResidentFamilyRecord> row,
            bool[] used,
            int sourceIndex)
        {
            StrategyResidentFamilyRecord source = row[sourceIndex];
            int bestIndex = -1;
            int bestSharedChildren = -1;
            for (int i = sourceIndex + 1; i < row.Count; i++)
            {
                if (used[i] || !AreCoParents(source, row[i]))
                {
                    continue;
                }

                int sharedChildren = CountSharedChildren(source, row[i]);
                if (sharedChildren > bestSharedChildren)
                {
                    bestIndex = i;
                    bestSharedChildren = sharedChildren;
                }
            }

            return bestIndex;
        }

        private static void AddOrderedCoParents(
            FamilyLayoutBlock block,
            StrategyResidentFamilyRecord first,
            StrategyResidentFamilyRecord second)
        {
            if (first.Gender == StrategyResidentGender.Male && second.Gender != StrategyResidentGender.Male)
            {
                block.Members.Add(first);
                block.Members.Add(second);
                return;
            }

            if (second.Gender == StrategyResidentGender.Male && first.Gender != StrategyResidentGender.Male)
            {
                block.Members.Add(second);
                block.Members.Add(first);
                return;
            }

            block.Members.Add(first);
            block.Members.Add(second);
        }

        private static int CountSharedChildren(
            StrategyResidentFamilyRecord first,
            StrategyResidentFamilyRecord second)
        {
            int count = 0;
            IReadOnlyList<int> firstChildren = first.ChildIds;
            for (int i = 0; i < firstChildren.Count; i++)
            {
                int childId = firstChildren[i];
                IReadOnlyList<int> secondChildren = second.ChildIds;
                for (int j = 0; j < secondChildren.Count; j++)
                {
                    if (secondChildren[j] == childId)
                    {
                        count++;
                        break;
                    }
                }
            }

            return count;
        }

        private float GetBlockParentCenterX(FamilyLayoutBlock block, int fallbackIndex)
        {
            float total = 0f;
            int count = 0;
            for (int i = 0; i < block.Members.Count; i++)
            {
                AddKnownParentCenter(block.Members[i].FatherId, ref total, ref count);
                AddKnownParentCenter(block.Members[i].MotherId, ref total, ref count);
            }

            return count > 0 ? total / count : fallbackIndex * (CardWidth + FamilyBlockSpacing);
        }

        private void AddKnownParentCenter(int parentId, ref float total, ref int count)
        {
            if (parentId <= 0 || !cardCenterById.TryGetValue(parentId, out Vector2 center))
            {
                return;
            }

            total += center.x;
            count++;
        }

        private static int CompareLayoutBlocks(FamilyLayoutBlock left, FamilyLayoutBlock right)
        {
            float delta = left.ParentCenterX - right.ParentCenterX;
            if (Mathf.Abs(delta) > 0.1f)
            {
                return delta < 0f ? -1 : 1;
            }

            return left.SortSeed.CompareTo(right.SortSeed);
        }

        private static float CalculateBlockRowWidth(List<FamilyLayoutBlock> blocks)
        {
            float width = 0f;
            for (int i = 0; i < blocks.Count; i++)
            {
                width += blocks[i].Width;
                if (i < blocks.Count - 1)
                {
                    width += FamilyBlockSpacing;
                }
            }

            return width;
        }

        private void PositionBlocksCentered(
            List<FamilyLayoutBlock> blocks,
            int generation,
            float sectionWidth)
        {
            float rowWidth = CalculateBlockRowWidth(blocks);
            float startX = Mathf.Max(FamilyCardMargin, (sectionWidth - rowWidth) * 0.5f);
            float x = startX;
            for (int i = 0; i < blocks.Count; i++)
            {
                PlaceLayoutBlock(blocks[i], generation, x);
                x += blocks[i].Width + FamilyBlockSpacing;
            }
        }

        private void PositionBlocksNearParents(
            List<FamilyLayoutBlock> blocks,
            int generation,
            float sectionWidth)
        {
            List<float> lefts = new(blocks.Count);
            float cursor = FamilyCardMargin;
            for (int i = 0; i < blocks.Count; i++)
            {
                float desiredLeft = blocks[i].ParentCenterX - blocks[i].Width * 0.5f;
                desiredLeft = Mathf.Clamp(
                    desiredLeft,
                    FamilyCardMargin,
                    sectionWidth - FamilyCardMargin - blocks[i].Width);
                float left = Mathf.Max(cursor, desiredLeft);
                lefts.Add(left);
                cursor = left + blocks[i].Width + FamilyBlockSpacing;
            }

            FitBlockRowInsideSection(blocks, lefts, sectionWidth);
            for (int i = 0; i < blocks.Count; i++)
            {
                PlaceLayoutBlock(blocks[i], generation, lefts[i]);
            }
        }

        private static void FitBlockRowInsideSection(
            List<FamilyLayoutBlock> blocks,
            List<float> lefts,
            float sectionWidth)
        {
            if (blocks.Count <= 0)
            {
                return;
            }

            float rightEdge = lefts[^1] + blocks[^1].Width;
            float overflow = rightEdge - (sectionWidth - FamilyCardMargin);
            if (overflow <= 0f)
            {
                return;
            }

            float maxShift = lefts[0] - FamilyCardMargin;
            float shift = Mathf.Min(overflow, maxShift);
            for (int i = 0; i < lefts.Count; i++)
            {
                lefts[i] -= shift;
            }
        }

        private void PlaceLayoutBlock(FamilyLayoutBlock block, int generation, float left)
        {
            float y = 76f + generation * (CardHeight + GenerationGap);
            for (int i = 0; i < block.Members.Count; i++)
            {
                StrategyResidentFamilyRecord record = block.Members[i];
                float x = left + i * (CardWidth + CardSpacing);
                cardTopLeftById[record.ResidentId] = new Vector2(x, y);
                cardCenterById[record.ResidentId] = new Vector2(x + CardWidth * 0.5f, y + CardHeight * 0.5f);
            }
        }

        private void DrawGroupedFamilyConnections(RectTransform section)
        {
            Dictionary<FamilyParentKey, List<int>> childGroups = new();
            for (int i = 0; i < familyRecords.Count; i++)
            {
                StrategyResidentFamilyRecord child = familyRecords[i];
                bool hasFather = HasVisibleFamilyCard(child.FatherId);
                bool hasMother = HasVisibleFamilyCard(child.MotherId);
                if (hasFather && hasMother)
                {
                    FamilyParentKey key = new(child.FatherId, child.MotherId);
                    if (!childGroups.TryGetValue(key, out List<int> children))
                    {
                        children = new List<int>();
                        childGroups[key] = children;
                    }

                    children.Add(child.ResidentId);
                    continue;
                }

                if (hasFather)
                {
                    DrawSingleParentConnection(section, child.FatherId, child.ResidentId);
                }
                else if (hasMother)
                {
                    DrawSingleParentConnection(section, child.MotherId, child.ResidentId);
                }
            }

            foreach (KeyValuePair<FamilyParentKey, List<int>> pair in childGroups)
            {
                DrawParentPairConnections(section, pair.Key, pair.Value);
            }
        }

        private bool HasVisibleFamilyCard(int residentId)
        {
            return residentId > 0 && familyIds.Contains(residentId) && cardCenterById.ContainsKey(residentId);
        }

        private void DrawParentPairConnections(
            RectTransform section,
            FamilyParentKey parentKey,
            List<int> children)
        {
            if (!TryGetConnectionPoint(parentKey.FatherId, out Vector2 fatherCenter, out Vector2 fatherTop)
                || !TryGetConnectionPoint(parentKey.MotherId, out Vector2 motherCenter, out Vector2 motherTop)
                || children.Count <= 0)
            {
                return;
            }

            SortConnectionChildren(children);
            float parentBottom = Mathf.Max(fatherTop.y, motherTop.y) + CardHeight;
            if (!TryGetChildConnectionRange(children, out float minChildX, out float maxChildX, out float childTopY)
                || childTopY <= parentBottom)
            {
                return;
            }

            float joinY = Mathf.Min(parentBottom + 14f, childTopY - 10f);
            float branchY = Mathf.Clamp(
                Mathf.Lerp(joinY, childTopY, 0.55f),
                joinY + 6f,
                childTopY - 6f);
            float fatherX = fatherCenter.x;
            float motherX = motherCenter.x;
            float pairLeft = Mathf.Min(fatherX, motherX);
            float pairRight = Mathf.Max(fatherX, motherX);
            float pairCenter = (fatherX + motherX) * 0.5f;
            AddLine(section, fatherX - 1f, parentBottom, ConnectionThickness, joinY - parentBottom);
            AddLine(section, motherX - 1f, parentBottom, ConnectionThickness, joinY - parentBottom);
            AddLine(section, pairLeft, joinY, pairRight - pairLeft, ConnectionThickness);
            AddLine(section, pairCenter - 1f, joinY, ConnectionThickness, branchY - joinY);

            float branchLeft = Mathf.Min(pairCenter, minChildX);
            float branchRight = Mathf.Max(pairCenter, maxChildX);
            AddLine(section, branchLeft, branchY, branchRight - branchLeft, ConnectionThickness);
            for (int i = 0; i < children.Count; i++)
            {
                if (TryGetConnectionPoint(children[i], out Vector2 childCenter, out Vector2 childTop))
                {
                    AddLine(section, childCenter.x - 1f, branchY, ConnectionThickness, childTop.y - branchY);
                }
            }
        }

        private void DrawSingleParentConnection(RectTransform section, int parentId, int childId)
        {
            if (!TryGetConnectionPoint(parentId, out Vector2 parentCenter, out Vector2 parentTop)
                || !TryGetConnectionPoint(childId, out Vector2 childCenter, out Vector2 childTop))
            {
                return;
            }

            float parentBottom = parentTop.y + CardHeight;
            if (childTop.y <= parentBottom)
            {
                return;
            }

            float branchY = Mathf.Lerp(parentBottom, childTop.y, 0.55f);
            AddLine(section, parentCenter.x - 1f, parentBottom, ConnectionThickness, branchY - parentBottom);
            AddLine(section, Mathf.Min(parentCenter.x, childCenter.x), branchY, Mathf.Abs(childCenter.x - parentCenter.x), ConnectionThickness);
            AddLine(section, childCenter.x - 1f, branchY, ConnectionThickness, childTop.y - branchY);
        }

        private bool TryGetConnectionPoint(int residentId, out Vector2 center, out Vector2 topLeft)
        {
            center = default;
            topLeft = default;
            return cardCenterById.TryGetValue(residentId, out center)
                && cardTopLeftById.TryGetValue(residentId, out topLeft);
        }

        private void SortConnectionChildren(List<int> children)
        {
            children.Sort((left, right) =>
            {
                float leftX = cardCenterById.TryGetValue(left, out Vector2 leftCenter) ? leftCenter.x : 0f;
                float rightX = cardCenterById.TryGetValue(right, out Vector2 rightCenter) ? rightCenter.x : 0f;
                return leftX.CompareTo(rightX);
            });
        }

        private bool TryGetChildConnectionRange(
            List<int> children,
            out float minX,
            out float maxX,
            out float topY)
        {
            minX = float.MaxValue;
            maxX = float.MinValue;
            topY = float.MaxValue;
            bool found = false;
            for (int i = 0; i < children.Count; i++)
            {
                if (!TryGetConnectionPoint(children[i], out Vector2 center, out Vector2 top))
                {
                    continue;
                }

                minX = Mathf.Min(minX, center.x);
                maxX = Mathf.Max(maxX, center.x);
                topY = Mathf.Min(topY, top.y);
                found = true;
            }

            return found;
        }
    }
}
