# Work Log

Last updated: 2026-06-11

## Active

- None.

## Done

### 2026-06-11 - Smaller deer visual scale

- Reduced all deer visuals by applying a shared 0.88 global scale in `StrategyDeerAgent`, affecting adult deer and fawns consistently without changing behavior, pathing, reproduction, or sprite frame generation.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Deer wildlife MVP

- Added a new runtime Wildlife subsystem with 8-12 spawned deer grouped into small herds on meadows, grass, dirt, and forest-edge walkable cells away from the starter camp.
- Added male and female deer models: bucks are larger and have antlers, does are smaller/lighter; both are generated as procedural 2.5D pixel-art sprites.
- Added frame-based deer animation sets for idle breathing, walking, grazing, alert stance, fleeing/running, and resting.
- Deer now use local walkable-cell paths, keep a loose herd/home range, do not block map cells, do not reveal fog, and react to nearby residents/noisy work by becoming alert or fleeing.
- Deer can reproduce up to a hard population cap of 20: adult does with an adult buck in the same herd can spawn small fawns, and fawns grow into adults after scaled simulation time.
- Runtime bootstrap now creates/configures `StrategyWildlifeController` after the starter Storage Yard is placed.
- Added structured debug events for wildlife generation, deer spawn/birth/growth, population changes, alert, and flee reactions.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - House upgrade resource costs

- Added Logs/Stone costs for house upgrades: Garden Beds cost 2 Logs + 1 Stone, Chicken Coop costs 4 Logs + 2 Stone.
- Upgrades now spend available Storage Yard construction resources immediately when installed, while respecting resources already reserved for construction sites.
- The selected-house HUD now shows each upgrade cost, disables unaffordable upgrade buttons, and reports missing resources separately from missing placement space.
- Added structured debug events for upgrade install success and immediate Storage Yard resource spending.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Slower resident aging

- Slowed resident aging in `StrategyResidentAgent` from 20 seconds per year to 120 seconds per year.
- Children now take roughly 36 minutes at x1 speed to grow from birth to adulthood, or about 12 minutes at x3 speed.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Construction worker position fix

- Fixed builders standing too far from construction-site visuals during hammer/build animation.
- `StrategyConstructionSite.TryFindBuildWorkCell` now prefers close front/side cells around the building's technical footprint instead of the expanded 2.5D walk-block footprint.
- Construction resource dropoff still uses the broader blocker-adjacent search so deliveries can find valid cells around the whole reserved site.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - House population ignores workplace roles

- Fixed completed houses staying empty after the first house when the remaining homeless residents already had jobs such as Storage Yard builder/logistics roles.
- `StrategyPopulationController.TryFindAvailableResident` now treats home assignment and workplace assignment as independent: homeless adults can move into houses even if they already have a workplace.
- Active construction assignments still block house pickup so residents are not pulled away from another in-progress construction site.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Starter settlement Stone guarantee

- Added a starter-area Stone guarantee in `StrategyNaturePropController`: when a campfire exclusion center exists, nature generation now places at least 5 Stone deposits within the stonecutter work radius around the settlement spawn.
- Guaranteed starter Stone is placed outside the 3-cell camp clear radius and before trees/bushes, so vegetation cannot consume all nearby mining cells.
- Starter Stone placement checks for walkable adjacent cells so stonecutters have a reachable work position.
- Added structured `Stone/StarterStoneReady` and `Stone/StarterStoneFallbackShort` debug events.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Dedicated storage-yard builders

- Added a separate Storage Yard builder crew: each yard can hire up to 2 builders in addition to its 2 logistics workers.
- Construction sites are no longer rejected just because no builder is free; if resources can be reserved, the site is placed and periodically asks Storage Yards for hired builders.
- Only hired Storage Yard builders can receive construction assignments and perform the existing material delivery plus hammer/build loop.
- Storage Yard HUD now shows separate slots for logistics workers and builders, with assign/remove actions for both roles.
- Completed houses now try to populate from the homeless adult pair pool after construction, then fall back to free-house migration/partner logic; the construction builders do not become residents automatically.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Adult child household migration

- Raised house resident capacity from 4 to 5.
- Added house registration in `StrategyPopulationController` and periodic free-house checks.
- Empty-house migration and partner lookup now support completed houses that were not immediately populated by the free camp pair.
- Empty houses now try to accept the oldest adult child still living with parents, then try to find an adult opposite-gender partner from another parental household or the free camp pool.
- Single-resident houses periodically retry partner lookup; partner checks use `StrategyKinshipUtility.CanFormCouple` to block close relatives.
- Residents now continue aging after adulthood, so oldest-child selection is meaningful over time.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Resident HUD cleanup

- Removed the selected-resident subtitle label and visual model number from the right-side resident HUD profile.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Household birth and child growth MVP

- Added resident runtime IDs, age, life stage, parent IDs, and child ID links so family relationships can be checked later before marriage/couple logic.
- Added `StrategyHouseholdState` on occupied houses: after a randomized cooldown, an adult male/female pair in the same house can have a child if the house has room and the pair is not closely related.
- Houses now cap resident slots at 4, allowing the starting couple plus up to 2 children.
- Children spawn as residents attached to the house, inherit family/parent links, idle/walk near home, cannot be assigned as builders or workers, and grow into adults after scaled game time.
- Added generated child sprites, walking frames, and portraits through `StrategyResidentSpriteFactory`; house and resident HUDs now show child portraits plus age/life-stage details.
- Worker assignment filters for lumberjack camps, stonecutter camps, storage yards, and construction now require adult residents.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Y-based world sprite sorting fix

- Fixed 2.5D draw-order bugs where farther world objects could render in front of nearer objects because buildings, nature props, residents, and resources used fixed type-based `sortingOrder` values.
- Added `StrategyWorldSorting` as the shared sorting helper: terrain/water/fog keep fixed layers, while world objects sort by their world Y anchor with small offsets for overlays and carried/stock sprites.
- Applied Y-based sorting to placed buildings, construction sites, nature props, Stone deposits, planted/felled trees, building upgrades, stockpiles, campfire, residents, chickens, and the selection marker.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Construction site building flow

- Player build placement now creates a `StrategyConstructionSite` instead of instantly creating the final building.
- Build catalog tools now use Logs/Stone construction costs; the starter Storage Yard now starts with 13 Logs and 9 Stone, exactly enough for 3 Houses, 1 Lumberjack Camp, and 1 Stonecutter Camp.
- Storage yards reserve construction resources, expose available construction stock, and let assigned builders physically pick up reserved Logs/Stone before delivering them to the site.
- Construction sites block the target footprint, render delivered material stockpiles, show staged procedural building sprites, are clickable/selectable, and complete into the final placed building only after build progress finishes.
- Residents can perform construction with a 12-frame hammer/build animation and timed progress hits; this was later narrowed to dedicated Storage Yard builders.
- Selection HUD now supports construction sites with cost, delivered resources, builder count, and progress/status context.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; mojibake scan found no new localized-code issues.

### 2026-06-10 - Larger strategy map

- Increased the default generated city map from 64x64 to 128x128 cells.
- Scaled nature generation caps for the larger area: max nature props 900 -> 3600, max Stone deposits 110 -> 440, minimum Stone deposits 28 -> 112.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Stone generation minimum fallback fix

- Fixed Stone generation sessions where the procedural chance pass could create 0 Stone deposits.
- Added a deterministic post-pass in `StrategyNaturePropController` that fills up to a minimum number of Stone deposits on valid walkable cells outside the campfire exclusion radius.
- Fixed fallback coordinate selection to modulo the map width/height so generated candidate cells stay inside the map.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Stonecutter camp mining loop

- Added `Лагерь каменотёсов` as a `Промыслы` Build menu item with a 2x2 footprint, 3 procedural 2.5D camp variants, and a growing local Stone stockpile.
- Added worker assignment for `StrategyStonecutterCamp`: up to 2 residents can become каменотёсы from the selected building HUD.
- Residents assigned to a stonecutter camp now reserve nearby Stone deposits, path to a walkable adjacent cell, play frame-based pickaxe animation, mine Stone chunks after several hits, carry Stone to camp, and deposit it into local camp stock.
- Stone deposits now support hit-driven mining with shake, crack overlay, grey chip/dust effects, per-kind hit thresholds, chunk amounts, reservation release between chunks, and depletion cleanup.
- Storage Yard logistics now reserve Stone from stonecutter camps, send storage workers to pick it up, carry it to the yard, and add it to Storage Yard Stone stock.
- Selection HUD now shows stonecutter camp worker slots/status, resident каменотёс role/statuses, and the stonecutter camp building title/context.
- Added carried Stone sprites and stonecut effect sprites; added `StrategyStonecutEffectAnimator` and kept `Assembly-CSharp.csproj` in sync.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Generated Stone deposits

- Added runtime Stone deposits to map generation: standalone Boulders, Rock Clusters, and larger Cliff deposits.
- Added `StrategyStoneResourceController` / `StrategyStoneDeposit` as the resource registry for future stone-gathering jobs.
- Extended nature generation so Stone deposits are created before vegetation on a cell, skip the campfire clear radius, avoid water, and block their occupied cells from walking/building.
- Extended `StrategyNatureSpriteFactory` with procedural 2.5D pixel-art sprites for Boulders, Rock Clusters, and Cliffs.
- Added structured `Stone/Generated` and `Stone/StoneTaken` debug events.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Starter storage Stone resource

- Added `Stone` to `StrategyResourceType`.
- Storage yards now track local Stone stock in addition to Logs.
- Starter Storage Yard now spawns with 12 Logs and 20 Stone.
- Added a separate procedural stone stockpile sprite/renderer on Storage Yard, so Stone is visible beside the Logs stock.
- Storage Yard HUD status now shows Stone count.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Starter storage yard spawn

- Startup bootstrap now places an initial Storage Yard near the campfire after fog and placement systems are configured.
- The starter Storage Yard is created through the normal placement path, so it is clickable, blocks walkability, participates in fog reveal, and appears in the selection HUD.
- The starter Storage Yard begins with 12 Logs so its stockpile is visible immediately.
- Placement searches preferred nearby offsets around the campfire and falls back to a local radius scan while avoiding the campfire cell.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Storage yard logistics MVP

- Added `StorageYard` as a new build tool/catalog item with a 3x2 footprint and procedural 2.5D yard art.
- Added `StrategyStorageYard` with up to 2 assigned workers, local `LogsStored`, and a growing visible stockpile.
- Lumberjack camps can now reserve and hand off stored Logs to storage workers, and their local stock visual decreases when Logs are picked up.
- Residents can be assigned as storage workers: they walk to a lumberjack camp with available Logs, pick them up, carry them to the storage yard, and deposit them there.
- The right-side selection HUD now supports storage yard worker slots, storage stock/status text, resident storage-worker roles, and logistics statuses.
- Added structured debug events for storage configuration, worker assignment, source reservation, pickup, delivery, and storage stock changes.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Startup campfire camera focus fix

- Hardened `StrategyCameraController.FocusOn` so startup focus is applied immediately and held through the first few `LateUpdate` frames.
- Temporarily suppresses pan/zoom input after programmatic focus so edge-pan or first-frame input cannot pull the camera away from the campfire before the player sees the start state.
- Added a structured `Camera/FocusApplied` debug event with target, size, and final camera position.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Strategy debug log instrumentation

- Added `StrategyDebugLogger` as a runtime file logger configured at the start of strategy bootstrap.
- `debug.log` is written in the project root while running in the Unity Editor and is ignored by git.
- Unity warnings, errors, and exceptions are mirrored into the same structured log.
- Added structured gameplay events for bootstrap, map generation, nature generation, build menu/tool flow, placement failures/success, population assignment, forestry, lumberjack work, world selection, and time-scale changes.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Time scale hotkeys

- Added runtime time acceleration controls: F1 sets x1, F2 sets x2, and F3 sets x3.
- Added `StrategyTimeScaleController`, bootstrapped automatically with the MVP strategy layer.
- Time acceleration updates both `Time.timeScale` and `Time.fixedDeltaTime`, and resets back to x1 when disabled.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Lumberjack logs delivery loop

- Felled forestry trees now remain as blocked bucking targets instead of disappearing after a timer.
- Lumberjacks continue working after a tree falls: they buck the fallen trunk into split Logs, pick them up, carry them to their lumberjack camp, and deposit them there before planting saplings.
- Added local `LogsStored` stock to lumberjack camps with a growing visual log pile beside the camp.
- Added procedural split-log, carried-log, and camp log-stock sprites.
- Selected lumberjack camp HUD now shows Logs count and worker statuses for bucking, carrying, and depositing Logs.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Lumberjack camp destroyed-resident cleanup fix

- Fixed lumberjack camp cleanup so destroyed Unity resident components are skipped with Unity-aware null checks instead of null-conditional calls.
- Hardened resident workplace clearing against calls on already-destroyed resident components before touching transforms or sprites.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Woodcutting animation pass

- Added cached resident woodcut sprite frames with visible axes for all male/female visual variants.
- Resident lumberjack work now advances frame-based axe swings and sends hit events to the target tree on the impact frame.
- Forestry trees now support chopped/falling/felled states instead of instant destruction.
- Tree hits show chop marks, shake the tree, and spawn procedural woodchip/leaf hit effects.
- Final hits trigger a real falling rotation; after the fall the tree becomes a temporary felled log/stump and releases walkability.
- Felled trees remain in the forestry registry until they disappear so new saplings do not immediately overlap the fresh log.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Nature walkability blockers

- Generated standalone trees now block their map cell through `StrategyForestryTree`.
- Chopped forestry trees release their map cell back to walkable.
- Forest-group and bush nature props now mark their generated cells as not walkable.
- Lumberjacks now plant from a nearby walkable cell so new saplings can immediately block their own cell without trapping the worker.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Lumberjack camp forestry MVP

- Added `Лагерь дровосеков` as a new Build catalog tool under `Промыслы`, with procedural 2.5D camp sprites and a 2x2 footprint.
- Added `StrategyForestryController` / `StrategyForestryTree` to register generated trees, support chopping mature trees, and plant saplings that grow through 3 visual stages.
- Added `StrategyLumberjackCamp` with up to 2 assigned workers and a selected-camp HUD section for assigning/removing workers.
- Residents assigned to a lumberjack camp now path to nearby trees, chop them, then path to planting cells and plant new saplings without teleporting.
- Selected-resident HUD now shows lumberjack work states.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Resident readability highlight

- Added resident readability helpers:
  - a subtle dark ground shadow under each resident
  - a synced dark silhouette outline behind the current idle/walk/work sprite
- The outline mirrors resident sprite frame, flip direction, and sorting order while leaving movement, colliders, and selection behavior unchanged.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - House HUD cleanup and build menu close behavior

- Removed the standalone Status table from the selected-house HUD.
- Moved selected-house resources and upgrades upward to close the visual gap.
- Restyled the House upgrades area into compact action rows with title, state, and add/done action labels.
- Successful placement now closes the Build menu completely instead of only clearing the active tool.
- World selection now ignores the same input frame as a successful placement, preventing the newly placed house from being auto-selected.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Resident names and house resident HUD list

- Added persistent `FullName` identity to residents.
- Added 20 male first names, 20 female first names, and 20 family names with a medieval Germanic/Nordic tone for random startup resident naming.
- Placed houses now keep references to their assigned resident agents, not only a resident count.
- Added procedural resident portrait sprites matched to resident gender and visual variant.
- Replaced the selected-house HUD summary/production blocks with a resident list showing portraits, names, and current statuses.
- Selected-resident HUD now uses the resident's full name as the title and shows their portrait.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Selection HUD UX pass

- Reworked the runtime right-side selection HUD into a wider compact object panel.
- Added selected-object preview sprites for houses and residents.
- Split house HUD content into summary, status, production, resources, and upgrade sections.
- Split resident HUD content into profile, status, and home sections using the same visual shell.
- Kept selected-house resources filtered to rows with amount `> 1`, now in wider two-column rows.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Procedural ambient sprite animations

- Added frame-based chicken animation:
  - chickens now use procedural walk frames while moving
  - idle chickens periodically play a pecking animation
- Added `StrategyWaterAnimationController`, a transparent runtime overlay for animated water waves, sparkles, and shoreline foam.
- Added animated Garden Beds and Chicken Coop sprites via `StrategyBuildingUpgradeAnimator`.
- Added house ambient overlays for all house variants with chimney smoke and flickering window light.
- Added campfire smoke/spark overlay frames on top of the existing flame animation.
- Added nature leaf frame overlays on generated trees, forest groups, and bushes while keeping existing wind sway.
- Updated `Assembly-CSharp.csproj` and Unity `.meta` files for the new runtime scripts.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Resident sprite walk animation

- Extended `StrategyResidentSpriteFactory` with cached 8-frame walking sprites for all 5 male and 5 female resident variants.
- Walk frames keep each resident variant's skin, hair, clothing, and accent colors while animating legs, feet, arms, and a small body bob.
- `StrategyResidentAgent` now switches to frame-based walk animation only while actually moving and returns to the idle sprite for idle/garden work states.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - RTS fog of war MVP

- Added `StrategyFogOfWarController` as a runtime map-visibility layer.
- Fog now keeps explored cells persistent and recalculates current visibility from the starter camp, residents, and placed buildings.
- Fog renders as a generated semi-soft texture overlay above world sprites and below screen-space UI.
- F9 toggles player fog off/on without clearing explored state; while off, placement and selection treat map cells as explored.
- Build placement now rejects cells that have not been explored yet and refreshes fog after successful placement.
- World selection ignores clicks in unexplored cells.
- Exposed runtime resident and placed-building lists as read-only sources for fog visibility.
- Updated `Assembly-CSharp.csproj` and Unity `.meta` for the new runtime map script.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Campfire clear radius for nature props

- Changed runtime bootstrap order so population creates the starter camp before nature props are generated.
- Exposed the starter camp cell from `StrategyPopulationController`.
- Extended `StrategyNaturePropController` with an optional cell-radius exclusion.
- Generated trees, bushes, forest groups, and other nature props are now skipped within 3 cells of the startup campfire.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - WindZone-driven nature sway

- Added `StrategyWindController`, which creates/configures a Unity directional `WindZone` for the runtime strategy scene.
- Added `StrategyWindSway`, a 2D sprite adapter that reads `WindZone` main strength, pulse, frequency, and turbulence.
- Runtime bootstrap now creates/configures strategy wind before generating nature props.
- Generated trees, forest groups, and bushes now receive per-prop sway phases and amplitudes, so nature gently rotates/offsets/scales with wind instead of staying static.
- Updated `Assembly-CSharp.csproj` and Unity `.meta` files for the new wind runtime scripts.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Initial camera focus on campfire

- Added a public camp-world lookup on `StrategyPopulationController`.
- Runtime bootstrap now keeps the map-center focus as fallback, then refocuses the initial camera view on the startup campfire after population startup.
- Initial campfire view uses a medium-close orthographic size so the player starts near the camp without losing surrounding context.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Residents walk to assigned houses

- Removed the teleport-style resident repositioning used during house assignment.
- Assigned camp residents now keep their current world position, receive a target cell near the new house, and walk there before returning to normal home idle behavior.
- Added a `MovingHome` resident activity state and a resident HUD status for walking to the house.
- Expanded resident path search from the old local cap to the full map; if no walkable route exists yet, the resident still walks directly to the house target instead of teleporting.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Starter campfire and finite initial residents

- Changed population startup to create an animated procedural campfire and 6 initial residents: 3 men and 3 women.
- House placement no longer creates new residents; it assigns one random free man and one random free woman from the camp when available.
- Assigned residents walk from their current camp position to walkable cells near the new house, become bound to that home, and then idle/work around it.
- Camp residents without a home now idle around the campfire and show camp/home-state labels in the resident HUD.
- Added procedural campfire sprite frames plus a runtime animator.
- House HUD now shows the real assigned resident count instead of a hardcoded value.
- Updated `Assembly-CSharp.csproj` and Unity `.meta` files for the new campfire runtime scripts.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Runtime nature props for trees, forests, and bushes

- Added `StrategyNaturePropController` to place visual nature props after map generation.
- Added `StrategyNatureSpriteFactory` with procedural 2.5D pixel-art sprites for 5 large-tree variants, 3 small-tree variants, 4 bush variants, and 3 forest-group variants.
- Forest terrain cells now receive dense tree/forest-group visuals; grass, meadow, dirt, and shore cells can receive sparse standalone trees or bushes.
- Nature layout uses `CityMapController.ActiveSeed` plus cell coordinates for deterministic placement within each generated map.
- Runtime bootstrap now creates/configures the nature-props controller after `CityMapController.GenerateMap()`.
- Nature props are visual-only for now and do not affect walkability, building placement, or resources.
- Updated `Assembly-CSharp.csproj` and Unity `.meta` files for the new runtime map scripts.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - 2.5D house walkability blocker

- Changed House placement checks to use an expanded navigation blocker instead of only the technical 2x2 footprint.
- Placed houses now block one extra cell to each horizontal side and two extra cells above the footprint, matching the current larger 2.5D sprite volume.
- The same expanded blocker is used for placement validation, occupied cells, and map walkability, so residents and future placed objects avoid the visual house area.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - More random city map generation

- Changed `CityMapController` to randomize the active generation seed by default on map generation while still exposing the active seed in runtime state.
- Replaced the fixed centered river recipe with a seed-derived generation profile: variable river direction, offset, curve, width, shoreline, and optional water blobs.
- Added multi-octave terrain noise for broader forest, meadow, grass, and dirt clusters.
- Added a light land-only smoothing pass so isolated land cells blend into neighboring terrain without disturbing water/shore boundaries.
- Terrain texture painting now receives the active randomized seed so tile variants match the generated map session.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - House HUD resource filtering

- Changed the selected-house resource HUD to hide resource entries with amount `<= 1`.
- Visible resource entries are compacted into the existing two-column grid so hidden zero/one entries do not leave holes.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Random house and resident visual variants

- Expanded `StrategyBuildingSpriteFactory` to generate and cache 5 procedural 2.5D house sprite variants.
- Build menu and placement preview still use the default house sprite, while successfully placed houses choose a random visual variant.
- Placed buildings now store their selected `VisualVariant` for future save/HUD work.
- Expanded `StrategyResidentSpriteFactory` to generate and cache 5 male and 5 female resident variants.
- Newly spawned male/female residents choose a random visual variant and store it on `StrategyResidentAgent`.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - House resources from upgrades

- Added the first house-local resource layer with `StrategyResourceType` and `StrategyHouseResourceStore`.
- Resources currently include Eggs, Turnip, Cabbage, Onion, Carrot, and Potato.
- Garden Beds now choose one deterministic crop when installed and store it on the upgrade.
- Residents add the Garden Beds crop to the owning house when they finish the garden work animation.
- Chicken Coop now passively adds Eggs to the owning house over time.
- Added runtime pixel-art HUD icons for every current resource.
- The selected-house HUD now shows a compact resource grid with icons/counts and the Garden Beds crop type.
- Updated `Assembly-CSharp.csproj` for the new runtime economy scripts.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Procedural terrain tile textures

- Added `StrategyTerrainTexturePainter` for runtime procedural terrain tile painting.
- Map cells now render as 16px pixel-art terrain tiles instead of flat single-color cells.
- Added the `Dirt` terrain kind as a buildable visual ground type.
- Current terrain textures cover Grass, Meadow, Forest, Dirt, Shore, and Water.
- Each terrain type has multiple deterministic variants selected from the map seed and cell coordinates.
- Neighbor-aware side and corner overlays blend shore/water and land/land transitions so adjacent terrain cells interact visually.
- Updated `Assembly-CSharp.csproj` for the new runtime map painter.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Residents work at Garden Beds

- Residents tied to a house now periodically choose the house's Garden Beds upgrade as a work target.
- Added resident activity states for idle movement, moving to garden, and working at garden.
- Residents path to a walkable cell next to the Garden Beds and play a simple bend/swing work animation for a few seconds.
- The resident selection HUD now updates selected resident status in real time and shows garden-work states.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Idle chickens for Chicken Coop

- Installing the Chicken Coop visual upgrade now spawns 3 idle chickens around that coop.
- Added `StrategyChickenAgent` for small local idle movement around the linked coop using walkable map cells.
- Added `StrategyChickenSpriteFactory` for a runtime-generated pixel-art chicken sprite.
- Chickens avoid targeting the coop's own footprint but do not affect walkability or economy yet.
- Updated `Assembly-CSharp.csproj` for the new runtime scripts.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - House visual upgrades via selection HUD

- Added visual-only house upgrades: Garden Beds and Chicken Coop.
- Added runtime upgrade records and a controller that places upgrade sprites near the selected house without blocking walkability.
- Generated simple pixel-art sprites for both upgrades in code.
- Extended the right-side house HUD with upgrade buttons, installed-state labels, and a short placement status message.
- Runtime bootstrap now creates/configures the building-upgrade controller.
- Updated `Assembly-CSharp.csproj` for the new runtime scripts.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Right-side selection HUD

- Added a runtime right-side selection HUD to `StrategyWorldSelectionController`.
- The HUD is a narrow full-height Screen Space Overlay panel that slides in from the right when a world object is selected.
- House selection shows building type, footprint, and resident count placeholder.
- Resident selection shows gender, home, and idle status.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Clickable houses/residents and walkable idle paths

- Added 2D click colliders to placed buildings and residents.
- Added `StrategyWorldSelectionController` for left-click world selection and a simple selection marker.
- Residents have selection priority over buildings when colliders overlap.
- Successful building placement now clears the active build tool so newly spawned objects can be clicked immediately.
- Replaced direct resident idle movement with short local grid paths over `CityMapController.IsCellWalkable`.
- This prevents residents from walking through cells blocked by house footprints during idle movement.
- Updated `Assembly-CSharp.csproj` for the new selection script.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - House residents and walkability blockers

- Added a dynamic walkability layer to `CityMapController`.
- Successful placement now marks the placed building footprint as not walkable.
- Added `StrategyPlacedBuilding` runtime records for placed buildings.
- Added `StrategyPopulationController` to spawn residents when a house is built.
- Each placed `Дом` now spawns two residents: one male and one female.
- Added `StrategyResidentAgent` for simple idle movement around the linked home.
- Added `StrategyResidentSpriteFactory` for runtime male/female resident sprites.
- Runtime bootstrap now creates/configures the population controller and wires it into placement.
- Updated `Assembly-CSharp.csproj` for the new runtime scripts.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Build menu single-item category activation

- Changed single-item Build categories to directly activate their only build tool on click.
- `Жилища` now selects/toggles `Дом` immediately, so the player does not need a second click on the item card.
- Disabled raycast targeting on the item cost badge background so decorative UI cannot intercept item-card clicks.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Larger House sprite and housing category

- Renamed the current Build category from `Medieval` to `Жилища`.
- Enlarged the generated House world sprite by lowering sprite pixels-per-unit and cropping transparent padding.
- Increased the Build item card/icon area so the House icon reads larger in the menu.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - First medieval building: House

- Added the first requested medieval building Build catalog entry: `Дом`.
- Added `StrategyBuildTool.House` with a 2x2 footprint and placement cost.
- Added `StrategyBuildingSpriteFactory` to generate/cache a 2.5D pixel-art house sprite at runtime.
- Build menu uses the generated house sprite as the item icon.
- Placement preview and placed objects use the house sprite instead of the old rectangle marker when the House tool is active.
- Updated `Assembly-CSharp.csproj` for the new runtime script.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Cleared Build catalog

- Removed all starter Build menu entries for now.
- Removed the prefilled tool enum values as well; only the empty `StrategyBuildTool.None` baseline remains.
- Build menu and placement infrastructure remain in place, but the current catalog intentionally has no buildable tools/buildings.
- Future tools/buildings should be added gradually only by explicit user request.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Build placement mode

- Added real placement mode for selected Build menu tools.
- `CityMapController` now exposes world-to-cell helpers and world cell bounds.
- `StrategyBuildMenuController` exposes active tool info, footprint, affordability, treasury spending, and active-tool cancel.
- Added `StrategyBuildPlacementController`:
  - hover preview aligned to generated map cells
  - valid/invalid preview color
  - left-click placement
  - right-click/Escape active-tool cancel
  - occupied-cell and water blocking
  - simple placed world sprites with short labels
- Runtime bootstrap now creates/configures placement.
- Updated `Assembly-CSharp.csproj` for the new placement script.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Build menu HUD MVP

- Added `StrategyBuildMenuController` as the first runtime HUD layer.
- Build menu is inspired by the `Gruzovichky` bottom Build dock/category tray pattern:
  - bottom Build button
  - category dock
  - item tray with build cards
  - cost badges and active state
  - `B`, numeric hotkeys, Escape, and right-click layered cancel
- Added a temporary SimCity-style starter catalog, later removed so future tools/buildings can be added only by explicit user request.
- Split Build menu catalog/icon definitions into `StrategyBuildMenuController.Catalog.cs` to keep runtime UI logic easier to scan.
- Runtime bootstrap now creates the Build menu automatically.
- Strategy camera now ignores mouse zoom/drag/edge-pan while the pointer is over UI.
- Updated `Assembly-CSharp.csproj` for the new runtime UI script.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Unity obsolete API warning cleanup

- Replaced `Object.FindFirstObjectByType<CityMapController>()` with `Object.FindAnyObjectByType<CityMapController>()` in the runtime strategy bootstrap.
- This removes Unity `CS0618` for the MVP bootstrap because map lookup does not require deterministic instance ordering.

### 2026-06-10 - MVP map and strategy camera

- Added runtime strategy bootstrap that creates/wires the initial play-mode scene layer without editing `SampleScene.unity`.
- Added generated 64x64 city map with terrain cell data and point-filtered sprite rendering.
- Added orthographic strategy camera controls:
  - mouse-wheel zoom
  - keyboard pan with WASD/arrows
  - right/middle mouse drag pan
  - edge pan
  - map-bound clamping
- Updated project memory for the new runtime bootstrap, map, and camera systems.
- Verification note: `Assembly-CSharp.csproj` was not present, and Unity was already running for this project, so no separate `dotnet build` or Unity batchmode compile was started.

### 2026-06-10 - AI memory infrastructure bootstrap

- Created root `AI.md` entry point.
- Created root `AGENTS.md` working contract.
- Created shared memory folder `ai/`.
- Added baseline memory files:
  - `ai/README.md`
  - `ai/project-overview.md`
  - `ai/system-tree.md`
  - `ai/systems-map.md`
  - `ai/architecture-notes.md`
  - `ai/work-log.md`
  - `ai/prompt-templates.md`
  - `ai/tutorial-scenario.md`
  - `ai/release-notes.md`
  - `ai/Design/worker-thought-tree.md`
  - `ai/Design/worker-thought-influence-matrix.md`
- Recorded current project baseline as a fresh Unity `6000.4.10f1` 2D/URP starter project with no custom gameplay systems documented yet.

## Notes For Next Agent

- Read `ai/README.md` first.
- Trust Unity assets/project files over memory if they disagree.
- Update `work-log.md` after implementation tasks.
