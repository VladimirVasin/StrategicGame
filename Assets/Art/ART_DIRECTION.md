# ProjectUnknown Art Direction

## Visual Identity

- Grounded medieval settlement pixel art with readable 2.5D silhouettes.
- World assets use point filtering and no mipmaps.
- Light comes from the upper-left; cast shadows extend down-right.
- Materials use restrained hue families: warm timber, cool stone/metal, muted cloth, natural foliage, and readable fire accents.
- Detail density increases near buildings and roads while open terrain keeps larger quiet areas.

## Scale And Pivots

- Terrain and trails: 16 pixels per world cell.
- Buildings: preserve source PPU, normally 24, with a bottom-center world pivot.
- Residents: preserve 32 PPU and align every pose to one shared foot pivot per atlas.
- Nature: preserve each source PPU and pivot so forestry footprints and shadows remain stable.
- UI icons: 32 pixel source grid, point filtered, displayed at integer-friendly sizes.

## Readability Rules

- A building must be recognizable by silhouette before color or small props.
- Residents keep a dark one-pixel outer contour and distinct skin, hair, tunic, and tool layers.
- Carried resources and tools may extend outside the body frame but must preserve the foot pivot.
- Snow, darkness, fog, and weather may shift value and saturation but must not erase silhouettes.
- Roads use one-cell cardinal masks and may not create filled 2x2 blocks.

## Asset Pipeline

- `Assets/Resources/Visual/StrategyVisualCatalog.asset` is the runtime catalog.
- `Assets/Resources/Visual/Baked` contains baseline PNG assets produced by the Editor baker.
- `Assets/Resources/Visual/Authored` contains durable hand-improved replacements and mirrors the relative `Baked` paths it overrides.
- Runtime factories remain fallback and source-of-truth for rebaking until a sprite is manually replaced.
- Manually improved PNG assets must keep their `.meta`, dimensions, PPU, pivot, and catalog slot stable.
- Rebuilding the baseline replaces only `Visual/Baked`; building sprites and construction atlases with matching `Visual/Authored/Buildings/...` or `Visual/Authored/Construction/...` paths are validated and assigned to the catalog automatically.

## Authored House Family

- The five `80x80` House variants translate the menu architecture into gameplay scale through steeper roof masses, denser half-timber framing, stone plinths, and cool-shadow/warm-material contrast.
- Houses remain neutral daylight assets at `24 PPU` with a bottom-center `(0.5, 0.1)` pivot; runtime systems continue to own night tint, window light, weather, snow, and cast shadows.
- Each variant's chimney mouth and lower window panes have matching runtime effect anchors/masks in `StrategyHouseAmbientSpriteFactory`; update those profiles and their EditMode coverage whenever the authored House geometry changes.
- Fine cinematic texture must collapse into large readable pixel clusters at common gameplay zoom instead of adding painterly noise.

## Authored House Construction

- Each House variant owns one `644x82` atlas containing seven horizontal `92x82` stages at `24 PPU` with a bottom-center `(0.5, 0.1)` pivot.
- Stages grow monotonically from survey marks through foundation, frame, walls, and roofing; stages 3-6 progressively inherit the selected House variant's structure and roof material.
- Stage 6 uses the accepted final House pixels with only removable scaffolding around them, so completion changes construction dressing instead of replacing the building silhouette.
- Runtime systems own delivered resource piles, workers, hammer effects, cast shadows, weather, snow, and night lighting; construction atlases must not duplicate those layers.

## Authored Forager Camp

- The single `88x58` Forager Camp sprite reads as an open timber-and-canvas woodland work shelter with baskets, a preparation table, gathered herbs, and a distinct lantern hook.
- The camp uses `24 PPU` and a bottom-center `(0.5, 0.2)` pivot so its compact ground line stays aligned with its `2x2` technical footprint and the surrounding 2.5D blocker.
- Runtime systems own the hanging lantern body, flame, glow, darkness cutout, live forage-stock marker, cast shadow, weather, and snow; keep those dynamic layers out of the completed base sprite.
- Keep this family at one final visual variant unless an explicit future art pass expands the runtime and save contracts together.

## Authored Forager Camp Construction

- The Forager Camp owns one `644x82` atlas containing seven horizontal `92x82` stages at `24 PPU`.
- Its construction pivot is `(0.5, 11.6 / 82)` so the completed `88x58` sprite preserves the same `(44, 11.6)` source pivot after being embedded with two pixels of horizontal padding.
- Stages progress from survey marks through stone footing, platform, timber posts, upper frame, canvas shell, and the completed camp with removable scaffolding.
- Stage 6 preserves every visible completed-camp pixel exactly; rebuild the atlas with `Tools/Art/Build-ForagerCampConstructionAtlas.ps1` after changing the accepted final sprite or construction storyboard.

## Verification Views

- Use the deterministic 1600x900 Noon, Night, and Winter gameplay captures.
- Review common zoom first, then close and strategic zoom.
- Confirm sprite alignment, sorting, fog masking, snow caps, carried overlays, and nighttime light readability.
