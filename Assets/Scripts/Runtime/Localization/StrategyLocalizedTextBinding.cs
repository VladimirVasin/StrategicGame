using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public sealed class StrategyLocalizedTextBinding : MonoBehaviour
    {
        [SerializeField] private Text target;
        [SerializeField] private string table = StrategyLocalizationTables.Common;
        [SerializeField] private string key;
        [SerializeField] private bool useLiteral;
        [SerializeField, TextArea] private string englishLiteral;

        private object[] arguments = Array.Empty<object>();
        private string lastApplied;

        public Text Target => target;
        public string Table => table;
        public string Key => key;
        public bool UsesLiteral => useLiteral;

        public static StrategyLocalizedTextBinding Bind(
            Text textTarget,
            string tableName,
            string entryKey,
            params object[] formatArguments)
        {
            if (textTarget == null)
            {
                throw new ArgumentNullException(nameof(textTarget));
            }

            StrategyLocalizedTextBinding binding =
                textTarget.GetComponent<StrategyLocalizedTextBinding>()
                ?? textTarget.gameObject.AddComponent<StrategyLocalizedTextBinding>();
            binding.Configure(textTarget, tableName, entryKey, formatArguments);
            return binding;
        }

        public static StrategyLocalizedTextBinding BindLiteral(
            Text textTarget,
            string english)
        {
            if (textTarget == null)
            {
                throw new ArgumentNullException(nameof(textTarget));
            }

            StrategyLocalizedTextBinding binding =
                textTarget.GetComponent<StrategyLocalizedTextBinding>()
                ?? textTarget.gameObject.AddComponent<StrategyLocalizedTextBinding>();
            binding.ConfigureLiteral(textTarget, english);
            return binding;
        }

        public void Configure(
            Text textTarget,
            string tableName,
            string entryKey,
            params object[] formatArguments)
        {
            target = textTarget != null ? textTarget : GetComponent<Text>();
            Configure(tableName, entryKey, formatArguments);
        }

        public void Configure(
            string tableName,
            string entryKey,
            params object[] formatArguments)
        {
            table = string.IsNullOrWhiteSpace(tableName)
                ? StrategyLocalizationTables.Common
                : tableName;
            key = entryKey ?? string.Empty;
            arguments = formatArguments ?? Array.Empty<object>();
            useLiteral = false;
            englishLiteral = string.Empty;
            Refresh();
        }

        public void ConfigureLiteral(Text textTarget, string english)
        {
            target = textTarget != null ? textTarget : GetComponent<Text>();
            ConfigureLiteral(english);
        }

        public void ConfigureLiteral(string english)
        {
            englishLiteral = english ?? string.Empty;
            useLiteral = true;
            table = StrategyLocalizationTables.Legacy;
            key = StrategyLocalization.GetLegacyKey(englishLiteral);
            arguments = Array.Empty<object>();
            Refresh();
        }

        public void SetArguments(params object[] formatArguments)
        {
            arguments = formatArguments ?? Array.Empty<object>();
            Refresh();
        }

        public void Refresh()
        {
            EnsureTarget();
            if (target == null)
            {
                return;
            }

            string localized;
            if (useLiteral)
            {
                localized = StrategyLocalization.TranslateLiteral(englishLiteral);
            }
            else if (string.IsNullOrEmpty(key))
            {
                localized = string.Empty;
            }
            else
            {
                localized = StrategyLocalization.Get(table, key, arguments);
            }

            target.text = localized;
            lastApplied = localized;
        }

        private void Awake()
        {
            EnsureTarget();
        }

        private void OnEnable()
        {
            StrategyLocalization.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            StrategyLocalization.LanguageChanged -= Refresh;
        }

        private void LateUpdate()
        {
            if (!useLiteral || target == null || target.text == lastApplied)
            {
                return;
            }

            string candidate = target.text;
            if (StrategyLocalization.TryTranslateLiteral(candidate, out _))
            {
                englishLiteral = candidate;
                key = StrategyLocalization.GetLegacyKey(candidate);
                Refresh();
            }
            else
            {
                englishLiteral = candidate;
                key = StrategyLocalization.GetLegacyKey(candidate);
                lastApplied = candidate;
            }
        }

        private void EnsureTarget()
        {
            if (target == null)
            {
                target = GetComponent<Text>();
            }
        }
    }
}
