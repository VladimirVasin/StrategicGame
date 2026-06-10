# Systems Map

Last updated: 2026-06-11

Use this file as the first navigation pass before broad searches. Owner cards are starting points, not hard boundaries.

## System Owner Map

### Project Foundation

Responsibilities:

- Unity version and project-wide settings.
- Package dependency baseline.
- Generated solution/project files if Unity or local tooling creates them.

Primary files/assets:

- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/`
- `Packages/manifest.json`
- `Packages/packages-lock.json` if present
- `ProjectUnknown.slnx`

Impact hints:

- Package and Unity version changes can affect rendering, input, tests, generated project files, and serialization.
- Update `project-overview.md`, `system-tree.md`, and this owner map when major dependencies or project structure change.

### Rendering Foundation

Responsibilities:

- URP pipeline setup.
- 2D renderer configuration.
- Default volume/rendering profiles.
- Shared Y-based sorting constants/helper for 2.5D world sprites.

Primary files/assets:

- `Assets/UniversalRenderPipelineGlobalSettings.asset`
- `Assets/DefaultVolumeProfile.asset`
- `Assets/Settings/UniversalRP.asset`
- `Assets/Settings/Renderer2D.asset`
- `Assets/Scripts/Runtime/Core/StrategyWorldSorting.cs`

Impact hints:

- Rendering settings affect scene appearance globally.
- World sprites should use `StrategyWorldSorting` instead of fixed type-based `sortingOrder` values so farther objects do not render in front of nearer ones.
- Verify scenes visually in Unity after meaningful changes.

### Scene Foundation

Responsibilities:

- Starter scenes and scene templates.
- Future scene flow and bootstrapping once implemented.

Primary files/assets:

- `Assets/Scenes/SampleScene.unity`
- `Assets/Settings/Scenes/URP2DSceneTemplate.unity`
- `Assets/Settings/Lit2DSceneTemplate.scenetemplate`

Impact hints:

- Scene edits can implicitly depend on rendering, input, UI, and gameplay scripts.
- Update this map when new gameplay scenes, bootstrap scenes, or scene loading systems are added.

### Runtime Bootstrap

Responsibilities:

- Start the MVP strategy layer when a scene loads.
- Configure the strategy debug logger before other runtime systems start.
- Ensure a city map exists.
- Ensure a Unity `WindZone`-backed strategy wind source exists.
- Ensure a usable orthographic main camera exists.
- Wire camera bounds to generated map bounds.
- Focus the initial camera view on the startup campfire after population creates it.
- Configure water/shore animation after map generation.
- Create/configure the Stone resource registry before nature generation.
- Configure nature props after the starter camp exists so generated props can avoid the campfire clear radius.
- Configure fog of war after population, placement, and map controllers exist.
- Place the starter Storage Yard near the campfire with initial Logs and Stone after placement is configured.
- Create/configure runtime wildlife after starter placement so deer avoid the camp and occupied cells.
- Create the runtime time-scale controller for F1/F2/F3 speed controls.

Primary files/assets:

- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assets/Scripts/Runtime/Core/StrategyDebugLogger.cs`
- `Assets/Scripts/Runtime/Core/StrategyTimeScaleController.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.cs`
- `Assets/Scripts/Runtime/Map/StrategyWaterAnimationController.cs`
- `Assets/Scripts/Runtime/Map/StrategyStoneResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyWindController.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.cs`
- `Assets/Scenes/SampleScene.unity`

Impact hints:

- Bootstrap runs through `RuntimeInitializeOnLoadMethod` and does not require scene YAML wiring.
- Any future menu, multi-scene, loading, or mode system should decide whether bootstrap remains global or becomes scene-specific.

### Strategy Debug Logging

Responsibilities:

- Create a structured runtime `debug.log` for gameplay debugging.
- Mirror Unity log messages, warnings, errors, and exceptions into the same file.
- Provide static event helpers for strategy systems without forcing scene references.
- Record important events and failure reasons for bootstrap, map generation, nature/Stone generation, build menu/tool flow, placement, population, forestry, wildlife, lumberjack camps, selection, and time-scale changes.

Primary files/assets:

- `Assets/Scripts/Runtime/Core/StrategyDebugLogger.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `.gitignore`
- `Assembly-CSharp.csproj`

Impact hints:

- In the Unity Editor, `debug.log` is written at the project root and should remain ignored by git.
- Prefer meaningful state-change, command, failure, and completion events over per-frame logging.
- Add future categories through `StrategyDebugLogger.Info/Warn/Error` so log formatting remains consistent.

### Generated City Map

Responsibilities:

- Hold basic map-cell data for the strategy MVP.
- Generate a visible 2D terrain map at runtime.
- Randomize the active map seed by default and derive a generation profile from it.
- Generate variable rivers, shorelines, optional water blobs, and clustered land terrain.
- Paint procedural pixel-art terrain textures for generated map cells.
- Render animated water waves, sparkles, and shoreline foam as a transparent overlay.
- Feed generated cell kinds and active seed into the visual nature-props layer.
- Feed generated land cells and active seed into Stone deposit generation.
- Provide the campfire exclusion center used by nature generation to guarantee starter-area Stone.
- Expose map bounds and cell buildability for future zoning/economy systems.
- Track dynamic walkability blockers for placed buildings and early agents.
- Host runtime fog-of-war exploration and visibility state.

Primary files/assets:

- `Assets/Scripts/Runtime/Map/CityMapController.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.cs`
- `Assets/Scripts/Runtime/Map/StrategyWaterAnimationController.cs`
- `Assets/Scripts/Runtime/Map/StrategyTerrainTexturePainter.cs`
- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.cs`
- `Assets/Scripts/Runtime/Map/StrategyNatureSpriteFactory.cs`
- `Assets/Scripts/Runtime/Map/StrategyStoneResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyStoneDeposit.cs`
- `Assets/Scripts/Runtime/Map/StrategyForestryController.cs`
- `Assets/Scripts/Runtime/Map/StrategyForestryTree.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Current map is runtime-generated with a randomized active seed by default and is not saved.
- Current terrain painter covers Grass, Meadow, Forest, Dirt, Shore, and Water with seeded variants and neighbor transition overlays.
- Terrain kind generation now uses a seed-derived profile plus multi-octave noise; texture painting consumes the active seed.
- Nature prop placement consumes the active seed and generated cell kinds.
- Stone deposit placement consumes the active seed and generated cell kinds.
- Generated standalone tree props register as mature forestry trees and block their cells.
- Forest groups and bushes remain non-interactive but block their cells.
- Generated Stone deposits register as Boulder, Rock Cluster, or Cliff resource deposits and block their cells.
- Future placement/economy work should reuse `CityMapCell`/bounds rather than duplicating map dimensions.
- Future movement/pathfinding should use `IsCellWalkable` rather than terrain kind alone.
- Rendering is currently a generated point-filtered texture on a `SpriteRenderer`, not a Tilemap.
- Water and shore animation is a separate transparent `SpriteRenderer` overlay above the static map and below world props.

### Forestry MVP

Responsibilities:

- Track runtime tree entities that can be chopped or grown.
- Register generated standalone trees as mature forestry trees.
- Mark tree cells as not walkable while the tree exists.
- Release chopped tree cells back to walkable.
- Plant saplings that grow through 3 visual stages.
- Provide mature-tree, fallen-trunk, split-Logs, and planting-cell targets to lumberjack camps.
- Animate chop damage, tree shake, falling, fallen-trunk bucking, split Logs, and hit effects.

Primary files/assets:

- `Assets/Scripts/Runtime/Map/StrategyForestryController.cs`
- `Assets/Scripts/Runtime/Map/StrategyForestryTree.cs`
- `Assets/Scripts/Runtime/Map/StrategyWoodcutEffectAnimator.cs`
- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.cs`
- `Assets/Scripts/Runtime/Map/StrategyNatureSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyLumberjackCamp.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Forestry is runtime-only and is not saved yet.
- Existing mature tree props can be chopped, while planted saplings use growth sprites and wind sway.
- Current tree cells block walkability; fallen trunks stay blocked until their Logs are collected; residents path to nearby walkable cells when working.
- Tree chopping and fallen-trunk bucking are hit-driven by resident axe animation frames; final tree hits start falling and final trunk hits create split Logs.
- Felled trees remain in the registry until Logs are collected, so planting does not overlap the fresh log.
- Future wood resources, regrowth balance, and forest ownership should extend this subsystem instead of adding tree logic directly to residents or HUD.

### Fog of War

Responsibilities:

- Track persistent explored map cells and current visible cells.
- Render a generated texture overlay above world sprites and below screen-space UI.
- Use the starter camp, residents, and placed buildings as visibility sources.
- Provide exploration checks to placement and world selection.
- Toggle player fog off/on with F9 without clearing explored state.

Primary files/assets:

- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Fog state is runtime-only and is not saved yet.
- Placement currently requires explored cells, not current visibility.
- Selection ignores clicks in unexplored cells, while the overlay visually hides world sprites.
- F9 hides the fog overlay and makes map cells count as explored for player placement/selection until toggled back on.
- Future scouting, enemies, stealth, minimap, or save/load should extend this subsystem instead of duplicating visibility arrays.

### Nature Props

Responsibilities:

- Place visual trees, forest groups, bushes, and Stone deposits over generated terrain.
- Generate and cache runtime 2.5D pixel-art nature sprites.
- Use `CityMapController.ActiveSeed` plus cell coordinates for deterministic prop layout per generated map.
- Guarantee a small starter Stone field within stonecutter work distance around the startup campfire.
- Make `Forest` cells read as dense forest while adding sparse standalone trees/bushes to other land terrain.
- Attach wind-sway animation to trees, forest groups, and bushes using the runtime strategy wind source.
- Add procedural leaf frame overlays to trees, forest groups, and bushes.
- Skip generated nature props inside the startup campfire's 3-cell clear radius.
- Skip generated Stone deposits inside the same startup campfire clear radius.
- Place starter Stone outside the clear radius before vegetation so nearby trees/bushes do not consume all accessible mining cells.

Primary files/assets:

- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.cs`
- `Assets/Scripts/Runtime/Map/StrategyNatureSpriteFactory.cs`
- `Assets/Scripts/Runtime/Map/StrategyStoneResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyStoneDeposit.cs`
- `Assets/Scripts/Runtime/Map/StrategyNatureFrameAnimator.cs`
- `Assets/Scripts/Runtime/Map/StrategyForestryController.cs`
- `Assets/Scripts/Runtime/Map/StrategyForestryTree.cs`
- `Assets/Scripts/Runtime/Map/StrategyWoodcutEffectAnimator.cs`
- `Assets/Scripts/Runtime/Map/StrategyWindController.cs`
- `Assets/Scripts/Runtime/Map/StrategyWindSway.cs`
- `Assets/Scripts/Runtime/Map/CityMapController.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Standalone tree props are registered for forestry chopping and block walkability/build placement.
- Forest groups and bushes are still non-interactive but block walkability/build placement.
- Stone deposits are mined by stonecutter workers, block walkability/build placement while present, and are registered as Stone resource nodes.
- Starter Stone placement verifies that each guaranteed deposit has adjacent walkable work cells for stonecutters.
- Bootstrap creates/configures the wind controller, creates population so the camp cell is known, then configures nature after `CityMapController.GenerateMap()`.
- Unity `WindZone` does not animate 2D sprites directly; `StrategyWindSway` adapts its values to sprite rotation/offset/scale.
- Leaf frame overlays complement wind sway and should stay visual-only unless future forestry gameplay needs extra real prop state.
- Future clearing or wood resources should extend the Forestry MVP registry instead of duplicating generated decoration data.

### Wildlife MVP

Responsibilities:

- Spawn ambient deer herds on suitable walkable land away from the starter camp.
- Generate and cache runtime 2.5D pixel-art deer sprites for male bucks and female does.
- Animate deer idle breathing, walking, grazing, alert stance, fleeing/running, and resting.
- Keep deer on local walkable-cell paths inside loose herd/home ranges without blocking map cells.
- React to nearby residents and noisy work by switching to alert/flee states.
- Let adult does reproduce when an adult buck is nearby in the same herd.
- Spawn fawns that grow into adults after scaled simulation time.
- Keep deer reproduction under the hard 20-deer runtime population cap.

Primary files/assets:

- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyDeerAgent.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyDeerSpriteFactory.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assets/Scripts/Runtime/Core/StrategyWorldSorting.cs`
- `Assets/Scripts/Runtime/Map/CityMapController.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Wildlife is runtime-only and not saved yet.
- Deer do not reveal fog, block walkability, or provide resources yet.
- Deer pathing depends on `CityMapController.IsCellWalkable` and should stay local/cheap until a shared pathfinding service exists.
- Reproduction is owned by `StrategyWildlifeController`; `StrategyDeerAgent` owns sex, life stage, growth, movement, and animation state.
- Future hunting, butchering, leather/meat resources, predators, mortality, or animal HUD should extend this subsystem instead of adding animal behavior into population or nature-prop code.

### Stone Resources MVP

Responsibilities:

- Track generated Stone resource deposits.
- Register standalone Boulders, Rock Clusters, and Cliffs placed by nature generation.
- Store deposit footprint, kind, remaining Stone amount, and reservation state for stonecutter jobs.
- Keep occupied deposit cells blocked from walking/build placement while deposits exist.
- Provide nearby-deposit lookup for stonecutter camps.
- Animate mining hits with shake, cracks, chip/dust effects, chunk extraction, and depletion cleanup.

Primary files/assets:

- `Assets/Scripts/Runtime/Map/StrategyStoneResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyStoneDeposit.cs`
- `Assets/Scripts/Runtime/Map/StrategyStonecutEffectAnimator.cs`
- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.cs`
- `Assets/Scripts/Runtime/Map/StrategyNatureSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyStonecutterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Stone deposits are runtime-only and are not saved yet.
- Nature generation guarantees at least 5 Stone deposits within `StrategyStonecutterCamp.WorkRadius` of the startup campfire when the campfire cell is known.
- Stone production currently flows from deposits to `StrategyStonecutterCamp` local stock, then optionally to `StrategyStorageYard` via storage workers.
- Stone deposit walkability is footprint-based and should be respected by placement and resident pathing through `CityMapController.IsCellWalkable`.
- Future quarries or richer Stone production should extend this registry instead of scanning visual sprites directly.

### Strategy Camera

Responsibilities:

- Orthographic map navigation.
- Initial medium-close campfire focus from runtime bootstrap.
- Mouse-wheel and keyboard zoom.
- WASD/arrow/edge/drag panning.
- Camera clamping to map bounds.

Primary files/assets:

- `Assets/Scripts/Runtime/Camera/StrategyCameraController.cs`

Impact hints:

- Controls currently read direct Input System devices, not generated input actions.
- Mouse zoom/drag/edge pan are suppressed while the pointer is over UI.
- UI, placement, and selection systems should coordinate with right/middle drag and edge-pan behavior when added.

### Strategy Time Scale

Responsibilities:

- Provide runtime simulation speed controls.
- Map F1/F2/F3 to x1/x2/x3 speed.
- Keep `Time.fixedDeltaTime` synchronized with `Time.timeScale`.
- Reset simulation speed back to x1 when the controller is disabled.

Primary files/assets:

- `Assets/Scripts/Runtime/Core/StrategyTimeScaleController.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Current controls read direct Input System keyboard state, not generated input actions.
- Time scale affects gameplay timers using scaled `Time.deltaTime`; UI using `Time.unscaledDeltaTime` should remain visually stable.
- Future pause, speed HUD, or settings should extend this controller instead of adding separate `Time.timeScale` writes.

### Build Menu HUD

Responsibilities:

- Runtime-created Build menu inspired by `Gruzovichky` bottom Build dock.
- Bottom Build button.
- Category cards and item tray.
- Build item cards with Logs/Stone construction costs, affordability state, and active state.
- Current catalog entries: `Жилища` / `Дом`, `Промыслы` / `Лагерь дровосеков` and `Лагерь каменотёсов`, and `Хранилища` / `Склад`.
- Hotkeys for open/close, category/item selection, and layered cancel.
- EventSystem/Input System UI setup when the scene has no UI event module.
- Add tools/buildings gradually only by explicit user request.
- Single-item categories behave as direct build-tool buttons.

Primary files/assets:

- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Catalog.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyLumberjackCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyStonecutterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Economy/StrategyConstructionResourceCost.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- The menu owns selected active build tool data and reads Storage Yard construction resources for affordability.
- Placement reads `StrategyBuildMenuController.ActiveTool` / active tool info.
- Current catalog has user-requested buildings only: `Дом`, `Лагерь дровосеков`, `Лагерь каменотёсов`, and `Склад`; do not add more without a user request.
- Current `Жилища` category directly activates `Дом` because it has one item.
- Current `Промыслы` category opens a tray with `Лагерь дровосеков` and `Лагерь каменотёсов`.
- Current `Хранилища` category directly activates `Склад` because it has one item.
- Successful placement asks the menu to close all open layers and records the placement frame.
- If a full HUD/menu shell appears later, decide whether this controller remains standalone or becomes part of the HUD shell.

### Build Placement

Responsibilities:

- Convert mouse world position to map cells.
- Show selected-tool placement preview.
- Validate terrain, bounds, affordability, and occupied cells.
- Reject unexplored fog cells unless player fog is toggled off.
- Create construction sites for player build tools before final buildings exist.
- Use generated building sprites when a tool has art.
- Choose random house visual variants for placed houses while keeping menu/preview art stable.
- Choose random lumberjack camp visual variants for placed camps while keeping menu/preview art stable.
- Choose random stonecutter camp visual variants for placed camps while keeping menu/preview art stable.
- Choose random storage yard visual variants for placed storage yards while keeping menu/preview art stable.
- Add ambient smoke/window-light overlays to placed houses.
- Reserve construction Logs/Stone from Storage Yards before accepting a construction site.
- Mark occupied cells when construction sites are accepted.
- Create runtime placed-building records with selected visual variant data after construction completes.
- Mark construction/final building walk-blocker cells as not walkable.
- House uses an expanded 2.5D visual/navigation blocker around and above its technical footprint.
- Lumberjack camp places a `StrategyLumberjackCamp` worksite component, blocks its technical 2x2 footprint plus one visual row above, and hosts a local visual Logs stockpile.
- Stonecutter camp places a `StrategyStonecutterCamp` worksite component, blocks its technical 2x2 footprint plus one visual row above, and hosts a local visual Stone stockpile.
- Storage yard places a `StrategyStorageYard` worksite component, blocks its technical 3x2 footprint plus one visual row above, and hosts local visual Logs/Stone stockpiles.
- Accepted construction sites request up to 2 hired Storage Yard builders and can wait if none are free yet.
- Completed house sites ask population to populate the finished house separately from the construction crew.
- Seed placed-building records used by later visual upgrades.

Primary files/assets:

- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyPlacedBuilding.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyLumberjackCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyStonecutterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Build/StrategyHouseAmbientAnimator.cs`
- `Assets/Scripts/Runtime/Build/StrategyHouseAmbientSpriteFactory.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.cs`
- `Assets/Scripts/Runtime/Map/CityMapController.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.cs`
- `Assets/Scripts/Runtime/Map/StrategyForestryController.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.cs`
- `Assets/Scripts/Runtime/Economy/StrategyConstructionResourceCost.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Placement is runtime-only and is not saved yet.
- Placed objects use tool-specific sprites when available; unknown future tools still fall back to colored sprites/TextMesh labels.
- Build placement consults fog exploration state, so early expansion starts around the camp and other revealed areas unless player fog is toggled off with F9.
- House ambient overlays are visual-only child sprites and should not be used for footprint/collider calculations.
- With the current catalog, `Дом`, `Лагерь дровосеков`, `Лагерь каменотёсов`, and `Склад` can be selected and placed only where their technical footprint and tool-specific walk blocker fit on buildable terrain.
- Successful player placement creates a construction site, closes the full Build menu, and marks the frame so world selection ignores the placement click.
- Construction site placement depends on reservable Logs/Stone, not on immediately available builders; waiting sites retry hired-builder dispatch.
- Final building creation happens through construction-site completion, not the original placement click.
- Future zoning/economy should replace or extend the placed marker with durable city state.
- Occupancy currently lives in the placement controller; move it into a city/map state service when save/load or simulation appears.

### House Visual Upgrades

Responsibilities:

- Install visual/production upgrades for placed houses from the selected-house HUD.
- Charge small Logs/Stone costs from available Storage Yard resources when upgrades are installed.
- Track installed upgrade types on each placed house.
- Generate and cache upgrade sprites at runtime.
- Find nearby walkable cells for upgrade visuals without changing map walkability yet.
- Provide installed Garden Beds records for resident work behavior.
- Assign a produced resource to Garden Beds and Chicken Coop.
- Spawn idle chickens when a Chicken Coop upgrade is installed.
- Animate installed Garden Beds and Chicken Coop sprites with procedural frames.
- Show upgrade costs and affordability state in the selected-house HUD.

Primary files/assets:

- `Assets/Scripts/Runtime/Build/StrategyBuildingUpgrade.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingUpgradeAnimator.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingUpgradeController.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingUpgradeSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyPlacedBuilding.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Economy/StrategyConstructionResourceCost.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceType.cs`
- `Assets/Scripts/Runtime/Economy/StrategyHouseResourceStore.cs`
- `Assets/Scripts/Runtime/Population/StrategyChickenAgent.cs`
- `Assets/Scripts/Runtime/Population/StrategyChickenSpriteFactory.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Current upgrades are visual/behavioral/resource-producing: Garden Beds chooses one crop and resident work adds that crop to the owning house; Chicken Coop spawns idle chickens and passively adds Eggs.
- Current upgrade costs are intentionally small: Garden Beds 2 Logs/1 Stone, Chicken Coop 4 Logs/2 Stone.
- Upgrade installation spends available Storage Yard stock immediately and respects construction reservations.
- Upgrade sprite animation remains visual-only and does not change upgrade footprint or walkability.
- Placement uses `CityMapController.IsCellWalkable` to avoid houses/water but keeps residents free to walk through the visual upgrade cells for now.
- Future production/upkeep effects should extend this subsystem and the house resource layer instead of putting production logic directly into the HUD.

### House Resources MVP

Responsibilities:

- Define the first resource types.
- Store resource counts locally on placed houses.
- Generate runtime pixel-art resource icons for HUD display.
- Provide resource display ordering for the selected-house HUD.

Primary files/assets:

- `Assets/Scripts/Runtime/Economy/StrategyResourceType.cs`
- `Assets/Scripts/Runtime/Economy/StrategyHouseResourceStore.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceIconFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyPlacedBuilding.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingUpgrade.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Current resources are house-local runtime counts, not global economy inventory.
- Current resource sources are Garden Beds work completion and Chicken Coop passive egg production.
- Future trade, taxes, storage caps, spoilage, and needs should decide whether house stores remain local or feed into a settlement-level resource service.

### Storage Yard Logistics

Responsibilities:

- Add `Склад` as a placed storage building with local Logs and Stone stock.
- Spawn a starter Storage Yard near the campfire with 13 Logs and 9 Stone.
- Assign up to 2 residents as storage workers.
- Hire up to 2 additional residents as dedicated construction builders.
- Find lumberjack camps with available stored Logs and reserve stock for haulers.
- Find stonecutter camps with available stored Stone and reserve stock for haulers.
- Reserve Logs/Stone for accepted construction sites.
- Provide reserved construction resource pickup cells for builders.
- Dispatch hired builders to waiting construction sites.
- Route storage workers to source camps, pick up Logs, carry them to storage, and deposit them.
- Route storage workers to stonecutter camps, pick up Stone, carry it to storage, and deposit it.
- Update lumberjack/stonecutter camp and storage yard stock visuals as resources move, and show Stone as a separate storage pile.
- Show separate storage worker and builder slots, staff statuses, Logs/Stone stock, and available source count in the selection HUD.

Primary files/assets:

- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Build/StrategyLumberjackCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyStonecutterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceType.cs`
- `Assets/Scripts/Runtime/Economy/StrategyConstructionResourceCost.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Catalog.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Storage workers reserve camp Logs/Stone before walking to prevent multiple haulers from targeting the same stock.
- Construction resources are reserved against Storage Yard stock at site creation, then physically removed when builders pick them up.
- Residents currently support one active workplace: lumberjack camp, stonecutter camp, storage logistics, or storage builder crew.
- Storage is runtime-only and does not yet feed a global economy, save data, or consumption loop.
- Future resources should extend the logistics stock model; current Logs and Stone still have explicit carrying visuals/states.

### Population MVP

Responsibilities:

- Create the starter camp with an animated campfire.
- Expose the starter camp world position for the initial camera focus.
- Spawn the initial 6 residents at startup: 3 men and 3 women.
- Assign random Germanic/Nordic-style full names and age 18-30 to startup residents.
- Track resident runtime IDs, age, life stage, parent links, and child links.
- Track placed house records for household migration checks.
- Attach household birth state to occupied houses.
- Check close kinship through resident parent/child links for future family/couple rules.
- Populate completed houses from the homeless adult male/female pool when possible, even if those residents already have workplaces.
- Fall back to free-house migration and partner lookup when no free pair can immediately occupy a completed house.
- Bind assigned residents to their home building.
- Spawn children for valid adult male/female house pairs after randomized household cooldowns when house capacity allows.
- Grow children into adults after scaled game time.
- Continue resident aging after adulthood.
- Move the oldest adult child still living with parents into empty houses.
- Move an eligible adult opposite-gender partner into single-resident adult-child houses while blocking close relatives.
- Drive simple idle movement around the current camp/home through short walkable grid paths.
- Periodically send residents to work at their home's Garden Beds upgrade.
- Add the Garden Beds crop to the owning house when garden work completes.
- Assign residents to lumberjack camps as workplace targets.
- Route assigned lumberjacks to mature trees, chopping work, fallen-trunk bucking, Logs pickup, camp stock deposit, planting cells, and sapling planting.
- Assign residents to stonecutter camps as workplace targets.
- Route assigned stonecutters to Stone deposits, pickaxe mining, Stone carrying, and camp stock deposit.
- Assign residents to storage yards as workplace targets.
- Route assigned storage workers to lumberjack camp stock, stored-Logs pickup, storage-yard delivery, and deposit.
- Route assigned storage workers to stonecutter camp stock, stored-Stone pickup, storage-yard delivery, and deposit.
- Assign residents to Storage Yards as dedicated builders.
- Route hired builders to reserved Storage Yard stock, construction resource pickup, site delivery, and hammer/build work after materials arrive.
- Drive frame-based axe swing animation and hit timing for lumberjacks.
- Drive frame-based pickaxe swing animation and hit timing for stonecutters.
- Drive frame-based hammer/build animation and progress hit timing for construction builders.
- Generate 5 male and 5 female resident sprite variants at runtime.
- Generate child resident sprites at runtime.
- Generate cached 8-frame walking sprites for each adult/child resident visual variant.
- Generate resident portrait sprites for HUD display.
- Choose random resident visual variants at startup.
- Add synced resident readability renderers: silhouette outline and ground shadow.
- Generate and animate procedural campfire flame plus smoke/spark overlay frames at runtime.
- Drive simple chicken idle movement around a linked Chicken Coop upgrade with walk and peck sprite animations.
- Expose runtime residents as read-only visibility sources for fog of war.

Primary files/assets:

- `Assets/Scripts/Runtime/Population/StrategyPopulationController.cs`
- `Assets/Scripts/Runtime/Population/StrategyHouseholdState.cs`
- `Assets/Scripts/Runtime/Population/StrategyKinshipUtility.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Build/StrategyLumberjackCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyStonecutterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.cs`
- `Assets/Scripts/Runtime/Map/StrategyForestryController.cs`
- `Assets/Scripts/Runtime/Map/StrategyForestryTree.cs`
- `Assets/Scripts/Runtime/Map/StrategyStoneDeposit.cs`
- `Assets/Scripts/Runtime/Map/StrategyStonecutEffectAnimator.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentSpriteFactory.cs`
- `Assets/Scripts/Runtime/Population/StrategyCampfireAnimator.cs`
- `Assets/Scripts/Runtime/Population/StrategyCampfireAmbientSpriteFactory.cs`
- `Assets/Scripts/Runtime/Population/StrategyCampfireSpriteFactory.cs`
- `Assets/Scripts/Runtime/Population/StrategyChickenAgent.cs`
- `Assets/Scripts/Runtime/Population/StrategyChickenSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyPlacedBuilding.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingUpgradeController.cs`
- `Assets/Scripts/Runtime/Economy/StrategyHouseResourceStore.cs`
- `Assets/Scripts/Runtime/Map/CityMapController.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Current residents are runtime-only and are not saved.
- Resident names are assigned at runtime from built-in first-name and family-name pools.
- Startup residents receive runtime ages 18-30; children start at age 0, use scaled game time for growth, and continue aging after adulthood.
- `StrategyPopulationController` owns the runtime resident ID registry used by kinship lookup.
- `StrategyPopulationController` also owns the runtime house registry used for free-house migration and partner retry checks.
- `StrategyHouseholdState` lives on occupied houses and owns the randomized birth timer.
- `StrategyKinshipUtility` treats close parent/child graph distance as a block for future couple/family rules.
- Resident readability helpers are visual-only child `SpriteRenderer`s and should stay synced when changing resident animation frames.
- Residents use short local grid paths for idle movement and frame-based sprite walk cycles while moving; no global pathfinding/job routing exists yet.
- Lumberjack work is the first explicit job assignment loop and remains local to the selected camp radius; it now includes tree chopping, trunk bucking, Logs delivery, and sapling planting.
- Resident woodcut sprites are generated for every male/female visual variant and should stay in sync with readability outline mirroring.
- Stonecutter work follows the same local-camp assignment model, but mines finite Stone deposits and does not plant/regrow Stone.
- Resident stonecut sprites are generated for every male/female visual variant and should stay in sync with readability outline mirroring.
- Construction assignment is a temporary exclusive task for hired Storage Yard builders; workplace assignment skips residents already attached to a construction site.
- Worker and builder assignment must check `StrategyResidentAgent.CanWork`; children idle/walk but cannot work.
- Resident construction sprites are generated for every male/female visual variant and should stay in sync with readability outline mirroring.
- Chickens use the same local path style as before; their animation is visual-only.
- House construction no longer consumes residents as builders; after completion, the finished house tries to pull one homeless adult male and one homeless adult female from the starter camp/free pool, regardless of workplace role.
- If no free pair exists, the completed house is available for adult-child migration and partner lookup.
- House occupation consumes the finite free-resident pool from the starter camp while it exists; later household births and adult-child migration are the first internal population growth path.
- Future jobs/families/economy should extend resident state rather than replacing the home/free-camp assignment model.

### World Selection

Responsibilities:

- Select placed buildings, construction sites, and residents with left-click.
- Ignore left-click selection in unexplored fog cells unless player fog is toggled off.
- Ignore world selection while the pointer is over UI.
- Show a simple marker under the selected world object.
- Show a compact full-height right-side selection HUD for the selected object.
- Show selected-object preview sprites and status/context blocks.
- Expose house-specific visual upgrade actions in the selected-house HUD.
- Show selected-house resident portraits/names/age/life stage/statuses up to house capacity, compact upgrade action rows, resource icons/counts, and Garden Beds crop.
- Show selected-lumberjack-camp worker slots with assign/remove actions, worker forestry statuses, Logs stock, and nearby tree/trunk counts.
- Show selected-stonecutter-camp worker slots with assign/remove actions, worker mining statuses, Stone stock, and nearby deposit counts.
- Show selected-storage-yard logistics-worker and builder slots with assign/remove actions, staff statuses, Logs/Stone stock, and available source count.
- Show selected-construction-site cost, delivered resources, builder count, and progress/status context.
- Show selected-resident full name, portrait, profile, age/life stage, current activity, and home/camp assignment.

Primary files/assets:

- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assets/Scripts/Runtime/Build/StrategyPlacedBuilding.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingUpgradeController.cs`
- `Assets/Scripts/Runtime/Build/StrategyLumberjackCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyStonecutterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.cs`
- `Assets/Scripts/Runtime/Economy/StrategyHouseResourceStore.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceIconFactory.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Map/StrategyForestryController.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Selection uses 2D colliders attached to placed buildings, construction sites, and residents.
- Residents have selection priority over buildings when colliders overlap.
- Selection ignores the same frame that completed placement so the new building is not auto-selected by the placement click.
- Selection consults fog exploration state before checking 2D world colliders.
- Selection HUD is runtime-created in the world selection controller and slides in from the right.
- House resident rows use the assigned resident references stored on `StrategyPlacedBuilding` and grow to the current house capacity.
- Lumberjack camp worker rows and stock context use references/counts stored on `StrategyLumberjackCamp`.
- Stonecutter camp worker rows and stock context use references/counts stored on `StrategyStonecutterCamp`.
- Storage yard logistics-worker/builder rows and stock context use references/counts stored on `StrategyStorageYard`.
- Construction site context uses cost/progress/builder data stored on `StrategyConstructionSite`.
- Current HUD layout is code-built with Unity UI primitives; future HUD shell work should decide whether this remains local or moves into a shared UI view layer.

### Input Foundation

Responsibilities:

- Input System package and action definitions.
- Future player input mapping once gameplay grows beyond direct MVP camera/menu controls.

Primary files/assets:

- `Assets/InputSystem_Actions.inputactions`
- `Packages/manifest.json`

Impact hints:

- Action map changes do not affect the current MVP camera/Build menu until controls are migrated to actions.
- Keep action names stable once code depends on generated wrappers or string keys.

### AI Memory Infrastructure

Responsibilities:

- Agent workflow contract.
- Project memory, system maps, and work log.
- Reusable prompt templates.

Primary files:

- `AI.md`
- `AGENTS.md`
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

Impact hints:

- Update `work-log.md` after implementation tasks.
- Update stable memory only when project reality changes.

## Unowned / Not Yet Present

- No custom editor/test scripts are documented yet.
- No economy, zoning, construction, or save folders are documented yet.

When a new system appears, add an owner card with responsibilities, primary files/assets, and impact hints.
