using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategySeasonAmbientDetailController : MonoBehaviour
    {
        private const int DetailCount = 24;
        private const float ViewPadding = 3f;

        private readonly List<AmbientDetail> details = new(DetailCount);
        private Camera strategyCamera;
        private StrategyWeatherController weather;
        private StrategyWindController wind;
        private bool wasActive;
        private static Sprite detailSprite;

        public void Configure(
            Camera camera,
            StrategyWeatherController weatherController,
            StrategyWindController windController)
        {
            strategyCamera = camera;
            weather = weatherController;
            wind = windController;
            EnsurePool();
            DisableAll();
        }

        private void Update()
        {
            if (strategyCamera == null)
            {
                return;
            }

            StrategyCalendarSnapshot calendar = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            float activity = GetActivity(calendar);
            if (activity <= 0.01f)
            {
                if (wasActive)
                {
                    DisableAll();
                }

                wasActive = false;
                return;
            }

            EnsurePool();
            Rect view = GetCameraBounds();
            Vector2 direction = GetMovementDirection(calendar.Season);
            int activeCount = Mathf.Clamp(Mathf.RoundToInt(DetailCount * activity), 1, DetailCount);
            float dt = Mathf.Max(0f, Time.unscaledDeltaTime);
            for (int i = 0; i < details.Count; i++)
            {
                AmbientDetail detail = details[i];
                bool enabled = i < activeCount;
                detail.Renderer.enabled = enabled;
                if (!enabled)
                {
                    continue;
                }

                if (!wasActive)
                {
                    Respawn(detail, view, false);
                }

                UpdateDetail(detail, view, direction, calendar.Season, activity, dt);
            }

            wasActive = true;
        }

        private float GetActivity(StrategyCalendarSnapshot calendar)
        {
            if (calendar.Season != StrategySeason.Spring && calendar.Season != StrategySeason.Autumn)
            {
                return 0f;
            }

            float daylight = calendar.Phase switch
            {
                StrategyTimeOfDayPhase.Dawn => Mathf.Lerp(0.15f, 1f, calendar.PhaseProgress),
                StrategyTimeOfDayPhase.Dusk => Mathf.Lerp(1f, 0f, calendar.PhaseProgress),
                StrategyTimeOfDayPhase.Night => 0f,
                _ => 1f
            };
            float weatherSuppression = weather != null
                ? Mathf.Clamp01(weather.RainIntensity * 0.50f + weather.SnowIntensity + weather.StormIntensity)
                : 0f;
            float seasonStrength = calendar.Season == StrategySeason.Autumn ? 0.72f : 0.50f;
            return daylight * seasonStrength * (1f - weatherSuppression * 0.70f);
        }

        private void UpdateDetail(
            AmbientDetail detail,
            Rect view,
            Vector2 direction,
            StrategySeason season,
            float activity,
            float dt)
        {
            float wave = Mathf.Sin(Time.unscaledTime * detail.WaveSpeed + detail.Phase) * detail.WaveAmplitude;
            Vector3 position = detail.Renderer.transform.position;
            position += new Vector3(direction.x * detail.Speed + wave, direction.y * detail.Speed, 0f) * dt;
            if (!view.Contains(new Vector2(position.x, position.y)))
            {
                Respawn(detail, view, true);
                position = detail.Renderer.transform.position;
            }
            else
            {
                detail.Renderer.transform.position = position;
            }

            float flutter = Mathf.Sin(Time.unscaledTime * detail.WaveSpeed * 1.7f + detail.Phase);
            detail.Renderer.transform.rotation = Quaternion.Euler(0f, flutter * 52f, detail.Rotation + flutter * 30f);
            float scaleY = Mathf.Lerp(0.42f, 1f, Mathf.Abs(flutter));
            detail.Renderer.transform.localScale = new Vector3(detail.Scale, detail.Scale * scaleY, 1f);
            Color color = GetColor(season, detail.ColorVariant);
            color.a *= Mathf.Lerp(0.42f, 0.82f, activity);
            detail.Renderer.color = color;
        }

        private Vector2 GetMovementDirection(StrategySeason season)
        {
            Vector2 windDirection = wind != null ? wind.PlanarDirection : Vector2.right;
            float fall = season == StrategySeason.Autumn ? -0.48f : -0.18f;
            return new Vector2(windDirection.x * 0.75f, fall + windDirection.y * 0.16f).normalized;
        }

        private Rect GetCameraBounds()
        {
            Vector3 center = strategyCamera.transform.position;
            float height = strategyCamera.orthographicSize * 2f + ViewPadding * 2f;
            float width = height * Mathf.Max(0.1f, strategyCamera.aspect) + ViewPadding * 2f;
            return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
        }

        private void Respawn(AmbientDetail detail, Rect view, bool leadingEdge)
        {
            Vector2 direction = GetMovementDirection(StrategyDayNightCycleController.CurrentCalendarSnapshot.Season);
            float x = Random.Range(view.xMin, view.xMax);
            float y = Random.Range(view.yMin, view.yMax);
            if (leadingEdge)
            {
                x = direction.x >= 0f ? view.xMin : view.xMax;
                y = direction.y >= 0f ? view.yMin : view.yMax;
                if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                {
                    y = Random.Range(view.yMin, view.yMax);
                }
                else
                {
                    x = Random.Range(view.xMin, view.xMax);
                }
            }

            detail.Renderer.transform.position = new Vector3(x, y, -0.13f);
        }

        private void EnsurePool()
        {
            while (details.Count < DetailCount)
            {
                GameObject detailObject = new GameObject("Season Ambient Detail");
                detailObject.transform.SetParent(transform, false);
                SpriteRenderer renderer = detailObject.AddComponent<SpriteRenderer>();
                renderer.sprite = GetDetailSprite();
                renderer.sortingOrder = StrategyWorldSorting.SeasonAmbientOverlayOrder;
                renderer.enabled = false;
                details.Add(new AmbientDetail
                {
                    Renderer = renderer,
                    Speed = Random.Range(0.35f, 1.05f),
                    Scale = Random.Range(0.48f, 1.05f),
                    Rotation = Random.Range(0f, 360f),
                    Phase = Random.Range(0f, Mathf.PI * 2f),
                    WaveSpeed = Random.Range(1.2f, 3.4f),
                    WaveAmplitude = Random.Range(0.08f, 0.30f),
                    ColorVariant = Random.Range(0, 4)
                });
            }
        }

        private void DisableAll()
        {
            for (int i = 0; i < details.Count; i++)
            {
                if (details[i].Renderer != null)
                {
                    details[i].Renderer.enabled = false;
                }
            }
        }

        private static Color GetColor(StrategySeason season, int variant)
        {
            if (season == StrategySeason.Spring)
            {
                return variant switch
                {
                    0 => new Color(1f, 0.74f, 0.82f, 1f),
                    1 => new Color(1f, 0.91f, 0.94f, 1f),
                    2 => new Color(0.86f, 0.96f, 0.68f, 1f),
                    _ => new Color(0.94f, 0.76f, 1f, 1f)
                };
            }

            return variant switch
            {
                0 => new Color(0.94f, 0.38f, 0.12f, 1f),
                1 => new Color(1f, 0.68f, 0.16f, 1f),
                2 => new Color(0.66f, 0.22f, 0.08f, 1f),
                _ => new Color(0.82f, 0.52f, 0.12f, 1f)
            };
        }

        private static Sprite GetDetailSprite()
        {
            if (detailSprite != null)
            {
                return detailSprite;
            }

            Texture2D texture = new(7, 5, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Season Ambient Detail"
            };
            Color[] pixels = new Color[texture.width * texture.height];
            texture.SetPixels(pixels);
            texture.SetPixel(2, 1, Color.white);
            texture.SetPixel(3, 1, Color.white);
            texture.SetPixel(4, 2, Color.white);
            texture.SetPixel(3, 3, Color.white);
            texture.SetPixel(2, 3, Color.white);
            texture.SetPixel(1, 2, new Color(1f, 1f, 1f, 0.68f));
            texture.Apply(false, true);
            detailSprite = Sprite.Create(texture, new Rect(0f, 0f, 7f, 5f), new Vector2(0.5f, 0.5f), 18f);
            detailSprite.name = "Season Ambient Detail Sprite";
            return detailSprite;
        }

        private sealed class AmbientDetail
        {
            public SpriteRenderer Renderer;
            public float Speed;
            public float Scale;
            public float Rotation;
            public float Phase;
            public float WaveSpeed;
            public float WaveAmplitude;
            public int ColorVariant;
        }
    }
}
