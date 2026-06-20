# Systems Map

Last updated: 2026-06-19

Use this file as the first navigation pass before broad searches. Owner cards are starting points, not hard boundaries.

Navigation note: `.cs` source files must not exceed 500 lines. Oversized runtime classes can be physically split into same-owner `.PartNN.cs` partial files; treat those parts as one owner with the base file, and keep `Assembly-CSharp.csproj` in sync when adding, removing, or renumbering them.

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
- Shared short-lived world-space visual effects for resource drops, construction hits, sawdust, dust, sparks, chips, and splashes.
- Runtime world tint overlay and calendar snapshot source for the visual day/night cycle.
- Runtime weather overlay sorting bands for wet ground, cloud shadows, mist, and rain.
- Runtime URP post-processing for soft color grading, bloom, and vignette driven by day/night and weather state.
- Runtime cinematic visuals for 2D global/local lights, emissive masks, animated building torch/lantern source sprites, light-aware nighttime darkness over unlit cells, wet puddle glints, lightning flashes, and foreground depth accents.

Primary files/assets:

- `Assets/UniversalRenderPipelineGlobalSettings.asset`
- `Assets/DefaultVolumeProfile.asset`
- `Assets/Settings/UniversalRP.asset`
- `Assets/Settings/Renderer2D.asset`
- `Assets/Scripts/Runtime/Core/StrategyWorldSorting.cs`
- `Assets/Scripts/Runtime/Core/StrategyWorldEffectAnimator.cs`
- `Assets/Scripts/Runtime/Core/StrategyPostProcessController.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicVisualController.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicVisualController.Part01.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicVisualController.Part03.cs`
- `Assets/Scripts/Runtime/Core/StrategyBuildingLightSpriteFactory.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicLightEmitter.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicLightEmitter.Torch.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicVisualMath.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicVisualSprites.cs`
- `Assets/Scripts/Runtime/Core/StrategyDayNightCycleController.cs`
- `Assets/Scripts/Runtime/Core/StrategyShadowCaster2D.cs`
- `Assets/Scripts/Runtime/Core/StrategyShadowSpriteFactory.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherVisualController.cs`

Impact hints:

- Rendering settings affect scene appearance globally.
- World sprites should use `StrategyWorldSorting` instead of fixed type-based `sortingOrder` values so farther objects do not render in front of nearer ones.
- Short-lived world effects should use `StrategyWorldEffectAnimator` and `StrategyRuntimeObjectCreationGuard` instead of spawning one-off ad hoc particle objects.
- The day/night and weather overlays sort around world sprites while staying below placement preview/fog/UI; keep that ordering when adding more world overlays.
- Day/night owns the canonical display day, 24-hour clock, time-of-day phase labels, phase accent colors, and dawn/nightfall event-log triggers; HUDs should read that snapshot instead of inventing separate clocks.
- Post-process tuning should stay subtle and pixel-readable; avoid blur, heavy chromatic aberration, or aggressive grain for normal strategy view.
- Cinematic visual effects should stay bounded to reusable emitters/controllers rather than adding per-building one-off light scripts; building torch/lantern source sprites and the night darkness mask are cheap overlays, while real `Light2D` point lights should stay LOD-capped and lazily created because many simultaneous 2D lights can cause visible frame spikes.
- `StrategyShadowCaster2D` is the shared runtime shadow path for world sprites; tune shape/scale/offset per object type and let day/night control opacity/length globally.
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
- Configure runtime weather after camera/day-night setup and before ambience audio.
- Configure runtime ambience audio after camera setup.
- Focus the initial camera view on the startup campfire after population creates it.
- Configure water/shore animation after map generation.
- Create/configure runtime trail wear and trail visuals after map generation.
- Create/configure the Stone resource registry before nature generation.
- Configure nature props after the starter camp exists so generated props can avoid the campfire clear radius.
- Create/configure forage resource nodes after nature generation so they use current walkability.
- Configure fog of war after population, placement, and map controllers exist.
- Create/configure the F9 runtime debug panel after fog/weather are ready.
- Provide shared debug options such as instant free construction for runtime test workflows.
- Place the starter Storage Yard near the campfire with initial Logs and Stone after placement is configured.
- Create/configure runtime wildlife after starter placement so deer/rabbits use valid land and fish use valid water cells.
- Create/configure visual day/night cycle after camera setup.
- Create/configure runtime post-processing after weather/day-night setup.
- Create/configure runtime cinematic visuals after post-processing setup.
- Create the runtime time-scale controller for F1/F2/F3 speed controls.
- Create/configure the top status HUD with population counts, the larger resident roster HUD, the family tree modal scene, and the compact event log with birth/death/adoption messages.
- Create/configure the runtime goals controller and starter goal sequence that gates early Build menu tools.
- Create/configure refugee arrivals and the modal refugee decision HUD.
- Create/configure the reusable confirmation dialog used by destructive world-selection actions.
- Create/configure the auto workforce controller before the Profession HUD so automation settings and manual overrides have one shared runtime owner.
- Guard runtime loose-resource pile factories against scene-object creation during Play Mode shutdown.

Primary files/assets:

- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assets/Scripts/Runtime/Core/StrategyDebugLogger.cs`
- `Assets/Scripts/Runtime/Core/StrategyDebugOptions.cs`
- `Assets/Scripts/Runtime/Core/StrategyRuntimeObjectCreationGuard.cs`
- `Assets/Scripts/Runtime/Core/StrategyTimeScaleController.cs`
- `Assets/Scripts/Runtime/Core/StrategyDayNightCycleController.cs`
- `Assets/Scripts/Runtime/Core/StrategyPostProcessController.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicVisualController.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicVisualController.Part01.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicLightEmitter.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherKind.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherController.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherVisualController.cs`
- `Assets/Scripts/Runtime/Audio/StrategyAmbientAudioController.cs`
- `Assets/Scripts/Runtime/Audio/StrategyMusicController.cs`
- `Assets/Scripts/Runtime/Audio/StrategyResidentFootstepAudio.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.Visibility.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.WeatherFog.cs`
- `Assets/Scripts/Runtime/Map/StrategyWaterAnimationController.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.Diagnostics.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.Visibility.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailSpriteFactory.cs`
- `Assets/Scripts/Runtime/Map/StrategyStoneResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyWindController.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.cs`
- `Assets/Scripts/Runtime/Population/StrategyRefugeeArrivalController.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.cs`
- `Assets/Scripts/Runtime/UI/StrategyRefugeeDialogController.cs`
- `Assets/Scripts/Runtime/UI/StrategyConfirmationDialogController.cs`
- `Assets/Scripts/Runtime/UI/StrategyDebugPanelController.cs`
- `Assets/Scripts/Runtime/UI/StrategyTopStatusHudController.cs`
- `Assets/Scripts/Runtime/UI/StrategyPopulationRosterHudController.cs`
- `Assets/Scripts/Runtime/UI/StrategyPopulationRosterHudController.Part01.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.Part01.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.Part02.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.Part03.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.Part04.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.Part05.cs`
- `Assets/Scripts/Runtime/UI/StrategyPopulationRosterRowView.cs`
- `Assets/Scripts/Runtime/UI/StrategyResidentHudText.cs`
- `Assets/Scripts/Runtime/UI/StrategyEventLogHudController.cs`
- `Assets/Scripts/Runtime/UI/StrategyGoalDefinition.cs`
- `Assets/Scripts/Runtime/UI/StrategyGoalsController.cs`
- `Assets/Scripts/Runtime/UI/StrategyGoalsHudController.cs`
- `Assets/Scripts/Runtime/UI/StrategyStarterGoalSequenceController.cs`
- `Assets/Scenes/SampleScene.unity`

Impact hints:

- Bootstrap runs through `RuntimeInitializeOnLoadMethod` and does not require scene YAML wiring.
- Audio bootstrap expects non-generated clips under `Assets/Resources/Audio`; missing ambience or music clips should degrade quietly rather than blocking scene startup.
- Any future menu, multi-scene, loading, or mode system should decide whether bootstrap remains global or becomes scene-specific.

### Strategy Debug Logging

Responsibilities:

- Create a structured runtime `debug.log` for gameplay debugging.
- Mirror Unity log messages, warnings, errors, and exceptions into the same file.
- Provide static event helpers for strategy systems without forcing scene references.
- Record important events and failure reasons for bootstrap, map generation, nature/Stone generation, build menu/tool flow, placement, population, refugees, forestry, wildlife, lumberjack camps, selection, and time-scale changes.

Primary files/assets:

- `Assets/Scripts/Runtime/Core/StrategyDebugLogger.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `.gitignore`
- `Assembly-CSharp.csproj`

Impact hints:

- In the Unity Editor, `debug.log` is written at the project root and should remain ignored by git.
- Prefer meaningful state-change, command, failure, and completion events over per-frame logging.
- Add future categories through `StrategyDebugLogger.Info/Warn/Error` so log formatting remains consistent.

### Strategy Audio

Responsibilities:

- Load non-generated ambience and footstep clips from `Assets/Resources/Audio`.
- Load in-game music clips from `Assets/Resources/Audio/Music` as a playlist.
- Create runtime AudioSources without scene YAML wiring.
- Play layered forest birds, cicadas, night, rain, calm wind, and forest wind ambience.
- Follow the active runtime weather state for rain and weather wind ambience.
- Position a spatial river ambience source at the nearest generated water cell to the active camera.
- Play random in-game music tracks without repeating the previous track when 2+ tracks exist.
- Pause current in-game music on application focus loss and resume the same clip on focus return.
- Add quiet spatial grass footsteps to resident walk animation step frames.

Primary files/assets:

- `Assets/Scripts/Runtime/Audio/StrategyAmbientAudioController.cs`
- `Assets/Scripts/Runtime/Audio/StrategyMusicController.cs`
- `Assets/Scripts/Runtime/Audio/StrategyResidentFootstepAudio.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Audio/StrategyResidentFootstepAudio.cs`
- `Assets/Resources/Audio/Nature/`
- `Assets/Resources/Audio/Music/`
- `Assets/Resources/Audio/Footsteps/GrassWalk/`
- `Assembly-CSharp.csproj`

Impact hints:

- Ambience depends on generated map water cells, camera position, and strategy wind/weather values.
- Music files can be named freely, but `Music_01.mp3`, `Music_02.mp3`, etc. are the recommended convention; all direct AudioClips in `Resources/Audio/Music` are part of the playlist.
- Music focus handling must use `AudioSource.Pause`/`UnPause` so losing focus does not trigger playlist advancement.
- Footsteps are tied to resident walk sprite frames 1 and 5; changing walk frame counts or animation pacing should retune footstep phases.
- Keep imported ambience/footstep clips under `Resources/Audio` unless the loading path is updated at the same time.

### Strategy Weather

Responsibilities:

- Own the current runtime weather state and smooth atmospheric intensities.
- Randomly transition between Clear, Cloudy, LightRain, HeavyRain, Fog, and Storm.
- Drive procedural wet-ground, cloud-shadow, heavy-rain mist, and rain world overlays.
- Feed dense Fog weather into fog-of-war visibility and masked weather-fog rendering.
- Feed rain/wind ambience with a single weather source of truth.
- Boost the strategy `WindZone` so nature sway reacts to rain and storms.
- Expose rain intensity to water animation for rain ripple hits.

Primary files/assets:

- `Assets/Scripts/Runtime/Weather/StrategyWeatherKind.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherController.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherVisualController.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assets/Scripts/Runtime/Core/StrategyWorldSorting.cs`
- `Assets/Scripts/Runtime/Map/StrategyWindController.cs`
- `Assets/Scripts/Runtime/Map/StrategyWaterAnimationController.cs`
- `Assets/Scripts/Runtime/Audio/StrategyAmbientAudioController.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Weather visual overlays must stay below placement preview and fog-of-war UI-facing layers.
- Dense weather Fog rendering is owned by Fog of War so visible cells can stay clear; do not reintroduce a uniform `FogIntensity` mist overlay over the whole map.
- Rain/wind audio should continue reading `StrategyWeatherController.Active` instead of adding independent rain timers.
- Weather currently has visual/audio/wind/water effects only; future gameplay effects should extend this system rather than duplicating weather rolls in crops, illness, movement, or fire logic.

### Generated City Map

Responsibilities:

- Hold basic map-cell data for the strategy MVP.
- Generate a visible 2D terrain map at runtime.
- Randomize the active map seed by default and derive a generation profile from it.
- Generate variable rivers, shorelines, optional water blobs, and clustered land terrain.
- Tag generated water and shore cells with `CityMapWaterKind.River` or `CityMapWaterKind.Lake` for direct future gameplay queries.
- Expose `RiverFlowDirection` for systems that need to move or animate along the generated river current.
- Paint procedural pixel-art terrain textures for generated map cells.
- Render animated water waves, sparkles, shoreline foam, and weather-driven rain ripple hits as a transparent overlay.
- Track weighted resident footfall wear on walkable/buildable land cells, decay stale wear, and render formed trails as connected procedural sprites.
- Expose formed trails as a 15% resident movement-speed bonus and a reduced resident pathfinding cost.
- Feed generated cell kinds and active seed into the visual nature-props layer.
- Feed generated land cells and active seed into Stone deposit generation.
- Feed generated walkable land cells and active seed into underground Iron field generation.
- Feed generated walkable land cells and active seed into underground Coal field generation.
- Feed generated walkable near-water land/shore cells and active seed into Clay field generation.
- Feed generated walkable land cells and active seed into forage resource node generation.
- Provide the campfire exclusion center used by nature generation to guarantee starter-area Stone.
- Expose map bounds and cell buildability for future zoning/economy systems.
- Track dynamic walkability blockers for placed buildings and early agents.
- Track completed bridge walkability over River water cells without changing water/shore identity.
- Host runtime fog-of-war exploration, current visibility, day/night reveal tuning, weather Fog reveal tuning, weather-fog band rendering, and daylight-range visibility state.

Primary files/assets:

- `Assets/Scripts/Runtime/Map/CityMapController.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.Visibility.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.WeatherFog.cs`
- `Assets/Scripts/Runtime/Map/StrategyWaterAnimationController.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.Diagnostics.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.Visibility.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailSpriteFactory.cs`
- `Assets/Scripts/Runtime/Map/StrategyTerrainTexturePainter.cs`
- `Assets/Scripts/Runtime/Map/CityMapController.Buildability.cs`
- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.cs`
- `Assets/Scripts/Runtime/Map/StrategyNatureSpriteFactory.cs`
- `Assets/Scripts/Runtime/Map/StrategyStoneResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyStoneDeposit.cs`
- `Assets/Scripts/Runtime/Map/StrategyIronResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyIronDeposit.cs`
- `Assets/Scripts/Runtime/Map/StrategyCoalResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyCoalDeposit.cs`
- `Assets/Scripts/Runtime/Map/StrategyClayResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyClayDeposit.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageNode.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageSpriteFactory.cs`
- `Assets/Scripts/Runtime/Map/StrategyForestryController.cs`
- `Assets/Scripts/Runtime/Map/StrategyForestryTree.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Current map is runtime-generated with a randomized active seed by default and is not saved.
- Current terrain painter covers Grass, Meadow, Forest, Dirt, Shore, and Water with seeded variants and neighbor transition overlays.
- Water source identity is stored on `CityMapCell.WaterKind`; future systems should query that instead of guessing river/lake from geometry.
- River current direction is stored on `CityMapController.RiverFlowDirection`; river-specific ambience/gameplay should follow that instead of creating independent direction timers.
- Terrain kind generation now uses a seed-derived profile plus multi-octave noise; texture painting consumes the active seed.
- Nature prop placement consumes the active seed and generated cell kinds.
- Stone deposit placement consumes the active seed and generated cell kinds.
- Iron field placement consumes the active seed and generated walkable land cells; Iron deposits are underground fields that do not block walkability but block normal buildability.
- Coal field placement consumes the active seed and generated walkable land cells; Coal deposits are underground fields that do not block walkability but block normal buildability.
- Clay field placement consumes the active seed and generated walkable near-water land/shore cells; Clay deposits do not block walkability but block normal buildability.
- Forage placement consumes the active seed, generated cell kinds, and current walkability; forage nodes are non-blocking but reserved/depleted/regrown by household foragers.
- Generated standalone tree props register as mature forestry trees and block their cells.
- Forest groups and bushes remain non-interactive but block their cells.
- Generated Stone deposits register as Boulder, Rock Cluster, or Cliff resource deposits and block their cells.
- Generated Iron deposits register as multi-cell Iron-stained Ground or Iron Vein resource fields, stay walkable but not normally buildable, avoid adjacent Coal fields, and can be reserved/mined by `StrategyMine` worksites built over them.
- Generated Coal deposits register as multi-cell Coal Dust Ground or Coal Seam resource fields, stay walkable but not normally buildable, avoid adjacent Iron fields, and can be reserved/mined by `StrategyCoalPit` worksites built over them.
- Generated Clay deposits register as multi-cell Clay Patch or Clay Bank resource fields, stay walkable but not normally buildable, require nearby water across the full footprint, avoid adjacent Iron/Coal/Clay fields, and can be reserved/mined by `StrategyClayPit` worksites built over them.
- Future placement/economy work should reuse `CityMapCell`/bounds rather than duplicating map dimensions.
- Future movement/pathfinding should use `IsCellWalkable` rather than terrain kind alone.
- Rendering is currently a generated point-filtered texture on a `SpriteRenderer`, not a Tilemap.
- Water and shore animation is a separate transparent `SpriteRenderer` overlay above the static map and below world props; it reads active weather intensity for rain ripple hits.
- Trail visuals use one `SpriteRenderer` per visible trail cell under a `Trail Visuals` root, sorted above terrain/water overlays and below world props, with 8-direction masks and narrow line/brush sprites; faint trail cells require directional/strong-neighbor support before rendering so early traffic noise stays hidden.
- Trail wear is runtime-only and should be refreshed when walkability/buildability changes so blocked cells do not keep visible trails.
- Resident pathfinding should continue to treat trails as a cost preference, not as required connectivity.

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
- Small trees yield 3 Logs, while large generated trees and planted mature trees yield 6 Logs; lumberjacks carry the full yield in one trip.
- Felled trees remain in the registry until Logs are collected, so planting does not overlap the fresh log.
- Future wood resources, regrowth balance, and forest ownership should extend this subsystem instead of adding tree logic directly to residents or HUD.

### Fog of War

Responsibilities:

- Track persistent explored map cells and current visible cells.
- Render a generated texture overlay above world sprites and below screen-space UI.
- Use the starter camp, residents, and placed buildings as visibility sources.
- Reduce visibility source radii during Dusk/Night/Dawn from the shared day/night phase.
- Further reduce visibility source radii during dense Fog weather.
- Render dense weather Fog inside explored-but-not-visible cells while leaving current visible cells clear.
- Keep a daylight-range visibility mask for spawn systems that should not react to temporary nighttime sight loss.
- Provide exploration checks to placement and world selection.
- Expose a player-fog enabled setter for debug controls without clearing explored state.

Primary files/assets:

- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.Visibility.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.WeatherFog.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assets/Scripts/Runtime/UI/StrategyDebugPanelController.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Fog state is runtime-only and is not saved yet.
- Placement currently requires explored cells, not current visibility.
- Selection ignores clicks in unexplored cells, while the overlay visually hides world sprites.
- Current visibility is reduced by Dusk/Night/Dawn and dense Fog weather, but persistent explored state and daylight-range hidden checks stay separate.
- Weather Fog replaces normal explored gray-zone rendering with light/medium/dense fog bands around current visible cells.
- Wildlife spawn/reproduction/migration hidden checks should use daylight-range visibility so temporary night blindness does not count as a safe spawn opening.
- The F9 debug panel can hide the fog overlay and make map cells count as explored for player placement/selection until toggled back on; it also owns tester-facing instant construction toggles through shared debug options.
- Future scouting, enemies, stealth, minimap, or save/load should extend this subsystem instead of duplicating visibility arrays.

### Nature Props

Responsibilities:

- Place visual trees, forest groups, bushes, and Stone deposits over generated terrain.
- Place visual underground Iron/Coal fields and near-water Clay fields over generated terrain without blocking movement.
- Generate and cache runtime 2.5D pixel-art nature sprites.
- Use `CityMapController.ActiveSeed` plus cell coordinates for deterministic prop layout per generated map.
- Guarantee a small starter Stone field within stonecutter work distance around the startup campfire.
- Make `Forest` cells read as dense forest while adding sparse standalone trees/bushes to other land terrain.
- Attach wind-sway animation to trees, forest groups, and bushes using the runtime strategy wind source.
- Add procedural leaf frame overlays to trees, forest groups, and bushes.
- Skip generated nature props inside the startup campfire's 3-cell clear radius.
- Skip generated Stone deposits inside the same startup campfire clear radius.
- Skip generated Iron fields inside the same startup campfire clear radius.
- Skip generated Coal fields inside the same startup campfire clear radius.
- Skip generated Clay fields inside the same startup campfire clear radius.
- Place starter Stone outside the clear radius before vegetation so nearby trees/bushes do not consume all accessible mining cells.

Primary files/assets:

- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.cs`
- `Assets/Scripts/Runtime/Map/StrategyNatureSpriteFactory.cs`
- `Assets/Scripts/Runtime/Map/StrategyStoneResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyStoneDeposit.cs`
- `Assets/Scripts/Runtime/Map/StrategyIronResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyIronDeposit.cs`
- `Assets/Scripts/Runtime/Map/StrategyCoalResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyCoalDeposit.cs`
- `Assets/Scripts/Runtime/Map/StrategyClayResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyClayDeposit.cs`
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
- Iron fields are generated as flat multi-cell rust-stained/vein surface marks for underground ore, stay walkable but block normal building placement through the map buildability overlay, avoid adjacent Coal fields, and are mined by `StrategyMine` workers when under the Mine footprint.
- Coal fields are generated as flat multi-cell dark dust/seam surface marks for underground coal, stay walkable but block normal building placement through the map buildability overlay, avoid adjacent Iron fields, and are mined by `StrategyCoalPit` workers when under the Coal Pit footprint.
- Clay fields are generated as flat multi-cell wet clay patch/bank surface marks near water, stay walkable, block normal building placement through the map buildability overlay, avoid adjacent Iron/Coal/Clay fields, and are mined by `StrategyClayPit` workers when under the Clay Pit footprint.
- Starter Stone placement verifies that each guaranteed deposit has adjacent walkable work cells for stonecutters.
- Bootstrap creates/configures the wind controller, creates population so the camp cell is known, then configures nature after `CityMapController.GenerateMap()`.
- Unity `WindZone` does not animate 2D sprites directly; `StrategyWindSway` adapts its values to sprite rotation/offset/scale.
- Leaf frame overlays complement wind sway and should stay visual-only unless future forestry gameplay needs extra real prop state.
- Future clearing or wood resources should extend the Forestry MVP registry instead of duplicating generated decoration data.

### Wildlife MVP

Responsibilities:

- Spawn compact ambient deer herds only on currently hidden suitable land cells near completed buildings or active construction sites.
- Spawn compact ambient rabbit groups only on currently hidden suitable land cells near completed buildings or active construction sites.
- Spawn compact lake fish shoals only in currently hidden lake cells near settlement anchors, with strict per-shoal and per-lake population caps.
- Spawn one-way pass-through river fish from currently hidden near-settlement river-route cells through a single timer.
- Spawn decorative birds only on currently hidden species-appropriate land/water cells near settlement anchors, without reproduction or resources.
- Spawn compact wolf packs only on currently hidden safe land cells in a wider near-settlement ring, preferring alternating river sides when a generated river route exists.
- Use completed buildings and active construction sites as wildlife spawn anchors, with the startup camp as a fallback only when no building/construction anchor exists.
- Provide adult-rabbit reservation/count lookup for hunter camps.
- Provide catchable-fish reservation/count lookup for fisher huts.
- Provide wolf predator reservation hooks for rabbit/deer surplus above high population-control thresholds and for vulnerable far-from-settlement adult residents.
- Generate and cache runtime 2.5D pixel-art deer sprites for male bucks and female does.
- Generate and cache runtime 2.5D pixel-art rabbit sprites for male and female visuals.
- Generate and cache runtime pixel-art fish sprites for Minnow, Carp, and Perch visuals.
- Generate and cache runtime 2.5D pixel-art bird sprites for Sparrow, Crow, and Duck visuals.
- Generate and cache runtime 2.5D pixel-art wolf sprites for pack members.
- Animate deer idle breathing, walking, grazing, alert stance, fleeing/running, and resting.
- Animate rabbits idle movement, hopping, nibbling, alert stance, fleeing, grooming, and resting.
- Animate hunted rabbits through arrow hit, death, carcass, and butchering-ready states.
- Animate fish idle swimming, swimming, dart/flee, turning, feeding, hooked/reeling, and surface ripples.
- Animate birds idle movement, pecking, hopping, flying, landing, duck swimming, and flight shadows.
- Animate wolves through idle, roaming, stalking, chasing, attacking, feeding, avoiding-settlement, resting, and howling states.
- Keep deer on local walkable-cell paths inside loose herd/home ranges without blocking map cells.
- Keep rabbits on local walkable-cell paths inside loose group/home ranges without blocking map cells.
- Let deer, rabbits, and wolves treat generated River cells as passable transit-only swimming crossings with slowed movement and ripple visuals; final land-wildlife targets and hunt/prey reservations must stay on land cells, while Lake water remains rejected for land wildlife.
- Keep lake fish on local lake-water paths inside loose shoal/home ranges without blocking map cells.
- Keep river fish on the generated river route until they despawn at the route end.
- Keep birds on local habitat choices inside loose home ranges without blocking map cells.
- Periodically migrate deer herds, rabbit groups, wolf packs, decorative bird homes, and lake fish shoals by retargeting their loose home centers toward new suitable habitat.
- Keep land wildlife migration away from dense settlement pressure and advance it only through short connected walkable steps so herds/packs do not jump through water or blockers.
- React to nearby residents and noisy work by switching to alert/flee states.
- Let adult does reproduce when an adult buck is nearby in the same herd.
- Spawn fawns that grow into adults after scaled simulation time.
- Keep deer reproduction under the hard 24-deer runtime population cap and the 3-deer per-herd cap.
- Let adult female rabbits reproduce when an adult male is nearby in the same group.
- Spawn kits that grow into adults after scaled simulation time.
- Keep rabbit reproduction under the hard 30-rabbit runtime population cap and the 3-rabbit per-group cap.
- Let hunter camps reserve adult rabbits, stop their normal behavior during the shot sequence, yield `Game` after butchering on hit, and release/flee rabbits on missed arrows.
- Let adult lake fish reproduce when another adult of the same species is nearby in the same shoal.
- Spawn fry that grow into adults after scaled simulation time.
- Keep fish reproduction under the hard 36-fish runtime population cap, the 3-fish per-shoal cap, and the stricter per-lake region cap.
- Keep river fish non-reproductive and controlled by the active river-fish cap.
- Let fisher huts reserve adult fish, choose valid land/shore stand cells with adjacent water, abandon casts when fish leave cast range before hooking, and yield local `Fish` after reeling.
- Let wolves hunt rabbit/deer surplus with normal stalking plus a short fast pounce phase, then direct final-position chase fallback after reaching the target cell, and threaten adult residents only when the target is outside the settlement safety pressure.

Primary files/assets:

- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.Part09.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.Part10.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.Part11.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeRiverCrossing.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyDeerAgent.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyDeerAgent.Part03.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyDeerAgent.Part04.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyDeerSpriteFactory.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyRabbitAgent.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyRabbitAgent.Part03.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyRabbitAgent.Part04.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyRabbitSpriteFactory.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWolfAgent.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWolfAgent.Part04.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWolfAgent.Part05.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWolfAgent.Part07.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWolfSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyHunterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyFisherHut.cs`
- `Assets/Scripts/Runtime/Population/StrategyHuntingArrowProjectile.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyFishAgent.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyFishSpriteFactory.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyBirdAgent.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyBirdSpriteFactory.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assets/Scripts/Runtime/Core/StrategyWorldSorting.cs`
- `Assets/Scripts/Runtime/Map/CityMapController.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Wildlife is runtime-only and not saved yet.
- Deer and birds do not reveal fog, block walkability, or provide resources yet; rabbits can yield `Game` through the hunter-camp work loop, fish can yield `Fish` through the fisher-hut work loop, and wolves are predators rather than player-harvestable resources.
- Initial rabbit spawn, deer herds, fish shoals, birds, and wolf packs depend on hidden near-settlement candidate cells instead of map-wide placement; if no hidden candidate exists for a species, that species should skip spawning rather than appearing far from buildings or inside visible fog.
- Wildlife hidden checks use fog daylight-range visibility, not reduced nighttime current visibility, so animals do not spawn closer to the settlement just because night lowered player sight radius.
- Deer pathing depends on the wildlife land-travel predicate plus a separate land-target predicate, which wraps `CityMapController.IsCellWalkable`, River transit allowance, and the 4-cell structure buffer.
- Rabbit pathing uses the same local wildlife land-travel and land-target approach and should stay cheap until a shared pathfinding service exists.
- Land wildlife river crossing is intentionally scoped to wildlife path helpers through `StrategyWildlifeRiverCrossing`; River cells are transit-only for deer/rabbit/wolf paths and should not become final wildlife targets. Do not change global `CityMapController` walkability to make River water walkable for residents, buildings, or construction.
- Fish pathing uses `CityMapCellKind.Water` plus `CityMapWaterKind` instead of `IsCellWalkable`, because water is intentionally not walkable for land agents and lake/river fish now have separate movement rules.
- Migration state is owned by `StrategyWildlifeController`; agents only expose small retarget methods for their current home/roam center, and migration targets must stay in currently hidden near-settlement candidate cells.
- Reproduction is owned by `StrategyWildlifeController`; deer/rabbit/fish birth cells must be currently hidden and near settlement anchors, while agents own species or sex, life stage, growth, movement, and animation state. Birds are decorative and do not reproduce yet; wolves do not reproduce yet and use pack spawn only.
- Wolf settlement avoidance is pressure-based and reads camp position, placed buildings, active construction sites, and nearby residents; land wildlife pathing also uses the cached structure buffer, so keep it cheaper than per-frame global scans.
- Wolf prey lookup is population-control logic, not continuous hunting: rabbit hunting only starts above the rabbit control threshold after subtracting predator and hunter reservations, and deer hunting only starts above the deer control threshold after subtracting predator reservations.
- Wolves no longer use ordinary resident target acquisition when no surplus prey is available.
- Wolf pack placement uses the generated river route to prefer alternating river sides, but falls back to any valid safe side and logs `WolfPackRiverSideFallback` if one side has no candidate.
- Wolf movement diagnostics are owned by `StrategyWolfAgent` and log state changes, target acquisition/release, path readiness/failures, roam failures, and movement stalls under the `Wildlife` log category.
- `FishLakeBirthBlocked` debug logging is throttled per lake region; keep cap checks cheap and avoid per-fish spam when a lake is full.
- Future deer hunting, leather, broader predator ecology, wolf HUD, or animal saving should extend this subsystem instead of adding animal behavior into population or nature-prop code.

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
- Stone production currently flows from deposits to `StrategyStonecutterCamp` local stock, then to `StrategyStorageYard` via Haulers before construction can reserve it.
- Stone deposit walkability is footprint-based and should be respected by placement and resident pathing through `CityMapController.IsCellWalkable`.
- Future quarries or richer Stone production should extend this registry instead of scanning visual sprites directly.

### Iron Resources MVP

Responsibilities:

- Track generated underground Iron resource fields.
- Register multi-cell Iron-stained Ground and Iron Vein fields placed by nature generation.
- Store deposit footprint, kind, remaining Iron amount, and reservation state for Mine jobs.
- Keep Iron field cells walkable because the actual ore is underground, while marking their footprints not normally buildable.
- Expose world-inspect information for available, reserved, and depleted underground Iron.
- Provide footprint-overlap available-deposit queries and reservation/mining hooks for Mine workers.

Primary files/assets:

- `Assets/Scripts/Runtime/Map/StrategyIronResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyIronDeposit.cs`
- `Assets/Scripts/Runtime/Build/StrategyMine.cs`
- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.cs`
- `Assets/Scripts/Runtime/Map/StrategyNatureSpriteFactory.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceType.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceIconFactory.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceIconFactory.Part02.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Iron is runtime-only and not saved yet.
- Iron is produced by Mines built over Iron deposits and hauled to Storage Yards, but is not connected to food, construction costs, trade, or a global economy yet.
- Iron fields must not block walkability, must block normal building placement, and must not touch adjacent Coal fields; current mining keeps miners at the Mine/underground work loop instead of turning field cells into obstacles.
- Future Iron production upgrades should extend this registry instead of reusing `StrategyStoneDeposit`, because Stone deposits already imply above-ground blocking and active stonecutter mining behavior.

### Coal Resources MVP

Responsibilities:

- Track generated underground Coal resource fields.
- Register multi-cell Coal Dust Ground and Coal Seam fields placed by nature generation.
- Store deposit footprint, kind, remaining Coal amount, and reservation state for Coal Pit jobs.
- Keep Coal field cells walkable because the actual coal is underground, while marking their footprints not normally buildable.
- Expose world-inspect information for available, reserved, and depleted underground Coal.
- Provide footprint-overlap available-deposit queries and reservation/mining hooks for Coal Pit workers.

Primary files/assets:

- `Assets/Scripts/Runtime/Map/StrategyCoalResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyCoalDeposit.cs`
- `Assets/Scripts/Runtime/Build/StrategyCoalPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.cs`
- `Assets/Scripts/Runtime/Map/StrategyNatureSpriteFactory.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceType.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceIconFactory.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceIconFactory.Part02.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Coal is runtime-only and not saved yet.
- Coal is produced by Coal Pits built over Coal deposits and hauled to Storage Yards, but is not connected to food, construction costs, trade, or a global economy yet.
- Coal fields must not block walkability, must block normal building placement, and must not touch adjacent Iron fields; current mining keeps coal miners at the Coal Pit interior work loop instead of turning field cells into obstacles.
- Future Coal production upgrades should extend this registry instead of reusing `StrategyIronDeposit`, because Coal Pit work has different visible-worker behavior from hidden Mine extraction.

### Clay Resources MVP

Responsibilities:

- Track generated near-water Clay resource fields.
- Register multi-cell Clay Patch and Clay Bank fields placed by nature generation.
- Store deposit footprint, kind, remaining Clay amount, and reservation state for Clay Pit jobs.
- Keep Clay field cells walkable, while marking their footprints not normally buildable.
- Require nearby water across the full Clay footprint.
- Expose world-inspect information for available, reserved, and depleted Clay.
- Provide footprint-overlap available-deposit queries and reservation/mining hooks for Clay Pit workers.

Primary files/assets:

- `Assets/Scripts/Runtime/Map/StrategyClayResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyClayDeposit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part44.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part45.cs`
- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.cs`
- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.Part04.cs`
- `Assets/Scripts/Runtime/Map/StrategyNatureSpriteFactory.cs`
- `Assets/Scripts/Runtime/Map/StrategyNatureSpriteFactory.Part04.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.Part08.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceType.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceIconFactory.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceIconFactory.Part02.cs`
- `Assets/Scripts/Runtime/UI/StrategyProfessionHudController.Part06.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldInspectInfoFactory.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Clay is runtime-only and not saved yet.
- Clay fields must not block walkability, must block normal building placement, must remain near water, and must not touch adjacent Iron/Coal/Clay fields.
- Clay is produced by Clay Pits built over Clay fields and hauled to Storage Yards, but is not connected to food, construction costs, trade, or a global economy yet.
- Clay Pit work is visible like Coal Pit work, but uses near-water Clay placement rules instead of underground seam scoring.

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
- Expose a public requested-scale API used by the Build HUD speed buttons.
- Keep `Time.fixedDeltaTime` synchronized with `Time.timeScale`.
- Provide pause locks for modal gameplay decisions while preserving the requested x1/x2/x3 speed.
- Reset simulation speed back to x1 when the controller is disabled.

Primary files/assets:

- `Assets/Scripts/Runtime/Core/StrategyTimeScaleController.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Current controls read direct Input System keyboard state, not generated input actions.
- The Build HUD also calls this controller from x1/x2/x3 buttons under the top-left construction resource panel.
- Time scale affects gameplay timers using scaled `Time.deltaTime`; UI using `Time.unscaledDeltaTime` should remain visually stable.
- Future pause, speed HUD, or settings should extend this controller instead of adding separate `Time.timeScale` writes.
- Modal systems should use pause locks instead of writing `Time.timeScale = 0` directly.

### Build Menu HUD

Responsibilities:

- Runtime-created Build menu inspired by `Gruzovichky` bottom Build dock.
- Bottom Build button.
- Category cards and item tray.
- Build item cards with Logs/Stone/Planks construction costs, affordability state, and active state.
- F9 instant construction debug mode makes build tools affordable and shows item cost badges as `Free`.
- Temporary goal-driven tool locks that disable locked categories/items, block mouse and hotkey selection, and show `Locked` item badges.
- Top-left construction resource panel with x1/x2/x3 speed buttons directly beneath it.
- Current catalog entries: `Housing` / `House`, `Production` / `Lumberjack Camp`, `Stonecutter Camp`, `Sawmill`, `Kiln`, `Hunter Camp`, `Fisher Hut`, `Mine`, `Coal Pit`, and `Clay Pit`, `Storage` / `Storage Yard` and `Granary`, and `Infrastructure` / `Bridge`.
- Hotkeys for open/close, category/item selection, and layered cancel.
- EventSystem/Input System UI setup when the scene has no UI event module.
- Add tools/buildings gradually only by explicit user request.
- Single-item categories behave as direct build-tool buttons.

Primary files/assets:

- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Driver.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Driver.Debug.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Driver.Part01.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Driver.Locking.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Driver.Part03.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Catalog.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyLumberjackCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyStonecutterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategySawmill.cs`
- `Assets/Scripts/Runtime/Build/StrategyKiln.cs`
- `Assets/Scripts/Runtime/Build/StrategyKiln.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyKiln.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyHunterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyFisherHut.cs`
- `Assets/Scripts/Runtime/Build/StrategyMine.cs`
- `Assets/Scripts/Runtime/Build/StrategyCoalPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.Part08.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.Part09.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Build/StrategyGranary.cs`
- `Assets/Scripts/Runtime/Economy/StrategyConstructionResourceCost.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assets/Scripts/Runtime/Core/StrategyDebugOptions.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- The public `StrategyBuildMenuController` component is a thin wrapper; `StrategyBuildMenuControllerDriver` owns selected active build tool data and reads `StrategyStorageYard.GetTotalConstructionResources()` for affordability, including Storage Yard stock and loose piles, unless F9 instant construction debug mode is enabled.
- Placement reads `StrategyBuildMenuController.ActiveTool` / active tool info.
- Starter goals call `StrategyBuildMenuController.SetAllowedTools()` and `ClearAllowedTools()`; keep lock checks shared by mouse clicks, hotkeys, active tool info, and affordability/selection visuals.
- Current catalog has user-requested buildings only: `House`, `Lumberjack Camp`, `Stonecutter Camp`, `Sawmill`, `Kiln`, `Hunter Camp`, `Fisher Hut`, `Mine`, `Coal Pit`, `Clay Pit`, `Storage Yard`, `Granary`, and `Bridge`; do not add more without a user request.
- Current `Housing` category directly activates `House` because it has one item.
- Current `Production` category opens a tray with `Lumberjack Camp`, `Stonecutter Camp`, `Sawmill`, `Kiln`, `Hunter Camp`, `Fisher Hut`, `Mine`, `Coal Pit`, and `Clay Pit`.
- Current `Storage` category opens a tray with `Storage Yard` and `Granary`.
- Successful placement asks the menu to close all open layers and records the placement frame.
- If a full HUD/menu shell appears later, decide whether this controller remains standalone or becomes part of the HUD shell.

### Top Status HUD

Responsibilities:

- Runtime-created top status canvas.
- Show total settlement population, adult count, child count, display day, 24-hour clock time, time-of-day phase, and day progress.
- Treat the compact population panel as a click target that toggles the larger resident roster HUD.
- Show a larger residents roster HUD with settlement stats plus filterable rows for name, age, home/camp state, role, current status, and food status.
- Expose a `Family Trees` button from the residents roster.
- Show a fullscreen modal Family Trees HUD that pauses simulation, has permanent horizontal/vertical scrollbars, groups recorded members into connected same-surname family cards, lays those cards out as affinity-ordered left-to-right columns with compact generation rows, and draws parent-child portrait-card trees plus cross-family relationship lines with distinct deceased cards, gender symbols, and hover relationship labels.
- Share resident role/status/home/food label formatting through `StrategyResidentHudText`.
- Show compact birth, death, adoption, dawn, and nightfall messages through a separate event-log canvas.
- Refresh counts from `StrategyPopulationController` without owning population state.

Primary files/assets:

- `Assets/Scripts/Runtime/UI/StrategyTopStatusHudController.cs`
- `Assets/Scripts/Runtime/UI/StrategyPopulationRosterHudController.cs`
- `Assets/Scripts/Runtime/UI/StrategyPopulationRosterHudController.Part01.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.Part01.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.Part02.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.Part03.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.Part04.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.Part05.cs`
- `Assets/Scripts/Runtime/UI/StrategyPopulationRosterRowView.cs`
- `Assets/Scripts/Runtime/UI/StrategyResidentHudText.cs`
- `Assets/Scripts/Runtime/UI/StrategyEventLogHudController.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentFamilyRecord.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Population counts exclude pending refugee families until they are accepted into the settlement.
- The calendar/time widget reads `StrategyDayNightCycleController.CurrentSnapshot`; keep it separate from the clickable population panel so the roster entry point remains obvious.
- Family Trees reads recorded family data, including deceased residents preserved by `StrategyResidentFamilyRecord`, and renders deceased relatives as muted monochrome cards with a skull marker.
- Family Trees relationship labels, cross-family lines, and column affinity currently derive from recorded parent/child links plus co-parent inference through shared children; explicit marriage/birth-family links should extend this owner instead of overloading family-name grouping.
- Keep top HUD click targets coordinated with Build/Profession HUD positioning and raycasts.

### Goals HUD

Responsibilities:

- Provide a runtime goals/checklist infrastructure used by the starter onboarding build sequence.
- Track active goal definitions, completed goal kinds, and the derived HUD view state without owning tutorial scenario logic.
- Keep the goals HUD hidden when there are no active goals, then show the left-side starter build checklist when bootstrap starts the sequence.
- Render a compact non-blocking Screen Space Overlay checklist with completion marks and a short completion pulse.
- Run the initial build sequence: 3 Houses, then Lumberjack Camp plus Stonecutter Camp, then unlock the full Build menu.

Primary files/assets:

- `Assets/Scripts/Runtime/UI/StrategyGoalDefinition.cs`
- `Assets/Scripts/Runtime/UI/StrategyGoalsController.cs`
- `Assets/Scripts/Runtime/UI/StrategyGoalsHudController.cs`
- `Assets/Scripts/Runtime/UI/StrategyStarterGoalSequenceController.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Driver.Locking.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.Events.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Bootstrap creates/configures the goals layer and starts the current early build onboarding sequence.
- Goal progress listens to completed placed buildings, not construction-site placement, through `StrategyBuildPlacementController.BuildingCompleted`.
- Future tutorial/onboarding code should extend `StrategyStarterGoalSequenceController` or use `StrategyGoalsController` rather than building a separate checklist HUD.

### Refugee Decision HUD

Responsibilities:

- Runtime-created modal decision canvas for arriving refugee families.
- Show a family summary with names, roles, and ages.
- Block world interaction while visible and return accept/reject decisions to the refugee-arrival controller.

Primary files/assets:

- `Assets/Scripts/Runtime/UI/StrategyRefugeeDialogController.cs`
- `Assets/Scripts/Runtime/Population/StrategyRefugeeArrivalController.cs`
- `Assets/Scripts/Runtime/Core/StrategyTimeScaleController.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Modal gameplay decisions should pause through `StrategyTimeScaleController` pause locks rather than direct `Time.timeScale` writes.
- Dialog UI should stay above Build, Selection, and Profession HUD canvases.

### Profession HUD

Responsibilities:

- Runtime-created top-menu `Professions` button.
- Show a large profession panel with dynamic rows only for professions unlocked by currently built worksites.
- Show generated pixel-art profession icons, role labels, short role descriptions, assigned/capacity counts, and `-`/`+` controls.
- Show the `Auto Assign` toggle and compact priority steppers for Construction, Food, Logistics, Wood, Stone, Planks, Iron, Coal, Clay, and Pottery.
- Aggregate assignment capacity/counts across all current lumberjack camps, stonecutter camps, sawmills, kilns, hunter camps, fisher huts, mines, coal pits, clay pits, and storage yards.
- Treat Storage Yard Haulers and hired builders as unlimited-capacity roles once at least one Storage Yard exists; other worksite roles keep their own slot caps.
- Assign the next free adult resident to the first available worksite slot for the requested profession.
- Remove one currently assigned resident from the requested profession through the owning worksite API.
- Log player assignment/removal attempts and results.
- Notify auto workforce when the player manually removes a worker so automation briefly avoids refilling that profession.

Primary files/assets:

- `Assets/Scripts/Runtime/UI/StrategyProfessionHudController.cs`
- `Assets/Scripts/Runtime/UI/StrategyProfessionHudController.Part04.cs`
- `Assets/Scripts/Runtime/UI/StrategyProfessionHudController.Part05.cs`
- `Assets/Scripts/Runtime/UI/StrategyProfessionHudController.Part06.cs`
- `Assets/Scripts/Runtime/UI/StrategyProfessionIconFactory.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceSettings.cs`
- `Assets/Scripts/Runtime/Population/StrategyProfessionType.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.cs`
- `Assets/Scripts/Runtime/Build/StrategyLumberjackCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyStonecutterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategySawmill.cs`
- `Assets/Scripts/Runtime/Build/StrategyKiln.cs`
- `Assets/Scripts/Runtime/Build/StrategyKiln.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyKiln.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyHunterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyFisherHut.cs`
- `Assets/Scripts/Runtime/Build/StrategyMine.cs`
- `Assets/Scripts/Runtime/Build/StrategyCoalPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- This HUD owns player-facing worker assignment/removal; selected-building microHUDs should remain informational for worksite status/resource context.
- Assignment still uses each worksite's existing `TryAssignNextAvailable...` / `Unassign...At` API, so role state, reservations, and work loops remain owned by the worksite/resident systems.
- Hauler and builder `+` buttons should stay enabled as long as a Storage Yard exists and at least one free adult resident can work.
- Auto workforce controls are UI-facing only; actual assignment decisions belong to `StrategyAutoWorkforceController`.
- Dynamic rows are derived from currently existing worksite components, not from the build catalog.
- New professions should be added here when a new worksite role becomes assignable by the player.

### Auto Workforce

Responsibilities:

- Runtime-created settlement workforce automation.
- Keep player priority settings for Construction, Food, Logistics, Wood, Stone, Planks, Iron, Coal, Clay, and Pottery.
- Tick every few seconds instead of every frame.
- Cache current worksite arrays once per tick and reuse that snapshot through demand, fallback, release, and rebalance calculations.
- Scan eligible free adults through `StrategyPopulationController.Residents`.
- Compute desired targets for every auto-managed profession from the player priority values, release surplus workers through normal worksite unassign APIs, and let higher-scored shortages pull limited donors from lower-priority auto-managed roles when there are no free adults.
- Maintain a coverage floor of 1 worker for available auto-managed professions whose player counter is above 0; a counter at 0 is the explicit opt-out that allows that role/category to fall to 0.
- Let emergency food/resource shortages pull a limited donor from an at-target profession only when the shortage score strongly exceeds that profession's hold score, while never taking the last worker protected by a coverage floor.
- Ignore children, pending refugees, funeral duty, household foraging/food duty, householders, residents with external workplaces, and active construction assignees through resident availability flags.
- Build work demands from active construction sites, Granary ration reserve, production-worksite stock/capacity, Storage Yard/Granary logistics backlog, and construction material needs.
- Score demands by priority, urgency, shortage, worksite need, construction readiness, storage backlog, and resident distance.
- Assign nearest free adults through existing worksite APIs instead of mutating resident/worksite lists directly.
- Hire builders through Storage Yards so existing balanced construction dispatch remains the owner of construction-site assignment.
- Treat Haulers as the single automated logistics profession for storage resources and Granary food movement.
- Register a short manual-removal override per profession so auto-fill does not immediately undo player `-` clicks.
- Log demand, assignment, skipped assignment, manual override, priority, and tick status events.
- Rebalance only idle worksite workers during normal surplus/demand donor releases; explicit priority-0 profession shutdowns can still cancel active work.

Primary files/assets:

- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.Part01.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.Part02.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.Part03.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.Part04.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.Part05.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.Part06.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceDemand.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceSettings.cs`
- `Assets/Scripts/Runtime/UI/StrategyProfessionHudController.Part04.cs`
- `Assets/Scripts/Runtime/UI/StrategyProfessionHudController.Part05.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Auto workforce can release surplus workers from overstaffed auto-managed professions, release limited lower-priority donors for higher-scored shortages, and use a stricter emergency margin to pull at-target donors for severe food/resource shortages; coverage floors protect the last worker in nonzero-counter professions, worksite lookups should use the per-tick snapshot, successful assignments are capped per tick, and only residents who become idle are reused immediately while workers returning carried resources re-enter the free pool on later ticks.
- Free adult fallback assignment runs after demand assignment so idle adults are placed into the best enabled available role when any nonzero managed profession can accept them.
- Auto workforce does not force-reassign home duty, funeral duty, or residents still busy returning carried resources.
- Demand scoring should continue to call public worksite APIs (`AssignWorker`, `AssignBuilder`, Storage Yard builder dispatch) so cancellation, carried-resource return, reservations, and resident state cleanup stay centralized.
- Food automation should keep using Hunter/Fisher production plus shared Haulers into Granaries; do not reintroduce a separate player-facing Granary Worker profession.
- Builder automation should hire Storage Yard builders and let `StrategyStorageYard.TryAssignBuildersToSite` balance actual construction-site assignment.
- Priority UI labels and debug event labels must stay in English.

### Build Placement

Responsibilities:

- Convert mouse world position to map cells.
- Show selected-tool placement preview.
- Validate terrain, bounds, affordability, and occupied cells.
- Reject unexplored fog cells unless player fog is toggled off.
- Create construction sites for player build tools before final buildings exist.
- Validate normal buildings with strict technical foundation checks, softer final 2.5D blocker reservation checks, and a required nearby builder work-access check.
- Use generated building sprites when a tool has art.
- Choose random house visual variants for placed houses while keeping menu/preview art stable.
- Choose random lumberjack camp visual variants for placed camps while keeping menu/preview art stable.
- Choose random stonecutter camp visual variants for placed camps while keeping menu/preview art stable.
- Choose random sawmill visual variants for placed sawmills while keeping menu/preview art stable.
- Choose random storage yard visual variants for placed storage yards while keeping menu/preview art stable.
- Choose random hunter camp visual variants for placed camps while keeping menu/preview art stable.
- Choose random fisher hut visual variants for placed huts while keeping menu/preview art stable.
- Choose random coal pit visual variants for placed pits while keeping menu/preview art stable.
- Choose random granary visual variants for placed granaries while keeping menu/preview art stable.
- Add ambient smoke/window-light overlays to placed houses.
- Reserve construction Logs/Stone/Planks from Storage Yards before accepting a construction site, with loose construction piles as fallback sources.
- Mark occupied cells when construction sites are accepted.
- Support Bridge as a special two-click placement tool: select one valid river bank, highlight opposite-bank candidates across contiguous River water, then create a construction site from the selected span.
- Create runtime placed-building records with selected visual variant data after construction completes.
- Mark construction-site technical foundation cells as not walkable while reserving future final blocker cells for placement collision.
- Mark completed final building walk-blocker cells as not walkable.
- Mark completed Bridge span cells as occupied and set their River water cells walkable through the map bridge overlay instead of blocking them like buildings.
- House uses an expanded 2.5D visual/navigation blocker around and above its technical footprint.
- Lumberjack camp places a `StrategyLumberjackCamp` worksite component, blocks its technical 2x2 footprint plus one visual row above, and hosts a local visual Logs stockpile.
- Stonecutter camp places a `StrategyStonecutterCamp` worksite component, blocks its technical 2x2 footprint plus one visual row above, and hosts a local visual Stone stockpile.
- Sawmill places a `StrategySawmill` worksite component, blocks its technical 3x2 footprint plus one visual row above, and hosts local visual Logs/Planks stockpiles plus active sawing overlay art.
- Kiln places a `StrategyKiln` worksite component, blocks its technical 2x2 footprint plus one visual row above, and hosts local visual Clay/Coal/Pottery stockpiles plus active firing overlay art.
- Storage yard places a `StrategyStorageYard` worksite component, blocks its technical 3x2 footprint plus one visual row above, and hosts local visual Logs/Stone/Iron/Coal/Clay/Planks/Pottery stockpiles.
- Hunter camp places a `StrategyHunterCamp` worksite component, blocks its technical 2x2 footprint plus one visual row above, and hosts a local visual `Game` stockpile.
- Fisher hut places a `StrategyFisherHut` worksite component, blocks its technical 2x2 footprint plus one visual row above, requires nearby water/shore access, and hosts a local visual `Fish` stockpile.
- Mine places a `StrategyMine` worksite component, blocks its technical 2x2 footprint plus one visual row above, requires an available underground Iron deposit under its footprint, and hosts a local visual Iron stockpile.
- Coal Pit places a `StrategyCoalPit` worksite component, blocks its technical 2x2 footprint plus one visual row above, requires an available underground Coal deposit under its footprint, and hosts a local visual Coal stockpile.
- Clay Pit places a `StrategyClayPit` worksite component, blocks its technical 2x2 footprint plus one visual row above, requires an available near-water Clay field under its footprint, and hosts a local visual Clay stockpile.
- Granary places a `StrategyGranary` food-storage component, blocks its technical 3x2 footprint plus one visual row above, and hosts local visual `Game`/`Fish` stockpiles.
- Accepted construction sites request active builders from the uncapped hired Storage Yard builder pool through balanced dispatch across all active sites, show material-drop and hammer-hit effects, and can wait if none are free yet.
- Bridge creates no worksite component, stores its selected span cells/endpoints on the placed-building record, and exposes bank endpoint cells as construction work/dropoff candidates so builders choose a reachable shore and do not stand in water.
- Completed house sites ask population to populate the finished house separately from the construction crew.
- Completed construction emits a placed-building completion event used by starter goals and future onboarding/progression systems.
- Completed construction releases temporary construction-site map blockers before applying final building blockers.
- Confirmed construction cancellation releases temporary map state and drops delivered/carried Logs/Stone/Planks as loose construction resource piles.
- Confirmed building demolition releases final occupied/walkability cells; Bridge demolition also removes river-span walkability and House demolition detaches residents.
- Seed placed-building records used by later visual upgrades.

Primary files/assets:

- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.Events.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.Part03.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.Part04.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.Debug.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.Part03.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyLooseConstructionResourcePile.cs`
- `Assets/Scripts/Runtime/Build/StrategyLooseConstructionResourcePile.Part02.cs`
- `Assets/Scripts/Runtime/Build/IStrategyConstructionResourceSource.cs`
- `Assets/Scripts/Runtime/Build/StrategyPlacedBuilding.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyLumberjackCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyStonecutterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategySawmill.cs`
- `Assets/Scripts/Runtime/Build/StrategyHunterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyFisherHut.cs`
- `Assets/Scripts/Runtime/Build/StrategyMine.cs`
- `Assets/Scripts/Runtime/Build/StrategyCoalPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.Part08.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Build/StrategyGranary.cs`
- `Assets/Scripts/Runtime/Build/StrategyHouseAmbientAnimator.cs`
- `Assets/Scripts/Runtime/Build/StrategyHouseAmbientSpriteFactory.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.cs`
- `Assets/Scripts/Runtime/Map/CityMapController.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.cs`
- `Assets/Scripts/Runtime/Map/StrategyForestryController.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Driver.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Catalog.cs`
- `Assets/Scripts/Runtime/Economy/StrategyConstructionResourceCost.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assets/Scripts/Runtime/Core/StrategyDebugOptions.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Placement is runtime-only and is not saved yet.
- Placed objects use tool-specific sprites when available; unknown future tools still fall back to colored sprites/TextMesh labels.
- Build placement consults fog exploration state, so early expansion starts around the camp and other revealed areas unless player fog is disabled from the F9 debug panel.
- House ambient overlays are visual-only child sprites and should not be used for footprint/collider calculations.
- Bridge placement requires two valid explored, unoccupied, walkable river-bank endpoint cells with a straight contiguous River water span between them; Lake water is rejected.
- With the current catalog, `House`, `Lumberjack Camp`, `Stonecutter Camp`, `Sawmill`, `Hunter Camp`, `Fisher Hut`, `Mine`, `Coal Pit`, `Clay Pit`, `Storage Yard`, and `Granary` can be selected and placed only where their technical foundation is fully walkable/buildable/explored, their future final 2.5D blocker can be reserved on buildable/explored/unoccupied cells, and builders have a nearby walkable work cell; Mine, Coal Pit, and Clay Pit are the only tools allowed to use matching Iron/Coal/Clay build-blocked resource cells.
- Final blocker reservation no longer requires every future visual blocker cell to be walkable at construction-site placement time.
- `Fisher Hut` additionally requires a nearby water cell with adjacent walkable shore access.
- `Mine` additionally requires at least one available underground Iron deposit under its footprint.
- `Coal Pit` additionally requires at least one available underground Coal deposit under its footprint.
- `Clay Pit` additionally requires at least one available near-water Clay field under its footprint.
- Successful player placement creates a construction site, closes the full Build menu, and marks the frame so world selection ignores the placement click.
- Construction site placement normally depends on reservable Logs/Stone/Planks, not on immediately available builders; waiting sites retry hired-builder dispatch.
- When F9 instant construction debug mode is enabled, player placement still creates a construction-site handoff but skips resource reservation, marks resources/progress complete, and immediately finalizes through the normal placed-building completion path; enabling the toggle also completes already active construction sites.
- Final building creation happens through construction-site completion, not the original placement click.
- Goal/progression listeners should use the building completion event so unfinished construction sites are never counted as completed buildings.
- Loose construction resource piles left by cancelled sites count toward construction affordability and can be reserved before Storage Yard stock.
- Future zoning/economy should replace or extend the placed marker with durable city state.
- Occupancy currently lives in the placement controller; move it into a city/map state service when save/load or simulation appears.

### House Visual Upgrades

Responsibilities:

- Install default Garden Beds automatically for placed houses.
- Install optional visual/production upgrades for placed houses from the selected-house HUD.
- Charge small construction resource costs from available Storage Yard resources when optional upgrades are installed.
- Track installed upgrade types on each placed house.
- Generate and cache upgrade sprites at runtime.
- Find nearby walkable cells for upgrade visuals without changing map walkability yet.
- Provide installed Garden Beds records for timed crop production and householder work behavior.
- Assign a produced resource to Garden Beds and Chicken Coop.
- Spawn idle chickens when a Chicken Coop upgrade is installed.
- Animate installed Garden Beds and Chicken Coop sprites with procedural frames.
- Show upgrade state, costs, and affordability state in the selected-house HUD.

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

- Current upgrades are visual/behavioral/resource-producing: default Garden Beds choose one crop, harvest it into the owning house on a fixed growth tick, and Householder work boosts the growth cycle; Chicken Coop spawns idle chickens and passively adds Eggs.
- Garden Beds are installed for free automatically on each House; Chicken Coop costs 1 Stone/2 Planks.
- Optional upgrade installation spends available Storage Yard stock immediately and respects construction reservations.
- Garden Beds sprite animation follows growth progress toward the next harvest; other upgrade sprite animation remains visual-only and does not change upgrade footprint or walkability.
- Placement uses `CityMapController.IsCellWalkable` to avoid houses/water but keeps residents free to walk through the visual upgrade cells for now.
- Future production/upkeep effects should extend this subsystem and the house resource layer instead of putting production logic directly into the HUD.

### House Resources MVP

Responsibilities:

- Define the first resource types.
- Store resource counts locally on placed houses.
- Generate runtime pixel-art resource icons for HUD display.
- Provide resource display ordering for the selected-house HUD.
- Include house-local forage food as ingredients for prepared household dishes.
- Provide shared HUD icon support for non-house stock resources such as hunted `Game` and caught `Fish`.

Primary files/assets:

- `Assets/Scripts/Runtime/Economy/StrategyResourceType.cs`
- `Assets/Scripts/Runtime/Economy/StrategyHouseResourceStore.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceIconFactory.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceIconFactory.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyPlacedBuilding.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingUpgrade.cs`
- `Assets/Scripts/Runtime/Population/StrategyHouseholdForagingState.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageNode.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageSpriteFactory.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Current resources are house-local runtime counts, not global economy inventory.
- Current resource sources are Garden Beds harvest ticks, Chicken Coop passive egg production, and household foraging of Berries/Roots/Mushrooms.
- Household foraging is home labor, not a profession HUD assignment; householders are excluded and children younger than 7 do not forage.
- Eggs, crops, forage, `Game`, and `Fish` are raw ingredients that can be stored at home and cooked into prepared `Dish`.
- `Game` and `Fish` are local production-building/Granary stock resources with shared HUD icons, and Householders can move them into house-local ingredient storage.
- Future trade, taxes, storage caps, spoilage, and needs should decide whether house stores remain local or feed into a settlement-level resource service.

### Sawmill Production

Responsibilities:

- Add `Sawmill` as a placed production building with local Logs and Planks stock.
- Assign 1 resident as a Sawyer through the Profession HUD.
- Request input Logs through the shared production logistics contract.
- Route Haulers to pick up Logs from Storage Yard stock, deliver them into the Sawmill, and route Sawyers to saw delivered Logs into `Planks`.
- Keep Sawyers visible inside the building and drive the detailed saw/log/plank work overlay plus sawdust effects while work is active.
- Expose Sawmill-local Planks to Haulers for hauling.

Primary files/assets:

- `Assets/Scripts/Runtime/Build/StrategySawmill.cs`
- `Assets/Scripts/Runtime/Build/StrategySawmill.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategySawmill.Part03.cs`
- `Assets/Scripts/Runtime/Build/IStrategyProductionLogisticsNode.cs`
- `Assets/Scripts/Runtime/Build/StrategyProductionStorage.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Build/StrategyLumberjackCamp.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Population/StrategyProfessionType.cs`
- `Assets/Scripts/Runtime/UI/StrategyProfessionHudController.cs`
- `Assets/Scripts/Runtime/UI/StrategyProfessionHudController.Part05.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Catalog.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceType.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Sawmill input Log reservations are separate from construction Log reservations so construction sites and production-input Haulers do not double-claim Storage Yard Logs.
- Sawmill counts Logs, Planks, and pending Planks against the shared production local stock cap of 6, with input Logs capped at 4 so output Planks can reserve space.
- `Planks` flow from Sawmills to Storage Yards and are consumed by selected late construction costs; they are not part of a global economy yet.
- Sawmill workers are normal exclusive workplace residents and should remain distinct from Storage Yard haulers; Sawyers do not move resources between buildings.

### Kiln Production

Responsibilities:

- Add `Kiln` as a placed production building with local Clay, Coal, and Pottery stock.
- Assign 1 resident as a Potter through the Profession HUD.
- Request input Clay and Coal through the shared production logistics contract.
- Route Haulers to pick up Clay/Coal from Storage Yard stock, deliver them into the Kiln, and route Potters to fire delivered inputs into `Pottery`.
- Keep Potters visible at the building and drive the firing work overlay plus spark/dust effects while work is active.
- Expose Kiln-local Pottery to Haulers for hauling.

Primary files/assets:

- `Assets/Scripts/Runtime/Build/StrategyKiln.cs`
- `Assets/Scripts/Runtime/Build/StrategyKiln.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyKiln.Part02.cs`
- `Assets/Scripts/Runtime/Build/IStrategyProductionLogisticsNode.cs`
- `Assets/Scripts/Runtime/Build/StrategyProductionStorage.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.Part09.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.Part09.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part46.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part47.cs`
- `Assets/Scripts/Runtime/Population/StrategyProfessionType.cs`
- `Assets/Scripts/Runtime/UI/StrategyProfessionHudController.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Catalog.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceType.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Kiln input reservations are separate from construction and Sawmill reservations so Clay/Coal input delivery cannot double-claim Storage Yard stock.
- Kiln counts Clay, Coal, Pottery, pending Pottery, and reservations against the shared production local stock cap of 6.
- `Pottery` currently flows from Kilns to Storage Yards but is not consumed by construction, food, trade, or upkeep yet.
- Potters are normal exclusive workplace residents and should remain distinct from Storage Yard Haulers; Potters do not move resources between buildings.

### Storage Yard Logistics

Responsibilities:

- Add `Storage Yard` as a placed storage building with local Logs, Stone, Iron, Coal, Clay, Planks, and Pottery stock.
- Keep Storage Yard stock uncapped.
- Spawn a starter Storage Yard near the campfire with 20 Logs and 20 Stone.
- Assign uncapped residents as Haulers, constrained by available adult residents and exclusive workplace state.
- Hire uncapped additional residents as dedicated construction builders, constrained by available adult residents and exclusive workplace/construction state.
- Find lumberjack camps with available stored Logs and reserve stock for haulers.
- Find stonecutter camps with available stored Stone and reserve stock for haulers.
- Find Mines with available stored Iron and reserve stock for haulers.
- Find Coal Pits with available stored Coal and reserve stock for haulers.
- Find Clay Pits with available stored Clay and reserve stock for haulers.
- Find Sawmills with available stored Planks and reserve stock for haulers.
- Find Kilns with available stored Pottery and reserve stock for haulers.
- Find Hunter Camps/Fisher Huts or loose food piles with available `Game`/`Fish` and reserve food for delivery to the nearest Granary.
- Find loose construction resource piles and reserve Logs/Stone/Planks for haulers after construction cancellation.
- Reserve Logs/Stone/Planks for accepted construction sites.
- Include loose construction resource piles in construction affordability and reservations.
- Provide reserved construction resource pickup cells for builders.
- Provide a shared construction resource source path so builders can pick up from Storage Yards or loose construction resource piles.
- Dispatch hired builders across waiting construction sites, favoring empty and lower-builder-count sites before stacking extras.
- Route Haulers to source camps, pick up Logs, carry them to storage, and deposit them.
- Route Haulers to stonecutter camps, pick up Stone, carry it to storage, and deposit it.
- Route Haulers to Mines, pick up Iron, carry it to storage, and deposit it.
- Route Haulers to Coal Pits, pick up Coal, carry it to storage, and deposit it.
- Route Haulers to Clay Pits, pick up Clay, carry it to storage, and deposit it.
- Route Haulers to production nodes, deliver non-food inputs from Storage Yard stock, then pick up outputs such as Sawmill Planks and Kiln Pottery, carry them to storage, and deposit them.
- Route Haulers to food sources, pick up `Game`/`Fish`, carry it to the nearest Granary, and deposit it.
- Route Haulers to loose construction resource piles, pick up Logs/Stone/Planks, carry them to storage, and deposit them.
- Update lumberjack/stonecutter camp, Mine, Coal Pit, Clay Pit, Sawmill, Kiln, and storage yard stock visuals as resources move, and show Stone/Iron/Coal/Clay/Planks/Pottery as separate storage piles.
- Show Hauler/builder counts, Logs/Stone/Iron/Coal/Clay/Planks/Pottery stock, and available source count in the selection HUD; player assignment/removal lives in the Profession HUD.

Primary files/assets:

- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.Part07.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.Part05.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.Part08.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.Part09.cs`
- `Assets/Scripts/Runtime/Build/StrategyLooseConstructionResourcePile.cs`
- `Assets/Scripts/Runtime/Build/StrategyLooseConstructionResourcePile.Part02.cs`
- `Assets/Scripts/Runtime/Build/IStrategyConstructionResourceSource.cs`
- `Assets/Scripts/Runtime/Build/IStrategyProductionLogisticsNode.cs`
- `Assets/Scripts/Runtime/Build/StrategyLumberjackCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyStonecutterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategySawmill.cs`
- `Assets/Scripts/Runtime/Build/StrategyKiln.cs`
- `Assets/Scripts/Runtime/Build/StrategyMine.cs`
- `Assets/Scripts/Runtime/Build/StrategyCoalPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategySawmill.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part35.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part47.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceType.cs`
- `Assets/Scripts/Runtime/Economy/StrategyConstructionResourceCost.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Catalog.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Haulers reserve worksite Logs/Stone/Iron/Coal/Clay/Planks/Pottery before walking to prevent multiple haulers from targeting the same stock.
- Haulers run Granary food hauling after normal storage-resource hauling checks, using the shared food reservation cleanup paths.
- Hauler and builder staffing has no per-yard slot limit; construction sites no longer cap their active visible builder crew at 2.
- Construction resources are normally reserved against Storage Yard stock and loose construction resource piles at site creation, then physically removed when builders pick them up; F9 instant construction debug mode bypasses this reservation path for player-placed buildings.
- Stonecutter Camp haul reservations are separate from Storage Yard construction reservations so production stock must be moved into storage before construction can claim it.
- Builders also create a per-builder pickup claim after a path to the pickup cell is found; cancelled work releases that claim while the construction-site reservation remains intact.
- If a builder dies while carrying a construction resource, the dropped loose construction pile restores the original site's reservation when that site still needs the resource.
- Residents currently support one active workplace: lumberjack camp, stonecutter camp, sawmill, kiln, hunter camp, fisher hut, mine, coal pit, clay pit, storage logistics, granary food logistics, or storage builder crew.
- Storage Yard stock is runtime-only and uncapped; it does not yet feed a global economy, save data, or consumption loop.
- Future resources should extend the logistics stock model; current Logs, Stone, Iron, Coal, Clay, Planks, and Pottery still have explicit carrying visuals/states.
- Storage Yard construction pickup and stock-visual helpers are split into `StrategyStorageYard.Part05.cs`; stock drop effects are in `StrategyStorageYard.Part08.cs` to keep source files below the 500-line limit.

### Granary Food Logistics

Responsibilities:

- Add `Granary` as a placed food-storage building with local `Game` and `Fish` stock.
- Keep Granary food stock uncapped.
- Use shared Storage Yard Haulers instead of a separate player-facing Granary Worker profession.
- Find Hunter Camps with available stored `Game` and reserve stock for Haulers.
- Find Fisher Huts with available stored `Fish` and reserve stock for Haulers.
- Route Haulers to source camps/huts, pick up reserved food, carry it to the granary, and deposit it.
- Provide settlement-level raw food availability and reservation APIs for Householder pickup.
- Update Hunter Camp/Fisher Hut stock visuals as food is picked up.
- Update Granary `Game`/`Fish` stock visuals and food drop effects as food is deposited.
- Update Granary `Game`/`Fish` stock visuals as Haulers deposit stock and Householders reserve/pick up ingredients.
- Show food stock and available source counts in the selection HUD.

Primary files/assets:

- `Assets/Scripts/Runtime/Build/StrategyGranary.cs`
- `Assets/Scripts/Runtime/Build/StrategyGranary.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyHunterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyFisherHut.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceType.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Driver.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Catalog.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Haulers reserve food before walking so multiple haulers do not target the same local stock.
- Food source reservations prevent multiple Haulers from double-claiming the same `Game`/`Fish`.
- `Game` and `Fish` remain runtime-local raw food stock; completed houses can receive them from Householder Granary pickups, but nightly dinner consumes prepared house `Dish` rather than direct Granary fallback.
- Residents currently support one active workplace: lumberjack camp, stonecutter camp, hunter camp, fisher hut, mine, storage logistics, or storage builder crew.
- Future spoilage, food needs, recipe variety, market logistics, or settlement-level food services should extend this subsystem rather than folding food into construction Storage Yards.

### Population MVP

Responsibilities:

- Create the starter camp with an animated campfire.
- Block the campfire cell while the fire is burning, then release it after the campfire burns out and disappears.
- Expose the starter camp world position for the initial camera focus.
- Spawn 3 initial families at startup, each with a father, a mother, and 1-2 adult children.
- Assign random Germanic/Nordic-style full names and age-appropriate adult ages to startup family members.
- Track resident runtime IDs, age, life stage, parent links, and child links.
- Keep lightweight family records for live and dead residents so ancestry-based kinship checks survive parent/ancestor death.
- Apply the husband's family name to the wife when an adult male/female household pair is formed.
- Adopt minor children with no living parents into eligible adult households without rewriting biological parent IDs.
- Track placed house records for household migration checks.
- Attach household birth state to occupied houses.
- Attach household food state to occupied houses.
- Attach household foraging state to house buildings.
- Assign the oldest adult female resident in each house as `Householder` and move her into home-duty work.
- Check close kinship through resident parent/ancestor links for future family/couple rules.
- Populate completed houses from the homeless adult male/female pool when possible, even if those residents already have workplaces or construction assignments.
- Fall back to free-house migration and partner lookup when no free pair can immediately occupy a completed house.
- Refresh persistent family records after marriage surname changes so Family Trees and HUDs see the current name while biological kinship IDs stay intact.
- Bind assigned residents to their home building.
- Spawn children for valid adult male/female house pairs after randomized household cooldowns when house capacity allows.
- Keep children younger than 3 years old inside their assigned home by hiding their world sprite/collider and skipping outdoor idle/funeral movement until they age out.
- Send housed idle residents home to sleep inside during the `Night` phase by hiding their world sprite/collider until morning, while leaving homeless residents outside.
- Resolve one nightly household dinner from prepared house `Dish`, using resident age-based ration needs after eligible residents return home for `Night`.
- Send Householders to fetch reserved raw `Fish`/`Game` from reachable Granaries into their own house when ingredient reserves are low.
- Send Householders to cook stored ingredients into prepared `Dish` during `Dusk` when dinner coverage is low.
- Track per-resident nutrition debt, days hungry, hungry/starving status, and recovery when nightly dinner needs are met.
- Grow children into adults after scaled game time.
- Continue resident aging after adulthood.
- Roll annual resident mortality from age 1 using an accelerating age-risk curve.
- Multiply annual resident mortality by each resident's malnutrition severity when household dinner shortages accumulate.
- Block resident death attempts while the resident is in active funeral duty, preventing carrier/attendee death from freezing the funeral controller.
- Remove dead residents from homes, work assignments, construction assignments, active reservations, live population counts, and selected-HUD targets.
- Create resident death snapshots and animated corpses when residents die.
- Gather close family/household funeral participants for mourning, procession, and burial.
- Run silent service burials with one nearby adult carrier when no living family/household funeral participants exist.
- Create a spontaneous cemetery away from the settlement and reserve carrier-reachable grave cells for burials.
- Create clickable grave sprites after burial and mark grave cells as not walkable.
- Temporarily interrupt resident tasks for funeral activities without permanently removing workplace roles.
- Move the oldest adult child still living with parents into empty houses.
- Move an eligible adult opposite-gender partner into single-resident adult-child houses while blocking close relatives.
- Create temporary refugee families with 1-3 members, 1-2 adult parents, and optional children.
- Gate the first refugee family on 3 completed registered houses; schedule later families with the repeat interval, fading arrival intensity after 40 accepted residents and stopping arrivals at 50 accepted residents.
- Keep pending refugees outside the normal resident registry until accepted.
- Accept refugee families into the normal resident registry or destroy rejected temporary families after they leave the map.
- Drive simple idle movement around the current camp/home through short walkable grid paths.
- Periodically send Householders from `TendingHousehold` home duty to work at their home's default Garden Beds upgrade, fetch raw `Fish`/`Game` from Granaries, or cook stored ingredients into `Dish`.
- Periodically send non-householder, unemployed adults and older children to nearby forage nodes during daytime, then carry Berries/Roots/Mushrooms back to their own house.
- Boost the Garden Beds growth cycle when garden work completes.
- Assign residents to lumberjack camps as workplace targets.
- Route assigned lumberjacks to the nearest available mature trees/processable wood, chopping work, fallen-trunk bucking, Logs pickup, camp stock deposit, planting cells, and sapling planting.
- Assign residents to stonecutter camps as workplace targets.
- Route assigned stonecutters to the nearest available Stone deposits, pickaxe mining, Stone carrying, and camp stock deposit.
- Assign residents to Mines as workplace targets.
- Route assigned miners to Mine entrances, hide them underground during work, reserve underground Iron deposits, mine Iron, and add it to Mine stock.
- Assign residents to Coal Pits as workplace targets.
- Route assigned coal miners to Coal Pit entrances, keep them visible inside the pit during work, reserve underground Coal deposits, mine Coal, and add it to Coal Pit stock.
- Assign residents to Clay Pits as workplace targets.
- Route assigned clay diggers to Clay Pit entrances, keep them visible inside the pit during work, reserve near-water Clay deposits, dig Clay, and add it to Clay Pit stock.
- Assign residents to Kilns as workplace targets.
- Route assigned potters to Kiln entrances, keep them visible during work, wait for delivered Clay/Coal, fire Pottery, and add it to Kiln stock.
- Assign residents to hunter camps as workplace targets.
- Route assigned hunters to the nearest available reserved adult rabbits, roughly 2-3 tile bow stand cells, bow aiming, arrow shots with a 20% miss chance, carcass approach on hit, butchering, `Game` carrying, and camp stock deposit.
- Assign residents to fisher huts as workplace targets.
- Route assigned fishers to the nearest available fish with validated land/shore cells, line casting with cast-range revalidation, hooked-fish reeling, `Fish` carrying, and hut stock deposit.
- Assign residents to storage yards as Haulers.
- Route assigned Haulers to lumberjack camp stock, stored-Logs pickup, storage-yard delivery, and deposit.
- Route assigned Haulers to stonecutter camp stock, stored-Stone pickup, storage-yard delivery, and deposit.
- Route assigned Haulers to Mine stock, stored-Iron pickup, storage-yard delivery, and deposit.
- Route assigned Haulers to Coal Pit stock, stored-Coal pickup, storage-yard delivery, and deposit.
- Route assigned Haulers to Clay Pit stock, stored-Clay pickup, storage-yard delivery, and deposit.
- Route assigned Haulers to Kiln stock, stored-Pottery pickup, storage-yard delivery, and deposit.
- Route assigned Haulers to Hunter Camp/Fisher Hut stock, stored-food pickup, granary delivery, and deposit.
- Assign residents to Storage Yards as dedicated builders.
- Route hired builders to reserved Storage Yard stock, construction resource pickup, site delivery, and hammer/build work after materials arrive.
- Drive frame-based axe swing animation and hit timing for lumberjacks.
- Drive frame-based pickaxe swing animation and hit timing for stonecutters.
- Drive frame-based bow shot and butchering animation/timing for hunters.
- Drive frame-based fishing rod/cast/reel animation and timing for fishers.
- Drive frame-based hammer/build animation and progress hit timing for construction builders.
- Generate 5 male and 5 female resident sprite variants at runtime.
- Generate child resident sprites at runtime.
- Generate cached 8-frame walking sprites for each adult/child resident visual variant.
- Generate resident portrait sprites for HUD display.
- Generate corpse/death animation frames, dragged shroud/rope sprites, crying resident frames, and grave sprites for funeral flow.
- Choose random resident visual variants at startup.
- Add synced resident readability renderers: silhouette outline and ground shadow.
- Generate and animate procedural campfire flame plus smoke/spark overlay frames at runtime.
- Drive campfire burnout visuals and restore campfire-cell walkability after extinguish.
- Drive simple chicken idle movement around a linked Chicken Coop upgrade with walk and peck sprite animations.
- Drive Chicken Coop egg production from a cycle timer synchronized with the coop's nest/egg animation frames.
- Expose runtime residents as read-only visibility sources for fog of war.

Primary files/assets:

- `Assets/Scripts/Runtime/Population/StrategyPopulationController.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.Part06.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentDeathSnapshot.cs`
- `Assets/Scripts/Runtime/Population/StrategyFuneralController.cs`
- `Assets/Scripts/Runtime/Population/StrategyFuneralController.Part02.cs`
- `Assets/Scripts/Runtime/Population/StrategyCemeteryController.cs`
- `Assets/Scripts/Runtime/Population/StrategyCorpse.cs`
- `Assets/Scripts/Runtime/Population/StrategyFuneralSpriteFactory.cs`
- `Assets/Scripts/Runtime/Population/StrategyGraveMarker.cs`
- `Assets/Scripts/Runtime/Population/StrategyRefugeeArrivalController.cs`
- `Assets/Scripts/Runtime/Population/StrategyHouseholdState.cs`
- `Assets/Scripts/Runtime/Population/StrategyHouseholdFoodState.cs`
- `Assets/Scripts/Runtime/Population/StrategyHouseholdFoodState.NightMeal.cs`
- `Assets/Scripts/Runtime/Population/StrategyHouseholdForagingState.cs`
- `Assets/Scripts/Runtime/Population/StrategyKinshipUtility.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part36.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part37.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part38.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part39.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part40.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part41.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part42.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part43.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part44.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part45.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part46.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part47.cs`
- `Assets/Scripts/Runtime/Build/StrategyLumberjackCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyStonecutterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyMine.cs`
- `Assets/Scripts/Runtime/Build/StrategyCoalPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyKiln.cs`
- `Assets/Scripts/Runtime/Build/StrategyKiln.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyKiln.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyHunterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyFisherHut.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Build/StrategyGranary.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.cs`
- `Assets/Scripts/Runtime/Map/StrategyForestryController.cs`
- `Assets/Scripts/Runtime/Map/StrategyForestryTree.cs`
- `Assets/Scripts/Runtime/Map/StrategyStoneDeposit.cs`
- `Assets/Scripts/Runtime/Map/StrategyIronDeposit.cs`
- `Assets/Scripts/Runtime/Map/StrategyCoalDeposit.cs`
- `Assets/Scripts/Runtime/Map/StrategyClayDeposit.cs`
- `Assets/Scripts/Runtime/Map/StrategyStonecutEffectAnimator.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageNode.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageSpriteFactory.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyRabbitAgent.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyFishAgent.cs`
- `Assets/Scripts/Runtime/Population/StrategyHuntingArrowProjectile.cs`
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
- Startup residents spawn as family-linked adults; children born during play start at age 0, stay inside assigned homes until age 3, become adults at age 16 through scaled game time, and continue aging after adulthood.
- `StrategyPopulationController` owns the live resident ID registry plus persistent family records used by kinship lookup after deaths.
- Resident death should continue to flow through `StrategyPopulationController` so death snapshots, assignment cleanup, selection cleanup, funeral queuing, and family records stay in one path.
- Active funeral duty is a hard death guard in the central population death path; future funeral recovery work can replace this only after carrier/attendee death is safely handled.
- `StrategyFuneralController` owns the runtime funeral state machine; it should stay separate from normal workplace AI and should only use public resident funeral hooks.
- Service burials are selected when a funeral has no living family/household participants; they use one silent adult carrier chosen from the nearest eligible adults and should not start crying/mourning poses.
- `StrategyFuneralController` builds a carrier reachable-cell set before reserving a grave; funeral resident movement must fail rather than fall back to direct world movement when no walkable path exists.
- Funeral processions drag the corpse behind a primary carrier with a visible rope and clamp the corpse within one map-cell distance from that carrier.
- Burial starts after reachable expected attendees gather around the reserved grave or after the grave-gather timeout prevents a deadlock.
- `StrategyCemeteryController` owns spontaneous cemetery placement and grave reservation; graves are world blockers and clickable markers, so map walkability and grave HUD data must be updated when grave cells are created.
- Cemetery placement should remain away from active settlement space without drifting to extreme map edges; current scoring favors a moderate camp distance, penalizes edge cells, and rejects grave candidates without enough carrier-reachable stand cells.
- `StrategyPopulationController` also owns the runtime house registry used for free-house migration and partner retry checks.
- Pending refugee families are rendered as resident agents but are not counted as residents, workers, or fog sources until accepted.
- Accepted refugee families join the normal registry as a preserved family block, stay near camp while homeless, and get priority to fill the first empty House as a whole household before normal single-adult migration or random pair assignment.
- `StrategyHouseholdState` lives on occupied houses and owns the randomized birth timer.
- `StrategyHouseholdState` blocks births while the same house has sustained ration shortages or birth-blocked residents.
- `StrategyHouseholdFoodState` lives on occupied houses, resolves one nightly dinner per day after a one-day settling grace, waits for eligible residents to enter home for `Night` with a fallback deadline, consumes prepared house `Dish`, applies short rations to resident nutrition debt, and exposes aggregate food status for HUDs.
- Householder home duty can reserve one raw `Fish`/`Game` unit from a Granary, path to pickup, carry it home, store it as an ingredient, and cook stored ingredients into prepared `Dish` during `Dusk`.
- `StrategyHouseholdForagingState` lives on house buildings and dispatches only unassigned non-householder residents; it should remain separate from the Profession HUD/worksite assignment model.
- `StrategyPlacedBuilding` owns the current Householder reference for houses, preferring the oldest adult female resident and refreshing on home changes, death/unregister, and resident adulthood.
- `StrategyResidentAgent.HasWorkplace` includes the Householder role, so profession assignment should treat householders as occupied home workers.
- Householder assignment clears external worksite/builder roles through their owning worksite APIs and uses `TendingHousehold` instead of `Idle` for home duty.
- `StrategyKinshipUtility` treats close parent/ancestor graph distance as a block for future couple/family rules, including ancestors whose resident GameObjects were destroyed after death.
- Resident readability helpers are visual-only child `SpriteRenderer`s and should stay synced when changing resident animation frames.
- Residents use trail-aware 8-direction A* grid paths with no diagonal corner cutting and post-path smoothing for idle, home, workplace, construction, logistics, and funeral travel while keeping frame-based sprite walk cycles.
- Resident movement records activity-weighted trail footfall per entered visible resident cell, and formed trails apply a 15% speed bonus.
- Resident pathfinding can recover a blocked start cell by snapping to a nearby walkable cell and logging `PathStartRecovered`.
- Resident scheduled work starts only during `StrategyDayNightCycleController.IsSettlementWorkTime`; Day 1 Dawn is also work time to avoid starter construction delay, while later Dawn phases remain off normal work time. Keep carried-resource returns, deposits, and cleanup paths schedule-safe so nightfall cannot strand stock reservations.
- Resident night sleep is separate from homebound young-child hiding: housed residents only enter the hidden home interior during `Night` when they are not carrying resources, in funeral duty, or underground, notify household food state for dinner readiness, then reappear at the home exit after night ends.
- Resident footstep audio is attached by `StrategyResidentAgent` and plays grass clips on selected walk frames; keep it low-volume/spatial when adding more residents or faster simulation speeds.
- Lumberjack work keeps the same camp worksite component but chooses the nearest available tree/processable wood on the map; it tests nearby work cells for real path reachability before starting tree/log/plant movement, and includes tree chopping, trunk bucking, Logs delivery, and sapling planting.
- Resident woodcut sprites are generated for every male/female visual variant and should stay in sync with readability outline mirroring.
- Stonecutter work keeps the same camp worksite component but chooses the nearest available finite Stone deposit on the map and does not plant/regrow Stone.
- Resident stonecut sprites are generated for every male/female visual variant and should stay in sync with readability outline mirroring.
- Mine work follows the local worksite assignment model but keeps miners hidden underground during the timed work loop, reserves walkable underground Iron indicators, and stores produced Iron locally at the Mine.
- Coal Pit work follows the local worksite assignment model but keeps coal miners visible inside the pit during the timed work loop, reserves walkable underground Coal indicators, and stores produced Coal locally at the Coal Pit.
- Clay Pit work follows the local worksite assignment model but keeps clay diggers visible inside the pit during the timed work loop, reserves walkable near-water Clay fields, and stores produced Clay locally at the Clay Pit.
- Sawmill work follows the local worksite assignment model, waits for Hauler-delivered Logs from Storage Yard stock, keeps Sawyers visible inside the Sawmill during the timed work loop, and stores produced Planks locally at the Sawmill.
- Kiln work follows the local worksite assignment model, waits for Hauler-delivered Clay and Coal from Storage Yard stock, keeps Potters visible during the timed firing loop, and stores produced Pottery locally at the Kiln.
- Hunter work keeps the same camp worksite component but reserves the nearest available adult rabbit through `StrategyWildlifeController`, chooses a reachable roughly 2-3 tile bow stand cell, uses a 20% arrow miss chance, and stores produced `Game` locally at the hunter camp on hits.
- Resident bow and butchering sprites are generated for every male/female visual variant and should stay in sync with readability outline mirroring.
- Fisher work keeps the same hut worksite component but reserves the nearest available fish through `StrategyWildlifeController`, requires a valid land/shore stand cell around the target, abandons casts when the fish leaves cast range during cast/wait/reel phases, and stores produced `Fish` locally at the fisher hut for now.
- Resident fishing sprites are generated for every male/female visual variant and should stay in sync with readability outline mirroring.
- Granary food logistics is serviced by shared Haulers, moving food from production buildings into food storage after normal storage-resource hauling checks.
- Storage Yard Haulers move Logs, Stone, Iron, Coal, Clay, Planks, and Pottery outputs from production worksites into Storage Yard stock, and deliver non-food production inputs from Storage Yard stock into production nodes; Coal, Clay, Planks, and Pottery use their own carried sprite and return/drop cleanup paths.
- Construction assignment is a temporary exclusive task for hired Storage Yard builders; there is no hired-builder pool cap or construction-site builder cap, balanced dispatch spreads free builders across active sites first, and workplace assignment skips residents already attached to a construction site. Construction assignment does not block home/family assignment.
- Builder construction pickup path failures include start/pickup walkability details in `debug.log`; repeated pickup path failures drop that builder's current site assignment so another builder can retry.
- Worker and builder assignment must check `StrategyResidentAgent.CanWork`; children under age 3 remain inside assigned homes, and older children idle/walk but cannot work.
- Resident construction sprites are generated for every male/female visual variant and should stay in sync with readability outline mirroring.
- Resident crying sprites are generated for adult and child funeral mourning/waiting states and should stay in sync with readability outline mirroring.
- Chickens use the same local path style as before; their animation is visual-only.
- House construction no longer consumes residents as builders; after completion, the finished house tries to pull one homeless adult male and one homeless adult female from the starter camp/free pool, regardless of workplace or construction role.
- Male/female household pair creation and partner move-in rename the wife to the husband's family name; this is a current display/name rule, not a separate explicit marriage entity yet.
- Assigning a home should not cancel active workplace/construction tasks; idle residents can walk home immediately, and busy residents keep the home binding for later idle/home behavior.
- If no free pair exists, the completed house is available for adult-child migration and partner lookup.
- House occupation consumes the finite free-resident pool from the starter camp while it exists; later household births and adult-child migration are the first internal population growth path.
- Resident death must continue to go through the centralized population cleanup path; direct `Destroy` on accepted residents risks stale worksite, construction, home, HUD, or kinship state.
- Resident helper methods for carried-resource return, construction work, workplace clearing, readability sync, refugee path following, tree movement, fishing cast/reel flow, production-input delivery, trail movement, ranged hunt stand selection, reachable forestry work-cell selection, reachable construction dropoff selection, worker-triggered visual effects, day/night work scheduling, night home sleep, Clay work/logistics, and Kiln/Pottery work/logistics are split across `StrategyResidentAgent.Part27.cs` through `StrategyResidentAgent.Part47.cs` to keep source files below the 500-line limit.
- Future jobs/families/economy should extend resident state rather than replacing the home/free-camp assignment model.

### World Selection

Responsibilities:

- Select placed buildings, construction sites, residents, and completed graves with left-click.
- Ignore left-click selection in unexplored fog cells unless player fog is toggled off.
- Ignore world selection while the pointer is over UI.
- Show a simple marker under the selected world object.
- Show dynamic linked-resident markers/lines when a completed building or construction site is selected while keeping the HUD focused on the clicked object.
- Show a compact full-height right-side selection HUD for the selected object.
- Show a separate bottom-right world inspect microHUD for clicked graves, resources, nature props, and wildlife; residents, placed buildings, construction sites, and house upgrades use the right-side selection HUD only.
- Resolve inspect information through `IStrategyWorldInspectable` and visible sprite bounds for non-building world objects; empty terrain cells do not open the microHUD.
- Render typed inspect chip/row dashboards for wildlife, mineral deposits, trees, forage, loose carried resources, and loose construction materials while keeping legacy body text as a fallback.
- Show selected-object preview sprites and status/context blocks.
- Show selected residents with a dedicated compact dashboard: identity subtitle, portrait, role/home/food chips, and icon-led task, home, food, and family rows.
- Expose house-specific visual upgrade actions in the selected-house HUD.
- Show selected-house resident portraits/names/age/life stage/statuses up to house capacity, including the Householder marker, compact upgrade action rows, prepared dishes, ingredient rations, resource icons/counts, and Garden Beds crop.
- Show selected worksite status/resource context without worker assignment controls.
- Show selected Storage Yards with a dedicated icon-led logistics dashboard for Haulers, builders, available sources, resource stock, and readiness status.
- Show selected lumberjack/stonecutter/sawmill/kiln/hunter/fisher/mine/coal pit/clay pit/granary/storage stock and nearby source/target counts.
- Show selected-construction-site cost, delivered resources, builder count, and progress/status context.
- Show selected-resident full name, portrait, profile, age/life stage, current activity, and home/camp assignment.
- Show selected-grave deceased name, epitaph, age, final profession, family role, and memory text.
- Listen for `Delete` on selected construction sites/buildings and open the reusable confirmation dialog before cancellation or demolition.

Primary files/assets:

- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.Part09.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.Part10.cs`
- `Assets/Scripts/Runtime/Selection/IStrategyWorldInspectable.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldInspectInfo.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldInspectInfoFactory.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldInspectHudController.cs`
- `Assets/Scripts/Runtime/Selection/StrategyStaticWorldInspectable.cs`
- `Assets/Scripts/Runtime/UI/StrategyConfirmationDialogController.cs`
- `Assets/Scripts/Runtime/Build/StrategyPlacedBuilding.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingUpgradeController.cs`
- `Assets/Scripts/Runtime/Build/StrategyLumberjackCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyStonecutterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategySawmill.cs`
- `Assets/Scripts/Runtime/Build/StrategyHunterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyFisherHut.cs`
- `Assets/Scripts/Runtime/Build/StrategyMine.cs`
- `Assets/Scripts/Runtime/Build/StrategyCoalPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Build/StrategyGranary.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.cs`
- `Assets/Scripts/Runtime/Population/StrategyGraveMarker.cs`
- `Assets/Scripts/Runtime/Economy/StrategyHouseResourceStore.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceIconFactory.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceIconFactory.Part02.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Map/StrategyForestryController.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Selection uses 2D colliders attached to placed buildings, construction sites, and residents.
- Residents have selection priority over buildings when colliders overlap.
- Destructive selection actions route through `StrategyBuildPlacementController` so map blockers, bridge walkability, residents, worksite assignments, and construction resources clean up consistently.
- Selection ignores the same frame that completed placement so the new building is not auto-selected by the placement click.
- Selection consults fog exploration state before checking 2D world colliders.
- Selection HUD is runtime-created in the world selection controller and slides in from the right.
- World inspect microHUD is runtime-created by the selection controller, uses non-blocking Screen Space Overlay UI, shifts left while the right-side selected-object HUD is open, and intentionally excludes residents, placed buildings, construction sites, and house upgrades.
- Non-selectable world objects should implement `IStrategyWorldInspectable`; do not add mass click-only physics colliders for inspect objects unless they are also truly selectable.
- Building selection links are visual-only world overlays: Houses use `StrategyPlacedBuilding.Residents`; worksites use their assigned worker lists; Storage Yards include both Haulers and builders; selected construction sites link to their assigned builders.
- House resident rows use the assigned resident references stored on `StrategyPlacedBuilding` and grow to the current house capacity.
- Worksite context uses references/counts stored on the selected worksite component, but player assignment/removal is owned by the Profession HUD.
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
