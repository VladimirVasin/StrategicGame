using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyFoundingJourneyAtmosphere : MonoBehaviour
    {
        private const int ParticleCount = 28;
        private const int SmokeCount = 5;
        private const int AshCount = 11;
        private const float ReferenceWidth = 1600f;
        private const float ReferenceHeight = 900f;

        private readonly RectTransform[] particles = new RectTransform[ParticleCount];
        private readonly Image[] images = new Image[ParticleCount];
        private readonly float[] speeds = new float[ParticleCount];
        private readonly float[] phases = new float[ParticleCount];
        private RectTransform referenceFrame;
        private RectTransform fireGlowRect;
        private Image fireGlow;
        private Texture2D glowTexture;
        private Sprite glowSprite;
        private StrategyFoundingAtmosphere atmosphere;
        private bool configured;
        private bool reducedMotion;

        public void Configure(RectTransform parent, bool reduceMotion)
        {
            if (configured)
            {
                SetReducedMotion(reduceMotion);
                return;
            }

            configured = true;
            reducedMotion = reduceMotion;
            referenceFrame = CreateReferenceFrame(parent);
            CreateFireGlow(referenceFrame);
            for (int i = 0; i < ParticleCount; i++)
            {
                GameObject particle = new("AtmosphereParticle" + i, typeof(RectTransform), typeof(Image));
                particle.transform.SetParent(referenceFrame, false);
                RectTransform rect = particle.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                Image image = particle.GetComponent<Image>();
                image.raycastTarget = false;
                particles[i] = rect;
                images[i] = image;
                speeds[i] = 0.72f + Hash01(i, 17) * 0.95f;
                phases[i] = Hash01(i, 41) * Mathf.PI * 2f;
            }

            ConfigureAtmosphere(StrategyFoundingAtmosphere.Embers);
            SetReducedMotion(reduceMotion);
            RefreshReferenceScale();
        }

        internal void ConfigureAtmosphere(StrategyFoundingAtmosphere value)
        {
            atmosphere = value;
            bool departure = value == StrategyFoundingAtmosphere.Embers;
            fireGlow.gameObject.SetActive(departure);
            SetGlowAppearance(reducedMotion ? 0f : Time.unscaledTime);
            int activeCount = GetActiveCount(value);
            for (int i = 0; i < ParticleCount; i++)
            {
                bool active = i < activeCount;
                particles[i].gameObject.SetActive(active && !reducedMotion);
                if (!active)
                {
                    continue;
                }

                ConfigureParticle(i, value);
                ResetParticle(i, true);
            }
        }

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            if (fireGlow != null && atmosphere == StrategyFoundingAtmosphere.Embers)
            {
                SetGlowAppearance(value ? 0f : Time.unscaledTime);
            }

            int activeCount = GetActiveCount(atmosphere);
            for (int i = 0; i < ParticleCount; i++)
            {
                particles[i].gameObject.SetActive(!value && i < activeCount);
            }
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            RefreshReferenceScale();

            if (atmosphere == StrategyFoundingAtmosphere.Embers)
            {
                SetGlowAppearance(reducedMotion ? 0f : Time.unscaledTime);
            }

            if (reducedMotion)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;
            int activeCount = GetActiveCount(atmosphere);
            for (int i = 0; i < activeCount; i++)
            {
                RectTransform rect = particles[i];
                Vector2 position = rect.anchoredPosition;
                switch (atmosphere)
                {
                    case StrategyFoundingAtmosphere.Embers:
                        if (UpdateDepartureParticle(i, ref position, time, dt))
                        {
                            continue;
                        }

                        break;
                    case StrategyFoundingAtmosphere.Rain:
                        position += new Vector2(-125f, -520f) * speeds[i] * dt;
                        if (position.y < -490f || position.x < -850f)
                        {
                            ResetParticle(i, false);
                            continue;
                        }

                        break;
                    case StrategyFoundingAtmosphere.Mist:
                        position.x += 18f * speeds[i] * dt;
                        position.y += Mathf.Sin(time * 0.18f + phases[i]) * 0.45f * dt;
                        if (position.x > 880f)
                        {
                            ResetParticle(i, false);
                            continue;
                        }

                        break;
                    case StrategyFoundingAtmosphere.Fireflies:
                        position.x += Mathf.Sin(time * speeds[i] + phases[i]) * 8f * dt;
                        position.y += (8f + Mathf.Cos(time * 0.7f + phases[i]) * 4f) * dt;
                        if (position.y > 480f)
                        {
                            ResetParticle(i, false);
                            continue;
                        }

                        images[i].color = WithAlpha(images[i].color, 0.20f + (Mathf.Sin(time * 1.7f + phases[i]) + 1f) * 0.22f);
                        break;
                }

                rect.anchoredPosition = position;
            }
        }

        private void ConfigureParticle(int index, StrategyFoundingAtmosphere value)
        {
            RectTransform rect = particles[index];
            Image image = images[index];
            switch (value)
            {
                case StrategyFoundingAtmosphere.Embers when index < SmokeCount:
                    rect.sizeDelta = new Vector2(
                        105f + Hash01(index, 5) * 105f,
                        65f + Hash01(index, 7) * 85f);
                    rect.localRotation = Quaternion.Euler(0f, 0f, -5f + Hash01(index, 9) * 10f);
                    image.sprite = glowSprite;
                    image.color = new Color(0.22f, 0.19f, 0.17f, 0.035f + Hash01(index, 11) * 0.035f);
                    break;
                case StrategyFoundingAtmosphere.Embers when index < AshCount:
                    rect.sizeDelta = new Vector2(2f + Hash01(index, 13) * 3f, 4f + Hash01(index, 17) * 7f);
                    rect.localRotation = Quaternion.Euler(0f, 0f, Hash01(index, 19) * 70f - 35f);
                    image.sprite = null;
                    image.color = new Color(0.68f, 0.65f, 0.60f, 0.13f + Hash01(index, 23) * 0.13f);
                    break;
                case StrategyFoundingAtmosphere.Embers:
                    float emberSize = 2f + Hash01(index, 29) * 4f;
                    rect.sizeDelta = new Vector2(emberSize, emberSize);
                    rect.localRotation = Quaternion.identity;
                    image.sprite = null;
                    image.color = new Color(1f, 0.42f + Hash01(index, 31) * 0.24f, 0.12f, 0.30f);
                    break;
                case StrategyFoundingAtmosphere.Rain:
                    rect.sizeDelta = new Vector2(2f, 22f + Hash01(index, 7) * 20f);
                    rect.localRotation = Quaternion.Euler(0f, 0f, -14f);
                    image.sprite = null;
                    image.color = new Color(0.60f, 0.72f, 0.84f, 0.18f + Hash01(index, 11) * 0.18f);
                    break;
                case StrategyFoundingAtmosphere.Mist:
                    rect.sizeDelta = new Vector2(90f + Hash01(index, 13) * 150f, 2f);
                    rect.localRotation = Quaternion.identity;
                    image.sprite = null;
                    image.color = new Color(0.76f, 0.84f, 0.86f, 0.035f + Hash01(index, 19) * 0.035f);
                    break;
                case StrategyFoundingAtmosphere.Fireflies:
                    float fireflySize = 2f + Hash01(index, 23) * 3f;
                    rect.sizeDelta = new Vector2(fireflySize, fireflySize);
                    rect.localRotation = Quaternion.identity;
                    image.sprite = null;
                    image.color = new Color(1f, 0.76f, 0.32f, 0.35f);
                    break;
            }
        }

        private void ResetParticle(int index, bool anywhere)
        {
            if (atmosphere == StrategyFoundingAtmosphere.Embers)
            {
                ResetDepartureParticle(index, anywhere);
                return;
            }

            float x = Mathf.Lerp(-820f, 820f, Hash01(index, anywhere ? 53 : Mathf.FloorToInt(Time.unscaledTime) + 59));
            float y = Mathf.Lerp(-460f, 460f, Hash01(index, anywhere ? 67 : Mathf.FloorToInt(Time.unscaledTime) + 71));
            particles[index].anchoredPosition = atmosphere switch
            {
                StrategyFoundingAtmosphere.Rain => new Vector2(anywhere ? x : 840f, anywhere ? y : 470f),
                StrategyFoundingAtmosphere.Mist => new Vector2(anywhere ? x : -870f, y * 0.45f),
                StrategyFoundingAtmosphere.Fireflies => new Vector2(x, anywhere ? y : -470f),
                _ => Vector2.zero
            };
        }

        private bool UpdateDepartureParticle(int index, ref Vector2 position, float time, float deltaTime)
        {
            if (index < SmokeCount)
            {
                position.x += (7f + Mathf.Sin(time * 0.28f + phases[index]) * 5f) * speeds[index] * deltaTime;
                position.y += (10f + speeds[index] * 5f) * deltaTime;
                images[index].color = WithAlpha(
                    images[index].color,
                    0.025f + (Mathf.Sin(time * 0.35f + phases[index]) + 1f) * 0.017f);
                if (position.y > 345f || position.x > 130f)
                {
                    ResetParticle(index, false);
                    return true;
                }

                return false;
            }

            if (index < AshCount)
            {
                position.x += (13f + Mathf.Sin(time * 0.72f + phases[index]) * 9f) * speeds[index] * deltaTime;
                position.y += (12f + speeds[index] * 8f) * deltaTime;
                particles[index].localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Sin(time * 0.9f + phases[index]) * 42f);
                if (position.y > 365f || position.x > 270f)
                {
                    ResetParticle(index, false);
                    return true;
                }

                return false;
            }

            position.x += (7f + Mathf.Sin(time * 1.15f + phases[index]) * 6f) * speeds[index] * deltaTime;
            position.y += (28f + speeds[index] * 12f) * deltaTime;
            images[index].color = WithAlpha(
                images[index].color,
                0.18f + (Mathf.Sin(time * 2.1f + phases[index]) + 1f) * 0.14f);
            if (position.y > 370f || position.x > 170f)
            {
                ResetParticle(index, false);
                return true;
            }

            return false;
        }

        private void ResetDepartureParticle(int index, bool anywhere)
        {
            int timeSalt = anywhere ? 0 : Mathf.FloorToInt(Time.unscaledTime * 3f);
            if (index < SmokeCount)
            {
                float smokeX = Mathf.Lerp(-365f, -105f, Hash01(index, 83 + timeSalt));
                float smokeY = anywhere
                    ? Mathf.Lerp(55f, 245f, Hash01(index, 89))
                    : Mathf.Lerp(45f, 105f, Hash01(index, 97 + timeSalt));
                particles[index].anchoredPosition = new Vector2(smokeX, smokeY);
                return;
            }

            if (index < AshCount)
            {
                float ashX = Mathf.Lerp(-385f, 95f, Hash01(index, 101 + timeSalt));
                float ashY = anywhere
                    ? Mathf.Lerp(15f, 290f, Hash01(index, 103))
                    : Mathf.Lerp(10f, 85f, Hash01(index, 107 + timeSalt));
                particles[index].anchoredPosition = new Vector2(ashX, ashY);
                return;
            }

            float emberX = Mathf.Lerp(-355f, -70f, Hash01(index, 109 + timeSalt));
            float emberY = anywhere
                ? Mathf.Lerp(20f, 305f, Hash01(index, 113))
                : Mathf.Lerp(20f, 95f, Hash01(index, 127 + timeSalt));
            particles[index].anchoredPosition = new Vector2(emberX, emberY);
        }

        private void CreateFireGlow(RectTransform parent)
        {
            glowTexture = CreateRadialTexture(64);
            glowSprite = Sprite.Create(
                glowTexture,
                new Rect(0f, 0f, glowTexture.width, glowTexture.height),
                new Vector2(0.5f, 0.5f),
                64f);
            GameObject glow = new("DepartureFireGlow", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(parent, false);
            fireGlowRect = glow.GetComponent<RectTransform>();
            fireGlowRect.anchorMin = new Vector2(0.5f, 0.5f);
            fireGlowRect.anchorMax = new Vector2(0.5f, 0.5f);
            fireGlowRect.pivot = new Vector2(0.5f, 0.5f);
            fireGlowRect.anchoredPosition = new Vector2(-245f, 112f);
            fireGlowRect.sizeDelta = new Vector2(620f, 350f);
            fireGlow = glow.GetComponent<Image>();
            fireGlow.sprite = glowSprite;
            fireGlow.raycastTarget = false;
        }

        private static RectTransform CreateReferenceFrame(RectTransform parent)
        {
            GameObject frame = new("ArtworkReferenceFrame", typeof(RectTransform));
            frame.transform.SetParent(parent, false);
            RectTransform rect = frame.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(ReferenceWidth, ReferenceHeight);
            return rect;
        }

        private void RefreshReferenceScale()
        {
            if (referenceFrame == null || transform is not RectTransform owner)
            {
                return;
            }

            float scale = Mathf.Min(owner.rect.width / ReferenceWidth, owner.rect.height / ReferenceHeight);
            if (scale > 0.001f)
            {
                referenceFrame.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void SetGlowAppearance(float time)
        {
            if (fireGlow == null)
            {
                return;
            }

            float pulse = reducedMotion
                ? 0f
                : Mathf.Sin(time * 1.17f) * 0.5f + Mathf.Sin(time * 2.43f + 0.8f) * 0.5f;
            fireGlow.color = new Color(1f, 0.29f, 0.07f, reducedMotion ? 0.065f : 0.085f + pulse * 0.012f);
            float scale = reducedMotion ? 1f : 1f + pulse * 0.015f;
            fireGlowRect.localScale = new Vector3(scale, scale, 1f);
        }

        private static Texture2D CreateRadialTexture(int size)
        {
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "Founding Journey Radial Glow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float radius = Mathf.Max(1f, center);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / radius;
                    float dy = (y - center) / radius;
                    float falloff = 1f - Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    byte alpha = (byte)Mathf.RoundToInt(falloff * falloff * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void OnDestroy()
        {
            if (glowSprite != null)
            {
                Destroy(glowSprite);
            }

            if (glowTexture != null)
            {
                Destroy(glowTexture);
            }
        }

        private static int GetActiveCount(StrategyFoundingAtmosphere value)
        {
            return value switch
            {
                StrategyFoundingAtmosphere.Rain => 28,
                StrategyFoundingAtmosphere.Mist => 10,
                StrategyFoundingAtmosphere.Fireflies => 16,
                _ => 18
            };
        }

        private static float Hash01(int index, int salt)
        {
            unchecked
            {
                uint value = (uint)(index * 374761393 + salt * 668265263);
                value = (value ^ (value >> 13)) * 1274126177u;
                return (value & 0x00ffffffu) / 16777215f;
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
