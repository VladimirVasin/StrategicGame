# Project Overview

Last updated: 2026-06-16

## Identity

- Project name: `ProjectUnknown`
- Engine: Unity
- Unity Editor version: `6000.4.11f1`
- Current baseline: fresh 2D/URP starter project

## Current Shape

- `Assets/`
  Main Unity content folder.
- `Assets/Scripts/Runtime/`
  Runtime C# scripts for the MVP strategy foundation, camera, map, visual day/night/weather, simulation, and UI.
  Runtime `.cs` files must stay at or below 500 lines; oversized classes are split into same-owner `.PartNN.cs` partial files or extracted when a real service/type boundary exists.
- `Assets/Resources/Audio/`
  Runtime-loadable non-generated ambience, footstep, and in-game music audio.
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

- Runtime bootstrap creates the first MVP strategy scene layer on play, including a Unity `WindZone`-backed strategy wind source, runtime Stone/Iron/Coal resource registries, runtime weather, and runtime ambience audio.
- Runtime rendering includes visual day/night and weather overlays plus shared procedural ground/cast shadows for buildings, construction, nature props, Stone deposits, resource piles, and small ambient agents.
- Runtime debug logging writes structured gameplay diagnostics to `debug.log`, including bootstrap, audio, map, nature, wildlife, Stone, Iron, Coal, build, population, forestry, selection, time-scale, and Unity warning/error events.
- A generated 2D city map appears at runtime with randomized seed-driven terrain generation and procedural pixel-art terrain textures.
- Current terrain generation covers grass, meadow, forest, dirt, shore, and water with randomized rivers/water blobs, clustered land biomes, seeded texture variants, and transition overlays; generated water/shore cells are tagged as River or Lake for technical gameplay distinction, and generated rivers expose a technical flow direction used by river animation and river fish.
- A runtime nature-props layer places procedural 2.5D trees, forest groups, bushes, Stone deposits, and walkable but not normally buildable mineable underground Iron/Coal fields over the generated terrain.
- A runtime wildlife layer spawns more numerous compact deer herds distributed over suitable walkable land away from the camp, keeps the first few rabbit groups in a near-camp walkable ring for early hunting while distributing later groups map-wide, spawns compact lake fish shoals on generated lake regions, one-way pass-through river fish along the river current, compact wolf packs away from settlement pressure and preferably across both river sides, and decorative birds across species-appropriate land/water habitats; current wildlife has procedural 2.5D/pixel-art sprites, frame-based ambient/threat/work animations, and reacts to nearby residents without revealing fog or blocking cells. Deer, rabbits, and wolves can cross generated River cells with slowed swimming movement and ripples while still choosing land cells for homes/targets and avoiding placed buildings, active construction sites, and the campfire within a 4-cell structure buffer. Deer, rabbits, and lake fish have global and per-group reproduction/growth caps; river fish do not reproduce and despawn at the route end; wolves now hunt rabbits/deer only when population surplus is above control thresholds, birds are decorative-only, while adult rabbits can be reserved and hunted by Hunter Camp workers for local `Game`, and adult fish can be reserved and caught by Fisher Hut workers for local `Fish`.
- The nature layer also places procedural Stone resource deposits as standalone boulders, rock clusters, and larger cliffs, plus mineable underground Iron and Coal fields as walkable multi-cell surface stains/seams that block normal building placement and do not appear adjacent to the opposite mineral.
- Trees, forest groups, and bushes sway through a 2D adapter driven by the Unity `WindZone` values.
- Forest terrain cells receive dense visual forest props, while grass/meadow/dirt/shore cells can receive sparse standalone trees or bushes.
- Standalone generated trees are registered as mature forestry trees; small trees yield 3 Logs, large trees yield 6 Logs, and planted saplings grow through 3 visual stages into large mature trees.
- Orthographic strategy camera supports map pan/scroll and zoom controls.
- Runtime time controls support F1/F2/F3 for x1/x2/x3 simulation speed.
- Runtime weather randomizes Clear, Cloudy, LightRain, HeavyRain, Fog, and Storm states, drives cloud-shadow/rain/mist/wet-ground overlays, boosts wind, intensifies water ripples, and feeds rain/wind ambience.
- Runtime ambience audio loads non-generated forest birds, cicadas, night, rain, wind, and river loops from `Assets/Resources/Audio/Nature`; river ambience is spatial and follows the nearest water to the camera, while rain/wind ambience follows the current weather state.
- Runtime in-game music loads all AudioClips from `Assets/Resources/Audio/Music` as a random playlist, avoids repeating the same track twice in a row when multiple tracks exist, and pauses/resumes the current clip when the game loses/regains focus.
- Resident walking now uses non-generated grass footstep clips from `Assets/Resources/Audio/Footsteps/GrassWalk` through quiet spatial AudioSources on residents.
- Storage Yard is implemented as the first storage/logistics building: it has procedural 2.5D art, uncapped assigned Haulers/builders, uncapped local Logs, Stone, Iron, Coal, and Planks stock, growing visual stockpiles, resident hauling from production camps/Mines/Coal Pits/Sawmills, food hauling to Granaries, and construction resource reservations.
- Granary is implemented as the first food-storage building: it has procedural 2.5D art, uncapped local `Game` and `Fish` stock, growing visual food stockpiles, and is filled by the shared Storage Yard Hauler profession rather than a separate player-facing Granary Worker profession.
- A starter Storage Yard appears near the campfire with 16 Logs and 12 Stone at the beginning of play.
- Runtime Build menu appears as the first HUD layer, inspired by the `Gruzovichky` bottom Build dock/category tray.
- Build menu infrastructure supports category cards, build item cards, Logs/Stone/Planks construction costs, active tool state, `B` open/close, numeric hotkeys, and Escape/right-click layer cancel.
- Current Build catalog has Houses/Home, Industries/Lumberjack Camp, Stonecutter Camp, Sawmill, Hunter Camp, Fisher Hut, Mine, and Coal Pit, Storage/Storage Yard and Granary, and Infrastructure/Bridge.
- Build placement mode is implemented for catalog tools: hover preview, valid/invalid placement coloring, left-click construction-site creation, occupied-cell blocking, strict foundation checks, softer future 2.5D blocker reservation, builder-access checks, non-water terrain checks, map buildability checks, fog checks, Storage Yard resource affordability, Mine/Coal Pit matching-deposit-under-footprint validation with matching mineral-cell exceptions, and Bridge's two-click river-bank placement.
- Player-built tools now construct through runtime construction sites: resources are reserved from Storage Yards, loose construction piles, and Stonecutter Camp Stone fallback; available hired builders are balanced across active sites before extras stack onto a site, residents animate hammer/build work, staged site sprites progress, and the final building appears only after progress completes.
- Smart auto workforce assignment can be toggled from the Profession HUD; it treats player values for Construction, Food, Logistics, Wood, Stone, Planks, Iron, and Coal as desired worker targets, rebalances overstaffed professions, and assigns eligible free adults through existing worksite APIs while using shortages/backlog/readiness/distance to score urgency.
- Bridge construction selects one valid river bank cell, highlights opposite-bank candidates across contiguous River water, creates a construction site after the second bank click, and makes the final bridge span walkable across the river without changing river/lake water identity.
- `House` uses runtime-generated 2.5D pixel-art sprites: a stable default for the menu/preview and 5 random visual variants for placed houses.
- `Lumberjack Camp` uses runtime-generated 2.5D pixel-art sprites and supports up to 2 assigned resident workers.
- `Hunter Camp` uses runtime-generated 2.5D pixel-art sprites, supports up to 2 assigned resident workers, and keeps a local visual `Game` stockpile.
- `Fisher Hut` uses runtime-generated 2.5D pixel-art sprites, requires nearby water/shore access for placement, supports up to 2 assigned resident workers, and keeps a local visual `Fish` stockpile.
- `Mine` uses runtime-generated 2.5D pixel-art sprites, requires underground Iron under its footprint for placement, supports up to 2 assigned resident workers, hides miners while they work underground, and keeps a local visual Iron stockpile.
- `Coal Pit` uses runtime-generated 2.5D pixel-art sprites, requires underground Coal under its footprint for placement, supports up to 2 assigned resident workers, keeps coal miners visible while they work inside the pit, and keeps a local visual Coal stockpile.
- `Sawmill` uses runtime-generated 2.5D pixel-art sprites, supports 1 assigned resident Sawyer, consumes Logs from Storage Yards or Lumberjack Camps with a capped input-Logs buffer, keeps the sawyer visible inside the building with an animated sawing overlay, and keeps local visual Logs/Planks stockpiles.
- `Granary` uses runtime-generated 2.5D pixel-art sprites, keeps uncapped local visual `Game`/`Fish` stockpiles, and is serviced by shared Haulers.
- Production buildings keep at most 6 local resources and stop starting new production when the next unit/cycle would exceed that cap; Storage Yards and Granaries stay uncapped.
- A startup camp places an animated procedural campfire and 3 initial families; each family has a father, a mother, and 1-2 adult children with parent/child links.
- The startup campfire blocks its own cell while burning, then gradually burns out, disappears, and releases the cell back to walkable.
- The startup camp reserves a 3-cell clear radius where generated trees, bushes, forest groups, and other nature props are skipped.
- The initial camera view starts focused near the startup campfire with a medium-close zoom.
- Completed Houses first try to move in one whole homeless family that fits, then fall back to one random free adult man and one random free adult woman from the camp instead of creating new residents; home assignment is independent from workplace or construction assignment, and newly formed male/female household pairs apply the husband's family name to the wife.
- Houses can hold up to 5 residents; adult male/female house pairs can have children after a randomized cooldown when they are not close relatives and the house is not full.
- Houses resolve one evening daily ration after a settling grace: house-local Eggs/crops/forage plus house-stored `Fish`/`Game` are eaten before Granary fallback stock, each food resource contributes its own ration value, and each resident has age-based ration needs plus nutrition debt.
- Householders can fetch reserved `Fish`/`Game` from the nearest reachable Granary into their own house when local ration value is low.
- Empty houses can accept the oldest adult child still living with parents, and single adult-child households can pull in an adult opposite-gender partner from another parental home or the free camp pool after kinship checks; partner move-in also applies the husband's family name to the wife without changing biological parent/child IDs.
- Residents roll annual mortality from age 1, with very low youth risk, a gentler but rising 40-50 curve, stronger old-age risk after 50, and persistent family records so dead ancestors still block close-relative pairings.
- Residents with severe malnutrition receive a multiplicative annual mortality chance increase.
- Resident death now creates an animated corpse; close family/household members gather, cry/mourn, drag the dead by rope to a spontaneous cemetery, wait for reachable attendees at the grave, burial leaves a clickable grave sprite that blocks its cell, deaths with no living family/household participants use a silent one-adult service burial fallback, and minor orphans can be adopted into eligible adult households.
- The first refugee family spawns after 3 completed houses; later refugee families periodically spawn from a map edge that can route to the reachable camp-side arrival area, walk to the startup campfire, pause the game with a decision dialog, and either join the settlement or leave the map based on player choice.
- The top status HUD shows total population with separate adult and child counts; clicking it opens a larger residents roster with settlement stats, filters, age, home/camp state, role, current status, and food status. The roster can open a fullscreen paused Family Trees scene that groups recorded family members by connected kinship components, lays family groups out as affinity-ordered left-to-right columns with compact generation rows, and draws portrait-card parent-child trees with distinct deceased markers, always-visible gender symbols, and hover relationship labels. A compact top event log reports births, deaths, and adoptions.
- Residents have runtime IDs, full names, family names, age, life stage, parent/child links, 5 runtime-generated male variants, 5 runtime-generated female variants, child sprites, matching portrait sprites, and idle movement around their current camp/home through nearby walkable cells.
- Children are attached to their birth house, cannot be assigned as workers/builders, stay inside the house until age 3, idle/walk near home after that, grow into adults at age 16 after scaled game time, and continue aging after adulthood.
- Residents assigned to a lumberjack camp choose the nearest available mature/processable tree on the map, chop it, buck fallen trunks into Logs, carry Logs to the camp stockpile, then walk to planting cells and plant new saplings.
- Residents assigned to a hunter camp reserve the nearest available adult rabbit on the map, move into bow range, fire animated arrows, butcher carcasses, carry `Game`, and deposit it at the camp stockpile.
- Residents assigned to a fisher hut reserve the nearest available fish on the map, walk to valid land/shore fishing cells, cast fishing lines only while the fish remains in cast range, reel hooked fish in, carry `Fish`, and deposit it at the hut stockpile.
- Residents assigned to a mine walk to the entrance, become hidden underground during work, mine reserved underground Iron deposits, and deposit Iron at the mine stockpile.
- Residents assigned to a coal pit walk to the pit entrance, remain visible inside the pit during work, mine reserved underground Coal deposits, and deposit Coal at the pit stockpile.
- Residents assigned to a sawmill reserve Logs, carry them into the Sawmill, saw them into Planks while visible inside the building, and deposit Planks at the Sawmill stockpile.
- Residents assigned as Storage Yard Haulers can reserve Logs, Stone, Iron, Coal, and Planks from production worksites, carry them to the Storage Yard, and deposit them into storage; the same Haulers also haul `Game`/`Fish` to the nearest Granary.
- Stored Granary `Game`/`Fish` is consumed by households for food before it becomes a broader food economy.
- Residents assigned to construction sites fetch reserved Logs/Stone/Planks from construction resource sources, deliver them to the site, and build with generated hammer animation frames.
- Lumberjack chopping now uses generated axe-swing sprite frames; impact frames shake/damage the tree, spawn woodchip/leaf effects, final tree hits make the tree fall, and final trunk hits split it into collectable Logs.
- Houses mark an expanded 2.5D visual/navigation blocker as not walkable after successful construction, not only the technical 2x2 footprint.
- Placed houses, residents, and completed graves are clickable world objects with a simple selection marker and right-side selection HUD.
- Placed lumberjack camps are clickable, expose worker assignment/removal in the right-side selection HUD, and keep a local visual Logs stockpile.
- Placed hunter camps are clickable, expose worker assignment/removal in the right-side selection HUD, and show local `Game` stock plus nearby huntable rabbit counts.
- Placed fisher huts are clickable, expose worker assignment/removal in the right-side selection HUD, and show local `Fish` stock plus nearby catchable fish counts.
- Placed mines are clickable, expose status/resource context in the right-side selection HUD, and show local Iron stock plus available underground Iron under the mine footprint.
- Placed coal pits are clickable, expose status/resource context in the right-side selection HUD, and show local Coal stock plus available underground Coal under the pit footprint.
- Placed sawmills are clickable, expose status/resource context in the right-side selection HUD, show linked Sawyer assignments, and show local Logs/Planks stock.
- Placed granaries are clickable and show local `Game`/`Fish` stock plus available food-source counts; player-facing food hauling uses the shared Hauler profession.
- Selected houses can install Garden Beds and Chicken Coop upgrades from the right-side HUD, spending small construction resource costs from Storage Yard stock.
- Residents periodically work at their house's Garden Beds with a simple animation.
- Installing Chicken Coop spawns idle chickens that walk around the coop.
- Houses now have local runtime resource counts shown in the selected-house HUD.
- Garden Beds produce one assigned crop per house from: Turnip, Cabbage, Onion, Carrot, or Potato.
- Chicken Coop produces Eggs through a cycle-driven timer synchronized with its nest/egg sprite animation.
- Standalone trees, forest groups, bushes, and fallen trunks block their occupied map cells; forestry trees release their cell back to walkable after Logs are collected.
- Generated Stone deposits block their occupied map cells and are mined by assigned stonecutter residents.
- Generated Iron fields stay walkable because ore is underground, block normal building placement, and can be reserved/mined into local Iron stock by Mines built over them.
- Generated Coal fields stay walkable because coal is underground, block normal building placement, and can be reserved/mined into local Coal stock by Coal Pits built over them.
- No global economy simulation, save system, or full UI shell is implemented yet; production buildings have capped local buffers, hunter-camp `Game` and fisher-hut `Fish` can be hauled to Granaries, moved into houses by Householders, and consumed by household daily rations, Sawmill Planks can be hauled to Storage Yards and consumed by late construction costs, while Mine Iron and Coal Pit Coal can be hauled to Storage Yards but are not consumed or used in costs yet.

## Source Of Truth

- Treat Unity assets, scenes, package manifest, project settings, and scripts as source of truth.
- Update this file when the project gains meaningful folders, systems, scenes, or runtime responsibilities.
