using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFamilyTreeHudController
    {
        private const float CrossFamilyConnectionThickness = 2f;

        private readonly struct CrossFamilyConnectionKey : System.IEquatable<CrossFamilyConnectionKey>
        {
            public CrossFamilyConnectionKey(int firstId, int secondId, int type)
            {
                if (firstId <= secondId)
                {
                    FirstId = firstId;
                    SecondId = secondId;
                }
                else
                {
                    FirstId = secondId;
                    SecondId = firstId;
                }

                Type = type;
            }

            public int FirstId { get; }
            public int SecondId { get; }
            public int Type { get; }

            public bool Equals(CrossFamilyConnectionKey other)
            {
                return FirstId == other.FirstId && SecondId == other.SecondId && Type == other.Type;
            }

            public override bool Equals(object obj)
            {
                return obj is CrossFamilyConnectionKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = FirstId;
                    hash = (hash * 397) ^ SecondId;
                    hash = (hash * 397) ^ Type;
                    return hash;
                }
            }
        }

        private void RegisterContentCardPositions(
            List<StrategyResidentFamilyRecord> records,
            int familyGroupIndex,
            float sectionLeft,
            float sectionTop)
        {
            for (int i = 0; i < records.Count; i++)
            {
                StrategyResidentFamilyRecord record = records[i];
                if (!cardTopLeftById.TryGetValue(record.ResidentId, out Vector2 topLeft)
                    || !cardCenterById.TryGetValue(record.ResidentId, out Vector2 center))
                {
                    continue;
                }

                Vector2 sectionOffset = new(sectionLeft, sectionTop);
                contentCardTopLeftById[record.ResidentId] = topLeft + sectionOffset;
                contentCardCenterById[record.ResidentId] = center + sectionOffset;
                familyGroupByResidentId[record.ResidentId] = familyGroupIndex;
            }
        }

        private void DrawCrossFamilyConnections()
        {
            if (contentRoot == null || allRecords.Count <= 0)
            {
                return;
            }

            HashSet<CrossFamilyConnectionKey> drawn = new();
            Color parentChildColor = new(0.86f, 0.68f, 0.34f, 0.34f);
            Color coParentColor = new(1f, 0.78f, 0.38f, 0.42f);
            for (int i = 0; i < allRecords.Count; i++)
            {
                StrategyResidentFamilyRecord record = allRecords[i];
                DrawCrossFamilyParentChildConnection(record.FatherId, record.ResidentId, drawn, parentChildColor);
                DrawCrossFamilyParentChildConnection(record.MotherId, record.ResidentId, drawn, parentChildColor);
                if (record.FatherId > 0 && record.MotherId > 0)
                {
                    DrawCrossFamilyCoParentConnection(record.FatherId, record.MotherId, drawn, coParentColor);
                }
            }
        }

        private void DrawCrossFamilyParentChildConnection(
            int parentId,
            int childId,
            HashSet<CrossFamilyConnectionKey> drawn,
            Color color)
        {
            CrossFamilyConnectionKey key = new(parentId, childId, 1);
            if (!drawn.Add(key) || !CanDrawCrossFamilyConnection(parentId, childId))
            {
                return;
            }

            DrawCrossFamilyRoutedConnection(parentId, childId, color, GetConnectionLaneOffset(key));
        }

        private void DrawCrossFamilyCoParentConnection(
            int firstParentId,
            int secondParentId,
            HashSet<CrossFamilyConnectionKey> drawn,
            Color color)
        {
            CrossFamilyConnectionKey key = new(firstParentId, secondParentId, 2);
            if (!drawn.Add(key) || !CanDrawCrossFamilyConnection(firstParentId, secondParentId))
            {
                return;
            }

            DrawCrossFamilyRoutedConnection(firstParentId, secondParentId, color, GetConnectionLaneOffset(key));
        }

        private bool CanDrawCrossFamilyConnection(int firstId, int secondId)
        {
            return firstId > 0
                && secondId > 0
                && familyGroupByResidentId.TryGetValue(firstId, out int firstGroup)
                && familyGroupByResidentId.TryGetValue(secondId, out int secondGroup)
                && firstGroup != secondGroup
                && contentCardTopLeftById.ContainsKey(firstId)
                && contentCardTopLeftById.ContainsKey(secondId)
                && contentCardCenterById.ContainsKey(firstId)
                && contentCardCenterById.ContainsKey(secondId);
        }

        private static float GetConnectionLaneOffset(CrossFamilyConnectionKey key)
        {
            return ((key.FirstId + key.SecondId + key.Type * 7) % 5 - 2) * 5f;
        }

        private void DrawCrossFamilyRoutedConnection(int firstId, int secondId, Color color, float laneOffset)
        {
            Vector2 firstCenter = contentCardCenterById[firstId];
            Vector2 secondCenter = contentCardCenterById[secondId];
            Vector2 firstTopLeft = contentCardTopLeftById[firstId];
            Vector2 secondTopLeft = contentCardTopLeftById[secondId];
            bool firstIsLeft = firstCenter.x <= secondCenter.x;
            Vector2 leftCenter = firstIsLeft ? firstCenter : secondCenter;
            Vector2 rightCenter = firstIsLeft ? secondCenter : firstCenter;
            Vector2 leftTopLeft = firstIsLeft ? firstTopLeft : secondTopLeft;
            Vector2 rightTopLeft = firstIsLeft ? secondTopLeft : firstTopLeft;
            Vector2 start = new(leftTopLeft.x + CardWidth, leftCenter.y);
            Vector2 end = new(rightTopLeft.x, rightCenter.y);
            float midX = Mathf.Lerp(start.x, end.x, 0.5f) + laneOffset;
            Vector2 elbowA = new(midX, start.y);
            Vector2 elbowB = new(midX, end.y);
            AddCrossFamilySegment(start, elbowA, color);
            AddCrossFamilySegment(elbowA, elbowB, color);
            AddCrossFamilySegment(elbowB, end, color);
        }

        private void AddCrossFamilySegment(Vector2 start, Vector2 end, Color color)
        {
            float dx = end.x - start.x;
            float dy = end.y - start.y;
            if (Mathf.Abs(dx) < 0.1f && Mathf.Abs(dy) < 0.1f)
            {
                return;
            }

            RectTransform line = CreateUiObject("CrossFamilyLink", contentRoot).GetComponent<RectTransform>();
            Image image = line.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            {
                float x = Mathf.Min(start.x, end.x);
                float width = Mathf.Max(CrossFamilyConnectionThickness, Mathf.Abs(dx));
                SetTopLeft(line, x, start.y - CrossFamilyConnectionThickness * 0.5f, width, CrossFamilyConnectionThickness);
                return;
            }

            float y = Mathf.Min(start.y, end.y);
            float height = Mathf.Max(CrossFamilyConnectionThickness, Mathf.Abs(dy));
            SetTopLeft(line, start.x - CrossFamilyConnectionThickness * 0.5f, y, CrossFamilyConnectionThickness, height);
        }
    }
}
