using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyProceduralSound
    {
        SettlementDay,
        SettlementNight,
        Fire,
        WinterWind,
        DistantWork,
        BuildComplete,
        TorchIgnite,
        ResourcePickup,
        ResourceDrop,
        WolfHowl,
        BurialDirt
    }

    public static class StrategyProceduralAudioLibrary
    {
        private const int SampleRate = 22050;
        private static readonly Dictionary<StrategyProceduralSound, AudioClip> clips = new();

        public static AudioClip Get(StrategyProceduralSound sound)
        {
            if (clips.TryGetValue(sound, out AudioClip clip) && clip != null)
            {
                return clip;
            }

            clip = sound switch
            {
                StrategyProceduralSound.SettlementDay => CreateSettlementLoop(false),
                StrategyProceduralSound.SettlementNight => CreateSettlementLoop(true),
                StrategyProceduralSound.Fire => CreateFireLoop(),
                StrategyProceduralSound.WinterWind => CreateWinterLoop(),
                StrategyProceduralSound.DistantWork => CreateImpact("Distant Work", 0.52f, 92f, 0.42f, 13),
                StrategyProceduralSound.BuildComplete => CreateBuildComplete(),
                StrategyProceduralSound.TorchIgnite => CreateTorchIgnite(),
                StrategyProceduralSound.ResourcePickup => CreateImpact("Resource Pickup", 0.24f, 170f, 0.24f, 31),
                StrategyProceduralSound.ResourceDrop => CreateImpact("Resource Drop", 0.34f, 105f, 0.38f, 47),
                StrategyProceduralSound.WolfHowl => CreateWolfHowl(),
                StrategyProceduralSound.BurialDirt => CreateImpact("Burial Dirt", 0.62f, 68f, 0.46f, 61),
                _ => null
            };
            clips[sound] = clip;
            return clip;
        }

        private static AudioClip CreateSettlementLoop(bool night)
        {
            const float seconds = 8f;
            int seed = night ? 811 : 397;
            return CreateClip(night ? "Settlement Night Bed" : "Settlement Day Bed", seconds, true, (time, random) =>
            {
                float breeze = SmoothNoise(time, random, seed) * (night ? 0.018f : 0.025f);
                float voices = Mathf.Sin(time * 6.1f + seed) * Mathf.Sin(time * 0.37f) * (night ? 0.004f : 0.012f);
                return breeze + voices;
            }, (uint)seed);
        }

        private static AudioClip CreateFireLoop()
        {
            return CreateClip("Fire Crackle Loop", 5f, true, (time, random) =>
            {
                float noise = Noise(random);
                float bed = noise * 0.022f * (0.65f + Mathf.Sin(time * 2.1f) * 0.18f);
                float crackle = Mathf.Max(0f, Mathf.Abs(noise) - 0.84f) * Mathf.Sign(noise) * 0.22f;
                return bed + crackle;
            }, 1019);
        }

        private static AudioClip CreateWinterLoop()
        {
            return CreateClip("Winter Wind Bed", 9f, true, (time, random) =>
            {
                float gust = 0.55f + Mathf.Sin(time * 0.46f) * 0.22f + Mathf.Sin(time * 0.13f + 2f) * 0.15f;
                float whistle = Mathf.Sin(time * 430f + Mathf.Sin(time * 0.7f) * 3f) * 0.004f;
                return SmoothNoise(time, random, 73) * 0.032f * gust + whistle * gust;
            }, 733);
        }

        private static AudioClip CreateImpact(string name, float seconds, float frequency, float noiseLevel, int seed)
        {
            return CreateClip(name, seconds, false, (time, random) =>
            {
                float envelope = Mathf.Exp(-time * 10f);
                float body = Mathf.Sin(time * frequency * Mathf.PI * 2f) * envelope * 0.34f;
                return body + Noise(random) * noiseLevel * envelope;
            }, (uint)seed);
        }

        private static AudioClip CreateBuildComplete()
        {
            return CreateClip("Construction Complete", 1.15f, false, (time, random) =>
            {
                float hit = Mathf.Exp(-time * 12f) * (Mathf.Sin(time * 115f * Mathf.PI * 2f) * 0.22f + Noise(random) * 0.18f);
                float chimeTime = Mathf.Max(0f, time - 0.18f);
                float chime = Mathf.Sin(chimeTime * 510f * Mathf.PI * 2f) * Mathf.Exp(-chimeTime * 3.7f) * 0.10f;
                return hit + chime;
            }, 1709);
        }

        private static AudioClip CreateTorchIgnite()
        {
            return CreateClip("Torch Ignite", 0.72f, false, (time, random) =>
            {
                float rise = Mathf.Clamp01(time * 8f);
                float fade = Mathf.Exp(-time * 3.5f);
                return Noise(random) * rise * fade * 0.16f + Mathf.Sin(time * 74f) * fade * 0.025f;
            }, 919);
        }

        private static AudioClip CreateWolfHowl()
        {
            return CreateClip("Wolf Howl", 2.15f, false, (time, random) =>
            {
                float attack = Mathf.Clamp01(time * 4.5f);
                float release = Mathf.Clamp01((2.15f - time) * 1.8f);
                float frequency = 315f + Mathf.Sin(time * 2.2f) * 38f + Mathf.Sin(time * 7.1f) * 9f;
                float voice = Mathf.Sin(time * frequency * Mathf.PI * 2f);
                voice += Mathf.Sin(time * frequency * 2.01f * Mathf.PI * 2f) * 0.24f;
                return voice * attack * release * 0.18f + Noise(random) * attack * release * 0.012f;
            }, 1217);
        }

        private static AudioClip CreateClip(string name, float seconds, bool loop, Func<float, uint, float> sample, uint seed)
        {
            int sampleCount = Mathf.CeilToInt(seconds * SampleRate);
            float[] data = new float[sampleCount];
            uint random = seed;
            float low = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                random = random * 1664525u + 1013904223u;
                float value = sample(i / (float)SampleRate, random);
                low = Mathf.Lerp(low, value, 0.18f);
                data[i] = Mathf.Clamp(low, -0.72f, 0.72f);
            }

            Normalize(data, loop ? 0.58f : 0.72f);
            if (loop)
            {
                SmoothLoopEdges(data);
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static void Normalize(float[] data, float targetPeak)
        {
            float peak = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }

            float gain = peak > 0.0001f ? Mathf.Min(8f, targetPeak / peak) : 1f;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Mathf.Clamp(data[i] * gain, -targetPeak, targetPeak);
            }
        }

        private static void SmoothLoopEdges(float[] data)
        {
            int edgeSamples = Mathf.Min(256, data.Length / 8);
            float join = (data[0] + data[data.Length - 1]) * 0.5f;
            for (int i = 0; i < edgeSamples; i++)
            {
                float blend = 1f - i / (float)edgeSamples;
                int end = data.Length - 1 - i;
                data[i] = Mathf.Lerp(data[i], join, blend);
                data[end] = Mathf.Lerp(data[end], join, blend);
            }
        }

        private static float Noise(uint random)
        {
            return ((random >> 8) & 0x00FFFFFF) / 8388607.5f - 1f;
        }

        private static float SmoothNoise(float time, uint random, int seed)
        {
            float slow = Mathf.Sin(time * 1.7f + seed * 0.13f) * 0.55f;
            float fast = Mathf.Sin(time * 4.3f + seed * 0.07f) * 0.25f;
            return Noise(random) * 0.45f + slow + fast;
        }

    }
}
