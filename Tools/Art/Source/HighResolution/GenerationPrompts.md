# Authored Building Generation Provenance

Generation date: 2026-07-14

Tool and mode: built-in `imagegen`, new-image generation. The model is selected by the tool and is not pinned by this repository.

The generated masters were requested on an exact solid `#FF00FF` background, converted to transparency, then cropped and resampled only through the manifest-owned deterministic builders in `Tools/Art`. Runtime PNG hashes, dimensions, PPU, pivots, and target bounds are owned by the corresponding manifests.

## Shared Prompt Specification

Use the supplied ProjectUnknown geometry/style reference sheet. Create production-ready medieval settlement sprites that preserve the reference silhouette, 2.5D orthographic camera, ground contact, facing, footprint, and relative scale. Match the detailed House and Forager Camp material language: warm timber, readable masonry, cloth and roof texture, restrained outlines, rich tonal variation, and crisp high-resolution detail. Avoid coarse block quantization or a limited palette. Keep neutral daylight lighting so runtime day/night, weather, snow, shadows, and emissive effects remain authoritative.

Place each requested sprite as a fully isolated object on an exact flat `#FF00FF` background. Do not add text, labels, UI frames, scenery, terrain, a horizon, cast shadows outside the object, or cropped details. Keep generous clean separation between sheet cells and preserve transparent padding around every silhouette.

For three-variant sheets, place exactly three equal-scale variants in one horizontal row. Vary only materials and restrained construction details; preserve the same structure, footprint, perspective, ground line, and attachment regions.

## Per-Source Prompt Addenda

| Durable master | Prompt addendum |
| --- | --- |
| `LumberjackCamp-Variants.png` | Three open timber-and-canvas lumber camps with stacked logs, chopping tools, and a readable covered work bay. |
| `StonecutterCamp-Variants.png` | Three compact stonecutter shelters with stone piles, workbench, mallet/chisel details, and a canvas roof. |
| `Mine-Variants.png` | Three timber-braced mine entrances with a deep dark opening, track/ore-cart details, rock footing, and a side lantern fixture. |
| `CoalPit-Variants.png` | Three roofed coal storage/extraction sheds with a clearly readable black coal mound and timber bin. |
| `ClayPit-Variants.png` | Three low open clay pits with a warm clay bed, timber retaining frame, and stone edging; keep the center unobstructed for workers. |
| `HunterCamp-Variants.png` | Three lean-to hunter camps with bows, drying/rack details, game-preparation props, and a small lantern fixture. |
| `FisherHut-Variants.png` | Three waterside fishing shelters with dock boards, hanging net, bucket/rod details, and a compact patch of water contained inside the silhouette. |
| `Sawmill-Variants.png` | Three open timber sawmills with a prominent log, readable saw blade, stacked planks, and an unobstructed worker bay. |
| `Kiln-Variants.png` | Three masonry pottery kilns with a large warm opening, brick/stone texture, fuel and pottery props, and a readable chimney. |
| `Forge-Variants.png` | Three open-sided forges with a bright hearth opening, anvil, tools, masonry base, and chimney. |
| `StorageYard-Variants.png` | Three open storage yards with a low platform, corner posts, partial canvas canopy, and crates while leaving the center available for runtime stock layers. |
| `Granary-Variants.png` | Three compact enclosed granaries with raised timber footing, broad doors, grain sacks/jars, and warm window details. |
| `TradingPost-Variants.png` | Three wide open-front trading stalls with a shallow roof, strong side supports, counter goods, cloth rolls, and a visible right-side light fixture. |
| `StarterCaravanCart.png` | One compact founding caravan cart in side three-quarter view, including its harnessed horse, supplies, wheels, and the painted left lantern; preserve the legacy cart's wide-low aspect. |
| `ChickenCoop-Animation.png` | Exactly six equal-size frames in one horizontal row of one small enclosed chicken coop. Keep the coop registered to the same ground point while the door/interior and subtle production details change across frames. |
| `Bridge-Modular.png` | Two isolated complete three-span timber bridges: one horizontal and one vertical. Use the same plank, rope, post, and rail design; make all repeated span boundaries geometrically seamless so start, middle, and end modules can compose spans of 3-12 cells. |

House and Forager Camp masters predate this batch and retain their existing accepted provenance and manifests.
