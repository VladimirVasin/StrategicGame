# ProjectUnknown Localization

Russian and English are developed together. Every new player-facing string must be
added in both languages in the same change. Russian is the default for first launch;
the saved `settings.language` preference can select English on later launches.

## Source catalogs

Author UTF-8 TSV files in `Assets/Localization/Source/`. The required columns are:

```text
Key<TAB>English<TAB>Russian
```

An optional fourth `Smart` column accepts `true` or `false`. Entries containing
format placeholders are detected as Smart Strings automatically. Use literal `\n`
inside a TSV cell for a runtime line break. Tabs and physical multiline cells are
not supported.

Use stable lowercase dotted semantic keys such as `menu.continue` or
`building.sawmill.title`. Do not derive semantic keys from the displayed English
text. English and Russian must be non-empty and must expose the same placeholder
set.

Each official collection has a primary `<Table>.tsv` source. Independent work can
be split into deterministic `<Table>.*.tsv` fragments; the generator merges them
and rejects duplicate keys. Collections are `Common`, `Menu`, `Founding`, `Hud`,
`Buildings`, `Resources`, `Residents`, `Stories`, and `Legacy`.

## Generation

In Unity, run `ProjectUnknown > Localization > Generate String Tables`. For batch
generation, use:

```text
-executeMethod ProjectUnknown.Strategy.EditorTools.StrategyLocalizationAssetGenerator.GenerateAndExit
```

The generator creates the official RU/EN locale, settings, fallback, and preloaded
String Table assets under `Assets/Localization/Generated/`. TSV is the source of
truth; do not hand-edit generated table assets.

## Legacy bridge

`Legacy` exists only to migrate remaining hardcoded `UnityEngine.UI.Text` values.
At import, its keys are deterministic hashes of the exact English source. The
runtime bridge translates only an exact catalog match and restores the retained
English source when English is selected. New UI should use semantic keys and
`StrategyLocalization.Get` or `StrategyLocalizedTextBinding` instead.
