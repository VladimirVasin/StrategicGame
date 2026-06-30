# Project Overview

Last updated: 2026-06-30

## Identity

- Project name: `ProjectUnknown`
- Engine: Unity
- Unity Editor version: `6000.5.0f1`
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

- Universal Render Pipeline: `com.unity.render-pipelines.universal` `17.5.0`
- Input System: `com.unity.inputsystem` `1.19.0`
- 2D package set: animation, Aseprite import, PSD import, sprites, SpriteShape, tilemap, tilemap extras, tooling
- UI: `com.unity.ugui` `2.0.0`
- Visual Scripting: `com.unity.visualscripting` `1.9.11`
- Test Framework: `com.unity.test-framework` `1.7.0`

## Implemented Gameplay

- Runtime bootstrap creates the first MVP strategy scene layer on play, including a Unity `WindZone`-backed strategy wind source, runtime Stone/Iron/Coal/Clay resource registries, runtime weather, and runtime ambience audio.
- Runtime rendering includes a calendar-aware visual day/night overlay, weather overlays, a runtime URP post-process volume for stronger dawn/dusk/night color grading/bloom/vignette, a cinematic visual layer with 2D ambient/local lights, emissive pixel masks, animated building torch/lantern source sprites with manual lit state, a light-aware nighttime darkness mask over unlit cells, wet puddle glints, lightning flashes, subtle foreground depth props, shared procedural ground/cast shadows, and reusable short-lived world effects for dust, sawdust, chips, sparks, splash, and resource pop/fade moments.
- Runtime fog of war tracks persistent explored cells separately from current visibility; camp, residents, and placed buildings reveal less during Dusk/Night/Dawn and even less during dense Fog weather, explored-but-not-visible cells become darker at night or turn into layered weather fog around visible zones, and wildlife spawn checks use a daylight-range visibility mask so temporary darkness does not create extra near-settlement spawn openings.
- Runtime debug panel opens with F9 and provides a player-fog bypass checkbox plus forced weather-state buttons for Clear, Cloudy, Light Rain, Heavy Rain, Fog, and Storm.
- Runtime debug logging writes structured gameplay diagnostics to `debug.log`, including bootstrap, audio, map, nature, wildlife, Stone, Iron, Coal, Clay, build, population, forestry, selection, time-scale, and Unity warning/error events.
- A generated 192x192 2D city map appears at runtime with randomized seed-driven terrain generation and procedural pixel-art terrain textures.
- A runtime road/trail layer records completed building-to-building route traversals for immediate stable visible route roads, renders connected procedural right-angle cardinal trail sprites, derives sparse non-blocking roadside torch props from eligible straight road cells, gives formed roads a 15% resident movement-speed bonus, and makes resident pathfinding prefer roads when practical.
- Current terrain generation covers grass, meadow, forest, dirt, shore, and water with randomized rivers/water blobs, clustered land biomes, seeded texture variants, and transition overlays; generated water/shore cells are tagged as River or Lake for technical gameplay distinction, and generated rivers expose a technical flow direction used by river animation and river fish. The runtime water overlay adds depth tint, directional river streaks, lake sparkles, shoreline foam, wet shore edges, and weather-driven rain ripples.
- A runtime nature-props layer uses seeded full-map shuffled placement plus macro cluster weighting to place procedural 2.5D trees, forest groups, bushes, Stone deposits, walkable but not normally buildable mineable underground Iron/Coal fields, and near-water walkable Clay fields over the generated terrain.
- A runtime wildlife layer spawns compact deer herds, rabbit groups, lake fish shoals, river fish entry points, wolf packs, and decorative birds only in currently hidden cells that remain within a broad ring around completed buildings or active construction sites, with the startup camp used only as a fallback anchor if no building anchor exists. Current wildlife has procedural 2.5D/pixel-art sprites, frame-based ambient/threat/work animations, and reacts to nearby residents without revealing fog or blocking cells. Deer, rabbits, and wolves can cross generated River cells with slowed swimming movement and ripples, but River cells are transit-only for them: final path targets, homes, relaxed/flee targets, and hunter/wolf prey reservations stay on land cells while land travel avoids placed buildings, active construction sites, and the campfire within a 4-cell structure buffer. Deer, rabbits, and lake fish have global and per-group reproduction/growth caps, and births/migration centers must also use hidden near-settlement cells; river fish do not reproduce and despawn at the route end; wolves now hunt rabbits/deer only when population surplus is above control thresholds, birds are decorative-only, while adult rabbits can be reserved and hunted by Hunter Camp workers for local `Game`, and adult fish can be reserved and caught by Fisher Hut workers for local `Fish`.
- The nature layer also places procedural Stone resource deposits as standalone boulders, rock clusters, and larger cliffs, plus mineable underground Iron/Coal fields and mineable near-water Clay fields as walkable multi-cell surface marks that block normal building placement, avoid adjacent incompatible resource fields, and cluster into seeded resource regions instead of using purely linear map scans.
- Trees, forest groups, and bushes sway through a 2D adapter driven by the Unity `WindZone` values.
- Forest terrain cells receive dense visual forest props, while grass/meadow/dirt/shore cells can receive sparse standalone trees or bushes.
- Standalone generated trees are registered as mature forestry trees; small trees yield 3 Logs, large trees yield 6 Logs, and planted saplings grow through 3 visual stages into large mature trees.
- Orthographic strategy camera supports map pan/scroll, zoom controls, and a `Space` shortcut that recenters on the startup campfire cell while preserving the current zoom, with max zoom-out scaled to the current 192-cell default map size.
- Runtime time controls support F1/F2/F3 for x1/x2/x3 simulation speed.
- Runtime weather randomizes Clear, Cloudy, LightRain, HeavyRain, Fog, and Storm states, drives cloud-shadow/rain/heavy-rain-mist/wet-ground overlays, feeds dense Fog weather into fog-of-war visibility, boosts wind, intensifies water ripples, and feeds rain/wind ambience.
- Runtime ambience audio loads non-generated forest birds, cicadas, night, rain, wind, and river loops from `Assets/Resources/Audio/Nature`; river ambience is spatial and follows the nearest water to the camera, while rain/wind ambience follows the current weather state.
- Runtime in-game music loads all AudioClips from `Assets/Resources/Audio/Music` as a random playlist, avoids repeating the same track twice in a row when multiple tracks exist, and pauses/resumes the current clip when the game loses/regains focus.
- Resident walking now uses non-generated grass footstep clips from `Assets/Resources/Audio/Footsteps/GrassWalk` through quiet spatial AudioSources on residents.
- Storage Yard is implemented as the first storage/logistics building: it has procedural 2.5D art, uncapped assigned Haulers/builders, uncapped local Logs, Stone, Iron, Coal, Clay, Planks, Pottery, and Tools stock, growing visual stockpiles with resource drop effects, resident hauling from production camps/Mines/Coal Pits/Clay Pits/Sawmills/Kilns/Forges, food hauling to Granaries, householder-facing Pottery pickup reservations, production-input delivery to production nodes, and construction resource reservations.
- Granary is implemented as the first food-storage building: it has procedural 2.5D art, uncapped local `Game`, `Fish`, `Eggs`, Berries, Roots, and Mushrooms stock, growing visual food stockpiles with food drop effects, and is filled by the shared settlement Hauler profession rather than a separate player-facing Granary Worker profession.
- A temporary starter Caravan Cart appears near the campfire with 20 Logs, 20 Stone, and randomized raw food covering 3 days of ration needs for the initial population; it does not accept deliveries, transfers available construction resources into a completed Storage Yard, and despawns when empty.
- Runtime Build menu appears as the first HUD layer, inspired by the `Gruzovichky` bottom Build dock/category tray.
- Build menu infrastructure supports category cards, build item cards, Logs/Stone/Planks construction costs, active tool state, `B` open/close, numeric hotkeys, and Escape/right-click layer cancel.
- Current Build catalog has Housing/House, Extraction/Lumberjack Camp, Stonecutter Camp, Mine, Coal Pit, Clay Pit, Hunter Camp, Fisher Hut, Forager Camp, and Chicken Coop, Production/Sawmill, Kiln, and Forge, Storage/Storage Yard and Granary, Trade/Trading Post, and Infrastructure/Bridge.
- Build placement mode is implemented for catalog tools: hover preview, valid/invalid placement coloring, left-click construction-site creation, occupied-cell blocking, strict foundation checks, softer future 2.5D blocker reservation, builder-access checks, non-water terrain checks, map buildability checks, fog checks, Storage Yard resource affordability, Mine/Coal Pit/Clay Pit matching-deposit-under-footprint validation with matching resource-cell exceptions, and Bridge's two-click river-bank placement.
- Player-built tools now construct through runtime construction sites: resources are reserved from physically present loose construction piles, Storage Yards, production-local Lumberjack/Stonecutter/Sawmill stock, and the low-priority starter Caravan Cart; available settlement Builders are balanced across active sites before extras stack onto a site, early construction can assign free adults into normal Builder roles when needed, residents animate hammer/build work with hit effects, delivered materials pop onto the site, staged site sprites progress, and the final building appears only after progress completes.
- Smart auto workforce assignment can be toggled from the Profession HUD; it treats player values for Construction, Food, Logistics, Wood, Stone, Planks, Iron, Coal, Clay, Pottery, and Tools as desired worker targets, rebalances overstaffed professions, and assigns eligible free adults through existing worksite APIs plus settlement-level Hauler/Builder role APIs while using shortages/backlog/readiness/distance to score urgency.
- Bridge construction selects one valid river bank cell, highlights opposite-bank candidates across contiguous River water, creates a construction site after the second bank click, and makes the final bridge span walkable across the river without changing river/lake water identity.
- `House` uses runtime-generated 2.5D pixel-art sprites: a stable default for the menu/preview and 5 random visual variants for placed houses.
- `Lumberjack Camp` uses runtime-generated 2.5D pixel-art sprites and supports up to 2 assigned resident workers.
- `Hunter Camp` uses runtime-generated 2.5D pixel-art sprites, supports up to 2 assigned resident workers, and keeps a local visual `Game` stockpile.
- `Fisher Hut` uses runtime-generated 2.5D pixel-art sprites, requires nearby water/shore access for placement, supports up to 2 assigned resident workers, and keeps a local visual `Fish` stockpile.
- `Forager Camp` uses runtime-generated 2.5D pixel-art sprites, supports up to 2 assigned Foragers, reserves generated forage nodes, and keeps local Berries/Roots/Mushrooms stock for Granary hauling.
- `Chicken Coop` uses enlarged runtime-generated chicken-coop art on a `4x4` placement footprint, needs no assigned workers, animates slightly larger idle chickens around the building, hides them inside at `Night` and releases them outside after `Night`, produces capped local `Eggs` stock on a timer, and exposes Eggs to Granary hauling or Householder fallback pickup.
- `Mine` uses runtime-generated 2.5D pixel-art sprites, requires underground Iron under its footprint for placement, supports up to 2 assigned resident workers, hides miners while they work underground, animates dust/sparks at the entrance, and keeps a local visual Iron stockpile with ore pop effects.
- `Coal Pit` uses runtime-generated 2.5D pixel-art sprites, requires underground Coal under its footprint for placement, supports up to 2 assigned resident workers, keeps coal miners visible while they work inside the pit, adds dust/coal-chip work effects, and keeps a local visual Coal stockpile.
- `Clay Pit` uses runtime-generated 2.5D pixel-art sprites, requires near-water Clay under its footprint for placement, supports up to 2 assigned Clay Diggers, keeps workers visible while they dig inside the pit, and keeps a local visual Clay stockpile.
- `Sawmill` uses runtime-generated 2.5D pixel-art sprites, supports 1 assigned resident Sawyer, consumes Logs delivered by Haulers from Storage Yard stock through a capped input-Logs buffer, keeps the sawyer visible inside the building with an animated sawing overlay plus sawdust effects, and keeps local visual Logs/Planks stockpiles.
- `Kiln` uses runtime-generated 2.5D pixel-art sprites, supports 1 assigned resident Potter, consumes Hauler-delivered Clay and Coal from Storage Yard stock, fires them into Pottery with an animated work overlay, and keeps local visual Clay/Coal/Pottery stockpiles.
- `Forge` uses runtime-generated 2.5D pixel-art sprites, supports 1 assigned resident Blacksmith, consumes Hauler-delivered Iron, Coal, and Logs from Storage Yard stock, forges `1 Iron + 1 Coal + 1 Log` into `1 Tools` with animated work sprites and sparks, and keeps local visual Iron/Coal/Logs/Tools stockpiles.
- `Granary` uses runtime-generated 2.5D pixel-art sprites, keeps uncapped local visual `Game`/`Fish`/`Eggs`/forage food stockpiles, and is serviced by shared Haulers.
- `Trading Post` uses runtime-generated 2.5D pixel-art sprites, opens a right-side trade HUD, waits for a caravan visit, and exchanges Storage Yard/Granary stock for settlement Coins or buys goods back into the nearest valid storage building.
- Production buildings keep at most 6 local resources and stop starting new production when the next unit/cycle would exceed that cap; Storage Yards and Granaries stay uncapped.
- A startup camp selects a walkable land cell at least 6 cells from generated water/shore when possible, then places an animated procedural campfire and 3 initial families; each family has a father, a mother, and 1-2 adult children with parent/child links.
- The startup campfire begins Day 1 as a lit central fire, fades out by `Noon`, blocks its own cell while burning, then releases the cell while extinguished and can later be relit by homeless residents at night.
- The startup camp reserves a 3-cell clear radius where generated trees, bushes, forest groups, and other nature props are skipped.
- The initial camera view starts focused near the startup campfire with a medium-close zoom, and pressing `Space` later recenters the camera on that campfire cell.
- Completed Houses first try to move in one whole homeless family that fits, then fall back to one random free adult man and one random free adult woman from the camp instead of creating new residents; home assignment is independent from workplace or construction assignment, and newly formed male/female household pairs apply the husband's family name to the wife.
- Houses can hold up to 5 residents; adult male/female house pairs can have children after a randomized cooldown when they are not close relatives and the house is not full.
- Houses resolve one nightly dinner after a settling grace: Householders bring raw `Fish`/`Game`/`Eggs`/forage food from Granaries, then the starter Caravan Cart while it has food, or directly from Hunter Camps/Fisher Huts/Forager Camps/Chicken Coops when no stored food is available, and Pottery from Storage Yards; they cook stored ingredients into recipe-based prepared dishes during `Dusk` only when house Pottery is available, each prepared dish consumes 1 Pottery, and dinner consumes prepared dishes first before falling back to house-local ingredients after eligible residents return home for `Night`.
- Ingredients and prepared dish recipe stacks keep separate ration values and HUD labels; recipes currently span Poor/Common/Hearty/Fine/Feast quality tiers, and each resident still has age-based ration needs plus nutrition debt.
- Empty houses can accept the oldest adult child still living with parents, and single adult-child households can pull in an adult opposite-gender partner from another parental home or the free camp pool after kinship checks; partner move-in also applies the husband's family name to the wife without changing biological parent/child IDs.
- Residents roll annual mortality from age 1, with very low youth risk, a gentler but rising 40-50 curve, stronger old-age risk after 50, and persistent family records so dead ancestors still block close-relative pairings.
- Residents with severe malnutrition receive a multiplicative annual mortality chance increase.
- Resident death now creates an animated corpse; close family/household members gather, cry/mourn, drag the dead by rope to a spontaneous cemetery, wait for reachable attendees at the grave, burial leaves a clickable grave sprite that blocks its cell, deaths with no living family/household participants use a silent one-adult service burial fallback, and minor orphans can be adopted into eligible adult households.
- The first refugee family is scripted to start no earlier than `Dusk` on Day 2; later refugee families periodically spawn inside the map about 4 cells beyond a random side of the daylight-visible fog boundary, must route to the reachable camp-side arrival area, walk to the startup campfire, pause the game with a decision dialog, and either join the settlement or return to their hidden staging point for cleanup based on player choice. Refugee family size is 1-3 members with 1-2 adult parents and optional children; arrival intensity fades after 40 accepted residents and stops at 50 accepted residents.
- The top status HUD shows total population with separate adult and child counts; clicking it opens a larger residents roster with settlement stats, filters, age, home/camp state, role, current status, and food status. A separate top-right calendar/time widget shows day number, 24-hour clock time, phase label, phase color, and day progress. The roster can open a fullscreen paused Family Trees scene that groups recorded family members into connected same-surname family cards, lays family cards out as affinity-ordered left-to-right columns with compact generation rows, and draws portrait-card parent-child trees with local parent-pair connectors, cross-family relationship lines, distinct deceased markers, always-visible gender symbols, and hover relationship labels. A compact top event log reports births, deaths, adoptions, dawn, and nightfall. The left-side goals HUD now starts with an onboarding build sequence: build 3 Houses, then build a Forager Camp, then build a Lumberjack Camp and Stonecutter Camp, with the Build menu temporarily locked to the current stage.
- Residents have runtime IDs, full names, family names, age, life stage, parent/child links, 5 runtime-generated male variants, 5 runtime-generated female variants, child sprites, matching portrait sprites, and trail-aware 8-direction A* movement with corner-cutting prevention and path smoothing around their current camp/home and job targets through nearby walkable cells.
- Assigned resident work follows the day/night schedule: new production, construction, logistics, hunting, fishing, and household-food pickup tasks use the Dawn-through-Dusk work window, household cooking starts specifically during `Dusk`, and carried resources/deposits/returns can finish after nightfall to avoid stuck reservations. Around 20:00 during `Dusk`, future night-lamp workers can gradually light and carry their own hand torches while still following ordinary idle/household walking; actual building/roadside lamp routes still begin during `Night`. During `Night`, housed idle residents path to their home, hide inside the house, and wake at the home exit after night ends.
- Children are attached to their birth house, cannot be assigned as workers/builders, stay inside the house until age 3, run daytime ambient play near home/camp after that with solo play, pair play, and tag, grow into adults at age 16 after scaled game time, and continue aging after adulthood.
- Residents assigned to a lumberjack camp choose the nearest available mature/processable tree on the map, chop it, buck fallen trunks into Logs, carry Logs to the camp stockpile, then walk to planting cells and plant new saplings.
- Residents assigned to a hunter camp reserve the nearest available adult rabbit on the map, move to roughly 2-3 tile bow range, fire animated arrows with a 20% miss chance, butcher carcasses on hit, carry `Game`, and deposit it at the camp stockpile; missed arrows stick in the ground and make the rabbit flee.
- Residents assigned to a fisher hut reserve the nearest available fish on the map, walk to valid land/shore fishing cells, cast fishing lines only while the fish remains in cast range, reel hooked fish in, carry `Fish`, and deposit it at the hut stockpile.
- Residents assigned to a forager camp reserve generated Berries/Roots/Mushrooms nodes, gather with forage animation frames, carry the food back to the camp stockpile, and leave final delivery to Granary Haulers.
- Residents assigned to a mine walk to the entrance, become hidden underground during work, mine reserved underground Iron deposits, and deposit Iron at the mine stockpile.
- Residents assigned to a coal pit walk to the pit entrance, remain visible inside the pit during work, mine reserved underground Coal deposits, and deposit Coal at the pit stockpile.
- Residents assigned to a clay pit walk to the pit entrance, remain visible inside the pit during work, mine reserved near-water Clay deposits, and deposit Clay at the pit stockpile.
- Residents assigned to a sawmill wait for Hauler-delivered input Logs, saw them into Planks while visible inside the building, and deposit Planks at the Sawmill stockpile.
- Residents assigned to a kiln wait for Hauler-delivered Clay and Coal, fire them into Pottery while visible at the Kiln, and deposit Pottery at the Kiln stockpile.
- Residents assigned to a forge wait for Hauler-delivered Iron, Coal, and Logs, forge them into Tools while visible at the Forge, and deposit Tools at the Forge stockpile.
- Residents assigned as settlement Haulers can reserve Logs, Stone, Iron, Coal, Clay, Planks, Pottery, and Tools from production worksites, carry them to a selected Storage Yard when one exists, deposit them into storage, deliver non-food production inputs such as Logs, Iron, Coal, and Clay into production nodes such as Sawmills, Kilns, and Forges, haul `Game`/`Fish`/`Eggs`/forage food to the nearest Granary, and fallback-deliver reserved construction Logs/Stone/Planks to active construction sites; Householders handle Storage Yard/Granary to House pickup plus direct production-food fallback for house-bound resources.
- Stored Granary and starter-cart `Game`/`Fish`/`Eggs`/forage food is raw ingredient stock that Householders can move into houses before cooking; households do not directly eat stored stock at dinner anymore, and direct production-source pickup is only a fallback when no stored food is available.
- Residents assigned to construction sites fetch reserved Logs/Stone/Planks from construction resource sources, deliver them to the site, and build with generated hammer animation frames.
- Lumberjack chopping now uses generated axe-swing sprite frames; impact frames shake/damage the tree, spawn woodchip/leaf effects, final tree hits make the tree fall, and final trunk hits split it into collectable Logs.
- Houses mark an expanded 2.5D visual/navigation blocker as not walkable after successful construction, not only the technical 2x2 footprint.
- Placed houses, residents, and completed graves are clickable world objects with a simple selection marker and right-side selection HUD.
- Placed lumberjack camps are clickable, expose worker assignment/removal in the right-side selection HUD, and keep a local visual Logs stockpile.
- Placed hunter camps are clickable, expose worker assignment/removal in the right-side selection HUD, and show local `Game` stock plus nearby huntable rabbit counts.
- Placed fisher huts are clickable, expose worker assignment/removal in the right-side selection HUD, and show local `Fish` stock plus nearby catchable fish counts.
- Placed mines are clickable, expose status/resource context in the right-side selection HUD, and show local Iron stock plus available underground Iron under the mine footprint.
- Placed coal pits are clickable, expose status/resource context in the right-side selection HUD, and show local Coal stock plus available underground Coal under the pit footprint.
- Placed clay pits are clickable, expose status/resource context in the right-side selection HUD, and show local Clay stock plus available near-water Clay under the pit footprint.
- Placed sawmills are clickable, expose status/resource context in the right-side selection HUD, show linked Sawyer assignments, and show local Logs/Planks stock.
- Placed granaries are clickable and show local `Game`/`Fish`/forage stock plus available food-source counts; player-facing food hauling uses the shared Hauler profession.
- Placed trading posts are clickable and show settlement Coins, caravan ETA/status, and active buy/sell offers while a caravan is trading.
- Houses now have local runtime resource counts shown in the selected-house HUD.
- Garden Beds and legacy House Chicken Coop are no longer installed from Houses; the old upgrade implementation remains inactive while external food production now includes Forager Camp and standalone Chicken Coop, with crop standalone buildings still future work.
- Standalone trees, forest groups, bushes, and fallen trunks block their occupied map cells; forestry trees release their cell back to walkable after Logs are collected.
- Generated Stone deposits block their occupied map cells and are mined by assigned stonecutter residents.
- Generated Iron fields stay walkable because ore is underground, block normal building placement, and can be reserved/mined into local Iron stock by Mines built over them.
- Generated Coal fields stay walkable because coal is underground, block normal building placement, and can be reserved/mined into local Coal stock by Coal Pits built over them.
- Generated Clay fields stay walkable, spawn only near water, block normal building placement, and can be mined by Clay Pits built over them.
- No save system or full UI shell is implemented yet; production buildings have capped local buffers, hunter-camp `Game`, fisher-hut `Fish`, and forager-camp forage food can be hauled to Granaries or picked up directly by Householders when no stored food is available, moved into houses, cooked with house Pottery into recipe-based prepared dishes, and consumed by household dinners with house-local ingredient fallback, Sawmill Planks can be hauled to Storage Yards and consumed by late construction costs, Coal and Clay can be consumed by Kilns to produce Pottery, Pottery can be hauled to Storage Yards then picked up by Householders for household cooking, Iron/Coal/Logs can be consumed by Forges to produce Tools, and Trading Posts can exchange stored goods with visiting caravans for settlement Coins.

## Source Of Truth

- Treat Unity assets, scenes, package manifest, project settings, and scripts as source of truth.
- Update this file when the project gains meaningful folders, systems, scenes, or runtime responsibilities.
