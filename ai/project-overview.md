# Project Overview

Last updated: 2026-06-11

## Identity

- Project name: `ProjectUnknown`
- Engine: Unity
- Unity Editor version: `6000.4.10f1`
- Current baseline: fresh 2D/URP starter project

## Current Shape

- `Assets/`
  Main Unity content folder.
- `Assets/Scripts/Runtime/`
  Runtime C# scripts for the MVP strategy foundation, camera, map, and UI.
- `Assets/Scenes/SampleScene.unity`
  Default starter scene.
- `Assets/InputSystem_Actions.inputactions`
  Default Input System action asset.
- `Assets/Settings/`
  URP 2D renderer, URP pipeline asset, and template scene settings.
- `Assets/DefaultVolumeProfile.asset`
  Default rendering volume profile.
- `Packages/manifest.json`
  Unity package dependencies.
- `ProjectSettings/`
  Unity project settings and editor version.
- `AI.md`, `AGENTS.md`, `ai/`
  AI collaboration and memory infrastructure.

Generated/local Unity folders:

- `Library/`
- `Logs/`
- `Temp/`
- `UserSettings/`

## Packages And Runtime Foundations

Confirmed from `Packages/manifest.json`:

- Universal Render Pipeline: `com.unity.render-pipelines.universal` `17.4.0`
- Input System: `com.unity.inputsystem` `1.19.0`
- 2D package set: animation, Aseprite import, PSD import, sprites, SpriteShape, tilemap, tilemap extras, tooling
- UI: `com.unity.ugui` `2.0.0`
- Visual Scripting: `com.unity.visualscripting` `1.9.11`
- Test Framework: `com.unity.test-framework` `1.6.0`

## Implemented Gameplay

- Runtime bootstrap creates the first MVP strategy scene layer on play, including a Unity `WindZone`-backed strategy wind source.
- Runtime debug logging writes structured gameplay diagnostics to `debug.log`, including bootstrap, map, nature, Stone, build, population, forestry, selection, time-scale, and Unity warning/error events.
- A generated 2D city map appears at runtime with randomized seed-driven terrain generation and procedural pixel-art terrain textures.
- Current terrain generation covers grass, meadow, forest, dirt, shore, and water with randomized rivers/water blobs, clustered land biomes, seeded texture variants, and transition overlays.
- A runtime nature-props layer places procedural 2.5D trees, forest groups, and bushes over the generated terrain.
- A runtime wildlife layer spawns small deer herds on suitable walkable land away from the starter camp; deer have male/female procedural 2.5D sprites, idle/walk/graze/alert/flee/rest animations, reproduce with fawns up to a 20-deer population cap, and react to nearby residents without revealing fog or blocking cells.
- The nature layer also places procedural Stone resource deposits as standalone boulders, rock clusters, and larger cliffs.
- Trees, forest groups, and bushes sway through a 2D adapter driven by the Unity `WindZone` values.
- Forest terrain cells receive dense visual forest props, while grass/meadow/dirt/shore cells can receive sparse standalone trees or bushes.
- Standalone generated trees are registered as mature forestry trees; planted saplings grow through 3 visual stages.
- Orthographic strategy camera supports map pan/scroll and zoom controls.
- Runtime time controls support F1/F2/F3 for x1/x2/x3 simulation speed.
- Storage Yard is implemented as the first storage/logistics building: it has procedural 2.5D art, up to 2 assigned workers, local Logs and Stone stock, growing visual stockpiles, resident hauling from production camps, and construction resource reservations.
- A starter Storage Yard appears near the campfire with 13 Logs and 9 Stone at the beginning of play.
- Runtime Build menu appears as the first HUD layer, inspired by the `Gruzovichky` bottom Build dock/category tray.
- Build menu infrastructure supports category cards, build item cards, Logs/Stone construction costs, active tool state, `B` open/close, numeric hotkeys, and Escape/right-click layer cancel.
- Current Build catalog has Houses/Home, Industries/Lumberjack Camp and Stonecutter Camp, and Storage/Storage Yard.
- Build placement mode is implemented for catalog tools: hover preview, valid/invalid placement coloring, left-click construction-site creation, occupied-cell blocking, non-water terrain checks, fog checks, and Storage Yard resource affordability.
- Player-built tools now construct through runtime construction sites: resources are reserved from Storage Yards, builders carry Logs/Stone to the site, residents animate hammer/build work, staged site sprites progress, and the final building appears only after progress completes.
- `Дом` uses runtime-generated 2.5D pixel-art sprites: a stable default for the menu/preview and 5 random visual variants for placed houses.
- `Лагерь дровосеков` uses runtime-generated 2.5D pixel-art sprites and supports up to 2 assigned resident workers.
- A startup camp places an animated procedural campfire and 6 initial residents: 3 men and 3 women, each with a random age from 18 to 30.
- The startup camp reserves a 3-cell clear radius where generated trees, bushes, forest groups, and other nature props are skipped.
- The initial camera view starts focused near the startup campfire with a medium-close zoom.
- Building a house construction site first tries to reserve one random free adult man and one random free adult woman from the camp as future residents instead of creating new residents; if no free future-home pair exists, general adult builders can still build a free house.
- Houses can hold up to 5 residents; adult male/female house pairs can have children after a randomized cooldown when they are not close relatives and the house is not full.
- Empty houses can accept the oldest adult child still living with parents, and single adult-child households can pull in an adult opposite-gender partner from another parental home or the free camp pool after kinship checks.
- Residents have runtime IDs, full names, family names, age, life stage, parent/child links, 5 runtime-generated male variants, 5 runtime-generated female variants, child sprites, matching portrait sprites, and idle movement around their current camp/home through nearby walkable cells.
- Children are attached to their birth house, cannot be assigned as workers/builders, idle/walk near home, grow into adults after scaled game time, and continue aging after adulthood.
- Residents assigned to a lumberjack camp walk to nearby mature trees, chop them, buck fallen trunks into Logs, carry Logs to the camp stockpile, then walk to planting cells and plant new saplings.
- Residents assigned to construction sites fetch reserved Logs/Stone from Storage Yards, deliver them to the site, and build with generated hammer animation frames.
- Lumberjack chopping now uses generated axe-swing sprite frames; impact frames shake/damage the tree, spawn woodchip/leaf effects, final tree hits make the tree fall, and final trunk hits split it into collectable Logs.
- Houses mark an expanded 2.5D visual/navigation blocker as not walkable after successful construction, not only the technical 2x2 footprint.
- Placed houses and residents are clickable world objects with a simple selection marker and right-side selection HUD.
- Placed lumberjack camps are clickable, expose worker assignment/removal in the right-side selection HUD, and keep a local visual Logs stockpile.
- Selected houses can install Garden Beds and Chicken Coop upgrades from the right-side HUD, spending small Logs/Stone costs from Storage Yard stock.
- Residents periodically work at their house's Garden Beds with a simple animation.
- Installing Chicken Coop spawns idle chickens that walk around the coop.
- Houses now have local runtime resource counts shown in the selected-house HUD.
- Garden Beds produce one assigned crop per house from: Turnip, Cabbage, Onion, Carrot, or Potato.
- Chicken Coop passively produces Eggs.
- Standalone trees, forest groups, bushes, and fallen trunks block their occupied map cells; forestry trees release their cell back to walkable after Logs are collected.
- Generated Stone deposits block their occupied map cells and are mined by assigned stonecutter residents.
- No global economy simulation, save system, or full UI shell is implemented yet.

## Source Of Truth

- Treat Unity assets, scenes, package manifest, project settings, and scripts as source of truth.
- Update this file when the project gains meaningful folders, systems, scenes, or runtime responsibilities.
