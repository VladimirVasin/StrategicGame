using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWeatherVisualController
    {
        private void EnsureSnowflakes()
        {
            while (snowflakes.Count < MaxSnowflakes)
            {
                GameObject flakeObject = new GameObject("Weather Snowflake");
                flakeObject.transform.SetParent(transform, false);
                SpriteRenderer renderer = flakeObject.AddComponent<SpriteRenderer>();
                renderer.sprite = GetSnowflakeSprite();
                renderer.sortingOrder = StrategyWorldSorting.WeatherSnowOverlayOrder;
                renderer.enabled = false;
                snowflakes.Add(new SnowflakeVisual
                {
                    Renderer = renderer,
                    Speed = Random.Range(1.25f, 3.75f),
                    Scale = Random.Range(0.58f, 1.18f),
                    Alpha = Random.Range(0.62f, 1f),
                    WavePhase = Random.Range(0f, Mathf.PI * 2f),
                    WaveSpeed = Random.Range(1.15f, 2.75f),
                    WaveAmplitude = Random.Range(0.08f, 0.28f)
                });
            }
        }

        private void UpdateSnowflakes(float dt)
        {
            if (weather == null)
            {
                return;
            }

            float snow = weather.SnowIntensity;
            if (snow <= 0.015f)
            {
                for (int i = 0; i < snowflakes.Count; i++)
                {
                    if (snowflakes[i].Renderer != null)
                    {
                        snowflakes[i].Renderer.enabled = false;
                    }
                }

                snowWasActive = false;
                return;
            }

            EnsureSnowflakes();
            Rect view = GetCameraSnowBounds();
            Vector2 move = GetSnowMoveDirection();
            float heavy = weather.HeavySnowIntensity;
            int activeCount = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(30f, MaxSnowflakes, snow)), 0, MaxSnowflakes);
            float alpha = Mathf.Lerp(0.38f, 0.78f, snow);
            float speedBoost = Mathf.Lerp(0.78f, 1.85f, Mathf.Max(snow * 0.65f, weather.WindIntensity));

            for (int i = 0; i < snowflakes.Count; i++)
            {
                SnowflakeVisual flake = snowflakes[i];
                SpriteRenderer renderer = flake.Renderer;
                if (renderer == null)
                {
                    continue;
                }

                bool active = i < activeCount;
                renderer.enabled = active;
                if (!active)
                {
                    continue;
                }

                if (!snowWasActive)
                {
                    RespawnSnowflake(flake, view, false);
                }

                Transform flakeTransform = renderer.transform;
                float sway = Mathf.Sin(Time.unscaledTime * flake.WaveSpeed + flake.WavePhase) * flake.WaveAmplitude;
                Vector3 position = flakeTransform.position
                    + new Vector3(move.x * flake.Speed * speedBoost + sway, move.y * flake.Speed * speedBoost, 0f) * dt;
                if (position.x < view.xMin - SnowViewPadding
                    || position.x > view.xMax + SnowViewPadding
                    || position.y < view.yMin - SnowViewPadding
                    || position.y > view.yMax + SnowViewPadding)
                {
                    RespawnSnowflake(flake, view, true);
                    position = flakeTransform.position;
                }
                else
                {
                    flakeTransform.position = position;
                }

                flakeTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 0.75f + flake.WavePhase) * 12f);
                float scale = flake.Scale * Mathf.Lerp(0.90f, 1.28f, snow);
                flakeTransform.localScale = new Vector3(scale, scale, 1f);
                Color color = Color.Lerp(new Color(0.86f, 0.94f, 1f, 1f), Color.white, 0.35f + heavy * 0.30f);
                color.a = alpha * flake.Alpha;
                renderer.color = color;
            }

            snowWasActive = true;
        }

        private Rect GetCameraSnowBounds()
        {
            if (strategyCamera != null && strategyCamera.orthographic)
            {
                Vector3 center = strategyCamera.transform.position;
                float height = strategyCamera.orthographicSize * 2f + SnowViewPadding * 2f;
                float width = height * Mathf.Max(0.1f, strategyCamera.aspect) + SnowViewPadding * 2f;
                return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
            }

            Bounds bounds = map != null ? map.WorldBounds : new Bounds(Vector3.zero, new Vector3(40f, 24f, 1f));
            return new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
        }

        private Vector2 GetSnowMoveDirection()
        {
            Vector2 windDirection = wind != null ? wind.PlanarDirection : Vector2.right;
            float lateral = Mathf.Lerp(0.16f, 0.70f, weather != null ? weather.HeavySnowIntensity : 0f);
            return new Vector2(windDirection.x * lateral, -1f).normalized;
        }

        private void RespawnSnowflake(SnowflakeVisual flake, Rect view, bool fromLeadingEdge)
        {
            if (flake.Renderer == null)
            {
                return;
            }

            Vector2 move = GetSnowMoveDirection();
            float x = Random.Range(view.xMin, view.xMax);
            float y = fromLeadingEdge ? view.yMax + Random.Range(0f, SnowViewPadding) : Random.Range(view.yMin, view.yMax);
            if (fromLeadingEdge && move.x > 0.05f)
            {
                x = Random.Range(view.xMin - SnowViewPadding, view.xMax);
            }
            else if (fromLeadingEdge && move.x < -0.05f)
            {
                x = Random.Range(view.xMin, view.xMax + SnowViewPadding);
            }

            flake.Renderer.transform.position = new Vector3(x, y, -0.125f);
        }

        private static Sprite GetSnowflakeSprite()
        {
            if (snowflakeSprite != null)
            {
                return snowflakeSprite;
            }

            Texture2D texture = new Texture2D(5, 5, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Weather Snowflake"
            };
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }

            Color core = new Color(0.96f, 1f, 1f, 0.95f);
            Color arm = new Color(0.84f, 0.94f, 1f, 0.54f);
            texture.SetPixel(2, 2, core);
            texture.SetPixel(2, 1, arm);
            texture.SetPixel(2, 3, arm);
            texture.SetPixel(1, 2, arm);
            texture.SetPixel(3, 2, arm);
            texture.Apply(false, true);

            snowflakeSprite = Sprite.Create(texture, new Rect(0f, 0f, 5f, 5f), new Vector2(0.5f, 0.5f), 22f);
            snowflakeSprite.name = "Weather Snowflake Sprite";
            return snowflakeSprite;
        }

        private sealed class SnowflakeVisual
        {
            public SpriteRenderer Renderer;
            public float Speed;
            public float Scale;
            public float Alpha;
            public float WavePhase;
            public float WaveSpeed;
            public float WaveAmplitude;
        }
    }
}
