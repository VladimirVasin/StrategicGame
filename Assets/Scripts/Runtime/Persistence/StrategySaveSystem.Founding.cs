using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySaveSystem
    {
        internal const int MaxFoundingAnswers = 16;
        internal const int MaxFoundingStableIdLength = 64;
        private static readonly Vector2Int StarterCartFootprint = new(3, 3);

        private void RefreshFoundingStartGeometry()
        {
            foundingStart ??= new StrategyFoundingStartSaveData();
            if (population != null && population.TryGetCampCell(out Vector2Int campCell))
            {
                foundingStart.hasStarterCamp = true;
                foundingStart.starterCampX = campCell.x;
                foundingStart.starterCampY = campCell.y;
            }

            if (placement == null)
            {
                return;
            }

            foundingStart.hasStarterCartOrigin = false;
            foundingStart.starterCartOriginX = 0;
            foundingStart.starterCartOriginY = 0;

            for (int i = 0; i < placement.PlacedBuildings.Count; i++)
            {
                StrategyPlacedBuilding building = placement.PlacedBuildings[i];
                if (building == null || building.Tool != StrategyBuildTool.StarterCaravanCart)
                {
                    continue;
                }

                foundingStart.hasStarterCartOrigin = true;
                foundingStart.starterCartOriginX = building.Origin.x;
                foundingStart.starterCartOriginY = building.Origin.y;
                return;
            }
        }

        private static bool ValidateFoundingStart(
            StrategyFoundingStartSaveData data,
            int mapWidth,
            int mapHeight,
            out string reason)
        {
            if (data == null || data.answers == null)
            {
                reason = "missing_founding_start";
                return false;
            }

            if (data.hasStarterCamp)
            {
                if (!IsCellInside(data.starterCampX, data.starterCampY, mapWidth, mapHeight))
                {
                    reason = "invalid_starter_camp_cell";
                    return false;
                }
            }
            else if (data.starterCampX != 0 || data.starterCampY != 0)
            {
                reason = "unexpected_starter_camp_cell";
                return false;
            }

            if (data.hasStarterCartOrigin)
            {
                if (!data.hasStarterCamp
                    || !IsFootprintInsideMap(
                        data.starterCartOriginX,
                        data.starterCartOriginY,
                        StarterCartFootprint.x,
                        StarterCartFootprint.y,
                        mapWidth,
                        mapHeight))
                {
                    reason = "invalid_starter_cart_origin";
                    return false;
                }
            }
            else if (data.starterCartOriginX != 0 || data.starterCartOriginY != 0)
            {
                reason = "unexpected_starter_cart_origin";
                return false;
            }

            if (data.profileVersion < 0 || data.answers.Count > MaxFoundingAnswers)
            {
                reason = "invalid_founding_profile";
                return false;
            }

            if (data.profileVersion == 0)
            {
                if (!string.IsNullOrEmpty(data.profileId) || data.answers.Count != 0)
                {
                    reason = "invalid_founding_profile";
                    return false;
                }

                reason = string.Empty;
                return true;
            }

            if (!IsStableFoundingId(data.profileId))
            {
                reason = "invalid_founding_profile";
                return false;
            }

            HashSet<string> questionIds = new(StringComparer.Ordinal);
            for (int i = 0; i < data.answers.Count; i++)
            {
                StrategyFoundingAnswerSaveData answer = data.answers[i];
                if (answer == null
                    || !IsStableFoundingId(answer.questionId)
                    || !IsStableFoundingId(answer.answerId)
                    || !questionIds.Add(answer.questionId))
                {
                    reason = "invalid_founding_answer_" + i;
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        private static bool IsStableFoundingId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaxFoundingStableIdLength)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                bool valid = character >= 'a' && character <= 'z'
                    || character >= 'A' && character <= 'Z'
                    || character >= '0' && character <= '9'
                    || character == '-'
                    || character == '_'
                    || character == '.';
                if (!valid)
                {
                    return false;
                }
            }

            return true;
        }

        private static StrategyFoundingStartSaveData CopyFoundingStartData(StrategyFoundingStartSaveData source)
        {
            StrategyFoundingStartSaveData copy = new();
            if (source == null)
            {
                return copy;
            }

            copy.hasStarterCamp = source.hasStarterCamp;
            copy.starterCampX = source.starterCampX;
            copy.starterCampY = source.starterCampY;
            copy.hasStarterCartOrigin = source.hasStarterCartOrigin;
            copy.starterCartOriginX = source.starterCartOriginX;
            copy.starterCartOriginY = source.starterCartOriginY;
            copy.profileVersion = source.profileVersion;
            copy.profileId = source.profileId ?? string.Empty;
            if (source.answers == null)
            {
                return copy;
            }

            for (int i = 0; i < source.answers.Count; i++)
            {
                StrategyFoundingAnswerSaveData answer = source.answers[i];
                copy.answers.Add(answer == null
                    ? null
                    : new StrategyFoundingAnswerSaveData
                    {
                        questionId = answer.questionId ?? string.Empty,
                        answerId = answer.answerId ?? string.Empty
                    });
            }

            return copy;
        }
    }
}
