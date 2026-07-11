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
- Runtime factories remain fallback and source-of-truth for rebaking until a sprite is manually replaced.
- Manually improved PNG assets must keep their `.meta`, dimensions, PPU, pivot, and catalog slot stable.
- Rebuilding the baseline replaces only `Visual/Baked`; hand-authored replacements should live in a sibling `Visual/Authored` folder and be assigned to the catalog after baking.

## Verification Views

- Use the deterministic 1600x900 Noon, Night, and Winter gameplay captures.
- Review common zoom first, then close and strategic zoom.
- Confirm sprite alignment, sorting, fog masking, snow caps, carried overlays, and nighttime light readability.
