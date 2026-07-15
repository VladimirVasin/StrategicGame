# High-resolution authored building masters

These source sheets preserve the accepted authored designs for every buildable
building family, the Starter Caravan Cart, the Chicken Coop production
animation, and the modular Bridge kit. They stay outside `Assets/` so Unity does
not import the large masters into the game.

The deterministic manifests in `Tools/Art/` own source dimensions and hashes,
crop rectangles, runtime canvases, pivots, pixels per unit, output paths, and
accepted output hashes. Runtime sprites are generated under
`Assets/Resources/Visual/Authored/`; intermediate animation frames stay under
`Tools/Art/Generated/`.

Use these builders:

- `Build-AuthoredBuildingAssets.ps1` for final building sprites. Pass
  `HighResolutionBuildingAnimationFrames.manifest.json` through `-ManifestPath`
  to rebuild the six Chicken Coop animation frames.
- `Build-AuthoredConstructionAtlases.ps1` for seven-stage construction atlases.
- `Build-AuthoredBuildingAnimationAtlases.ps1` for the final Chicken Coop
  horizontal animation atlas.
- `Build-AuthoredBridgeKit.ps1` for horizontal and vertical Bridge modules and
  their construction stages.

Every builder supports `-ValidateOnly`. The animation-frame manifest uses
`legacyFrame` to select one frame from the former horizontal atlas when checking
that an authored frame is not a nearest-neighbor copy of the legacy art.

`Construction1x/` retains frozen pre-upgrade construction atlases as tool inputs;
they are not runtime assets. `GenerationPrompts.md` records the accepted source
art direction and generation provenance.
