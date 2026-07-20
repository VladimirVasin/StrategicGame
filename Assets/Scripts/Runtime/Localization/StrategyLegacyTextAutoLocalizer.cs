using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DefaultExecutionOrder(32000)]
    internal sealed class StrategyLegacyTextAutoLocalizer : MonoBehaviour
    {
        private const float DiscoveryInterval = 0.75f;
        private const float RefreshInterval = 0.10f;
        private const string ObjectName = "Strategy Legacy Text Auto Localizer";

        private static StrategyLegacyTextAutoLocalizer instance;

        private readonly Dictionary<EntityId, TrackedText> tracked =
            new Dictionary<EntityId, TrackedText>();
        private readonly List<EntityId> removals = new List<EntityId>();
        private float nextDiscovery;
        private float nextRefresh;

        internal static void Install()
        {
            if (instance != null || !Application.isPlaying)
            {
                return;
            }

            GameObject host = new GameObject(ObjectName);
            host.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(host);
            instance = host.AddComponent<StrategyLegacyTextAutoLocalizer>();
        }

        internal static void ResetStatics()
        {
            instance = null;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnEnable()
        {
            StrategyLocalization.LanguageChanged += HandleLanguageChanged;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            nextDiscovery = 0f;
            nextRefresh = 0f;
        }

        private void OnDisable()
        {
            StrategyLocalization.LanguageChanged -= HandleLanguageChanged;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            if (now >= nextDiscovery)
            {
                DiscoverTexts();
                nextDiscovery = now + DiscoveryInterval;
            }

            if (now >= nextRefresh)
            {
                RefreshDynamicSources();
                nextRefresh = now + RefreshInterval;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            nextDiscovery = 0f;
        }

        private void HandleLanguageChanged()
        {
            ApplyAll();
            nextDiscovery = 0f;
        }

        private void DiscoverTexts()
        {
            Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Include);
            for (int i = 0; i < texts.Length; i++)
            {
                Text candidate = texts[i];
                if (candidate == null
                    || candidate.GetComponent<StrategyLocalizedTextBinding>() != null)
                {
                    continue;
                }

                EntityId id = candidate.GetEntityId();
                if (tracked.ContainsKey(id)
                    || !StrategyLocalization.TryTranslateLiteral(
                        candidate.text, out string localized))
                {
                    continue;
                }

                TrackedText entry = new TrackedText(candidate, candidate.text);
                tracked.Add(id, entry);
                Apply(entry, localized);
            }
        }

        private void RefreshDynamicSources()
        {
            removals.Clear();
            foreach (KeyValuePair<EntityId, TrackedText> pair in tracked)
            {
                TrackedText entry = pair.Value;
                if (entry.Target == null
                    || entry.Target.GetComponent<StrategyLocalizedTextBinding>() != null)
                {
                    removals.Add(pair.Key);
                    continue;
                }

                if (string.Equals(entry.Target.text, entry.LastApplied, StringComparison.Ordinal))
                {
                    continue;
                }

                string candidate = entry.Target.text;
                if (!StrategyLocalization.TryTranslateLiteral(candidate, out string localized))
                {
                    removals.Add(pair.Key);
                    continue;
                }

                entry.EnglishSource = candidate;
                Apply(entry, localized);
            }

            for (int i = 0; i < removals.Count; i++)
            {
                tracked.Remove(removals[i]);
            }
        }

        private void ApplyAll()
        {
            removals.Clear();
            foreach (KeyValuePair<EntityId, TrackedText> pair in tracked)
            {
                TrackedText entry = pair.Value;
                if (entry.Target == null)
                {
                    removals.Add(pair.Key);
                    continue;
                }

                string localized = StrategyLocalization.CurrentLanguage
                    == StrategyGameLanguage.English
                    ? entry.EnglishSource
                    : StrategyLocalization.TranslateLiteral(entry.EnglishSource);
                Apply(entry, localized);
            }

            for (int i = 0; i < removals.Count; i++)
            {
                tracked.Remove(removals[i]);
            }
        }

        private static void Apply(TrackedText entry, string localized)
        {
            entry.LastApplied = localized;
            entry.Target.text = localized;
        }

        private sealed class TrackedText
        {
            internal TrackedText(Text target, string englishSource)
            {
                Target = target;
                EnglishSource = englishSource;
                LastApplied = target.text;
            }

            internal Text Target { get; }
            internal string EnglishSource { get; set; }
            internal string LastApplied { get; set; }
        }
    }
}
