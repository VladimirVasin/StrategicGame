using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFamilyTreeHudController
    {
        private static Sprite deathMarkerSprite;

        private float BuildFamilySection(
            string familyTitle,
            List<StrategyResidentFamilyRecord> records,
            int familyGroupIndex,
            float left,
            float top,
            out float sectionHeight)
        {
            PrepareGenerations(records);
            int maxGeneration = BuildRows(records, out int maxRowCount);
            float sectionWidth = CalculateFamilySectionWidth(maxRowCount);
            sectionHeight = 76f
                + (maxGeneration + 1) * CardHeight
                + maxGeneration * GenerationGap
                + 28f;

            RectTransform section = CreateUiObject("Family_" + familyTitle, contentRoot).GetComponent<RectTransform>();
            SetTopLeft(section, left, top, sectionWidth, sectionHeight);
            Image background = section.gameObject.AddComponent<Image>();
            background.color = new Color(0.04f, 0.052f, 0.055f, 0.92f);
            AddFamilySectionBorder(section, sectionWidth, sectionHeight);

            Text header = CreateText("Header", section, familyTitle, 19, TextAnchor.MiddleLeft, Color.white);
            header.fontStyle = FontStyle.Bold;
            header.resizeTextForBestFit = true;
            header.resizeTextMinSize = 12;
            header.resizeTextMaxSize = 19;
            SetTopLeft(header.rectTransform, 24f, 14f, sectionWidth - 48f, 28f);

            Text count = CreateText("Count", section, records.Count + " members", 12, TextAnchor.MiddleLeft, new Color(0.78f, 0.86f, 0.82f));
            SetTopLeft(count.rectTransform, 24f, 41f, sectionWidth - 48f, 20f);

            PositionCards(sectionWidth);
            RegisterContentCardPositions(records, familyGroupIndex, left, top);
            DrawConnections(section);
            DrawCards(section, records);
            return sectionWidth;
        }

        private static void AddFamilySectionBorder(RectTransform section, float width, float height)
        {
            AddLine(section, 0f, 0f, width, 2f);
            AddLine(section, 0f, height - 2f, width, 2f);
            AddLine(section, 0f, 0f, 2f, height);
            AddLine(section, width - 2f, 0f, 2f, height);
        }

        private void PrepareGenerations(List<StrategyResidentFamilyRecord> records)
        {
            generationsById.Clear();
            familyIds.Clear();
            for (int i = 0; i < records.Count; i++)
            {
                generationsById[records[i].ResidentId] = 0;
                familyIds.Add(records[i].ResidentId);
            }

            for (int pass = 0; pass < records.Count; pass++)
            {
                bool changed = false;
                for (int i = 0; i < records.Count; i++)
                {
                    StrategyResidentFamilyRecord record = records[i];
                    int parentGeneration = -1;
                    if (generationsById.TryGetValue(record.FatherId, out int fatherGeneration))
                    {
                        parentGeneration = Mathf.Max(parentGeneration, fatherGeneration);
                    }

                    if (generationsById.TryGetValue(record.MotherId, out int motherGeneration))
                    {
                        parentGeneration = Mathf.Max(parentGeneration, motherGeneration);
                    }

                    int targetGeneration = parentGeneration >= 0 ? parentGeneration + 1 : 0;
                    if (targetGeneration > generationsById[record.ResidentId])
                    {
                        generationsById[record.ResidentId] = targetGeneration;
                        changed = true;
                    }
                }

                if (!changed)
                {
                    break;
                }
            }
        }

        private int BuildRows(List<StrategyResidentFamilyRecord> records, out int maxRowCount)
        {
            rowsByGeneration.Clear();
            int maxGeneration = 0;
            maxRowCount = 0;
            for (int i = 0; i < records.Count; i++)
            {
                int generation = generationsById.TryGetValue(records[i].ResidentId, out int value) ? value : 0;
                maxGeneration = Mathf.Max(maxGeneration, generation);
                if (!rowsByGeneration.TryGetValue(generation, out List<StrategyResidentFamilyRecord> row))
                {
                    row = new List<StrategyResidentFamilyRecord>();
                    rowsByGeneration[generation] = row;
                }

                row.Add(records[i]);
            }

            foreach (KeyValuePair<int, List<StrategyResidentFamilyRecord>> pair in rowsByGeneration)
            {
                pair.Value.Sort(CompareFamilyMembers);
                maxRowCount = Mathf.Max(maxRowCount, pair.Value.Count);
            }

            return maxGeneration;
        }

        private void PositionCards(float sectionWidth)
        {
            cardTopLeftById.Clear();
            cardCenterById.Clear();
            int maxGeneration = GetMaxLayoutGeneration();
            for (int generation = 0; generation <= maxGeneration; generation++)
            {
                PositionGenerationCards(generation, sectionWidth);
            }
        }

        private void DrawConnections(RectTransform section)
        {
            DrawGroupedFamilyConnections(section);
        }

        private void DrawCards(RectTransform section, List<StrategyResidentFamilyRecord> records)
        {
            for (int i = 0; i < records.Count; i++)
            {
                StrategyResidentFamilyRecord record = records[i];
                if (!cardTopLeftById.TryGetValue(record.ResidentId, out Vector2 position))
                {
                    continue;
                }

                CreateMemberCard(section, record, position);
            }
        }

        private void CreateMemberCard(RectTransform section, StrategyResidentFamilyRecord record, Vector2 position)
        {
            RectTransform card = CreateUiObject("Member_" + record.ResidentId, section).GetComponent<RectTransform>();
            SetTopLeft(card, position.x, position.y, CardWidth, CardHeight);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition += new Vector2(CardWidth * 0.5f, -CardHeight * 0.5f);
            Image background = card.gameObject.AddComponent<Image>();
            background.color = record.IsAlive
                ? new Color(0.11f, 0.14f, 0.145f, 0.96f)
                : new Color(0.055f, 0.055f, 0.06f, 0.94f);
            StrategyFamilyTreeCardHoverAnimator hover = card.gameObject.AddComponent<StrategyFamilyTreeCardHoverAnimator>();
            hover.Configure(this, record.ResidentId);

            RectTransform portraitFrame = CreateUiObject("PortraitFrame", card).GetComponent<RectTransform>();
            SetTopLeft(portraitFrame, 8f, 8f, 46f, 46f);
            Image frame = portraitFrame.gameObject.AddComponent<Image>();
            frame.color = new Color(1f, 1f, 1f, record.IsAlive ? 0.10f : 0.05f);

            RectTransform portraitRect = CreateUiObject("Portrait", portraitFrame).GetComponent<RectTransform>();
            Stretch(portraitRect, 2f, 2f, 2f, 2f);
            Image portrait = portraitRect.gameObject.AddComponent<Image>();
            portrait.sprite = StrategyResidentSpriteFactory.GetPortraitSprite(record.Gender, record.VisualVariant, record.LifeStage);
            portrait.preserveAspect = true;
            portrait.color = record.IsAlive ? Color.white : new Color(0.48f, 0.48f, 0.48f, 0.90f);

            Color nameColor = record.IsAlive ? Color.white : new Color(0.72f, 0.72f, 0.72f, 0.95f);
            float nameWidth = record.IsAlive ? 82f : 61f;
            Text name = CreateText("Name", card, record.FullName, 11, TextAnchor.UpperLeft, nameColor);
            name.fontStyle = FontStyle.Bold;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 8;
            name.resizeTextMaxSize = 11;
            SetTopLeft(name.rectTransform, 60f, 8f, nameWidth, 30f);

            Text gender = CreateText(
                "Gender",
                card,
                record.Gender == StrategyResidentGender.Male ? "♂" : "♀",
                15,
                TextAnchor.MiddleCenter,
                record.Gender == StrategyResidentGender.Male
                    ? new Color(0.70f, 0.84f, 1f, 0.95f)
                    : new Color(1f, 0.72f, 0.88f, 0.95f));
            gender.fontStyle = FontStyle.Bold;
            SetTopLeft(gender.rectTransform, CardWidth - 26f, 27f, 18f, 18f);

            Color statusColor = record.IsAlive
                ? new Color(0.74f, 0.82f, 0.78f)
                : new Color(0.62f, 0.62f, 0.62f, 0.92f);
            Text status = CreateText("Status", card, GetMemberStatus(record), 10, TextAnchor.UpperLeft, statusColor);
            status.resizeTextForBestFit = true;
            status.resizeTextMinSize = 8;
            status.resizeTextMaxSize = 10;
            SetTopLeft(status.rectTransform, 60f, 42f, 82f, 15f);

            if (!record.IsAlive)
            {
                AddDeathMarker(card);
            }

            Text relation = CreateText(
                "Relation_" + record.ResidentId,
                card,
                string.Empty,
                11,
                TextAnchor.MiddleCenter,
                new Color(0.94f, 0.78f, 0.42f, 0.96f));
            relation.fontStyle = FontStyle.Bold;
            relation.resizeTextForBestFit = true;
            relation.resizeTextMinSize = 8;
            relation.resizeTextMaxSize = 11;
            SetTopLeft(
                relation.rectTransform,
                8f,
                CardHeight - RelationshipLabelHeight - 2f,
                CardWidth - 16f,
                RelationshipLabelHeight);
            relation.gameObject.SetActive(false);
            relationshipLabelsById[record.ResidentId] = relation;
        }

        private static void AddDeathMarker(RectTransform card)
        {
            RectTransform marker = CreateUiObject("DeathMarker", card).GetComponent<RectTransform>();
            SetTopLeft(marker, CardWidth - 27f, 7f, 18f, 18f);
            Image image = marker.gameObject.AddComponent<Image>();
            image.sprite = GetDeathMarkerSprite();
            image.preserveAspect = true;
            image.color = new Color(0.86f, 0.86f, 0.82f, 0.96f);
            image.raycastTarget = false;
        }

        private static Sprite GetDeathMarkerSprite()
        {
            if (deathMarkerSprite != null)
            {
                return deathMarkerSprite;
            }

            const int size = 16;
            string[] pattern =
            {
                "................",
                ".....XXXXXX.....",
                "....XXXXXXXX....",
                "...XXXXXXXXXX...",
                "...XXKKXXKKXX...",
                "...XXKKXXKKXX...",
                "...XXXXXXXXXX...",
                "....XXXXXXXX....",
                ".....XXXXXX.....",
                "......XXXX......",
                "....X.XXXX.X....",
                "...XX.XXXX.XX...",
                "...XXXXXXXXXX...",
                "...XX.XX.XX.XX..",
                "................",
                "................"
            };
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
            Color clear = Color.clear;
            Color bone = Color.white;
            Color dark = new(0.08f, 0.08f, 0.08f, 1f);
            for (int y = 0; y < size; y++)
            {
                string row = pattern[size - 1 - y];
                for (int x = 0; x < size; x++)
                {
                    char value = row[x];
                    texture.SetPixel(x, y, value == 'X' ? bone : value == 'K' ? dark : clear);
                }
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            deathMarkerSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            deathMarkerSprite.name = "FamilyTreeDeathMarker";
            return deathMarkerSprite;
        }

        private string GetMemberStatus(StrategyResidentFamilyRecord record)
        {
            if (record.IsAlive && population != null && population.TryGetResidentById(record.ResidentId, out StrategyResidentAgent resident))
            {
                return StrategyResidentHudText.GetRoleTitle(resident) + ", " + resident.DisplayAgeYears + "y";
            }

            return "deceased, " + record.DisplayAgeYears + "y";
        }
    }
}
