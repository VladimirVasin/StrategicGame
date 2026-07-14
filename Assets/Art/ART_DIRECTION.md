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
- Rebuilding the baseline replaces only `Visual/Baked`; building sprites with a matching `Visual/Authored/Buildings/...` path are validated and assigned to the catalog automatically.

## Authored House Family

- The five `80x80` House variants translate the menu architecture into gameplay scale through steeper roof masses, denser half-timber framing, stone plinths, and cool-shadow/warm-material contrast.
- Houses remain neutral daylight assets at `24 PPU` with a bottom-center `(0.5, 0.1)` pivot; runtime systems continue to own night tint, window light, weather, snow, and cast shadows.
- Fine cinematic texture must collapse into large readable pixel clusters at common gameplay zoom instead of adding painterly noise.

## Verification Views

- Use the deterministic 1600x900 Noon, Night, and Winter gameplay captures.
- Review common zoom first, then close and strategic zoom.
- Confirm sprite alignment, sorting, fog masking, snow caps, carried overlays, and nighttime light readability.
