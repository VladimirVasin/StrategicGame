using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyFoundingJourneyAtmosphere : MonoBehaviour
    {
        private const int ParticleCount = 28;

        private readonly RectTransform[] particles = new RectTransform[ParticleCount];
        private readonly Image[] images = new Image[ParticleCount];
        private readonly float[] speeds = new float[ParticleCount];
        private readonly float[] phases = new float[ParticleCount];
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
            for (int i = 0; i < ParticleCount; i++)
            {
                GameObject particle = new("AtmosphereParticle" + i, typeof(RectTransform), typeof(Image));
                particle.transform.SetParent(parent, false);
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
        }

        internal void ConfigureAtmosphere(StrategyFoundingAtmosphere value)
        {
            atmosphere = value;
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
            int activeCount = GetActiveCount(atmosphere);
            for (int i = 0; i < ParticleCount; i++)
            {
                particles[i].gameObject.SetActive(!value && i < activeCount);
            }
        }

        private void Update()
        {
            if (!configured || reducedMotion)
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
                    default:
                        position.x += Mathf.Sin(time * 0.9f + phases[i]) * 5f * dt;
                        position.y += 34f * speeds[i] * dt;
                        if (position.y > 480f)
                        {
                            ResetParticle(i, false);
                            continue;
                        }

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
                case StrategyFoundingAtmosphere.Rain:
                    rect.sizeDelta = new Vector2(2f, 22f + Hash01(index, 7) * 20f);
                    rect.localRotation = Quaternion.Euler(0f, 0f, -14f);
                    image.color = new Color(0.60f, 0.72f, 0.84f, 0.18f + Hash01(index, 11) * 0.18f);
                    break;
                case StrategyFoundingAtmosphere.Mist:
                    rect.sizeDelta = new Vector2(90f + Hash01(index, 13) * 150f, 2f);
                    rect.localRotation = Quaternion.identity;
                    image.color = new Color(0.76f, 0.84f, 0.86f, 0.035f + Hash01(index, 19) * 0.035f);
                    break;
                case StrategyFoundingAtmosphere.Fireflies:
                    float fireflySize = 2f + Hash01(index, 23) * 3f;
                    rect.sizeDelta = new Vector2(fireflySize, fireflySize);
                    rect.localRotation = Quaternion.identity;
                    image.color = new Color(1f, 0.76f, 0.32f, 0.35f);
                    break;
                default:
                    float emberSize = 2f + Hash01(index, 29) * 4f;
                    rect.sizeDelta = new Vector2(emberSize, emberSize);
                    rect.localRotation = Quaternion.identity;
                    image.color = new Color(1f, 0.42f + Hash01(index, 31) * 0.24f, 0.12f, 0.30f);
                    break;
            }
        }

        private void ResetParticle(int index, bool anywhere)
        {
            float x = Mathf.Lerp(-820f, 820f, Hash01(index, anywhere ? 53 : Mathf.FloorToInt(Time.unscaledTime) + 59));
            float y = Mathf.Lerp(-460f, 460f, Hash01(index, anywhere ? 67 : Mathf.FloorToInt(Time.unscaledTime) + 71));
            particles[index].anchoredPosition = atmosphere switch
            {
                StrategyFoundingAtmosphere.Rain => new Vector2(anywhere ? x : 840f, anywhere ? y : 470f),
                StrategyFoundingAtmosphere.Mist => new Vector2(anywhere ? x : -870f, y * 0.45f),
                StrategyFoundingAtmosphere.Fireflies => new Vector2(x, anywhere ? y : -470f),
                _ => new Vector2(x, anywhere ? y : -470f)
            };
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
