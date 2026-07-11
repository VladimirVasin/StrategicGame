using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyNaturePropController
    {
        private const float SeasonTintInterval = 0.12f;

        private readonly List<SeasonTintTarget> seasonTintTargets = new();
        private float seasonTintTimer;

        private void Update()
        {
            seasonTintTimer -= Mathf.Max(0f, Time.unscaledDeltaTime);
            if (seasonTintTimer > 0f)
            {
                return;
            }

            seasonTintTimer = SeasonTintInterval;
            ApplySeasonTint();
        }

        private void RegisterSeasonTintTarget(
            SpriteRenderer renderer,
            StrategyNaturePropKind kind,
            int variant)
        {
            if (renderer == null || !IsSeasonTintVegetation(kind))
            {
                return;
            }

            seasonTintTargets.Add(new SeasonTintTarget
            {
                Renderer = renderer,
                Evergreen = IsEvergreen(kind, variant),
                Variation = Mathf.Repeat(variant * 0.271f + renderer.transform.position.x * 0.037f, 1f)
            });
        }

        private void ApplySeasonTint()
        {
            StrategySeason season = StrategyDayNightCycleController.CurrentCalendarSnapshot.Season;
            for (int i = seasonTintTargets.Count - 1; i >= 0; i--)
            {
                SeasonTintTarget target = seasonTintTargets[i];
                if (target.Renderer == null)
                {
                    seasonTintTargets.RemoveAt(i);
                    continue;
                }

                Color desired = GetSeasonTint(season, target.Evergreen, target.Variation);
                target.Renderer.color = Color.Lerp(target.Renderer.color, desired, 0.34f);
            }
        }

        private static bool IsSeasonTintVegetation(StrategyNaturePropKind kind)
        {
            return kind == StrategyNaturePropKind.LargeTree
                || kind == StrategyNaturePropKind.SmallTree
                || kind == StrategyNaturePropKind.Bush
                || kind == StrategyNaturePropKind.ForestGroup;
        }

        private static bool IsEvergreen(StrategyNaturePropKind kind, int variant)
        {
            return kind == StrategyNaturePropKind.LargeTree && (variant == 1 || variant == 3)
                || kind == StrategyNaturePropKind.SmallTree && variant == 2;
        }

        private static Color GetSeasonTint(StrategySeason season, bool evergreen, float variation)
        {
            if (evergreen)
            {
                return season switch
                {
                    StrategySeason.Spring => new Color(0.92f, 1.04f, 0.94f, 1f),
                    StrategySeason.Autumn => new Color(0.82f, 0.92f, 0.78f, 1f),
                    StrategySeason.Winter => new Color(0.68f, 0.79f, 0.77f, 1f),
                    _ => Color.white
                };
            }

            return season switch
            {
                StrategySeason.Spring => Color.Lerp(
                    new Color(0.86f, 1.08f, 0.82f, 1f),
                    new Color(1.02f, 1.05f, 0.90f, 1f),
                    variation),
                StrategySeason.Autumn => Color.Lerp(
                    new Color(1.18f, 0.66f, 0.34f, 1f),
                    new Color(1.08f, 0.82f, 0.42f, 1f),
                    variation),
                StrategySeason.Winter => new Color(0.72f, 0.78f, 0.76f, 1f),
                _ => Color.white
            };
        }

        private sealed class SeasonTintTarget
        {
            public SpriteRenderer Renderer;
            public bool Evergreen;
            public float Variation;
        }
    }
}
