# Architecture Notes

Last updated: 2026-06-11

## Current Architecture

- The project is a Unity 2D/URP project with a thin runtime-generated MVP strategy layer.
- `StrategyGameBootstrap` runs after scene load, creates/wires the map, strategy wind, forestry, population, nature props, and camera without scene YAML dependencies, focuses the initial camera view on the startup campfire once population startup has created it, and configures nature after population so the camp clear radius can be excluded.
- `StrategyDebugLogger` is created before other strategy systems, writes structured session lines to project-root `debug.log` in the Unity Editor, mirrors Unity log messages, and exposes static `Info`/`Warn`/`Error` helpers for gameplay events.
- `CityMapController` owns basic map-cell data, randomizes an active generation seed by default, derives a terrain-generation profile from that seed, and renders the current map as a generated point-filtered texture on a `SpriteRenderer`.
- `StrategyTerrainTexturePainter` paints the generated map texture with active-seed 16px pixel-art variants for Grass, Meadow, Forest, Dirt, Shore, and Water, including neighbor-aware side/corner transition overlays.
- `StrategyNaturePropController` is created after map generation and places trees, forest groups, and bushes over generated terrain using `CityMapController.ActiveSeed` and cell kinds; these props mark their occupied cells as not walkable.
- `StrategyNaturePropController` supports a cell-radius exclusion and currently skips generated nature props within 3 cells of the startup campfire.
- `StrategyNaturePropController` also places generated Stone deposits before vegetation on a cell: standalone Boulders, Rock Clusters, and larger Cliffs that register with `StrategyStoneResourceController` and block their occupied footprints.
- When the startup campfire exclusion center is known, `StrategyNaturePropController` guarantees at least 5 starter-area Stone deposits within `StrategyStonecutterCamp.WorkRadius`, outside the camp clear radius and with adjacent walkable work cells.
- `StrategyWildlifeController` runs after starter placement, finds suitable walkable land away from the campfire, spawns small deer herds, and manages deer reproduction up to a 20-deer population cap; `StrategyDeerAgent` owns deer sex/life stage, fawn growth, state transitions, local pathing, resident/noisy-work threat checks, and frame-based idle/walk/graze/alert/flee/rest animation.
- `StrategyStoneResourceController` / `StrategyStoneDeposit` track runtime Stone resource nodes with deposit kind, footprint, remaining Stone amount, and reservation hooks for future worker jobs.
- `StrategyForestryController` / `StrategyForestryTree` register generated standalone trees as mature forestry targets, block tree cells while trees exist, support hit-driven chopping, animate tree shake/cut marks/falling, keep fallen trunks as bucking targets, split them into pickup Logs, release walkability after Logs are taken, and plant saplings that grow through 3 visual stages.
- `StrategyWindController` creates/configures a Unity directional `WindZone` as the runtime wind source; `StrategyWindSway` adapts the wind values into 2D sprite sway for generated trees, forest groups, and bushes.
- `StrategyTimeScaleController` is runtime-created by bootstrap and maps F1/F2/F3 to x1/x2/x3 simulation speed by updating `Time.timeScale` and `Time.fixedDeltaTime`.
- `StrategyNatureSpriteFactory` generates and caches procedural 2.5D pixel-art sprites for large trees, small trees, bushes, forest groups, Stone boulders/clusters/cliffs, felled trunks, split Logs, and carried Logs.
- `StrategyResourceType`, `StrategyHouseResourceStore`, and `StrategyResourceIconFactory` define the current house-local resource layer and runtime HUD icons.
- `CityMapController` also owns a dynamic walkability layer used by placement/population for runtime blockers.
- `StrategyCameraController` owns orthographic navigation, initial focus requests, and clamps movement to the map bounds.
- `StrategyBuildMenuController` owns the first runtime HUD: a bottom Build button, category dock, item tray, Logs/Stone construction costs, active build-tool state, resource affordability, and full menu closing after placement.
- `StrategyBuildPlacementController` owns MVP placement: map-cell hover preview, validation, construction-site creation, construction resource reservation, occupied cells, final placed runtime building records, expanded 2.5D walk-blocker footprints for houses, and tool-specific worksite component creation after completion; construction sites can be accepted while waiting for hired Storage Yard builders.
- `StrategyBuildPlacementController` closes the full Build menu after successful construction-site placement and marks the placement frame so world selection does not auto-select the new site from the same click.
- Bootstrap uses `StrategyBuildPlacementController.TryPlaceStarterStorageYard` to create the initial Storage Yard near the campfire through the same placed-building path as player construction.
- `StrategyPlacedBuilding` is the runtime record attached to placed buildings; it stores tool, origin, footprint, footprint bounds, selected visual variant, assigned resident references, installed visual upgrades, exposes installed upgrade records, owns the house resource store, and has a 2D click collider.
- `StrategyConstructionSite` is the runtime bridge between placement and final buildings: it stores final tool metadata, target footprint, reserved/delivered resources, up to 2 hired builders, staged construction sprites, delivered material stockpiles, progress, and completion into a final placed building; it periodically requests builders from Storage Yards while waiting.
- `StrategyStorageYard` is the current storage/logistics worksite: it stores local Logs and Stone, supports up to 2 assigned logistics workers plus up to 2 hired builders, finds production camps with available camp stock, reserves Logs/Stone for haulers and construction sites, provides construction pickup cells, dispatches hired builders to waiting sites, and updates visible Logs/Stone stockpiles.
- `StrategyLumberjackCamp` remains the lumber production worksite, but its local Logs stock can now be reserved and picked up by storage workers.
- `StrategyBuildingUpgradeController` installs house upgrades near placed houses. It currently supports Garden Beds and Chicken Coop, charges small Logs/Stone costs from available Storage Yard resources, assigns produced resources to upgrades, uses map walkability only as a placement filter, does not block cells after installation, and spawns idle chickens when Chicken Coop is installed.
- `StrategyBuildingUpgradeSpriteFactory` generates and caches runtime pixel-art sprites for the current visual upgrades.
- `StrategyPopulationController` creates a startup camp with an animated procedural campfire, spawns 3 male and 3 female initial residents, assigns random Germanic/Nordic-style full names and ages 18-30, owns the runtime resident ID and house registries, and populates completed houses from the homeless adult pair pool before falling back to adult-child migration/partner lookup; workplace roles do not prevent home assignment.
- `StrategyPopulationController` periodically checks empty and single-resident houses: empty houses receive the oldest adult child still living with parents, and single adult-child households try to pull in an adult opposite-gender partner from another parental home or the free camp pool while using kinship checks.
- `StrategyHouseholdState` lives on occupied house buildings and runs the randomized household birth timer; it asks `StrategyPopulationController` to spawn children only for adult male/female pairs that pass `StrategyKinshipUtility` close-relative checks and when the house has a free resident slot.
- `StrategyResidentAgent` stores a persistent runtime full name, family name, age, life stage, parent IDs, and child ID links, drives simple idle movement around the current camp/home through short walkable grid paths using `CityMapController.IsCellWalkable`, and owns visual-only readability child renderers for a synced silhouette outline plus ground shadow; children idle/walk near home, cannot be assigned to work/build, grow into adults after scaled game time, and continue aging after adulthood. Assigned adult residents also periodically path to their home Garden Beds, play a short work animation, and add the Garden Beds crop to the owning house when work completes. Residents assigned to a lumberjack camp path to nearby trees, play frame-based axe swings, send tree hit events on impact frames, wait for falling to finish, buck the fallen trunk into Logs, carry Logs to their camp stockpile, then plant saplings nearby. Residents hired as Storage Yard builders fetch reserved Logs/Stone from Storage Yards, deliver materials to construction sites, then build with frame-based hammer swings and timed progress hits.
- Storage workers are also driven by `StrategyResidentAgent`: they reserve camp Logs/Stone, walk to the source camp, pick up resources, carry them to the storage yard, and deposit them.
- `StrategyCampfireSpriteFactory` and `StrategyCampfireAnimator` generate and cycle the current campfire animation frames at runtime.
- `StrategyChickenAgent` drives small idle chickens around their linked Chicken Coop using the same early local walkability approach.
- `StrategyDeerSpriteFactory` generates procedural 2.5D male/female deer sprite frames, including larger antlered bucks and smaller does, for idle breathing, walking, grazing, alert, running, and resting behaviors.
- `StrategyBuildingSpriteFactory` generates and caches runtime building sprites in code; House has 5 larger 2.5D variants, Lumberjack Camp has 3 variants, Stonecutter Camp has 3 variants, and Storage Yard has 3 variants. Variant 0 is used by menu/preview and random variants are used for placed supported buildings.
- `StrategyResidentSpriteFactory` generates and caches 5 male and 5 female adult resident sprites, child resident sprites, walk frames, woodcut axe-swing frames, stonecut pickaxe frames, construction hammer/build frames, and matching portrait sprites at runtime.
- `StrategyBuildingSpriteFactory` now also generates Storage Yard variants plus storage yard Logs and Stone stockpile sprites.
- `StrategyConstructionSpriteFactory` generates staged construction-site sprites and delivered construction Logs/Stone stockpile sprites.
- `StrategyWoodcutEffectAnimator` generates short procedural woodchip/leaf/dust hit effects for tree chopping.
- `StrategyChickenSpriteFactory` generates and caches the current runtime chicken sprite.
- `StrategyWorldSelectionController` handles left-click world selection for placed buildings/construction sites/residents, ignores the placement-completion frame, displays a runtime selection marker, owns the right-side selection HUD panel, shows selected-object previews/status/context, shows selected-house resident portrait rows, exposes compact house visual-upgrade action rows, shows selected-house resource icons/counts, and shows selected-lumberjack-camp worker slots with assign/remove actions.
- `StrategyWorldSelectionController` also shows storage yard logistics-worker and builder slots, staff statuses, Logs/Stone stock, available source count, and construction-site cost/delivery/builder/progress context.
- Rendering is configured through URP assets under `Assets/` and `Assets/Settings/`.
- `StrategyWorldSorting` centralizes 2.5D draw order: terrain/water/fog/preview use fixed bands, while world sprites use a world-Y anchor so lower/nearer objects render in front of higher/farther ones.
- Input is available through Unity's Input System. MVP camera, Build menu, fog toggle, and time-scale controls read direct keyboard/mouse devices instead of the default action asset.
- Scene content is currently the default sample scene/template setup.

## Dependencies

- Unity Editor: `6000.4.10f1`
- URP: `17.4.0`
- Input System: `1.19.0`
- Unity Test Framework: `1.6.0`
- 2D package stack is installed for sprites, tilemaps, animation, Aseprite/PSD import, and SpriteShape.

## Current Hotspots

- Runtime bootstrap is global for every loaded scene. Future menus/multi-scene flow may need an explicit boot scene or mode gate.
- Map generation/rendering is texture-based and lightweight for the MVP, now using a randomized seed profile for terrain kinds, a procedural terrain painter for visual variants/transitions, and separate runtime nature props that can block cells. Future per-cell interaction may still need Tilemap, mesh picking, or a dedicated map model/service.
- Camera and Build menu controls use direct Input System device reads. Future configurable controls should move to input actions.
- Build placement is runtime-only and creates construction sites that finalize into runtime building records. It does not yet create durable city state, save data, zoning, or long-term economy effects.
- House resources are runtime-only and local to each placed house. Garden Beds currently cost 2 Logs/1 Stone and produce one assigned crop through resident work; Chicken Coop costs 4 Logs/2 Stone and passively produces Eggs. There is no global economy, save data, storage cap, trade, or consumption yet.
- Storage yard Logs and Stone are runtime-only and local to each placed storage yard; storage logistics now feeds local construction reservations, but does not yet feed a global economy, save data, or consumption loop.
- Generated Stone deposits are runtime-only map resource nodes mined by assigned stonecutters; starter-area deposits are guaranteed near the settlement spawn, but there is no Stone regrowth loop yet.
- Wildlife is runtime-only and visual/ambient for now. Deer do not block walkability, reveal fog, provide resources, save, or interact with hunting/economy systems yet; reproduction is capped locally by `StrategyWildlifeController`.
- Population is runtime-only. Residents currently idle locally around camp/home with short grid paths, move into houses from a finite starter-camp pool after house construction while that pool exists, can migrate from parental homes into empty houses as adult children, can form simple opposite-gender household pairs with kinship checks, can have children inside occupied houses, can be manually assigned to work camps/storage yards as adults, and can be hired as Storage Yard builders for construction, but do not have needs, save data, immigration, marriage UI, or broader economy effects.
- Construction resource costs are currently runtime-local and paid through Storage Yard Logs/Stone reservations; no treasury, revenue, taxes, upkeep, or global stock service exists yet.
- Future risk will likely appear next in scene bootstrapping, input routing, economy state ownership, and map persistence.

## Refactor Notes

- Keep map generation, map simulation, and map rendering separable once economy/zoning starts.
- Keep Build menu catalog/selection separate from placement rules. Add tools/buildings gradually by explicit user request.
- Move occupancy and placed-building records out of the placement controller when persistence or simulation starts.
- Move bootstrap from global runtime initialization to an explicit scene/root object if the project gains menus, save loading, or multiple game modes.
- Avoid larger abstractions until the first economy/zoning loop exists.

## Verification Notes

- For text/config-only memory changes, verify file presence and basic text integrity.
- For future `.cs` changes, run `dotnet build Assembly-CSharp.csproj -v:minimal` when Unity has generated that project file.
- For future Unity asset/scene changes, prefer a Unity editor or Play Mode sanity check when feasible.
- Current workspace did not have `Assembly-CSharp.csproj` when the first MVP map/camera scripts were added.
