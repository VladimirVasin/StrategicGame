using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyFoundingStartState : MonoBehaviour
    {
        public StrategyFoundingPreferences Preferences { get; private set; }
        public StrategyStarterLayout Layout { get; private set; }
        public bool HasCampCell { get; private set; }
        public Vector2Int CampCell { get; private set; }
        public bool HasCaravanOrigin { get; private set; }
        public Vector2Int CaravanOrigin { get; private set; }
        public bool IsRestoredFromSave { get; private set; }

        public void Configure(
            StrategyFoundingPreferences preferences,
            StrategyStarterLayout layout)
        {
            if (layout == null || !layout.IsValid)
            {
                throw new ArgumentException("A valid starter layout is required.", nameof(layout));
            }

            Preferences = preferences ?? StrategyFoundingPreferences.Balanced;
            Layout = layout;
            HasCampCell = true;
            CampCell = layout.CampCell;
            HasCaravanOrigin = layout.HasCaravanReservation;
            CaravanOrigin = layout.CaravanOrigin;
            IsRestoredFromSave = false;
        }

        public void Configure(StrategyFoundingStartSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Preferences = RestorePreferences(data);
            Layout = null;
            HasCampCell = data.hasStarterCamp;
            CampCell = new Vector2Int(data.starterCampX, data.starterCampY);
            HasCaravanOrigin = data.hasStarterCartOrigin;
            CaravanOrigin = new Vector2Int(data.starterCartOriginX, data.starterCartOriginY);
            IsRestoredFromSave = true;
        }

        public StrategyFoundingStartSaveData CreateSaveData()
        {
            StrategyFoundingPreferences preferences = Preferences ?? StrategyFoundingPreferences.Balanced;
            StrategyFoundingStartSaveData data = new()
            {
                hasStarterCamp = HasCampCell,
                starterCampX = HasCampCell ? CampCell.x : 0,
                starterCampY = HasCampCell ? CampCell.y : 0,
                hasStarterCartOrigin = HasCampCell && HasCaravanOrigin,
                starterCartOriginX = HasCampCell && HasCaravanOrigin ? CaravanOrigin.x : 0,
                starterCartOriginY = HasCampCell && HasCaravanOrigin ? CaravanOrigin.y : 0,
                profileVersion = preferences.Version,
                profileId = preferences.ProfileId
            };

            AddAnswer(data, StrategyFoundingChoiceIds.WaterQuestion, preferences.WaterChoiceId);
            AddAnswer(data, StrategyFoundingChoiceIds.LandscapeQuestion, preferences.LandscapeChoiceId);
            AddAnswer(data, StrategyFoundingChoiceIds.LivelihoodQuestion, preferences.LivelihoodChoiceId);
            AddAnswer(data, StrategyFoundingChoiceIds.PriorityQuestion, preferences.PriorityChoiceId);
            return data;
        }

        public static StrategyFoundingStartState GetOrCreate(CityMapController map)
        {
            if (map == null)
            {
                return null;
            }

            StrategyFoundingStartState state = map.GetComponent<StrategyFoundingStartState>();
            return state != null ? state : map.gameObject.AddComponent<StrategyFoundingStartState>();
        }

        private static StrategyFoundingPreferences RestorePreferences(StrategyFoundingStartSaveData data)
        {
            if (data.profileVersion <= 0 || data.answers == null)
            {
                return StrategyFoundingPreferences.Balanced;
            }

            string water = FindAnswer(
                data,
                StrategyFoundingChoiceIds.WaterQuestion,
                StrategyFoundingChoiceIds.WaterNoPreference);
            string landscape = FindAnswer(
                data,
                StrategyFoundingChoiceIds.LandscapeQuestion,
                StrategyFoundingChoiceIds.LandscapeMixed);
            string livelihood = FindAnswer(
                data,
                StrategyFoundingChoiceIds.LivelihoodQuestion,
                StrategyFoundingChoiceIds.LivelihoodBalanced);
            string priority = FindAnswer(
                data,
                StrategyFoundingChoiceIds.PriorityQuestion,
                StrategyFoundingChoiceIds.PriorityBalanced);
            return StrategyFoundingPreferences.TryCreate(
                water,
                landscape,
                livelihood,
                priority,
                out StrategyFoundingPreferences restored,
                data.profileId,
                data.profileVersion)
                ? restored
                : StrategyFoundingPreferences.Balanced;
        }

        private static string FindAnswer(
            StrategyFoundingStartSaveData data,
            string questionId,
            string fallback)
        {
            for (int i = 0; i < data.answers.Count; i++)
            {
                StrategyFoundingAnswerSaveData answer = data.answers[i];
                if (answer != null && answer.questionId == questionId)
                {
                    return answer.answerId;
                }
            }

            return fallback;
        }

        private static void AddAnswer(
            StrategyFoundingStartSaveData data,
            string questionId,
            string answerId)
        {
            data.answers.Add(new StrategyFoundingAnswerSaveData
            {
                questionId = questionId,
                answerId = answerId
            });
        }
    }
}
