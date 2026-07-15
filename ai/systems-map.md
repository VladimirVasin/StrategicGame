# Systems Map

Last updated: 2026-07-14

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
- `Assets/Scripts/Runtime/ProjectUnknown.Runtime.asmdef`
- `Assets/Editor/ProjectUnknown.Editor.asmdef`
- `Assets/Tests/EditMode/ProjectUnknown.EditModeTests.asmdef`
- `Tools/Verification/`
- `.github/workflows/technical-gates.yml`
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
- Runtime weather overlay sorting bands for wet ground/cold wash, chunk-repainted cloud shadows, chunk-repainted mist, rain, and snow.
- Runtime seasonal terrain/building surface overlays for snow cover and water ice.
- Runtime URP post-processing for soft color grading, bloom, and vignette driven by day/night and weather state.
- Runtime cinematic visuals for 2D global/local lights, emissive masks, animated building torch/lantern source sprites with manual night-light state, active hand-carried resident torch lights, light-aware nighttime darkness over unlit cells, wet puddle glints, lightning flashes, and foreground depth accents.
- Pooled spring/autumn camera-area details, centralized vegetation season tinting, and subtle view-gated event camera feedback.

Primary files/assets:

- `Assets/UniversalRenderPipelineGlobalSettings.asset`
- `Assets/DefaultVolumeProfile.asset`
- `Assets/Settings/UniversalRP.asset`
- `Assets/Settings/Renderer2D.asset`
- `Assets/Scripts/Runtime/Core/StrategyWorldSorting.cs`
- `Assets/Scripts/Runtime/Core/StrategyWorldEffectAnimator.cs`
- `Assets/Scripts/Runtime/Core/StrategyPostProcessController.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicVisualController.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicVisualController.LightLod.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicVisualController.Part01.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicVisualController.Part03.cs`
- `Assets/Scripts/Runtime/Core/StrategyBuildingLightSpriteFactory.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicLightEmitter.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicLightEmitter.Profile.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicLightEmitter.Torch.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicLightEmitter.Coverage.cs`
- `Assets/Scripts/Runtime/Core/StrategyNightLightSource.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicVisualMath.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicVisualSprites.cs`
- `Assets/Scripts/Runtime/Core/StrategySeasonCalendar.cs`
- `Assets/Scripts/Runtime/Core/StrategyDayNightCycleController.Calendar.cs`
- `Assets/Scripts/Runtime/Core/StrategyDayNightCycleController.cs`
- `Assets/Scripts/Runtime/Core/StrategyShadowCaster2D.cs`
- `Assets/Scripts/Runtime/Core/StrategyShadowSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSnowSpriteFactory.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherVisualController.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherVisualController.Chunks.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherVisualController.Snow.cs`
- `Assets/Scripts/Runtime/Weather/StrategySeasonalSurfaceController.cs`
- `Assets/Scripts/Runtime/Weather/StrategySeasonalSurfaceController.Painting.cs`
- `Assets/Scripts/Runtime/Weather/StrategySeasonAmbientDetailController.cs`
- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.Seasons.cs`
- `Assets/Scripts/Runtime/Camera/StrategyCameraFeedbackController.cs`

Impact hints:

- Rendering settings affect scene appearance globally.
- World sprites should use `StrategyWorldSorting` instead of fixed type-based `sortingOrder` values so farther objects do not render in front of nearer ones.
- Short-lived world effects should use `StrategyWorldEffectAnimator` and `StrategyRuntimeObjectCreationGuard` instead of spawning one-off ad hoc particle objects.
- The day/night and weather overlays sort around world sprites while staying below placement preview/fog/UI; keep that ordering when adding more world overlays.
- Seasonal surface overlays sort above terrain/water and below roads/weather wash so snow and ice read as ground cover without hiding roads or world sprites.
- Day/night owns the canonical display day, 24-hour clock, time-of-day phase labels, phase accent colors, the explicit Spring -> Summer -> Autumn -> Winter seven-day season cycle, and dawn/nightfall/season-start event-log triggers; HUDs should read that snapshot instead of inventing separate clocks.
- Post-process tuning should stay subtle and pixel-readable; avoid blur, heavy chromatic aberration, or aggressive grain for normal strategy view.
- Cinematic visual effects should stay bounded to reusable emitters/controllers rather than adding per-building one-off light scripts; building torch/lantern source sprites and the night darkness mask are cheap overlays, while real `Light2D` point lights should stay LOD-capped and lazily created because many simultaneous 2D lights can cause visible frame spikes. Non-campfire torch/lantern emitters read `StrategyNightLightSource` manual lit state; resident personal `Dusk` hand torches should use cheap sprite/mask lighting, with resident `Light2D` reserved for actual `Night` lamp-lighting duty. Fire-source daylight fade uses the shared Dawn-to-start-of-`Noon` factor rather than a hard Dawn cutoff, and should shrink/disable only flame layers instead of making torch/campfire bodies transparent. Night-mask work should keep using the cached camera-area emitter list from cinematic LOD instead of scanning every emitter per mask pixel.
- `StrategyShadowCaster2D` is the shared runtime shadow path for world sprites; tune shape/scale/offset per object type and let day/night control opacity/length globally.
- Verify scenes visually in Unity after meaningful changes.

### Visual Asset Pipeline

Responsibilities:

- Load one Resources-backed visual catalog before map generation and starter sprite prewarming.
- Bake editable building, resident, nature, terrain, construction, road, production-work, and stock PNGs through one Editor pipeline.
- Resolve authored building, resident-pose, nature, terrain, construction, road, work, and stock sprites before procedural factory fallback.
- Provide complete authored final/construction coverage for the current Build catalog, a shared standalone/upgrade Chicken Coop animation, and span-aware modular Bridge composition.
- Resolve authored building-ground sprites before generated trampled-ground fallback.
- Prewarm readable terrain sprites into immutable managed pixel arrays on the main thread before parallel terrain painting.
- Supply one readable Resources-backed runtime font and shared sliced panel/button frames through the centralized UI theme provider.
- Keep partial catalog population safe so art can migrate system by system.

Primary files/assets:

- `Assets/Resources/Visual/StrategyVisualCatalog.asset`
- `Assets/Resources/Visual/Baked/`
- `Assets/Resources/Visual/Authored/`
- `Assets/Art/ART_DIRECTION.md`
- `Assets/Editor/StrategyVisualCatalogBaker.cs`
- `Assets/Editor/StrategyVisualCatalogBaker.IO.cs`
- `Assets/Editor/StrategyVisualCatalogBaker.Residents.cs`
- `Assets/Editor/StrategyVisualCatalogBaker.Layers.cs`
- `Assets/Editor/StrategyVisualCatalogBaker.Terrain.cs`
- `Assets/Editor/StrategyVisualCatalogBaker.Bridge.cs`
- `Assets/Editor/StrategyVerificationRunner.AuthoredVisuals.cs`
- `Assets/Editor/StrategyVerificationRunner.AuthoredVisuals.Animations.cs`
- `Assets/Editor/StrategyVerificationRunner.AuthoredVisuals.Bridge.cs`
- `Assets/Editor/StrategyVerificationRunner.AuthoredVisuals.Bridge.Catalog.cs`
- `Assets/Editor/StrategyVerificationRunner.AuthoredVisuals.Construction.cs`
- `Assets/Editor/StrategyVerificationRunner.AuthoredVisuals.IO.cs`
- `Assets/Editor/StrategyVerificationRunner.AuthoredVisuals.Models.cs`
- `Assets/Scripts/Runtime/Visual/StrategyVisualCatalog.cs`
- `Assets/Scripts/Runtime/Visual/StrategyVisualCatalog.Atlases.cs`
- `Assets/Scripts/Runtime/Visual/StrategyVisualCatalog.Terrain.cs`
- `Assets/Scripts/Runtime/Visual/StrategyVisualCatalogProvider.cs`
- `Assets/Scripts/Runtime/Visual/StrategyVisualBakeSource.cs`
- `Assets/Scripts/Runtime/Visual/StrategyVisualSequenceIds.cs`
- `Assets/Scripts/Runtime/Visual/StrategyResidentVisualPose.cs`
- `Assets/Scripts/Runtime/Visual/StrategyBuildingGroundDetail.cs`
- `Assets/Scripts/Runtime/Visual/StrategyBuildingGroundSpriteFactory.cs`
- `Assets/Scripts/Runtime/UI/StrategyUiThemeProvider.cs`
- `Assets/Resources/Fonts/Inter-Regular.ttf`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.ChickenCoop.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSpriteFactory.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSnowSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingVariantProfile.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingVisualAlignment.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingVisualAnchorProfile.cs`
- `Assets/Scripts/Runtime/Build/StrategyChickenCoopVisualProfile.cs`
- `Assets/Scripts/Runtime/Build/StrategyBridgeVisualProfile.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentSpriteFactory.cs`
- `Assets/Scripts/Runtime/Menu/StrategyMapPreloadCoordinator.Content.cs`
- `Tools/Art/HighResolutionBuildings.manifest.json`
- `Tools/Art/HighResolutionConstruction.manifest.json`
- `Tools/Art/HighResolutionBuildingAnimations.manifest.json`
- `Tools/Art/HighResolutionBuildingAnimationFrames.manifest.json`
- `Tools/Art/HighResolutionBridge.manifest.json`
- `Tools/Art/Build-AuthoredBuildingAssets.ps1`
- `Tools/Art/Build-AuthoredConstructionAtlases.ps1`
- `Tools/Art/Build-AuthoredBuildingAnimationAtlases.ps1`
- `Tools/Art/Build-AuthoredBridgeKit.ps1`
- `Tools/Art/Build-HouseConstructionAtlas.ps1`
- `Tools/Art/Build-ForagerCampConstructionAtlas.ps1`
- `Tools/Art/Upgrade-ConstructionAtlas2x.ps1`
- `Tools/Art/Source/HighResolution/`

Impact hints:

- Catalog entries are optional; missing sets, variants, or frames must continue into the existing procedural fallback.
- The baker deletes and recreates only `Visual/Baked`; matching final, construction, and animation paths under `Visual/Authored` replace generated catalog entries after manifest and layout validation. All current non-Bridge building families use explicit high-density contracts (normally `48 PPU`, with the animated Chicken Coop at `42 PPU`) that preserve procedural world dimensions.
- Bridge art is a centered `48 PPU` exception: the runtime composes horizontal/vertical Start, Middle, and End modules for `3-12`-cell spans and requires every final/construction module sequence to remain cataloged and seam-compatible.
- Imported authored sprites use their manifest filter, PPU, and pivot contracts plus the existing Y-based sorting path. Geometry-dependent stock, worker, effect, light, snow, selection, and collider alignment must route through the shared visual alignment/anchor profiles.
- Atlas textures must stay `Sprite/Single`; automatic Multiple slicing breaks runtime rectangular frame extraction.
- Worker terrain code must consume only the prewarmed managed swatch cache, never Unity textures or sprites directly.
- Seasonal overlays, carried-resource layers, shadows, snow caps, selection bounds, and click colliders must still align after replacing a base sprite.
- Runtime text should read `StrategyUiThemeProvider.Font`; replace the centralized Inter asset instead of assigning per-HUD fonts.

### Scene Foundation

Responsibilities:

- Intro menu, founding journey, gameplay scene, and reusable scene templates.
- Build-scene order and scene-role separation.

Primary files/assets:

- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/FoundingJourney.unity`
- `Assets/Scenes/SampleScene.unity`
- `Assets/Scripts/Runtime/Menu/StrategySceneCatalog.cs`
- `ProjectSettings/EditorBuildSettings.asset`
- `Assets/Settings/Scenes/URP2DSceneTemplate.unity`
- `Assets/Settings/Lit2DSceneTemplate.scenetemplate`

Impact hints:

- Scene edits can implicitly depend on rendering, input, UI, and gameplay scripts.
- Update this map when new gameplay scenes, bootstrap scenes, or scene loading systems are added.

### Main Menu And Map Preloading

Responsibilities:

- Present the actual first-screen Continue/New Settlement/Settings/Quit experience without starting gameplay simulation.
- Present one dedicated generated static pixel-art key image instead of composing gameplay sprites or runtime backdrop layers.
- Keep music disabled in the menu while retaining HUD SFX, and animate pointer/selection hover feedback on every command button.
- Validate save availability and prepare one likely Continue/New map candidate while the menu remains interactive.
- Route Continue directly into gameplay and New Settlement through `FoundingJourney` without duplicating the prepared map.
- Prewarm starter visuals plus short HUD/footstep clips, report real generation progress, and transfer the prepared map into the founding/gameplay flow.
- Persist master/music/effects volume and fullscreen state.

Primary files/assets:

- `Assets/Scenes/MainMenu.unity`
- `Assets/Scripts/Runtime/Menu/StrategyMainMenuBootstrap.cs`
- `Assets/Scripts/Runtime/Menu/StrategyMainMenuController.cs`
- `Assets/Scripts/Runtime/Menu/StrategyMainMenuController.View.cs`
- `Assets/Scripts/Runtime/Menu/StrategyMainMenuBackdrop.cs`
- `Assets/Scripts/Runtime/Menu/StrategyMainMenuButtonHover.cs`
- `Assets/Resources/Visual/Menu/MainMenuKeyArt.png`
- `Assets/Scripts/Runtime/Menu/StrategyMapPreloadCoordinator.cs`
- `Assets/Scripts/Runtime/Menu/StrategyMapPreloadCoordinator.Content.cs`
- `Assets/Scripts/Runtime/Menu/StrategyGameSettings.cs`
- `Assets/Scripts/Runtime/Menu/StrategySceneCatalog.cs`
- `Assets/Scripts/Runtime/Map/CityMapController.Generation.cs`
- `Assets/Scripts/Runtime/Persistence/StrategySaveSystem.cs`

Impact hints:

- Keep only one map candidate alive; a 192x192 map at 16 pixels per cell needs a roughly 36 MB terrain pixel buffer before upload.
- Map cells and progress can be staged, and pure terrain pixels can run on workers; `Texture2D`, `Sprite`, scene changes, and all other Unity objects stay on the main thread.
- Continue and New Settlement must cancel mismatched candidate work before starting a different seed.
- Main-menu key art is independent from gameplay sprite factories; keep point filtering, cover-crop behavior, the dark left UI-safe area, and the single static-image ownership model when replacing it.
- Do not create/configure `StrategyMusicController` or bulk-load long Music/Nature folders in the menu; hover feedback should remain one quiet, cooldown-limited HUD cue per pointer entry.

### Founding Journey And Start-Site Selection

Responsibilities:

- Present four pre-game story panels and four stable-ID founding questions without starting gameplay simulation.
- Support Back/Skip, balanced defaults, centralized UI input, authored cover-cropped shots, cinematic chrome/staged reveals, artwork-bound atmosphere, scene-owned ambience, and a persistent reduced-motion option.
- Capture the prepared map into defensive plain data, select a deterministic safe camp/cart layout, and expose fallback diagnostics.
- Carry the selected profile/layout into population startup, nature/forage exclusions, exact starter-cart placement, persistence, and launch verification.

Primary files/assets:

- `Assets/Scenes/FoundingJourney.unity`
- `Assets/Resources/Visual/Founding/`
- `Assets/Scripts/Runtime/Menu/StrategyFoundingJourneyBootstrap.cs`
- `Assets/Scripts/Runtime/Menu/StrategyFoundingJourneyController.cs`
- `Assets/Scripts/Runtime/Menu/StrategyFoundingJourneyController.Flow.cs`
- `Assets/Scripts/Runtime/Menu/StrategyFoundingJourneyController.View.cs`
- `Assets/Scripts/Runtime/Menu/StrategyFoundingJourneyController.Chrome.cs`
- `Assets/Scripts/Runtime/Menu/StrategyFoundingJourneyAtmosphere.cs`
- `Assets/Scripts/Runtime/Menu/StrategyFoundingJourneyPresentation.cs`
- `Assets/Scripts/Runtime/Menu/StrategyFoundingJourneyAudio.cs`
- `Assets/Scripts/Runtime/Founding/`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.Founding.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.Founding.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.Founding.cs`
- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.Founding.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageResourceController.Founding.cs`
- `Assets/Tests/EditMode/StrategyStartSiteSelectorTests.cs`
- `Assets/Editor/StrategyVerificationRunner.FoundingJourney.cs`

Impact hints:

- Question/answer IDs and profile versions are persistence contracts; rename them only with an explicit save migration.
- Safety, connected land, resident spawn capacity, and cart reservation remain hard constraints; preferences only rank valid candidates.
- Keep `StrategyStartSiteSelector` deterministic and free of `UnityEngine.Random`; the same map seed and answers must return the same layout.
- The reserved cart footprint is `3x3` even though the visible cart is `3x2`; population, nature, forage, placement, validation, and smoke checks must stay aligned.
- Continue uses restored-state semantics: create the camp anchor required for bootstrap, but do not spawn transient new-game residents or a second starter cart before applying the save.
- Story art stays Resources-backed Sprite/Single with point filtering; missing art must log and degrade without breaking scene navigation.
- Presentation owns shot/crossfade/reveal transforms, atmosphere follows the active artwork transform, and the journey audio component owns only scene-local Weather/Fire loops; keep those lifetimes inside `FoundingJourney`.

### Runtime Bootstrap

Responsibilities:

- Install the scene-loaded hook and start the strategy layer only for the gameplay scene.
- Own one typed scene-local service context and explicit bootstrap lifecycle/failure/disposal state.
- Transfer prepared map ownership explicitly and clear scene statics on unload/return to menu.
- Configure the strategy debug logger before other runtime systems start.
- Ensure a city map exists.
- Ensure a Unity `WindZone`-backed strategy wind source exists.
- Ensure a usable orthographic main camera exists.
- Wire camera bounds to generated map bounds.
- Configure runtime weather after camera/day-night setup and before ambience audio.
- Configure seasonal surface visuals after weather setup so snow/ice coverage shares weather and temperature state.
- Configure the central runtime audio mix controller after camera/weather setup and before ambience/music sources.
- Configure runtime ambience audio after camera setup.
- Focus the initial camera view on the startup campfire after population creates it.
- Configure water/shore animation after map generation.
- Create/configure runtime building-route road capture and road visuals after map generation.
- Create/configure the Stone resource registry before nature generation.
- Configure nature props after the starter camp exists so generated props can avoid the campfire clear radius.
- Create/configure forage resource nodes after nature generation so they use current walkability.
- Configure fog of war after population, placement, and map controllers exist.
- Create/configure the F9 runtime debug panel after fog/weather are ready.
- Provide shared debug options such as instant free construction for runtime test workflows.
- Place the temporary starter Caravan Cart near the campfire with initial Logs, Stone, and randomized food after placement is configured.
- Create/configure the world chunk registry after starter placement so runtime systems can share spatial indexing, camera-near/settlement-active chunk flags, and dirty chunk markers.
- Create/configure runtime wildlife after starter placement so deer/rabbits use valid land and fish use valid water cells.
- Create/configure visual day/night cycle after camera setup.
- Create/configure runtime post-processing after weather/day-night setup.
- Create/configure runtime cinematic visuals after post-processing setup.
- Create/configure the night-light task controller after population exists.
- Create the runtime time-scale controller for F1/F2/F3 speed controls.
- Keep desktop Player updates running while the application is unfocused without treating focus as a simulation pause source.
- Create/configure the in-game Escape pause menu after persistence so it receives the established input, time-scale, save, and confirmation owners.
- Create/configure the top status HUD with population counts, the larger resident roster HUD, the family tree modal scene, and the compact event log with birth/death/adoption messages.
- Create/configure the runtime goals controller and starter goal sequence that gates early Build menu tools.
- Create/configure refugee arrivals and the modal refugee decision HUD.
- Create/configure the reusable confirmation dialog used by destructive world-selection actions.
- Create/configure the auto workforce controller before the Profession HUD so automation settings and manual overrides have one shared runtime owner.
- Guard runtime loose-resource pile factories against scene-object creation during Play Mode shutdown.

Primary files/assets:

- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.SceneFlow.cs`
- `Assets/Scripts/Runtime/Core/StrategyBootstrapRunner.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameContext.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.StarterResources.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.WorldChunks.cs`
- `Assets/Scripts/Runtime/Core/StrategyDebugLogger.cs`
- `Assets/Scripts/Runtime/Core/StrategyDebugOptions.cs`
- `Assets/Scripts/Runtime/Core/StrategyRuntimeObjectCreationGuard.cs`
- `Assets/Scripts/Runtime/Core/StrategyTimeScaleController.cs`
- `Assets/Scripts/Runtime/Audio/StrategyAudioMixController.ApplicationFocus.cs`
- `Assets/Scripts/Runtime/Core/StrategySeasonCalendar.cs`
- `Assets/Scripts/Runtime/Core/StrategyDayNightCycleController.Calendar.cs`
- `Assets/Scripts/Runtime/Core/StrategyDayNightCycleController.cs`
- `Assets/Scripts/Runtime/Core/StrategyPostProcessController.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicVisualController.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicVisualController.LightLod.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicVisualController.Part01.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicLightEmitter.cs`
- `Assets/Scripts/Runtime/Core/StrategyCinematicLightEmitter.Profile.cs`
- `Assets/Scripts/Runtime/Population/StrategyNightLightTaskController.cs`
- `Assets/Scripts/Runtime/Population/StrategyNightLightTaskController.Utilities.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherKind.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherController.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherVisualController.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherVisualController.Chunks.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherVisualController.Snow.cs`
- `Assets/Scripts/Runtime/Weather/StrategySeasonalSurfaceController.cs`
- `Assets/Scripts/Runtime/Weather/StrategySeasonalSurfaceController.Painting.cs`
- `Assets/Scripts/Runtime/Audio/StrategyAmbientAudioController.cs`
- `Assets/Scripts/Runtime/Audio/StrategyAudioMixController.cs`
- `Assets/Scripts/Runtime/Audio/StrategyMusicController.cs`
- `Assets/Scripts/Runtime/Audio/StrategyResidentFootstepAudio.cs`
- `Assets/Scripts/Runtime/Audio/StrategyResidentWorkSfxAudio.cs`
- `Assets/Scripts/Runtime/Audio/StrategyForestrySfxAudio.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.Chunks.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.Visibility.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.WeatherFog.cs`
- `Assets/Scripts/Runtime/Map/StrategyWaterAnimationController.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.Diagnostics.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.Network.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.Roadside.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.RouteConnectionRepair.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.RouteConnections.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.Routes.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.Visibility.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailRouteCellBuilder.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailSpriteFactory.cs`
- `Assets/Scripts/Runtime/Map/StrategyWorldChunkRegistry.cs`
- `Assets/Scripts/Runtime/Map/StrategyWorldChunkRegistry.Index.cs`
- `Assets/Scripts/Runtime/Map/StrategyWorldChunkRegistry.Queries.cs`
- `Assets/Scripts/Runtime/Map/StrategyRoadsideLightSource.cs`
- `Assets/Scripts/Runtime/Map/StrategyRoadsidePropController.cs`
- `Assets/Scripts/Runtime/Map/StrategyRoadsidePropPlanner.cs`
- `Assets/Scripts/Runtime/Map/StrategyStoneResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyWindController.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.cs`
- `Assets/Scripts/Runtime/Population/StrategyRefugeeArrivalController.cs`
- `Assets/Scripts/Runtime/Population/StrategyRefugeeArrivalController.Part02.cs`
- `Assets/Scripts/Runtime/Population/StrategyRefugeeArrivalController.Part03.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.cs`
- `Assets/Scripts/Runtime/UI/StrategyRefugeeDialogController.cs`
- `Assets/Scripts/Runtime/UI/StrategyConfirmationDialogController.cs`
- `Assets/Scripts/Runtime/UI/StrategyDebugPanelController.cs`
- `Assets/Scripts/Runtime/UI/StrategyDebugPanelController.Weather.cs`
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
- `Assets/Scripts/Runtime/Menu/StrategySceneCatalog.cs`

Impact hints:

- Bootstrap registers through `RuntimeInitializeOnLoadMethod`, then handles initial/runtime gameplay loads and unload cleanup without requiring scene YAML wiring. A scene-local context owns service registration, lifecycle state, and the bootstrap pause while the runner spreads deterministic nature generation across bounded frame batches.
- Audio bootstrap expects non-generated clips under `Assets/Resources/Audio`; missing ambience or music clips should degrade quietly rather than blocking scene startup.
- New scenes must declare their role through `StrategySceneCatalog` or migrate the catalog to a data-driven scene-role system.

### Strategy Debug Logging

Responsibilities:

- Create a structured runtime `debug.log` for gameplay debugging.
- Preserve prior sessions, rotate bounded logs, retain recent archives, and survive canonical-file locks from another process.
- Mirror Unity log messages, warnings, errors, and exceptions into the same file.
- Provide static event helpers for strategy systems without forcing scene references.
- Record important events and failure reasons for bootstrap, map generation, nature/Stone generation, build menu/tool flow, placement, population, refugees, forestry, wildlife, lumberjack camps, selection, and time-scale changes.

Primary files/assets:

- `Assets/Scripts/Runtime/Core/StrategyDebugLogger.cs`
- `Assets/Scripts/Runtime/Core/StrategyLogFileRotation.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `.gitignore`
- `Assembly-CSharp.csproj`

Impact hints:

- In the Unity Editor, `debug.log` is written at the project root and should remain ignored by git.
- Prefer meaningful state-change, command, failure, and completion events over per-frame logging.
- Add future categories through `StrategyDebugLogger.Info/Warn/Error` so log formatting remains consistent.

### Strategy Performance Diagnostics

Responsibilities:

- Record low-overhead frame samples in stable 10-second runtime windows.
- Separate startup and 15/30/50-resident results by time phase, weather, and requested simulation speed.
- Log frame-time percentiles, hitch counts, memory/GC, active simulation counts, and resident path/decision workload.
- Expose stable Unity Profiler samples for known hot paths, including terrain paint, without dynamic marker names.
- Allow repeat benchmark sessions to force the generated map seed with `-strategyBenchmarkSeed`.

Primary files/assets:

- `Assets/Scripts/Runtime/Core/StrategyPerformanceDiagnostics.cs`
- `Assets/Scripts/Runtime/Core/StrategyPerformanceMarkers.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.Diagnostics.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentPerformanceCounters.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part36.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part41.cs`
- `Assets/Scripts/Runtime/Map/CityMapController.Generation.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Keep per-frame sampling allocation-free; add expensive aggregation only at window completion.
- Reset or split a window when benchmark context changes so pause/dialog/weather transitions do not contaminate results.
- Add new simulation workload counters through cumulative snapshots instead of per-event logging.

### Strategy Audio

Responsibilities:

- Load non-generated ambience and footstep clips from `Assets/Resources/Audio`.
- Load in-game music clips from `Assets/Resources/Audio/Music` as a playlist.
- Load resident work one-shot clips from `Assets/Resources/Audio/WorkSfx`.
- Load forestry tree-fall and split-Logs one-shot clips from `Assets/Resources/Audio/WorkSfx`.
- Load HUD interaction one-shot clips from `Assets/Resources/Audio/HudSfx`.
- Own Unity AudioMixer routing plus runtime mix profiles for music, ambience, weather, water, settlement, work, footsteps, wildlife, fire, important events, and HUD.
- Bound spatial one-shots to an 18-voice pool with per-family concurrency, cooldowns, priority stealing, camera-focus/zoom attenuation, low-pass, and reverb.
- Play layered season-aware forest birds, cicadas, night, rain, calm wind, forest wind, river, settlement, campfire, and winter ambience.
- Follow the active runtime weather state for rain and weather wind ambience.
- Position a spatial river ambience source at the nearest generated water cell to the active camera.
- Prefer `calm`/`night`/`winter`/`storm` music filename tags, use generic tracks as fallback, avoid immediate repeats, and insert contextual silence/end fades.
- Pause current in-game music on application focus loss and resume the same clip on focus return.
- Mute the global listener while application focus is lost or the application is suspended, then restore the exact captured listener volume only after both states clear.
- Shape resident walk frames into grass/forest/dirt/road/snow profiles and submit them to the shared voice pool.
- Add spatial resident work SFX for axe, pickaxe, construction hammer, fishing, and bow actions on animation impact/release frames.
- Add pooled spatial forestry SFX for `StrategyForestryTree` fall and split-Logs completion events.
- Add non-spatial HUD SFX for accepted, rejected, modal, roster, profession, build-menu, speed-control, and quiet hover interactions.
- Route Founding Journey wind/rain/fire loops through the existing Weather/Fire buses without starting the gameplay ambience or music owners.
- Add bounded procedural settlement/fire/winter layers and event details for completion, delivery, lamp ignition, wolf howl, and burial.

Primary files/assets:

- `Assets/Scripts/Runtime/Audio/StrategyAmbientAudioController.cs`
- `Assets/Scripts/Runtime/Audio/StrategyAudioMixController.cs`
- `Assets/Scripts/Runtime/Audio/StrategyAudioMixController.ApplicationFocus.cs`
- `Assets/Scripts/Runtime/Audio/StrategyAudioVoicePool.cs`
- `Assets/Scripts/Runtime/Audio/StrategyProceduralAudioLibrary.cs`
- `Assets/Scripts/Runtime/Audio/StrategyWorldAudioDirector.cs`
- `Assets/Scripts/Runtime/Audio/StrategyMusicController.cs`
- `Assets/Scripts/Runtime/Audio/StrategyHudSfxAudio.cs`
- `Assets/Scripts/Runtime/Menu/StrategyFoundingJourneyAudio.cs`
- `Assets/Scripts/Runtime/Audio/StrategyResidentFootstepAudio.cs`
- `Assets/Scripts/Runtime/Audio/StrategyResidentWorkSfxAudio.cs`
- `Assets/Scripts/Runtime/Audio/StrategyForestrySfxAudio.cs`
- `Assets/Editor/StrategyAudioMixerBuilder.cs`
- `Assets/Resources/Audio/StrategyAudioMixer.mixer`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.Audio.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentTask.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentTaskExecution.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.TaskArrival.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentHouseholdCookingTask.cs`
- `Assets/Tests/EditMode/StrategyResidentCharacterizationTests.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.WorkSfx.cs`
- `Assets/Scripts/Runtime/Audio/StrategyResidentFootstepAudio.cs`
- `Assets/Resources/Audio/Nature/`
- `Assets/Resources/Audio/Music/`
- `Assets/Resources/Audio/Footsteps/GrassWalk/`
- `Assets/Resources/Audio/WorkSfx/`
- `Assets/Resources/Audio/HudSfx/`
- `Assembly-CSharp.csproj`

Impact hints:

- Ambience depends on generated map water cells, camera position, and strategy wind/weather values.
- Music files can be named freely, but `Music_01.mp3`, `Music_02.mp3`, etc. are the recommended convention; all direct AudioClips in `Resources/Audio/Music` are part of the playlist.
- Music focus handling must use `AudioSource.Pause`/`UnPause` so losing focus does not trigger playlist advancement.
- Application focus must not write `Time.timeScale`; desktop background execution is a Player setting, while simulation pause remains owned by `StrategyTimeScaleController` locks.
- Global focus mute uses `AudioListener.volume`, not `AudioListener.pause`, because HUD sources intentionally ignore listener pause; always restore the captured value rather than assuming full volume.
- Keep long music clips on background `Streaming` and long ambience loops on background `Compressed In Memory`; Edit Mode verification enforces these profiles so scene bootstrap does not synchronously decode them.
- Footsteps are tied to resident walk sprite frames 1 and 5; changing walk frame counts or animation pacing should retune footstep phases.
- Work SFX are tied to resident impact/release frames (`WoodcutImpactFrame`, `ConstructionImpactFrame`, `FishingHookFrame`, `FishingReelFrame`, and `BowReleaseFrame`); changing those animation timelines should retune SFX triggers.
- Forestry SFX are tree-owned rather than resident-owned: `StartFalling()` plays tree-fall audio and `CompleteBucking()` plays split-Logs audio from the tree position.
- HUD SFX are event-driven from runtime UI controllers and use a singleton non-spatial source with small cooldowns; hover reuses a quiet cue with separate local/global throttling and never replaces semantic click/open/close/confirm sounds.
- `StrategyAudioMixController` is the shared mix/routing layer; keep new audio on a named `StrategyAudioBus` and use `StrategyAudioVoicePool` for spatial one-shots instead of creating AudioSources.
- The 18-voice pool is a hard performance budget. Add new sounds through concurrency keys/priorities and only raise capacity after benchmark evidence.
- Keep imported ambience/footstep/work/HUD clips under `Resources/Audio` unless the loading path is updated at the same time.

### Strategy Weather

Responsibilities:

- Own the current runtime weather state and smooth atmospheric intensities.
- Randomly transition between Clear, Cloudy, LightRain, HeavyRain, Fog, Storm, Snow, and Blizzard with season-biased weights from the shared calendar.
- Calculate informational outdoor temperature from season, time of day, stable daily variation, and current weather.
- Drive procedural wet-ground/cold-wash, cloud-shadow, heavy-precipitation mist, rain, and snow world overlays.
- Drive gradual seasonal snow/ice surface cover and expose frozen-water gameplay state for fish/fishing systems.
- Feed dense Fog weather into fog-of-war visibility and masked weather-fog rendering.
- Feed rain/wind ambience with a single weather source of truth.
- Boost the strategy `WindZone` so nature sway reacts to rain and storms.
- Expose rain intensity to water animation for rain ripple hits.

Primary files/assets:

- `Assets/Scripts/Runtime/Weather/StrategyWeatherKind.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherController.cs`
- `Assets/Scripts/Runtime/Weather/StrategyTemperatureModel.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherVisualController.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherVisualController.Chunks.cs`
- `Assets/Scripts/Runtime/Weather/StrategyWeatherVisualController.Snow.cs`
- `Assets/Scripts/Runtime/Weather/StrategySeasonalSurfaceController.cs`
- `Assets/Scripts/Runtime/Weather/StrategySeasonalSurfaceController.Painting.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSnowSpriteFactory.cs`
- `Assets/Scripts/Runtime/Core/StrategySeasonCalendar.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assets/Scripts/Runtime/Core/StrategyWorldSorting.cs`
- `Assets/Scripts/Runtime/Map/StrategyWindController.cs`
- `Assets/Scripts/Runtime/Map/StrategyWaterAnimationController.cs`
- `Assets/Scripts/Runtime/Audio/StrategyAmbientAudioController.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Weather visual overlays must stay below placement preview and fog-of-war UI-facing layers.
- Weather visual overlays are performance-budgeted: cloud/mist textures are 1 pixel per cell, rain and snow use capped camera-area sprite pools, and visual repaint cadences should stay unscaled and low-frequency.
- Dense weather Fog rendering is owned by Fog of War so visible cells can stay clear; do not reintroduce a uniform `FogIntensity` mist overlay over the whole map.
- Rain/wind audio should continue reading `StrategyWeatherController.Active` instead of adding independent rain timers; Snow/Blizzard expose separate snow intensity so they do not trigger rain audio or rain-driven water ripples.
- Weather and seasons currently have visual/audio/wind/water/probability effects plus outdoor temperature used by winter house warmth, seasonal surface snow/ice visuals, and frozen-water fish/fishing blocking; future gameplay effects should extend this system rather than duplicating weather or season rolls in crops, illness, movement, or fire logic.

### Strategy Navigation

Responsibilities:

- Own reusable A* working memory for resident and land-agent path requests.
- Preserve resident trail weighting, diagonal corner blocking, and line-of-sight smoothing.
- Preserve cardinal land-wildlife movement, River transit, and settlement structure buffers.
- Coalesce duplicate requests and warm deferred paths under a per-frame count/time budget.
- Drain critical resident transitions before normal decisions and background wildlife work.
- Cache short-lived path and reachability outcomes in pooled bounded buffers and invalidate them on map walkability revision changes.

Primary files/assets:

- `Assets/Scripts/Runtime/Navigation/StrategyNavigationTypes.cs`
- `Assets/Scripts/Runtime/Navigation/StrategyNavigationPathfinder.cs`
- `Assets/Scripts/Runtime/Navigation/StrategyNavigationService.cs`
- `Assets/Scripts/Runtime/Navigation/StrategyNavigationService.Storage.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.Navigation.cs`
- `Assets/Scripts/Runtime/Map/CityMapController.Buildability.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part36.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyDeerAgent.Part01.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyRabbitAgent.Part06.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWolfAgent.Part01.cs`
- `Assets/Scripts/Runtime/Population/StrategyChickenAgent.cs`
- `Assets/Scripts/Runtime/Economy/StrategyTradeCaravanController.Pathing.cs`
- `Assets/Scripts/Runtime/Population/StrategyRefugeeArrivalController.Part01.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Callers may receive `Deferred`; normal agent decision loops should retry rather than treating that state as a permanent unreachable target.
- Candidate-selection loops must stop on `Deferred`; do not enqueue alternate targets or increment failure/blacklist counters until an actual `Unreachable` result exists.
- Fish remain on their water-kind path logic, and special flood-fill searches for funeral coverage, refugee staging, migration reachability, and wolf River escape remain specialized queries rather than ordinary point-to-point paths.
- Any new runtime walkability mutation must go through `CityMapController` so `WalkabilityVersion` invalidates navigation results.

### Generated City Map

Responsibilities:

- Hold basic map-cell data for the strategy MVP.
- Generate a visible 192x192 default 2D terrain map at runtime.
- Randomize the active map seed by default and derive a generation profile from it.
- Generate variable rivers, shorelines, optional water blobs, and clustered land terrain.
- Tag generated water and shore cells with `CityMapWaterKind.River` or `CityMapWaterKind.Lake` for direct future gameplay queries.
- Assign visual-only relief height for lowlands, hills, and mountain-like terrain shading.
- Expose `RiverFlowDirection` for systems that need to move or animate along the generated river current.
- Paint procedural pixel-art terrain textures for generated map cells.
- Render animated water waves, sparkles, shoreline foam, and weather-driven rain ripple hits as a transparent overlay.
- Commit stable road cells after three completed traversals of the same non-Bridge building pair.
- Keep resident footfalls from creating functional or visible roads, and keep automatic route-network convergence disabled.
- Keep route-road recording connected by rejecting route tails left behind after square-prone cells are skipped and using a bounded local repair search when a no-square obstacle detour exists.
- Prune road cells only when map walkability or cell validity invalidates them; roads do not decay from disuse.
- Spawn sparse non-blocking roadside torch props from eligible straight route-road cells, refreshing the derived prop layer when roads or adjacent buildability change.
- Maintain a runtime 16x16 world chunk registry for spatial indexing of placed buildings, active construction sites, and residents plus camera-near, settlement-active, and dirty-chunk flags.
- Expose formed roads as a 15% resident movement-speed bonus and a reduced resident pathfinding cost.
- Feed generated cell kinds and active seed into the visual nature-props layer.
- Feed generated land cells and active seed into Stone deposit generation.
- Feed generated walkable land cells and active seed into underground Iron field generation.
- Feed generated walkable land cells and active seed into underground Coal field generation.
- Feed generated walkable near-water land/shore cells and active seed into Clay field generation.
- Feed generated walkable land cells and active seed into forage resource node generation.
- Provide the campfire exclusion center used by nature generation to guarantee starter-area Stone, Coal, and Iron.
- Expose map bounds and cell buildability for future zoning/economy systems.
- Track dynamic walkability blockers for placed buildings and early agents.
- Track completed bridge walkability over River water cells without changing water/shore identity.
- Host runtime fog-of-war exploration, current visibility, day/night reveal tuning, weather Fog reveal tuning, weather-fog band rendering, and daylight-range visibility state.
- Emit `Map/Generated` diagnostics with total and subphase generation timings.

Primary files/assets:

- `Assets/Scripts/Runtime/Map/CityMapController.cs`
- `Assets/Scripts/Runtime/Map/CityMapController.Generation.cs`
- `Assets/Scripts/Runtime/Map/CityMapController.Part01.cs`
- `Assets/Scripts/Runtime/Map/CityMapController.Relief.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.Chunks.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.Visibility.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.WeatherFog.cs`
- `Assets/Scripts/Runtime/Map/StrategyWaterAnimationController.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.Diagnostics.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.Network.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.Roadside.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.RouteConnectionRepair.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.RouteConnections.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.Routes.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.Visibility.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailRouteCellBuilder.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailSpriteFactory.cs`
- `Assets/Scripts/Runtime/Map/StrategyWorldChunkRegistry.cs`
- `Assets/Scripts/Runtime/Map/StrategyWorldChunkRegistry.Index.cs`
- `Assets/Scripts/Runtime/Map/StrategyWorldChunkRegistry.Queries.cs`
- `Assets/Scripts/Runtime/Map/StrategyRoadsideLightSource.cs`
- `Assets/Scripts/Runtime/Map/StrategyRoadsidePropController.cs`
- `Assets/Scripts/Runtime/Map/StrategyRoadsidePropPlanner.cs`
- `Assets/Scripts/Runtime/Map/StrategyMapDistributionUtility.cs`
- `Assets/Scripts/Runtime/Map/StrategyTerrainTexturePainter.cs`
- `Assets/Scripts/Runtime/Map/StrategyTerrainTexturePainter.Catalog.cs`
- `Assets/Scripts/Runtime/Map/StrategyTerrainTexturePainter.Macro.cs`
- `Assets/Scripts/Runtime/Map/StrategyTerrainTexturePainter.Palette.cs`
- `Assets/Scripts/Runtime/Map/StrategyTerrainTexturePainter.Relief.cs`
- `Assets/Scripts/Runtime/Map/CityMapController.Buildability.cs`
- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.cs`
- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.Generation.cs`
- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.Distribution.cs`
- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.Part05.cs`
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
- `Assets/Scripts/Runtime/Map/StrategyForageResourceController.Respawn.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageNode.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageSpriteFactory.cs`
- `Assets/Scripts/Runtime/Map/StrategyForestryController.cs`
- `Assets/Scripts/Runtime/Map/StrategyForestryTree.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Current map is runtime-generated with a randomized active seed by default; save loading restores that seed before generation so terrain identity is deterministic.
- Current terrain painter covers Grass, Meadow, Forest, Dirt, Shore, and Water with a hidden default grid, broad seeded macro palette variation, neighbor transition overlays, visual relief shading, and one cached per-tile paint/catalog context outside the inner pixel loop. Map generation classifies kind/water first and reuses that water mask when calculating relief.
- `CityMapCell.ReliefHeight` is a visual-only value; do not use it as a walkability, buildability, or resource-reachability rule without an explicit gameplay design pass.
- Water source identity is stored on `CityMapCell.WaterKind`; future systems should query that instead of guessing river/lake from geometry.
- River current direction is stored on `CityMapController.RiverFlowDirection`; river-specific ambience/gameplay should follow that instead of creating independent direction timers.
- Terrain kind generation now uses a seed-derived profile plus multi-octave noise; texture painting consumes the active seed.
- Nature prop placement consumes the active seed and generated cell kinds through a shared shuffled full-map pass, so capped prop budgets do not fill only the first scanned area on large maps; starter Coal/Iron guarantees and Iron/Coal minimum fallback are protected from that decorative cap.
- Stone deposit placement consumes the active seed, generated cell kinds, and macro cluster score.
- Iron field placement consumes the active seed, generated walkable land cells, and macro cluster score; Iron deposits are underground fields that do not block walkability but block normal buildability.
- Coal field placement consumes the active seed, generated walkable land cells, and macro cluster score; Coal deposits are underground fields that do not block walkability but block normal buildability.
- Clay field placement consumes the active seed, generated walkable near-water land/shore cells, and macro cluster score; Clay deposits do not block walkability but block normal buildability.
- Forage placement consumes the active seed, generated cell kinds, current walkability, the shared shuffled full-map pass, macro cluster score, and the current season gameplay profile; forage nodes are non-blocking, support reservation, disappear after gathering, and use a timed respawn queue that places replacements near mature standing trees inside active Forager Camp work radii. The controller leaves capacity headroom after initial generation, periodically supports under-supplied Forager Camps, applies local density/per-camp soft caps to avoid over-clustering, and uses season multipliers so Winter slows new forage without clearing existing nodes.
- Generated standalone tree props register as mature forestry trees and block their cells.
- Forest groups and bushes remain non-interactive but block their cells.
- Generated Stone deposits register as Boulder, Rock Cluster, or Cliff resource deposits and block their cells.
- Generated Iron deposits register as multi-cell Iron-stained Ground or Iron Vein resource fields, stay walkable but not normally buildable, avoid adjacent Coal fields, and can be reserved/mined by `StrategyMine` worksites built over them.
- Generated Coal deposits register as multi-cell Coal Dust Ground or Coal Seam resource fields, stay walkable but not normally buildable, avoid adjacent Iron fields, and can be reserved/mined by `StrategyCoalPit` worksites built over them.
- Generated Clay deposits register as multi-cell Clay Patch or Clay Bank resource fields, stay walkable but not normally buildable, require nearby water across the full footprint, avoid adjacent Iron/Coal/Clay fields, and can be reserved/mined by `StrategyClayPit` worksites built over them.
- Future placement/economy work should reuse `CityMapCell`/bounds rather than duplicating map dimensions.
- Future movement/pathfinding should use `IsCellWalkable` rather than terrain kind alone.
- Rendering is currently a generated point-filtered texture on a `SpriteRenderer`, not a Tilemap.
- Water and shore animation is a separate transparent `SpriteRenderer` overlay above the static map and below world props; it reads active weather intensity for rain ripple hits and repaints only cached water/shore cells after setup.
- Trail visuals use one `SpriteRenderer` per visible route-road cell under a `Trail Visuals` root, sorted above terrain/water overlays and below world props, with cardinal N/E/S/W right-angle masks and narrow line/brush sprites; visual road formation comes from direct-first completed building-to-building traversals rather than per-step footfall squares, raw A* detours, or background network convergence. Route recording starts from the endpoint not already connected, stops at the first cardinal contact with an existing route road, rejects full 2x2 route-road blocks, must not record disconnected tail cells after a skipped square candidate, and can run a bounded local cardinal repair search for a connected no-square detour. Roadside torch props are generated under a separate `Roadside Props` root and should remain visual-only, non-blocking derivatives of route-road cells.
- Road cells are runtime-only and should be refreshed when map walkability or cell validity changes so blocked cells do not keep visible or functional roads.
- Resident pathfinding should continue to use the shared road-aware pathfinder, reading functional road cells as a cost preference rather than required connectivity.
- `StrategyWorldChunkRegistry` is the shared chunk foundation for future Minecraft-style incremental work; first migrate expensive scans behind its safe query APIs, then switch fog/weather/props/lights/resources to dirty or active chunks only.
- Fog-of-war, cloud-shadow, and heavy-rain-mist texture repaint/upload paths are already chunk-aware for active camera/settlement/dirty chunks; gameplay fog visibility arrays still refresh globally for correctness.

### Forestry MVP

Responsibilities:

- Track runtime tree entities that can be chopped or grown.
- Register generated standalone trees as mature forestry trees.
- Mark tree cells as not walkable while the tree exists.
- Release chopped tree cells back to walkable.
- Plant saplings that grow through 3 visual stages.
- Provide mature-tree, fallen-trunk, split-Logs, and planting-cell targets to lumberjack camps, filtering harvest targets by camp work radius and full-yield storage capacity before reservation.
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
- Planting candidates require a nearby walkable work cell, and lumberjacks only start planting when their camp can accept at least the smallest future Logs yield.
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
- Refresh visibility and texture painting on an unscaled real-time cadence so time acceleration does not multiply fog repaint work.
- Provide exploration checks to placement and world selection.
- Provide read-only explored-cell checks to Scout frontier selection; moving Scouts remain ordinary resident reveal sources.
- Provide persistent-only explored checks to point-of-interest targeting independently of the F9 player-fog bypass.
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
- `Assets/Scripts/Runtime/Build/StrategyScoutTargetSelector.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Scouting.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Explored fog cells are persisted; current visibility is rebuilt from live reveal sources after load.
- Placement currently requires explored cells, not current visibility.
- Selection ignores clicks in unexplored cells, while the overlay visually hides world sprites.
- Current visibility is reduced by Dusk/Night/Dawn and dense Fog weather, but persistent explored state and daylight-range hidden checks stay separate.
- Weather Fog replaces normal explored gray-zone rendering with light/medium/dense fog bands around current visible cells.
- Wildlife spawn/reproduction/migration hidden checks should use daylight-range visibility so temporary night blindness does not count as a safe spawn opening.
- The F9 debug panel can hide the fog overlay and make map cells count as explored for player placement/selection until toggled back on; Fog of War also skips refresh ticks while player fog is disabled. The F9 panel also owns tester-facing instant construction toggles through shared debug options and can request a normal refugee arrival event through `StrategyRefugeeArrivalController.DebugStartArrival()`.
- Scout movement now extends explored state through existing resident reveal sources; future enemies, stealth, minimap, or additional exploration rules should extend this subsystem instead of duplicating visibility arrays.

### Scout Lodge Exploration MVP

Responsibilities:

- Place one manually staffed exploration worksite on an exact `2x4` footprint.
- Reserve distinct frontier targets across multiple Scout Lodges.
- Select only in-bounds unexplored, walkable cells with an in-bounds cardinal explored neighbor.
- Prefer the nearest frontier deterministically, using nearby unknown coverage and stable coordinates as tie-breakers.
- Route one assigned Scout with critical navigation priority, survey for 2.5-3.5 seconds, and repeat continuously through day and night without generic idle wandering.
- Prioritize the nearest persistently discovered uninvestigated point of interest, reserve it across Scouts, travel to it, and investigate for 1.5-2.5 seconds before resuming frontier work.
- Expose the single Scout slot through both the settlement-wide Profession HUD and the selected Scout Lodge HUD.
- Temporarily reject unreachable targets, release deferred paths immediately, and clear reservations on unassignment, death, funeral interruption, demolition, or resident-role change.
- Expand map exploration only through the Scout resident's existing Fog of War reveal source.
- Keep assigned Scouts out of home/camp sleep and settlement lamp-lighting duty, wake them when assigned at night, retain personal torch visuals, and treat their household ration as a field meal while they remain away.

Primary files/assets:

- `Assets/Scripts/Runtime/Build/StrategyScoutLodge.cs`
- `Assets/Scripts/Runtime/Build/StrategyScoutTargetSelector.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Scouting.cs`
- `Assets/Scripts/Runtime/Map/StrategyPointOfInterestController.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part36.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part41.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part42.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part49.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.NightLights.cs`
- `Assets/Scripts/Runtime/Population/StrategyHouseholdFoodState.NightMeal.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.ScoutWorkers.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentTask.cs`
- `Assets/Tests/EditMode/StrategyScoutTargetSelectorTests.cs`

Impact hints:

- Scout assignments and active frontier/point reservations are transient like other worksite assignments; the placed Lodge, explored fog cells, point positions, and investigated point state persist through their owning snapshots.
- Scout is intentionally manual-only in this MVP and is not managed by Auto Workforce priorities.
- A `No reachable frontier` state may mean remaining unknown land is isolated by water or blockers, not that every map cell is explored.

### Point Of Interest Exploration MVP

Responsibilities:

- Place 10 schematic seed-deterministic landmarks on separated camp-connected walkable/buildable cells outside the starter area and map edge.
- Keep landmarks walkable while blocking building placement and forage-node overlap.
- Expose only persistently discovered, uninvestigated landmarks to Scout reservation; reject conflicts across multiple Scouts and cool down unreachable targets.
- Mark investigation completion atomically and retain the marker in an investigated check state.
- Queue a one-action `OK` debug encounter, hold one simulation pause lock across queued notices, block all input, swallow cancel, and defer behind existing modal pause owners.
- Capture and restore stable point IDs, cells, and investigated state through save version 5.

Primary files/assets:

- `Assets/Scripts/Runtime/Map/StrategyPointOfInterest.cs`
- `Assets/Scripts/Runtime/Map/StrategyPointOfInterestPlacement.cs`
- `Assets/Scripts/Runtime/Map/StrategyPointOfInterestSpriteFactory.cs`
- `Assets/Scripts/Runtime/Map/StrategyPointOfInterestController.cs`
- `Assets/Scripts/Runtime/Map/StrategyPointOfInterestController.Notices.cs`
- `Assets/Scripts/Runtime/Map/StrategyPointOfInterestController.Persistence.cs`
- `Assets/Scripts/Runtime/UI/StrategyPointOfInterestDialogController.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Scouting.cs`
- `Assets/Scripts/Runtime/Persistence/StrategySaveData.cs`
- `Assets/Scripts/Runtime/Persistence/StrategySaveSystem.Capture.cs`
- `Assets/Scripts/Runtime/Persistence/StrategySaveSystem.Apply.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assets/Tests/EditMode/StrategyPointOfInterestPlacementTests.cs`
- `Assets/Tests/EditMode/StrategyPointOfInterestSaveValidationTests.cs`
- `Assets/Tests/EditMode/StrategyInputRouterTests.cs`
- `Assets/Tests/EditMode/StrategySaveSystemTests.cs`

Impact hints:

- Normal fog overlay ownership hides undiscovered marker sprites; F9 may expose them visually for debugging, but persistent-only targeting still prevents Scout assignment to undiscovered points.
- Reservations and unreachable cooldowns are transient; stable geometry and investigated state persist.
- Investigated markers intentionally remain in the world and continue to block building placement in this schematic MVP.
- Forestry planting, forage respawn, cemetery grave selection, save validation, and live restore occupancy checks all exclude landmark cells so other systems cannot make them permanently unreachable.
- A rejected pending save explicitly falls back to deterministic default POI generation because bootstrap suppresses fresh generation while a load candidate exists.

### Nature Props

Responsibilities:

- Place visual trees, forest groups, bushes, and Stone deposits over generated terrain.
- Place visual underground Iron/Coal fields and near-water Clay fields over generated terrain without blocking movement.
- Generate and cache runtime 2.5D pixel-art nature sprites.
- Use `CityMapController.ActiveSeed` plus cell coordinates for deterministic prop layout per generated map.
- Guarantee a small starter Stone field within stonecutter work distance around the startup campfire.
- Guarantee small starter Coal and Iron fields in a nearby ring before the shared decorative prop budget can fill.
- Guarantee global Stone, Clay, Iron, and Coal minimum deposit counts before decorative placement while keeping every generated child inside the shared 3600-prop limit.
- Make `Forest` cells read as dense forest while adding sparse standalone trees/bushes to other land terrain.
- Attach wind-sway animation to trees, forest groups, and bushes using the runtime strategy wind source.
- Add procedural leaf frame overlays to trees, forest groups, and bushes.
- Skip generated nature props inside the startup campfire's 3-cell clear radius.
- Skip generated Stone deposits inside the same startup campfire clear radius.
- Skip generated Iron fields inside the same startup campfire clear radius.
- Skip generated Coal fields inside the same startup campfire clear radius.
- Skip generated Clay fields inside the same startup campfire clear radius.
- Place starter Stone outside the clear radius before vegetation so nearby trees/bushes do not consume all accessible mining cells.
- Place starter Coal before starter Iron so Iron adjacency rules cannot starve Coal near the settlement.

Primary files/assets:

- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.cs`
- `Assets/Scripts/Runtime/Map/StrategyNaturePropController.Part05.cs`
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
- Starter Coal/Iron placement uses 2x2 mineable fields in a 10-24 cell ring around the campfire and logs nearby count/distance diagnostics.
- Bootstrap creates/configures the wind controller, creates population so the camp cell is known, then configures nature after `CityMapController.GenerateMap()`.
- Unity `WindZone` does not animate 2D sprites directly; `StrategyWindSway` adapts its values to sprite rotation/offset/scale.
- Leaf frame overlays complement wind sway and should stay visual-only unless future forestry gameplay needs extra real prop state.
- Future clearing or wood resources should extend the Forestry MVP registry instead of duplicating generated decoration data.

### Wildlife MVP

Responsibilities:

- Grow settlement cats and mice from completed-building, occupied-house, and food-building counts; mice hide and scurry around food structures while cats patrol, reserve, pursue, and catch nearby mice.

- Spawn compact ambient deer herds only on currently hidden suitable land cells near completed buildings or active construction sites.
- Spawn compact ambient rabbit groups only on currently hidden suitable land cells near completed buildings or active construction sites.
- Spawn compact lake fish shoals only in currently hidden lake cells near settlement anchors, with strict per-shoal and per-lake population caps.
- Spawn one-way pass-through river fish from currently hidden near-settlement river-route cells through a single timer.
- Spawn decorative birds only on currently hidden species-appropriate land/water cells near settlement anchors, without reproduction or resources.
- Spawn compact wolf packs only on currently hidden safe land cells in a wider near-settlement ring, preferring alternating river sides when a generated river route exists.
- Use completed buildings and active construction sites as wildlife spawn anchors, with the startup camp as a fallback only when no building/construction anchor exists.
- Periodically support under-supplied Hunter Camps by spawning huntable rabbits in a controlled 7-11 cell ring around the camp.
- Allow upgraded Hunter Camps with Deer Hunting Kit to receive rare deer support spawns in the same controlled ring.
- Provide adult-rabbit reservation/count lookup for hunter camps.
- Provide catchable-fish reservation/count lookup for fisher huts, including optional requester reachability filtering before a fish is reserved.
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
- Split migration processing by species across successive frames so migration spikes stay bounded under time acceleration.
- Keep land wildlife migration away from dense settlement pressure and choose/advance land targets only through connected walkable routes so herds/packs do not jump through water or blockers; migration cadence should stay unscaled and bounded so time acceleration does not multiply route checks.
- Temporarily cool down failed migration target cells so groups do not repeatedly choose cells that already aborted.
- React to nearby residents and noisy work by switching to alert/flee states.
- Let adult does reproduce when an adult buck is nearby in the same herd.
- Spawn fawns that grow into adults after scaled simulation time.
- Keep deer reproduction under the hard 24-deer runtime population cap and the 3-deer per-herd cap.
- Let adult female rabbits reproduce when an adult male is nearby in the same group.
- Spawn kits that grow into adults after scaled simulation time.
- Keep rabbit reproduction under the hard 30-rabbit runtime population cap and the 3-rabbit per-group cap.
- Let hunter camps reserve adult rabbits by default, unlock adult deer hunting through the Deer Hunting Kit production upgrade, stop target normal behavior during the shot sequence, yield `Game` after butchering on hit, and release/flee targets on missed arrows.
- Let adult lake fish reproduce when another adult of the same species is nearby in the same shoal.
- Spawn fry that grow into adults after scaled simulation time.
- Keep fish reproduction under the hard 36-fish runtime population cap, the 3-fish per-shoal cap, and the stricter per-lake region cap.
- Keep river fish non-reproductive and controlled by the active river-fish cap.
- Let fisher huts reserve adult fish, choose valid land/shore stand cells with adjacent water, abandon casts when fish leave cast range before hooking, and yield local `Fish` after reeling.
- Let wolves hunt rabbit/deer surplus with normal stalking plus a short fast pounce phase, then direct final-position chase fallback after reaching the target cell, and threaten adult residents only when the target is outside the settlement safety pressure.
- Let wolf roam recover from stale pack centers by falling back to the wolf's current reachable area and retargeting the pack center when settlement pressure makes the old center unusable.

Primary files/assets:

- `Assets/Scripts/Runtime/Wildlife/StrategySettlementFaunaController.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategySettlementFaunaController.Population.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategySettlementFaunaTypes.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyCatAgent.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyMouseAgent.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategySettlementFaunaSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyFishingAccessUtility.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.Fishing.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.MigrationDiagnostics.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.Part09.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.Part10.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.Part11.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.Part12.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.Part13.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.Part14.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeRiverCrossing.cs`
- `Assets/Scripts/Runtime/Wildlife/IStrategyHuntTarget.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyDeerAgent.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyDeerAgent.HuntTarget.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyDeerAgent.Part03.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyDeerAgent.Part04.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyDeerSpriteFactory.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyRabbitAgent.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyRabbitAgent.HuntTarget.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyRabbitAgent.Part03.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyRabbitAgent.Part04.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyRabbitAgent.Part05.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyRabbitSpriteFactory.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWolfAgent.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWolfAgent.Part04.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWolfAgent.Part05.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWolfAgent.Part07.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWolfSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyHunterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyHunterCamp.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyFisherHut.cs`
- `Assets/Scripts/Runtime/Build/StrategyFisherHut.Visuals.cs`
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
- Deer and birds do not reveal fog or block walkability; adult deer can yield `Game` only after the Hunter Camp upgrade, rabbits can yield `Game` through the base hunter-camp work loop, fish can yield `Fish` through the fisher-hut work loop, and wolves are predators rather than player-harvestable resources.
- Initial rabbit spawn, deer herds, fish shoals, birds, and wolf packs depend on hidden near-settlement candidate cells instead of map-wide placement; if no hidden candidate exists for a species, that species should skip spawning rather than appearing far from buildings or inside visible fog.
- Wildlife hidden checks use fog daylight-range visibility, not reduced nighttime current visibility, so animals do not spawn closer to the settlement just because night lowered player sight radius.
- Wildlife rendering uses fog current visibility: animal bodies, readability overlays, shadows, ripples, and wolf click colliders are hidden outside cells visible right now, even if those cells are already explored.
- Hunter Camp support spawns must remain hidden/daylight-valid, outside the near-building inner ring, inside `StrategyHunterCamp.WorkRadius`, and below local density plus global population caps.
- Deer pathing depends on the wildlife land-travel predicate plus a separate land-target predicate, which wraps `CityMapController.IsCellWalkable`, River transit allowance, and the 4-cell structure buffer.
- Rabbit pathing uses the same local wildlife land-travel and land-target approach and should stay cheap until a shared pathfinding service exists; repeated same-threat alert/flee reactions are throttled so rabbits do not rebuild flee paths every threat check.
- Land wildlife river crossing is intentionally scoped to wildlife path helpers through `StrategyWildlifeRiverCrossing`; River cells are transit-only for deer/rabbit/wolf paths and should not become final wildlife targets. Do not change global `CityMapController` walkability to make River water walkable for residents, buildings, or construction.
- Fish pathing uses `CityMapCellKind.Water` plus `CityMapWaterKind` instead of `IsCellWalkable`, because water is intentionally not walkable for land agents and lake/river fish now have separate movement rules.
- Migration state is owned by `StrategyWildlifeController`; land groups/packs retain the selected cardinal route, advance along it by species step size, and rebuild only after map walkability revision or route mismatch. Agents only expose small retarget methods for their current home/roam center, and migration targets must stay in currently hidden near-settlement candidate cells connected to the current land-wildlife travel region.
- Failed wildlife migration targets enter a short nearby-cell cooldown before the area can be selected again; keep this guard before committing targets so repeated abort loops do not spam logs or pathfinding.
- Reproduction is owned by `StrategyWildlifeController`; deer/rabbit/fish birth cells must be currently hidden and near settlement anchors, while agents own species or sex, life stage, growth, movement, and animation state. Birds are decorative and do not reproduce yet; wolves do not reproduce yet and use pack spawn only.
- Wolf settlement avoidance is pressure-based and reads camp position, placed buildings, active construction sites, and nearby residents; land wildlife pathing also uses the cached structure buffer, so keep it cheaper than per-frame global scans.
- Wolf prey lookup is population-control logic, not continuous hunting: rabbit hunting only starts above the rabbit control threshold after subtracting predator and hunter reservations, and deer hunting only starts above the deer control threshold after subtracting predator reservations.
- Wolves no longer use ordinary resident target acquisition when no surplus prey is available.
- Wolf pack placement uses the generated river route to prefer alternating river sides, but falls back to any valid safe side and logs `WolfPackRiverSideFallback` if one side has no candidate.
- Wolf movement diagnostics are owned by `StrategyWolfAgent` and log state changes, target acquisition/release, path readiness/failures, roam failures, and movement stalls under the `Wildlife` log category.
- Wildlife threat checks, decisions, migration, movement, and growth use scaled simulation time and stop under modal pause locks; only diagnostic/log throttles and visual presentation may use real time.
- `FishLakeBirthBlocked` debug logging is throttled per lake region; keep cap checks cheap and avoid per-fish spam when a lake is full.
- Fisher Hut fish reservation can pass a requester reachability filter into wildlife selection; keep this pre-reservation filter so fishers do not reserve fish whose shore stand cell is unreachable.
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
- `Space` key recentering on the startup campfire cell while preserving current zoom.
- Mouse-wheel and keyboard zoom.
- Maximum zoom-out is 54 orthographic units for the current 192x192 default map.
- WASD/arrow/edge/drag panning.
- Camera clamping to map bounds.

Primary files/assets:

- `Assets/Scripts/Runtime/Camera/StrategyCameraController.cs`
- `Assets/Scripts/Runtime/Input/StrategyInputRouter.cs`

Impact hints:

- Controls read Camera/Global actions through `StrategyInputRouter`; modal contexts can block camera motion without disabling the project action asset.
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
- `Assets/Scripts/Runtime/Input/StrategyInputRouter.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- F1/F2/F3 read Global actions through `StrategyInputRouter`; keep action ownership centralized when adding shortcuts.
- The Build HUD also calls this controller from x1/x2/x3 buttons under the top-left construction resource panel.
- Time scale affects gameplay timers using scaled `Time.deltaTime`; UI, visual overlays, service caches, diagnostics throttles, and expensive maintenance loops should use unscaled time/realtime when their cadence should remain stable at x2/x3.
- Future pause, speed HUD, or settings should extend this controller instead of adding separate `Time.timeScale` writes.
- Modal systems should use pause locks instead of writing `Time.timeScale = 0` directly.
- Application focus loss must not push or pop a pause lock; background simulation and explicit/modal pause are independent states.

### In-Game Pause Menu

Responsibilities:

- Open the gameplay menu from Global Cancel only when no higher-priority modal context owns the action.
- Dim the live map and present a Labyrinth-inspired dark left-side panel with gold accents using the existing runtime UI theme and feedback.
- Block every gameplay input channel and pause simulation through scoped owners while preserving the requested x1/x2/x3 speed.
- Provide Resume, manual Save Game status, persistent master/music/effects/fullscreen settings, and confirmed Main Menu/Quit actions.
- Return from Settings before closing on Escape and release input/time ownership on close, disable, or scene transition.

Primary files/assets:

- `Assets/Scripts/Runtime/Menu/StrategyPauseMenuController.cs`
- `Assets/Scripts/Runtime/Menu/StrategyPauseMenuController.View.cs`
- `Assets/Scripts/Runtime/Menu/StrategyGameSettings.cs`
- `Assets/Scripts/Runtime/Input/StrategyInputRouter.cs`
- `Assets/Scripts/Runtime/Core/StrategyTimeScaleController.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assets/Scripts/Runtime/Persistence/StrategySaveSystem.cs`
- `Assets/Scripts/Runtime/UI/StrategyConfirmationDialogController.cs`
- `Assets/Tests/EditMode/StrategyPauseMenuControllerTests.cs`
- `Assets/Editor/StrategyVerificationRunner.cs`

Impact hints:

- Keep pause ownership in `StrategyTimeScaleController`; never write `Time.timeScale` directly from the menu.
- Keep Escape arbitration on the input router's context and consumed-frame contract so closing another HUD cannot reopen this menu in the same frame.
- The pause canvas sorts below the shared confirmation canvas so Main Menu/Quit prompts remain the active top modal.
- Settings share `StrategyGameSettings` with the intro menu; changes are persistent and must stay synchronized across both surfaces.
- Returning to Main Menu deliberately warns about unsaved progress; Save Game remains an explicit separate action.

### Shared Runtime UI Feedback

Responsibilities:

- Attach consistent unscaled pointer, keyboard/controller focus, press tint/motion, and quiet hover audio to code-built player-facing buttons.
- Animate code-built panels with interruptible unscaled fade/slide/scale while preserving input/raycast safety and persistent reduced motion.

Primary files/assets:

- `Assets/Scripts/Runtime/UI/StrategyUiButtonFeedback.cs`
- `Assets/Scripts/Runtime/UI/StrategyUiPanelTransition.cs`
- `Assets/Scripts/Runtime/Audio/StrategyHudSfxAudio.cs`

Impact hints:

- Attach button feedback after final layout; use `SoundOnly` when another owner already animates the button transform, or wrap the competing animation on a separate transform.
- Keep semantic click/open/close/confirm SFX in the owning action. The shared layer owns hover and only adds generic click audio when an action had none.
- Closing modal panels must disable their controls immediately but keep raycast and modal-input shielding until the visual fade completes.

### Build Menu HUD

Responsibilities:

- Runtime-created Build menu inspired by `Gruzovichky` bottom Build dock.
- Bottom Build button.
- Category cards, optional subcategory dock, and item tray.
- Build item cards with Logs/Stone/Planks construction costs, affordability state, and active state.
- F9 instant construction debug mode makes build tools affordable and shows item cost badges as `Free`.
- Temporary goal-driven tool locks that disable locked categories/items, block mouse and hotkey selection, and show `Locked` item badges.
- Top-left construction resource panel with x1/x2/x3 speed buttons directly beneath it.
- Current catalog entries: `Housing` / `House`, `Extraction` / `Camps` (`Lumberjack Camp`, `Stonecutter Camp`), `Deposits` (`Mine`, `Coal Pit`, `Clay Pit`), and `Food` (`Hunter Camp`, `Fisher Hut`, `Forager Camp`, `Chicken Coop`), `Production` / `Sawmill`, `Kiln`, and `Forge`, `Storage` / `Storage Yard` and `Granary`, `Trade` / `Trading Post`, and `Infrastructure` / `Scout Lodge` and `Bridge`.
- Hotkeys for open/close, category/subcategory/item selection, and layered cancel.
- EventSystem/Input System UI setup when the scene has no UI event module.
- Add tools/buildings gradually only by explicit user request.
- Single-item categories behave as direct build-tool buttons.

Primary files/assets:

- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Driver.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildTool.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Driver.Animation.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Driver.Debug.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Driver.Hud.cs`
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
- `Assets/Scripts/Runtime/Build/StrategyForge.cs`
- `Assets/Scripts/Runtime/Build/StrategyForge.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyForge.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyHunterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyFisherHut.cs`
- `Assets/Scripts/Runtime/Build/StrategyForagerCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyForagerCamp.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyForagerCamp.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyScoutLodge.cs`
- `Assets/Scripts/Runtime/Build/StrategyMine.cs`
- `Assets/Scripts/Runtime/Build/StrategyCoalPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.Part08.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.Part09.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.Part10.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.Part11.cs`
- `Assets/Scripts/Runtime/Build/StrategyTradingPost.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Build/StrategyGranary.cs`
- `Assets/Scripts/Runtime/Economy/StrategyConstructionResourceCost.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assets/Scripts/Runtime/Core/StrategyDebugOptions.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- The public `StrategyBuildMenuController` component is a thin wrapper; `StrategyBuildMenuControllerDriver` owns selected active build tool data and reads `StrategyStorageYard.GetTotalConstructionResources()` for affordability, including Storage Yard stock, production-local construction stock, loose piles, and the starter cart unless F9 instant construction debug mode is enabled.
- Placement reads `StrategyBuildMenuController.ActiveTool` / active tool info.
- Starter goals call `StrategyBuildMenuController.SetAllowedTools()` and `ClearAllowedTools()`; keep lock checks shared by mouse clicks, hotkeys, active tool info, subcategory visibility, and affordability/selection visuals.
- Current catalog has user-requested buildings only: `House`, `Lumberjack Camp`, `Stonecutter Camp`, `Sawmill`, `Kiln`, `Forge`, `Hunter Camp`, `Fisher Hut`, `Forager Camp`, `Mine`, `Coal Pit`, `Clay Pit`, `Storage Yard`, `Granary`, `Trading Post`, `Scout Lodge`, and `Bridge`; do not add more without a user request.
- Current `Housing` category directly activates `House` because it has one item.
- Current `Extraction` category opens a subcategory dock before item cards: `Camps` for `Lumberjack Camp`/`Stonecutter Camp`, `Deposits` for Mine/Coal/Clay extraction, and `Food` for Hunter/Fisher/Forager/Chicken Coop food buildings.
- Current `Production` category opens a tray with processing buildings: `Sawmill`, `Kiln`, and `Forge`.
- Current `Storage` category opens a tray with `Storage Yard` and `Granary`.
- Current `Trade` category directly activates `Trading Post` because it has one item.
- Successful placement asks the menu to close all open layers and records the placement frame.
- If a full HUD/menu shell appears later, decide whether this controller remains standalone or becomes part of the HUD shell.

### Top Status HUD

Responsibilities:

- Runtime-created top status canvas.
- Show total settlement population, adult count, child count, display day, 24-hour clock time, outdoor temperature, season day, time-of-day phase, winter food/fuel readiness, and day progress.
- Treat the compact population panel as a click target that toggles the larger resident roster HUD.
- Show a larger residents roster HUD with settlement stats plus filterable rows for name, age, home/camp state, role, current status, and food status.
- Expose a `Family Trees` button from the residents roster.
- Show a fullscreen modal Family Trees HUD that pauses simulation, has permanent horizontal/vertical scrollbars, groups recorded members into connected same-surname family cards, lays those cards out as affinity-ordered left-to-right columns with compact generation rows, and draws parent-child portrait-card trees plus cross-family relationship lines with distinct deceased cards, gender symbols, and hover relationship labels.
- Animate roster and Family Trees opening/closing through the shared interruptible panel transition.
- Share resident role/status/home/food label formatting through `StrategyResidentHudText`.
- Show compact birth, death, adoption, dawn, nightfall, season-start, and late-Autumn winter-warning messages through a separate event-log canvas.
- Refresh counts from `StrategyPopulationController` without owning population state.

Primary files/assets:

- `Assets/Scripts/Runtime/UI/StrategyTopStatusHudController.cs`
- `Assets/Scripts/Runtime/Weather/StrategyTemperatureModel.cs`
- `Assets/Scripts/Runtime/Core/StrategySeasonCalendar.cs`
- `Assets/Scripts/Runtime/Core/StrategySeasonReadiness.cs`
- `Assets/Scripts/Runtime/Population/StrategyHouseWarmthState.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.SeasonReadiness.cs`
- `Assets/Scripts/Runtime/UI/StrategyPopulationRosterHudController.cs`
- `Assets/Scripts/Runtime/UI/StrategyPopulationRosterHudController.Part01.cs`
- `Assets/Scripts/Runtime/UI/StrategyPopulationRosterHudController.Input.cs`
- `Assets/Scripts/Runtime/UI/StrategyPopulationRosterHudController.Part02.cs`
- `Assets/Scripts/Runtime/UI/StrategyPopulationRosterHudController.Transition.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.Part01.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.Part02.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.Part03.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.Part04.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.Part05.cs`
- `Assets/Scripts/Runtime/UI/StrategyFamilyTreeHudController.Transition.cs`
- `Assets/Scripts/Runtime/UI/StrategyPopulationRosterRowView.cs`
- `Assets/Scripts/Runtime/UI/StrategyResidentHudText.cs`
- `Assets/Scripts/Runtime/UI/StrategyEventLogHudController.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentFamilyRecord.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Population counts exclude pending refugee families until they are accepted into the settlement.
- The calendar/time/season widget reads `StrategyDayNightCycleController.CurrentSnapshot`; its temperature readout comes from `StrategyTemperatureModel`, and its winter readiness line aggregates food rations plus household fuel Logs from Granaries, Storage Yards, starter cart, and house-local stores against accepted residents/occupied houses. Keep it separate from the clickable population panel so the roster entry point remains obvious.
- Family Trees reads recorded family data, including deceased residents preserved by `StrategyResidentFamilyRecord`, and renders deceased relatives as muted monochrome cards with a skull marker.
- Family Trees relationship labels, cross-family lines, and column affinity currently derive from recorded parent/child links plus co-parent inference through shared children; explicit marriage/birth-family links should extend this owner instead of overloading family-name grouping.
- Keep top HUD click targets coordinated with Build/Profession HUD positioning and raycasts.

### Goals HUD

Responsibilities:

- Provide a runtime goals/checklist infrastructure used by the starter onboarding build sequence.
- Track active goal definitions, completed goal kinds, and the derived HUD view state without owning tutorial scenario logic.
- Keep the goals HUD hidden when there are no active goals, then show the left-side starter build checklist when bootstrap starts the sequence.
- Render a compact non-blocking Screen Space Overlay checklist with completion marks and a short completion pulse.
- Run the initial build sequence: 3 Houses, then Forager Camp, then Lumberjack Camp plus Stonecutter Camp, then unlock the full Build menu.

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
- Use the shared modal transition and retain raycast shielding through the closing fade.

Primary files/assets:

- `Assets/Scripts/Runtime/UI/StrategyRefugeeDialogController.cs`
- `Assets/Scripts/Runtime/Population/StrategyRefugeeArrivalController.cs`
- `Assets/Scripts/Runtime/Population/StrategyRefugeeArrivalController.Routing.cs`
- `Assets/Scripts/Runtime/Population/StrategyRefugeeArrivalController.Part02.cs`
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
- Show the `Auto Assign` toggle and compact priority steppers for Construction, Food, Logistics, Wood, Stone, Planks, Iron, Coal, Clay, Pottery, and Tools.
- Aggregate assignment capacity/counts across all current lumberjack camps, stonecutter camps, sawmills, kilns, hunter camps, fisher huts, forager camps, Scout Lodges, mines, coal pits, clay pits, and storage yards.
- Treat Haulers and Builders as unlimited-capacity settlement-level population roles independent from any specific Storage Yard; other worksite roles keep their own slot caps.
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
- `Assets/Scripts/Runtime/Build/StrategyForagerCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyForagerCamp.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyForagerCamp.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyScoutLodge.cs`
- `Assets/Scripts/Runtime/Build/StrategyMine.cs`
- `Assets/Scripts/Runtime/Build/StrategyCoalPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- This HUD is the settlement-wide worker assignment/removal surface; the selected Scout Lodge HUD intentionally duplicates its single-slot Assign/Remove control while other selected-worksite HUDs remain informational.
- Assignment still uses each worksite's existing `TryAssignNextAvailable...` / `Unassign...At` API, so role state, reservations, and work loops remain owned by the worksite/resident systems.
- Hauler and builder `+` buttons should stay enabled as long as a Storage Yard exists and at least one free adult resident can work.
- Auto workforce controls are UI-facing only; actual assignment decisions belong to `StrategyAutoWorkforceController`.
- Dynamic rows are derived from currently existing worksite components, not from the build catalog.
- New professions should be added here when a new worksite role becomes assignable by the player.

### Auto Workforce

Responsibilities:

- Runtime-created settlement workforce automation.
- Keep player priority settings for Construction, Food, Logistics, Wood, Stone, Planks, Iron, Coal, Clay, Pottery, and Tools.
- Tick every few seconds instead of every frame.
- Cache current worksite arrays on a real-time cadence and reuse that snapshot through demand, fallback, release, and rebalance calculations without sorting the cached arrays in place.
- Scan eligible free adults through `StrategyPopulationController.Residents`.
- Compute desired targets for every auto-managed profession from the player priority values, release surplus workers through normal worksite unassign APIs, and let higher-scored shortages pull limited donors from lower-priority auto-managed roles when there are no free adults.
- Maintain a coverage floor of 1 worker for available auto-managed professions whose player counter is above 0; a counter at 0 is the explicit opt-out that allows that role/category to fall to 0.
- Let emergency food/resource shortages pull a limited donor from an at-target profession only when the shortage score strongly exceeds that profession's hold score, while never taking the last worker protected by a coverage floor.
- Ignore children, pending refugees, funeral duty, household foraging/food duty, householders, residents with external workplaces, and active construction assignees through resident availability flags.
- Build work demands from active construction sites, Granary ration reserve, food-production worksite stock/capacity, production-worksite stock/capacity, Storage Yard/Granary logistics backlog, Tools demand, winter household Log reserve demand, and construction material needs.
- Score demands by priority, urgency, shortage, worksite need, construction readiness, storage backlog, and resident distance.
- Assign nearest free adults through existing worksite APIs or settlement role APIs instead of mutating resident/worksite lists directly.
- Assign settlement Builder roles and use population-level balanced construction dispatch as the owner of construction-site assignment.
- Treat Haulers as the single automated logistics profession for storage resources and Granary food movement.
- Register a short manual-removal override per profession so auto-fill does not immediately undo player `-` clicks.
- Log demand, assignment, skipped assignment, manual override, priority, and tick status events.
- Rebalance only idle worksite workers during normal surplus/demand donor releases; explicit priority-0 profession shutdowns can still cancel active work.
- Protect Hunter/Fisher/Forager workers from non-food donor steals while a household food emergency or active food demand exists.

Primary files/assets:

- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.FoodEmergency.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.Cache.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.Part01.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.Part02.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.Part03.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.Part04.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.Part05.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.Part06.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceController.SettlementRoles.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceDemand.cs`
- `Assets/Scripts/Runtime/Population/StrategyAutoWorkforceSettings.cs`
- `Assets/Scripts/Runtime/Population/StrategyHouseWarmthState.cs`
- `Assets/Scripts/Runtime/UI/StrategyProfessionHudController.Part04.cs`
- `Assets/Scripts/Runtime/UI/StrategyProfessionHudController.Part05.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Auto workforce can release surplus workers from overstaffed auto-managed professions, release limited lower-priority donors for higher-scored shortages, and use a stricter emergency margin to pull at-target donors for severe food/resource shortages; Hunter/Fisher/Forager donors are protected from non-food steals during household food emergencies or active food demand, coverage floors protect the last worker in nonzero-counter professions, worksite lookups should use the cached snapshot rebuilt from active placed buildings and active construction sites on active-count changes plus a longer fallback interval instead of scene-wide lookups every scaled tick, repeated no-free-adult full scans and donor-failure diagnostics should stay throttled/lightweight, successful assignments are capped per tick, and only residents who become idle are reused immediately while workers returning carried resources re-enter the free pool on later ticks.
- Free adult fallback assignment runs after demand assignment so idle adults are placed into the best enabled available role when any nonzero managed profession can accept them.
- Auto workforce does not force-reassign home duty, funeral duty, or residents still busy returning carried resources.
- Demand scoring should continue to call public worksite APIs for capped jobs and settlement role APIs for Haulers/Builders so cancellation, carried-resource return, reservations, and resident state cleanup stay centralized.
- Food automation should keep using Hunter/Fisher/Forager production plus shared Haulers into Granaries; do not reintroduce a separate player-facing Granary Worker profession.
- Builder automation should assign settlement Builder roles and let population-level builder dispatch balance actual construction-site assignment.
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
- Use the single authored Forager Camp visual variant for placement/menu preview and normalize legacy saved variants to that accepted sprite.
- Choose random coal pit visual variants for placed pits while keeping menu/preview art stable.
- Choose random granary visual variants for placed granaries while keeping menu/preview art stable.
- Choose random trading post visual variants for placed trading posts while keeping menu/preview art stable.
- Attach variant-aligned chimney smoke to placed houses and expose matching lower-window masks to the cinematic dusk/night light layer.
- Reserve construction Logs/Stone/Planks through the shared storage facade before accepting a construction site, including loose piles, Storage Yards, production-local construction stock, and the low-priority starter Caravan Cart.
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
- Starter Caravan Cart places a `StrategyStarterCaravanCart` temporary stock component, blocks its technical 3x2 footprint plus one visual row above, transfers available construction stock into completed Storage Yards, and despawns when empty.
- Hunter camp places a `StrategyHunterCamp` worksite component, blocks its technical 2x2 footprint plus one visual row above, and hosts a local visual `Game` stockpile.
- Fisher hut places a `StrategyFisherHut` worksite component, blocks its technical 2x2 footprint plus one visual row above, requires nearby water/shore access, and hosts a local visual `Fish` stockpile.
- Forager Camp places a `StrategyForagerCamp` worksite component, blocks its technical 2x2 footprint plus one visual row above, and hosts local visual Berries/Roots/Mushrooms stock.
- Scout Lodge places a one-worker `StrategyScoutLodge` component, blocks its exact technical `2x4` footprint, and uses one elongated procedural final sprite plus a dedicated seven-stage procedural construction sequence.
- Mine places a `StrategyMine` worksite component, blocks its technical 2x2 footprint plus one visual row above, requires an available underground Iron deposit under its footprint, and hosts a local visual Iron stockpile.
- Coal Pit places a `StrategyCoalPit` worksite component, blocks its technical 2x2 footprint plus one visual row above, requires an available underground Coal deposit under its footprint, and hosts a local visual Coal stockpile.
- Clay Pit places a `StrategyClayPit` worksite component, blocks its technical 2x2 footprint plus one visual row above, requires an available near-water Clay field under its footprint, and hosts a local visual Clay stockpile.
- Granary places a `StrategyGranary` food-storage component, blocks its technical 3x2 footprint plus one visual row above, and hosts local visual `Game`/`Fish` stockpiles.
- Trading Post places a `StrategyTradingPost` trade endpoint component, blocks its technical 3x2 footprint plus one visual row above, and exposes nearby walkable stop cells for caravan visits.
- Accepted construction sites request active settlement Builders through balanced dispatch across all active sites, can promote early free adults into normal Builder roles when needed, show material-drop and hammer-hit effects, let builders hammer up to the progress cap allowed by physically delivered materials, and can wait if none are free yet.
- Bridge creates no worksite component, stores its selected span cells/endpoints on the placed-building record, and exposes bank endpoint cells as construction work/dropoff candidates so builders choose a reachable shore and do not stand in water.
- Completed house sites ask population to populate the finished house separately from the construction crew.
- Completed construction emits a placed-building completion event used by starter goals and future onboarding/progression systems.
- Completed construction releases temporary construction-site map blockers before applying final building blockers.
- Confirmed construction cancellation releases temporary map state and drops delivered/carried Logs/Stone/Planks as loose construction resource piles.
- Confirmed building demolition retires the building from new runtime targeting immediately, flushes teardown at end of frame (or before save capture), releases final map blockers, and detaches House residents; Bridge demolition also removes river-span walkability.
- Capture every distinct physical building store plus pending Sawmill/Kiln/Forge output and exact House prepared-dish stacks/leftovers before clearing the building, then create haulable loose piles over its former footprint.
- Seed placed-building records used by later visual upgrades.

Primary files/assets:

- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.Demolition.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.Events.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.Part03.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.Part04.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.Active.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.Debug.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.Part03.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.Part04.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSpriteFactory.ForagerCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyForagerCampVisualProfile.cs`
- `Assets/Scripts/Runtime/Build/StrategyLooseConstructionResourcePile.cs`
- `Assets/Scripts/Runtime/Build/StrategyLooseConstructionResourcePile.Part02.cs`
- `Assets/Scripts/Runtime/Build/IStrategyConstructionResourceSource.cs`
- `Assets/Scripts/Runtime/Build/StrategyPlacedBuilding.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.ScoutLodge.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSpriteFactory.ScoutLodge.cs`
- `Assets/Scripts/Runtime/Build/StrategyScoutLodge.cs`
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
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.Part11.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Build/StrategyGranary.cs`
- `Assets/Scripts/Runtime/Build/StrategyTradingPost.cs`
- `Assets/Scripts/Runtime/Build/StrategyHouseAmbientAnimator.cs`
- `Assets/Scripts/Runtime/Build/StrategyHouseAmbientSpriteFactory.cs`
- `Assets/Tests/EditMode/StrategyHouseVisualEffectTests.cs`
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

- Completed buildings, active construction sites, progress, delivered resources, and blockers participate in versioned persistence.
- Placed objects use tool-specific sprites when available; unknown future tools still fall back to colored sprites/TextMesh labels.
- Build placement consults fog exploration state, so early expansion starts around the camp and other revealed areas unless player fog is disabled from the F9 debug panel.
- House chimney smoke is a visual-only child sprite, while House window masks are full-sprite overlays aligned to the authored `(80,16)` pixel pivot at `48 PPU`; neither should be used for footprint/collider calculations. When House geometry changes, update the per-variant chimney/window profiles and their EditMode contract together.
- Forager Camp construction uses its authored body pivot at `(92,23.2)` pixels inside each `184x164 @ 48 PPU` frame; its lantern and live stock layers use `StrategyForagerCampVisualProfile` anchors, so update the sprite, profile, atlas, and EditMode contract together when camp geometry changes.
- Bridge placement requires two valid explored, unoccupied, walkable river-bank endpoint cells with a straight contiguous River water span between them; Lake water is rejected.
- With the current catalog, `House`, `Lumberjack Camp`, `Stonecutter Camp`, `Sawmill`, `Kiln`, `Forge`, `Hunter Camp`, `Fisher Hut`, `Forager Camp`, `Mine`, `Coal Pit`, `Clay Pit`, `Storage Yard`, and `Granary` can be selected and placed only where their technical foundation is fully walkable/buildable/explored, their future final 2.5D blocker can be reserved on buildable/explored/unoccupied cells, and builders have a nearby walkable work cell; Mine, Coal Pit, and Clay Pit are the only tools allowed to use matching Iron/Coal/Clay build-blocked resource cells.
- Final blocker reservation no longer requires every future visual blocker cell to be walkable at construction-site placement time.
- `Fisher Hut` additionally requires a nearby water cell with adjacent walkable shore access.
- `Mine` additionally requires at least one available underground Iron deposit under its footprint.
- `Coal Pit` additionally requires at least one available underground Coal deposit under its footprint.
- `Clay Pit` additionally requires at least one available near-water Clay field under its footprint.
- Successful player placement creates a construction site, closes the full Build menu, and marks the frame so world selection ignores the placement click.
- Construction site placement normally depends on reservable Logs/Stone/Planks, not on immediately available builders; waiting sites retry hired-builder dispatch.
- Construction sites register/unregister active-site membership for systems that need cheap current-site lookups; keep lifecycle calls paired with configure, completion, and destroy paths.
- When F9 instant construction debug mode is enabled, player placement still creates a construction-site handoff but skips resource reservation, marks resources/progress complete, and immediately finalizes through the normal placed-building completion path; enabling the toggle also completes already active construction sites.
- Final building creation happens through construction-site completion, not the original placement click.
- Goal/progression listeners should use the building completion event so unfinished construction sites are never counted as completed buildings.
- Loose construction resource piles left by cancelled sites count toward construction affordability and can be reserved before Storage Yard stock.
- Future zoning/economy should replace or extend the placed marker with durable city state.
- Occupancy currently lives in the placement controller; a future city-state service should own it if persistence scope or simulation complexity expands.

### House Visual Upgrades

Responsibilities:

- Keep the legacy Garden Beds and House Chicken Coop upgrade implementation available in code while it is inactive in normal House flow.
- Generate and cache old upgrade sprites at runtime.
- Track old installed upgrade types on placed houses if legacy code paths are re-enabled later.
- Support old Chicken Coop idle chicken spawning when a legacy coop exists.
- Avoid exposing Garden Beds or House Chicken Coop as selected-house actions; standalone Chicken Coop owns active egg production.

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

- Current House placement does not auto-install Garden Beds, and the selected-house HUD does not show Garden Beds or Chicken Coop actions.
- Householder home duty no longer starts Garden Beds work.
- The old upgrade controller remains inactive legacy; the standalone Chicken Coop now owns active egg production while reusing chicken sprites/agent behavior.
- Do not re-enable direct house-local food production unless the design intentionally returns food production to Houses.

### House Resources MVP

Responsibilities:

- Define the first resource types.
- Store resource counts locally on placed houses.
- Store prepared `Dish` as recipe stacks with quality/ration metadata while exposing aggregate Dish APIs.
- Provide the runtime dish recipe catalog used by Householder cooking.
- Generate runtime pixel-art resource icons for HUD display.
- Provide resource display ordering for the selected-house HUD.
- Keep existing forage food resource types usable as house-local ingredients when present.
- Provide shared HUD icon support for non-house stock resources such as hunted `Game` and caught `Fish`.

Primary files/assets:

- `Assets/Scripts/Runtime/Economy/StrategyResourceType.cs`
- `Assets/Scripts/Runtime/Economy/StrategyHouseResourceStore.cs`
- `Assets/Scripts/Runtime/Economy/StrategyHouseResourceStore.Dishes.cs`
- `Assets/Scripts/Runtime/Economy/StrategyDishQuality.cs`
- `Assets/Scripts/Runtime/Economy/StrategyDishRecipe.cs`
- `Assets/Scripts/Runtime/Economy/StrategyDishRecipeCatalog.cs`
- `Assets/Scripts/Runtime/Economy/StrategyPreparedDishStack.cs`
- `Assets/Scripts/Runtime/Economy/StrategyDishCookingSummary.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceIconFactory.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceIconFactory.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyPlacedBuilding.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingUpgrade.cs`
- `Assets/Scripts/Runtime/Population/StrategyHouseholdForagingState.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.HouseholdFoodPickup.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageResourceController.Respawn.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageNode.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageSpriteFactory.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Current resources are house-local runtime counts, not global economy inventory.
- Current normal house-local ingredient source is raw `Fish`/`Game`/`Eggs`/forage food delivery from Granaries, then the starter Caravan Cart while stocked, with direct Hunter Camp/Fisher Hut/Forager Camp/Chicken Coop fallback only when no stored food is available; Householders and displayed-age-6+ children can carry raw food to their own home.
- Household foraging from Houses and Garden Beds/House Chicken Coop upgrade paths are inactive legacy paths; Forager Camps are the active external source for forage food and standalone Chicken Coops are the active external source for Eggs.
- Eggs, crops, forage, `Game`, and `Fish` are raw ingredients that can be stored at home and cooked into recipe-based prepared dishes; Eggs now come from standalone Chicken Coops, while crop ingredients still have no normal active production path.
- Current dish recipes span Poor/Common/Hearty/Fine/Feast quality tiers; quality currently affects ration value and HUD/debug context, not morale.
- `Game`, `Fish`, `Eggs`, Berries, Roots, and Mushrooms are local production-building/Granary stock resources with shared HUD icons, and Householders plus displayed-age-6+ children can move Granary stock, or production stock as a Granary-empty fallback, into their own house-local ingredient storage.
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
- `Pottery` currently flows from Kilns to Storage Yards and is consumed by household `Dish` cooking; it is not consumed by construction, trade, or upkeep yet.
- Potters are normal exclusive workplace residents and should remain distinct from settlement Haulers; Potters do not move resources between buildings.

### Forge Production

Responsibilities:

- Add `Forge` as a placed production building with local Iron, Coal, Logs, and Tools stock.
- Assign 1 resident as a Blacksmith through the Profession HUD.
- Request input Iron, Coal, and Logs through the shared production logistics contract.
- Route Haulers to pick up Iron/Coal/Logs from Storage Yard stock, deliver them into the Forge, and route Blacksmiths to forge delivered inputs into `Tools`.
- Keep Blacksmiths visible at the building and drive the forging work overlay plus spark effects while work is active.
- Expose Forge-local Tools to Haulers for hauling.
- Feed Storage Yard `Tools` stock used by production-building upgrades.

Primary files/assets:

- `Assets/Scripts/Runtime/Build/StrategyForge.cs`
- `Assets/Scripts/Runtime/Build/StrategyForge.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyForge.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyProductionUpgradeCost.cs`
- `Assets/Scripts/Runtime/Build/StrategyProductionBuildingUpgrade.cs`
- `Assets/Scripts/Runtime/Build/StrategyProductionBuildingUpgradeCatalog.cs`
- `Assets/Scripts/Runtime/Build/IStrategyProductionLogisticsNode.cs`
- `Assets/Scripts/Runtime/Build/StrategyProductionStorage.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.Part10.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.Part10.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part50.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part51.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part53.cs`
- `Assets/Scripts/Runtime/Population/StrategyProfessionType.cs`
- `Assets/Scripts/Runtime/UI/StrategyProfessionHudController.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Catalog.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceType.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceIconFactory.Part03.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Forge input reservations are separate from construction, Sawmill, and Kiln reservations so Iron/Coal/Logs input delivery cannot double-claim Storage Yard stock.
- Forge counts Iron, Coal, Logs, Tools, pending Tools, and reservations against the shared production local stock cap of 6.
- `Tools` currently flow from Forges to Storage Yards, are consumed by Tools-based production-building upgrades, and can be sold through Trading Post caravan offers; they are not consumed by construction or upkeep yet.
- Blacksmiths are normal exclusive workplace residents and should remain distinct from settlement Haulers; Blacksmiths do not move resources between buildings.

### Storage Yard Logistics

Responsibilities:

- Add `Storage Yard` as a placed storage building with local Logs, Stone, Iron, Coal, Clay, Planks, Pottery, and Tools stock.
- Keep Storage Yard stock uncapped.
- Starter storage now begins in a temporary Caravan Cart near the campfire rather than a prebuilt Storage Yard.
- Assign uncapped residents as Haulers, constrained by available adult residents and exclusive workplace state.
- Hire uncapped additional residents as dedicated construction builders, constrained by available adult residents and exclusive workplace/construction state.
- Find lumberjack camps with available stored Logs and reserve stock for haulers.
- Find stonecutter camps with available stored Stone and reserve stock for haulers.
- Find Mines with available stored Iron and reserve stock for haulers.
- Find Coal Pits with available stored Coal and reserve stock for haulers.
- Find Clay Pits with available stored Clay and reserve stock for haulers.
- Find Sawmills with available stored Planks and reserve stock for haulers.
- Find Kilns with available stored Pottery and reserve stock for haulers.
- Find Forges with available stored Tools and reserve stock for haulers.
- Check for at least one active Storage Yard before householder Pottery reservation attempts, then reserve Pottery from Storage Yard stock for houses that need it for Dish cooking.
- Reserve winter household Logs from Storage Yard stock for Householders whose own houses are below the winter fuel target.
- Expose the temporary starter Caravan Cart as the lower-priority fallback household Log source; the cart still never accepts new deposits and despawns once stock/reservations are gone.
- Find Hunter Camps/Fisher Huts/Forager Camps or loose food piles with available `Game`/`Fish`/forage food and reserve food for delivery to the nearest Granary.
- Find loose construction resource piles and reserve Logs/Stone/Planks for haulers after construction cancellation.
- Reserve Logs/Stone/Planks for accepted construction sites.
- Include loose construction resource piles in construction affordability and reservations.
- Include production-local Lumberjack Camp Logs, Stonecutter Camp Stone, and Sawmill Planks in construction affordability and reservations while the stock is physically present.
- Include low-priority starter Caravan Cart Logs/Stone/Planks in construction affordability and reservations while it has stock.
- Provide reserved construction resource pickup cells for builders.
- Provide a shared construction resource source path so builders can pick up from Storage Yards, loose construction resource piles, production-local construction stock, or the low-priority starter Caravan Cart.
- Dispatch settlement Builders across waiting construction sites, favoring empty and lower-builder-count sites before stacking extras.
- Route Haulers to source camps, pick up Logs, carry them to storage, and deposit them.
- Route Haulers to stonecutter camps, pick up Stone, carry it to storage, and deposit it.
- Route Haulers to Mines, pick up Iron, carry it to storage, and deposit it.
- Route Haulers to Coal Pits, pick up Coal, carry it to storage, and deposit it.
- Route Haulers to Clay Pits, pick up Clay, carry it to storage, and deposit it.
- Route Haulers to production nodes, deliver non-food inputs from Storage Yard stock, then pick up outputs such as Sawmill Planks, Kiln Pottery, and Forge Tools, carry them to storage, and deposit them.
- Pay production-building upgrade costs from available Storage Yard `Tools`, `Planks`, and `Stone`.
- Route Householders from Storage Yard Pottery stock to their own houses with cooking demand.
- Route Haulers to food sources, pick up `Game`/`Fish`, carry it to the nearest Granary, and deposit it.
- Route idle Haulers to reserved construction Logs/Stone/Planks sources and deliver materials directly to active construction sites only when normal Hauler orders are unavailable.
- Expose non-food stock spend/receive helpers for Trading Post caravan transactions.
- Route Haulers to loose construction resource piles, pick up Logs/Stone/Planks, carry them to storage, and deposit them.
- Update lumberjack/stonecutter camp, Mine, Coal Pit, Clay Pit, Sawmill, Kiln, Forge, and storage yard stock visuals as resources move, and show Stone/Iron/Coal/Clay/Planks/Pottery/Tools as separate storage piles.
- Show settlement Hauler/Builder counts, Logs/Stone/Iron/Coal/Clay/Planks/Pottery/Tools stock, and available source count in the selection HUD; player assignment/removal lives in the Profession HUD.

Primary files/assets:

- `Assets/Scripts/Runtime/Build/StrategyStorageYard.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.ConstructionPriority.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.Part13.cs`
- `Assets/Scripts/Runtime/Build/StrategyProductionConstructionResources.cs`
- `Assets/Scripts/Runtime/Build/StrategyStarterCaravanCart.cs`
- `Assets/Scripts/Runtime/Build/StrategyStarterCaravanCart.Construction.cs`
- `Assets/Scripts/Runtime/Build/StrategyStarterCaravanCart.HouseholdLogs.cs`
- `Assets/Scripts/Runtime/Build/StrategyStarterCaravanCart.Food.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.Part07.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.Part05.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.Part08.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.Part09.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.Part10.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.Part11.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.Part12.cs`
- `Assets/Scripts/Runtime/Build/StrategyProductionUpgradeCost.cs`
- `Assets/Scripts/Runtime/Build/StrategyProductionBuildingUpgrade.cs`
- `Assets/Scripts/Runtime/Build/StrategyProductionBuildingUpgradeCatalog.cs`
- `Assets/Scripts/Runtime/Build/StrategyLooseConstructionResourcePile.cs`
- `Assets/Scripts/Runtime/Build/StrategyLooseConstructionResourcePile.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyLooseConstructionResourcePile.Part02.cs`
- `Assets/Scripts/Runtime/Build/IStrategyConstructionResourceSource.cs`
- `Assets/Scripts/Runtime/Build/IStrategyProductionLogisticsNode.cs`
- `Assets/Scripts/Runtime/Build/StrategyLumberjackCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyLumberjackCamp.Construction.cs`
- `Assets/Scripts/Runtime/Build/StrategyStonecutterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategySawmill.cs`
- `Assets/Scripts/Runtime/Build/StrategySawmill.Construction.cs`
- `Assets/Scripts/Runtime/Build/StrategyKiln.cs`
- `Assets/Scripts/Runtime/Build/StrategyForge.cs`
- `Assets/Scripts/Runtime/Build/StrategyForge.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyMine.cs`
- `Assets/Scripts/Runtime/Build/StrategyCoalPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.cs`
- `Assets/Scripts/Runtime/Build/StrategyClayPit.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategySawmill.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part35.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part47.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part48.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part51.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part60.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceType.cs`
- `Assets/Scripts/Runtime/Economy/StrategyConstructionResourceCost.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Catalog.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Haulers reserve worksite Logs/Stone/Iron/Coal/Clay/Planks/Pottery/Tools before walking to prevent multiple haulers from targeting the same stock.
- Haulers run Granary food hauling after normal storage-resource hauling checks, using the shared food reservation cleanup paths.
- Hauler construction-material fallback runs after normal storage, production, and Granary hauling checks; it reuses construction pickup/deposit states with a temporary delivery site and does not create a hired-builder assignment or hammer-work reservation.
- Hauler and builder staffing has no per-yard slot limit; construction sites no longer cap their active visible builder crew at 2.
- Construction resources are normally reserved against physically present loose construction piles, Storage Yard stock, production-local Lumberjack/Stonecutter/Sawmill stock, and the low-priority starter Caravan Cart at site creation, then physically removed when builders or construction-delivery Haulers pick them up; F9 instant construction debug mode bypasses this reservation path for player-placed buildings.
- Household Log reservations subtract from available construction/logistics Logs while pending so the same Log unit is not promised both to winter fuel and to construction/production input delivery.
- Non-carried logistics reservations do not hide physical stock from construction affordability; when construction claims that stock, matching not-yet-picked logistics reservations are cleared, while already-carried resources stay excluded because pickup removed them from source stock.
- Builders also create a per-builder pickup claim after a path to the pickup cell is found; cancelled work releases that claim while the construction-site reservation remains intact.
- If a builder dies while carrying a construction resource, the dropped loose construction pile restores the original site's reservation when that site still needs the resource.
- Residents currently support one active workplace: lumberjack camp, stonecutter camp, sawmill, kiln, hunter camp, fisher hut, forager camp, mine, coal pit, clay pit, storage logistics, granary food logistics, or storage builder crew.
- Storage Yard stock is runtime-only and uncapped; Pottery feeds household Dish cooking, Tools feed production-building upgrades, and Trading Posts can trade stored non-food resources through explicit transaction helpers rather than a separate global inventory.
- Future resources should extend the logistics stock model; current Logs, Stone, Iron, Coal, Clay, Planks, Pottery, and Tools still have explicit carrying visuals/states.
- Storage Yard construction pickup and stock-visual helpers are split into `StrategyStorageYard.Part05.cs`; stock drop effects are in `StrategyStorageYard.Part08.cs` to keep source files below the 500-line limit.

### Granary Food Logistics

Responsibilities:

- Add `Granary` as a placed food-storage building with local `Game`, `Fish`, `Eggs`, Berries, Roots, and Mushrooms stock.
- Keep Granary food stock uncapped.
- Use shared settlement Haulers instead of a separate player-facing Granary Worker profession.
- Find Hunter Camps with available stored `Game` and reserve stock for Haulers.
- Find Fisher Huts with available stored `Fish` and reserve stock for Haulers.
- Find Forager Camps with available stored Berries/Roots/Mushrooms and reserve stock for Haulers.
- Find Chicken Coops with available stored `Eggs` and reserve stock for Haulers.
- Route Haulers to source camps/huts, pick up reserved food, carry it to the granary, and deposit it.
- Provide settlement-level raw food availability and reservation APIs for preferred Householder Granary pickup, starter-cart fallback while it has food, and resident-side direct production-source fallback when no stored food is available.
- Provide raw food spend/receive helpers for Trading Post caravan transactions.
- Update Hunter Camp/Fisher Hut/Forager Camp/Chicken Coop stock visuals as food is picked up.
- Update Granary `Game`/`Fish`/`Eggs`/forage stock visuals and food drop effects as food is deposited.
- Update Granary `Game`/`Fish`/`Eggs`/forage stock visuals as Haulers deposit stock and Householders reserve/pick up ingredients.
- Show food stock and available source counts in the selection HUD.

Primary files/assets:

- `Assets/Scripts/Runtime/Build/StrategyGranary.cs`
- `Assets/Scripts/Runtime/Build/StrategyGranary.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyGranary.Part03.cs`
- `Assets/Scripts/Runtime/Build/StrategyGranary.Forage.cs`
- `Assets/Scripts/Runtime/Build/StrategyGranary.StockVisuals.cs`
- `Assets/Scripts/Runtime/Build/StrategyChickenCoop.cs`
- `Assets/Scripts/Runtime/Build/StrategyHunterCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyFisherHut.cs`
- `Assets/Scripts/Runtime/Build/StrategyForagerCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyForagerCamp.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyForagerCamp.Part02.cs`
- `Assets/Scripts/Runtime/Build/StrategyForagerCampVisualProfile.cs`
- `Assets/Resources/Visual/Authored/Buildings/ForagerCamp/V01.png`
- `Assets/Resources/Visual/Authored/Construction/ForagerCamp/V01.png`
- `Tools/Art/Build-ForagerCampConstructionAtlas.ps1`
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
- Food source reservations prevent multiple Haulers from double-claiming the same `Game`/`Fish`/`Eggs`/forage food.
- `Game`, `Fish`, `Eggs`, Berries, Roots, and Mushrooms remain runtime-local raw food stock; completed houses can receive them from Householder Granary pickups, starter-cart pickup while it has food, or direct production-source fallback when stored food is empty, nightly dinner consumes prepared house `Dish` before falling back to house-local ingredients, and each cooked Dish requires house-local Pottery.
- Residents currently support one active workplace: lumberjack camp, stonecutter camp, hunter camp, fisher hut, forager camp, mine, storage logistics, or storage builder crew.
- Future spoilage, food needs, recipe balancing, market logistics, or settlement-level food services should extend this subsystem rather than folding food into construction Storage Yards.

### Forager Camp Production

Responsibilities:

- Add `Forager Camp` as a cheap external food-production building with local Berries, Roots, and Mushrooms stock.
- Assign up to 2 adult residents as `Forager` through the Profession HUD or auto workforce Food demand.
- Reserve generated `StrategyForageNode` resources through the shared forage resource controller.
- Route Foragers to reachable forage nodes, gather with forage animation frames, carry food back to the camp, and deposit it into local camp stock.
- Expose Forager Camp food stock to Granary Haulers so households never forage directly from Houses.
- Show Forager Camp stock/source context in the selection HUD.

Primary files/assets:

- `Assets/Scripts/Runtime/Build/StrategyForagerCamp.cs`
- `Assets/Scripts/Runtime/Build/StrategyForagerCamp.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyForagerCamp.Part02.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageResourceController.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageResourceController.Respawn.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageNode.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part55.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part56.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part57.cs`
- `Assets/Scripts/Runtime/Population/StrategyProfessionType.cs`
- `Assets/Scripts/Runtime/UI/StrategyProfessionHudController.cs`
- `Assets/Scripts/Runtime/UI/StrategyProfessionIconFactory.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Catalog.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.Part11.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Forager Camp is the active source for forage ingredients; do not re-enable House-owned household foraging unless the design intentionally returns food production to homes.
- Camp stock is local and capped like other production sites; Granaries remain the uncapped food storage and Householder pickup source.
- Auto workforce treats Foragers as part of the Food category alongside Hunters and Fishers.
- The accepted camp has one final visual variant. Legacy construction/save variants normalize to V0, while the authored geometry profile owns the construction pivot and runtime lantern/stock anchors.

### Chicken Coop Production

Responsibilities:

- Add `Chicken Coop` as a cheap external egg-production building with no assigned worker slots.
- Use an enlarged standalone coop sprite on a `4x4` placement footprint; final walk blocking extends to `4x5` through the shared 2.5D blocker rule.
- Produce `Eggs` autonomously on a cycle timer into capped local production stock.
- Spawn slightly larger idle chickens around the placed building without creating a new profession, send them inside at `Night`, and release them outside after `Night`.
- Expose Chicken Coop `Eggs` stock to Granary Haulers and to Householder direct fallback pickup when no stored food is available.
- Show Chicken Coop egg stock, reservation count, no-worker state, and next-egg timer in the selection HUD.

Primary files/assets:

- `Assets/Scripts/Runtime/Build/StrategyChickenCoop.cs`
- `Assets/Scripts/Runtime/Build/StrategyChickenCoop.NightShelter.cs`
- `Assets/Scripts/Runtime/Population/StrategyChickenAgent.cs`
- `Assets/Scripts/Runtime/Population/StrategyChickenAgent.NightShelter.cs`
- `Assets/Scripts/Runtime/Population/StrategyChickenSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.Part11.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.Part03.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.Part04.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildTool.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Catalog.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Driver.Part01.cs`
- `Assets/Scripts/Runtime/Build/StrategyGranary.cs`
- `Assets/Scripts/Runtime/Build/StrategyGranary.Forage.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part56.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part59.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.Part01.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Chicken Coops intentionally do not appear in profession assignment because they have no workers.
- Local Egg stock is capped through `StrategyProductionStorage`; Granaries remain the uncapped food storage layer.
- Standalone Chicken Coop art wraps the legacy upgrade sprite frames at a lower pixels-per-unit value for build, placed, and Egg-production animation sprites; do not change `StrategyBuildingUpgradeSpriteFactory` unless the legacy House upgrade art also needs to change.
- Chicken night sheltering is visual-only: Egg production and stock reservations continue through the coop component while chicken sprites hide inside at `Night`.
- Keep House Chicken Coop upgrade behavior inactive unless the design explicitly returns egg production to Houses.

### Trade MVP

Responsibilities:

- Add `Trading Post` as a placed trade building with procedural art and a selectable trade HUD.
- Create runtime settlement Coins through `StrategySettlementTreasury`.
- Spawn visiting caravans after a completed Trading Post exists, path them from a reachable map edge to a stop cell beside the post, keep them for a trade window, then route them away.
- Expose fixed MVP buy/sell offers through `StrategyTradeOfferCatalog`.
- Execute sell offers by spending available Storage Yard or Granary stock and adding Coins.
- Execute buy offers by spending Coins and depositing goods into the nearest valid Storage Yard or Granary.
- Keep trade scene-object scans throttled/cached so caravan waiting does not call broad scene searches every frame.

Primary files/assets:

- `Assets/Scripts/Runtime/Build/StrategyTradingPost.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingSpriteFactory.Part11.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.Part04.cs`
- `Assets/Scripts/Runtime/Economy/StrategySettlementTreasury.cs`
- `Assets/Scripts/Runtime/Economy/StrategyTradeOffer.cs`
- `Assets/Scripts/Runtime/Economy/StrategyTradeOfferCatalog.cs`
- `Assets/Scripts/Runtime/Economy/StrategyTradeTransactionService.cs`
- `Assets/Scripts/Runtime/Economy/StrategyTradeCaravanSpriteFactory.cs`
- `Assets/Scripts/Runtime/Economy/StrategyTradeCaravanAgent.cs`
- `Assets/Scripts/Runtime/Economy/StrategyTradeCaravanController.cs`
- `Assets/Scripts/Runtime/Economy/StrategyTradeCaravanController.Pathing.cs`
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.Part12.cs`
- `Assets/Scripts/Runtime/Build/StrategyGranary.Part03.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.Part13.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildMenuController.Catalog.cs`
- `Assets/Scripts/Runtime/UI/StrategyBuildTool.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.cs`
- `Assembly-CSharp.csproj`

Impact hints:

- Trade currently uses direct transaction helpers; Haulers and Householders do not physically move goods to or from caravans.
- Selling food spends Granary `Game`/`Fish`; selling non-food spends Storage Yard stock only.
- Buying food requires at least one Granary; buying non-food requires at least one Storage Yard.
- Trading Post is not a worker profession and should not appear in the Profession HUD or auto workforce distribution.
- Future trade expansion should decide whether caravans have finite offer stock, variable prices, money sinks, and physical loading/unloading logistics.

### Population MVP

Responsibilities:

- Create the starter camp with an animated campfire.
- Select the starter camp cell on walkable land at least 6 cells from generated water/shore when possible.
- Start the campfire as a lit central fire on Day 1 Dawn/Morning, fade it out by `Noon`, then keep later campfire relight/lit behavior limited to `Night`.
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
- Keep legacy household foraging state compiled but do not attach it to Houses or dispatch it.
- Assign the oldest adult female resident in each house as `Householder` and move her into home-duty work.
- Check close kinship through resident parent/ancestor links for future family/couple rules.
- Populate completed houses from the homeless adult male/female pool when possible, even if those residents already have workplaces or construction assignments.
- Fall back to free-house migration and partner lookup when no free pair can immediately occupy a completed house.
- Refresh persistent family records after marriage surname changes so Family Trees and HUDs see the current name while biological kinship IDs stay intact.
- Bind assigned residents to their home building.
- Spawn children for valid adult male/female house pairs after randomized household cooldowns when house capacity allows.
- Keep children younger than 3 years old inside their assigned home by hiding their world sprite/collider and skipping outdoor idle/funeral movement until they age out.
- Give older children daytime ambient play activities near home/camp, including solo play, pair play with siblings or nearby children, and tag; children with displayed age 6+ can instead help carry raw household food to their own home when reserves are low.
- Send housed idle residents home to sleep inside during the `Night` phase by hiding their world sprite/collider until morning, while leaving homeless residents outside with a visible `Zzz...` sleep indicator.
- Prepare future lamp workers around 20:00 during `Dusk`, while every adult outside independently uses a personal torch only beyond active stationary or higher-priority resident light coverage. Assign only the lamp-worker subset to light building and roadside lamps at `Night` using nearest-to-home light queues and hand-carried torch walk/light animations before they return to sleep.
- Resolve one nightly household dinner from prepared house `Dish`, using resident age-based ration needs after eligible residents return home for `Night`.
- Send Householders to fetch reserved raw `Fish`/`Game`/forage food from reachable Granaries into their own house when ingredient reserves are low, then from the starter Caravan Cart while it has food, or from reachable Hunter/Fisher/Forager production stock when no stored food is available.
- Send Householders to fetch Pottery from active Storage Yards and cook stored ingredients plus 1 Pottery per prepared `Dish` during `Dusk` when dinner coverage is low; keep Pottery retry cooldown separate from raw-food pickup.
- Track per-resident nutrition debt, days hungry, hungry/starving status, and recovery when nightly dinner needs are met.
- Grow children into adults after scaled game time.
- Continue resident aging after adulthood.
- Roll annual resident mortality from age 1 using an accelerating age-risk curve.
- Multiply annual resident mortality by each resident's malnutrition severity when household dinner shortages accumulate.
- Block resident death attempts while the resident is in active funeral duty, preventing carrier/attendee death from freezing the funeral controller.
- Remove dead residents from homes, work assignments, construction assignments, active reservations, live population counts, and selected-HUD targets.
- Create resident death snapshots and animated corpses when residents die.
- Run multiple funeral processes at the same time while keeping each resident in at most one active funeral duty.
- Gather available close family/household funeral participants for mourning, procession, and optional burial attendance.
- Run silent service burials with one nearby adult carrier when no living family/household funeral participants exist.
- Create a spontaneous cemetery away from the settlement and reserve carrier-reachable grave cells for parallel burials.
- Create clickable grave sprites after burial and mark grave cells as not walkable.
- Temporarily interrupt resident tasks for funeral activities without permanently removing workplace roles.
- Move the oldest adult child still living with parents into empty houses.
- Move an eligible adult opposite-gender partner into a single-adult House while blocking close relatives; eligible partners may come from a parental household, the homeless pool, or another House where they live alone.
- Create temporary refugee families with 1-3 members, 1-2 adult parents, and optional children.
- Gate the first refugee family on the scripted evening of Day 2; schedule later families with the repeat interval, fading arrival intensity after 40 accepted residents and stopping arrivals at 50 accepted residents.
- Run one dynamic refugee roll per game day after the scripted first family; the chance rises when accepted adult count is below total finite worker slots from capped worksites plus one construction slot per active construction site, while uncapped settlement Hauler/Builder capacity is ignored.
- Spawn refugee arrivals inside the map about 4 cells beyond a random side of the daylight-visible fog boundary, with a walkable in-map edge fallback for debug/no-fog cases.
- Keep pending refugees outside the normal resident registry until accepted.
- Accept refugee families into the normal resident registry or destroy rejected temporary families after they return to the hidden in-map arrival staging point.
- Track accepted refugee families that could not be housed immediately as unsettled groups, including one-person arrivals so they receive priority for the next fitting empty House, while preventing generic pair assignment from splitting multi-person groups before all members share one house.
- Drive simple idle movement around the current camp/home through short walkable grid paths.
- Route homeless residents without houses to reachable reserved sleep spots around the startup campfire during `Night`.
- Let one homeless resident relight campfire embers with a visible kindling animation before the camp sleeps.
- Periodically send Householders from `TendingHousehold` home duty to fetch raw `Fish`/`Game`/forage food from Granaries or Granary-empty production fallback sources, fetch Pottery from Storage Yards, or cook stored ingredients plus Pottery into `Dish`.
- Do not send residents for household foraging directly from Houses; assigned Forager Camp workers own generated forage gathering.
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
- Assign residents to Forager Camps as workplace targets.
- Route assigned Foragers to generated Berries/Roots/Mushrooms nodes, forage gather timing, carried forage visuals, and camp stock deposit.
- Assign one resident to each Scout Lodge as a workplace target.
- Route assigned Scouts to reserved walkable unknown-side fog-frontier cells, survey there continuously through day/night, and release/retry targets safely across path deferral, death, funeral interruption, demolition, and unassignment.
- Assign residents to settlement-level Hauler roles.
- Route assigned Haulers to lumberjack camp stock, stored-Logs pickup, storage-yard delivery, and deposit.
- Resolve Hauler building pickup/delivery access from a reachable perimeter cell, stop and briefly retry when navigation is budget-deferred, and skip briefly cached genuinely inaccessible source buildings before reserving their stock.
- Route assigned Haulers to stonecutter camp stock, stored-Stone pickup, storage-yard delivery, and deposit.
- Route assigned Haulers to Mine stock, stored-Iron pickup, storage-yard delivery, and deposit.
- Route assigned Haulers to Coal Pit stock, stored-Coal pickup, storage-yard delivery, and deposit.
- Route assigned Haulers to Clay Pit stock, stored-Clay pickup, storage-yard delivery, and deposit.
- Route assigned Haulers to Kiln stock, stored-Pottery pickup, storage-yard delivery, and deposit.
- Route Householders to Storage Yard Pottery stock, house delivery, and deposit when their own house needs Pottery for cooking.
- Route assigned Haulers to Hunter Camp/Fisher Hut/Forager Camp/Chicken Coop stock, stored-food pickup, granary delivery, and deposit.
- Route assigned Haulers to reserved construction Logs/Stone/Planks sources for direct construction-site delivery when normal Hauler orders are unavailable.
- Assign residents to settlement-level Builder roles.
- Route settlement Builders to reserved construction stock, construction resource pickup, site delivery, and hammer/build work up to the currently delivered-material progress cap; before a Storage Yard exists, early construction can assign free adults into the same normal Builder role.
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
- Generate resident campfire kindling, ground-sleep, and hand-carried torch walk/light sprites for night sleep and night lamp duty.
- Generate and animate procedural campfire flame, smoke/spark, ember, and relight frames at runtime.
- Drive the first-morning campfire flame fade plus later burnout/daylight extinguish into embers and residual light that fade from `Dawn` to fully cold at the start of `Noon`, restore campfire-cell walkability while extinguished, and support resident-triggered nighttime relight.
- Drive simple chicken idle movement around standalone or legacy linked Chicken Coops with walk and peck sprite animations, plus standalone coop night shelter/release visuals.
- Drive standalone Chicken Coop egg production from a cycle timer synchronized with the coop's nest/egg animation frames.
- Expose runtime residents as read-only visibility sources for fog of war.

Primary files/assets:

- `Assets/Scripts/Runtime/Population/StrategyPopulationController.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.Part01.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.Part02.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.Part08.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.Part06.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.Part07.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.SettlementRoles.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentDeathSnapshot.cs`
- `Assets/Scripts/Runtime/Population/StrategyFuneralController.cs`
- `Assets/Scripts/Runtime/Population/StrategyFuneralController.Part01.cs`
- `Assets/Scripts/Runtime/Population/StrategyFuneralController.Part02.cs`
- `Assets/Scripts/Runtime/Population/StrategyFuneralController.Part03.cs`
- `Assets/Scripts/Runtime/Population/StrategyCemeteryController.cs`
- `Assets/Scripts/Runtime/Population/StrategyCorpse.cs`
- `Assets/Scripts/Runtime/Population/StrategyFuneralSpriteFactory.cs`
- `Assets/Scripts/Runtime/Population/StrategyGraveMarker.cs`
- `Assets/Scripts/Runtime/Population/StrategyRefugeeArrivalController.cs`
- `Assets/Scripts/Runtime/Population/StrategyRefugeeArrivalController.Part02.cs`
- `Assets/Scripts/Runtime/Population/StrategyRefugeeArrivalController.Part03.cs`
- `Assets/Scripts/Runtime/Population/StrategyHouseholdState.cs`
- `Assets/Scripts/Runtime/Population/StrategyHouseholdFoodState.cs`
- `Assets/Scripts/Runtime/Population/StrategyHouseholdFoodState.NightMeal.cs`
- `Assets/Scripts/Runtime/Population/StrategyHouseWarmthState.cs`
- `Assets/Scripts/Runtime/Population/StrategyHouseholdForagingState.cs`
- `Assets/Scripts/Runtime/Population/StrategyHomelessCampController.cs`
- `Assets/Scripts/Runtime/Population/StrategyNightLightTaskController.cs`
- `Assets/Scripts/Runtime/Population/StrategyNightLightTaskController.Utilities.cs`
- `Assets/Scripts/Runtime/Population/StrategyKinshipUtility.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.SettlementRoles.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.NightLights.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.NightTorchLight.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.PersonalTorch.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.WorkSfx.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Scouting.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part36.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.TrailRoutes.cs`
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
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part48.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part49.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part57.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part58.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.ChildIdle.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part60.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part62.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentSpriteFactory.Part05.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentSpriteFactory.NightTorch.cs`
- `Assets/Scripts/Runtime/Population/StrategyCampfireRelightSpriteFactory.cs`
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
- `Assets/Scripts/Runtime/Build/StrategyStorageYard.Part13.cs`
- `Assets/Scripts/Runtime/Build/StrategyStarterCaravanCart.HouseholdLogs.cs`
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
- `Assets/Scripts/Runtime/Map/StrategyForageResourceController.Respawn.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageNode.cs`
- `Assets/Scripts/Runtime/Map/StrategyForageSpriteFactory.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyWildlifeController.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyRabbitAgent.cs`
- `Assets/Scripts/Runtime/Wildlife/StrategyFishAgent.cs`
- `Assets/Scripts/Runtime/Population/StrategyHuntingArrowProjectile.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentSpriteFactory.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentSpriteFactory.Part06.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Part54.cs`
- `Assets/Scripts/Runtime/Population/StrategyCampfireAnimator.cs`
- `Assets/Scripts/Runtime/Population/StrategyCampfireAnimator.Daylight.cs`
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

- Resident identity, age/life state, home/kinship links, nutrition, and cold state are persisted; transient tasks and active movement are rebuilt after load.
- Resident names are assigned at runtime from built-in first-name and family-name pools.
- Startup residents spawn as family-linked adults; children born during play start at age 0, stay inside assigned homes until age 3, become adults at age 16 through scaled game time, and continue aging after adulthood.
- `StrategyPopulationController` owns the live resident ID registry plus persistent family records used by kinship lookup after deaths.
- Resident death should continue to flow through `StrategyPopulationController` so death snapshots, assignment cleanup, selection cleanup, funeral startup, and family records stay in one path.
- Active funeral duty is a hard death guard in the central population death path; future funeral recovery work can replace this only after carrier/attendee death is safely handled.
- `StrategyFuneralController` owns the runtime funeral state machines; parallel funeral processes should stay separate from normal workplace AI and should only use public resident funeral hooks.
- Service burials are selected when a funeral has no living available family/household participants; they use one silent adult carrier chosen from the nearest eligible non-funeral-duty adults and should not start crying/mourning poses.
- `StrategyFuneralController` keeps only movers with started funeral paths in family/attendee lists, tries nearby corpse/grave stand positions before rejection, skips residents already in other active funeral duties, and builds a carrier reachable-cell set before reserving a grave; funeral resident movement must fail rather than fall back to direct world movement when no walkable path exists.
- Funeral processions drag the corpse behind a primary carrier with a visible rope and clamp the corpse within one map-cell distance from that carrier.
- Burial starts once the corpse and carriers are ready at the reserved grave; non-carrier mourners can attend and pose but no longer block burial until timeout.
- `StrategyCemeteryController` owns spontaneous cemetery placement plus pending grave reservations; graves are world blockers and clickable markers, so map walkability and grave HUD data must be updated when grave cells are created.
- Cemetery placement should remain away from active settlement space without drifting to extreme map edges; current scoring favors a moderate camp distance, penalizes edge cells, and rejects grave candidates without enough carrier-reachable stand cells.
- `StrategyPopulationController` also owns the runtime house registry used for free-house migration and partner retry checks.
- Pending refugee families are rendered as resident agents but are not counted as residents, workers, or fog sources until accepted.
- Refugee entry selection depends on `StrategyFogOfWarController.IsCellVisibleAtDaylightRange` so temporary night/fog visibility reductions do not move arrivals closer to the settlement.
- Dynamic refugee demand rolls should compare accepted adults against finite worker capacity and should not treat settlement Hauler/Builder roles as infinite vacancies.
- Accepted refugee families join the normal registry as a preserved family block, stay near camp while homeless, and get priority to fill the first empty House as a whole household before normal single-adult migration or random pair assignment.
- `StrategyHouseholdState` lives on occupied houses and owns the randomized birth timer.
- `StrategyHouseholdState` blocks births while the same house has sustained ration shortages or birth-blocked residents.
- `StrategyHouseholdFoodState` lives on occupied houses, resolves one nightly dinner per day after a one-day settling grace, waits for eligible residents to enter home for `Night` with a fallback deadline, consumes prepared house recipe dishes first, falls back to house-local ingredients for missing rations, applies short rations to resident nutrition debt, and exposes aggregate food status for HUDs.
- `StrategyHouseWarmthState` lives on occupied houses, reads outdoor temperature/weather, consumes one house-local `Logs` unit on winter nights when available, tracks indoor warmth status for HUDs, and reports winter Log reserve demand.
- Household final-mile food duty can reserve one raw `Fish`/`Game`/`Eggs`/forage-food unit from a Granary, then the starter Caravan Cart while it has food, or from Hunter Camp/Fisher Hut/Forager Camp/Chicken Coop stock when no stored food is available, path to pickup, carry it home, and store it in the house; Householders and children with displayed age 6+ can do raw-food pickup for their own home, while only Householders fetch Pottery from a Storage Yard and cook stored ingredients plus 1 Pottery per prepared recipe dish during `Dusk`; Householders also fetch winter household `Logs` from Storage Yards, then the starter Caravan Cart, for their own house.
- `StrategyHouseholdForagingState` is compiled as inactive legacy code; placed Houses no longer attach it, and resident start guards return false so house-driven foraging cannot dispatch.
- Generated forage reach/crouch sprites and node pulse effects are used by Forager Camp workers and are not started by Houses.
- `StrategyPlacedBuilding` owns the current Householder reference for houses, preferring the oldest adult female resident and refreshing on home changes, death/unregister, and resident adulthood.
- `StrategyResidentAgent.HasWorkplace` includes the Householder role, so profession assignment should treat householders as occupied home workers.
- Householder assignment clears external worksite roles plus settlement Hauler/Builder roles through their owning APIs and uses `TendingHousehold` instead of `Idle` for home duty.
- `StrategyKinshipUtility` treats close parent/ancestor graph distance as a block for future couple/family rules, including ancestors whose resident GameObjects were destroyed after death.
- Resident readability helpers are visual-only child `SpriteRenderer`s and should stay synced when changing resident animation frames.
- Residents use the shared trail-aware 8-direction A* grid pathfinder with no diagonal corner cutting and post-path smoothing for idle, home, workplace, construction, logistics, and funeral travel while keeping frame-based sprite walk cycles.
- Resident trail-aware path creation has a per-frame budget to avoid x2/x3 mass state-change spikes; excess attempts retry through normal task flow.
- Resident scheduled-work task-start decisions have a per-frame budget in larger settlements; expensive household, logistics, construction, worksite, hunting, fishing, and child-play probes should remain behind this budget unless they are active-task continuation or carried-resource cleanup.
- Resident movement records completed building-to-building route traversals and commits stable roads after three traversals of the same building pair, using a direct route-line attempt, smoothed route waypoint fallback, connected single-sided route-road connectors plus bounded local repair to the existing road network, and canonical per-building-pair reinforcement so raw A* detours do not create square road pockets or disconnected road tails; a branch that joins the existing network does not restore its discarded tail, ordinary footfalls no longer create functional or visible roads, and formed roads apply a 15% speed bonus.
- Resident pathfinding can recover a blocked start cell by snapping to a nearby walkable cell and logging `PathStartRecovered`.
- Resident scheduled work starts only during `StrategyDayNightCycleController.IsSettlementWorkTime`, which covers Dawn through Dusk on every day, except assigned Scouts whose dedicated exploration branch remains active overnight. Keep carried-resource returns, deposits, and cleanup paths schedule-safe so nightfall cannot strand stock reservations.
- Resident night sleep is separate from homebound young-child hiding: housed residents only enter the hidden home interior during `Night` when they are not carrying resources, in funeral duty, underground, assigned to night lamp lighting, or assigned as Scouts, notify household food state for dinner readiness, then reappear at the home exit after night ends. Homeless residents use campfire sleep under the same Scout exception; an assigned Scout remains expected-away for dinner while still consuming a household field ration.
- Night lamp lighting is owned by `StrategyNightLightTaskController` plus `StrategyResidentAgent.NightLights.cs`; dynamic adult light-coverage decisions live in `StrategyResidentAgent.PersonalTorch.cs` and `StrategyCinematicLightEmitter.Coverage.cs`; carried torch light/glow/night-mask contribution is isolated in `StrategyResidentAgent.NightTorchLight.cs`. Assigned Scouts are excluded from stationary-lamp duty but can display personal torches during exploration. Late-Dusk personal hand-torch activation must not start stationary-lamp routes before `Night`, ordinary personal torches should avoid real `Light2D` point lights, and lamp sources should not automatically light when no eligible resident can reach them.
- Resident footstep audio is attached by `StrategyResidentAgent` and plays grass clips on selected walk frames; resident work SFX plays axe/pickaxe/hammer/fishing/bow clips on selected work frames; both route through `StrategyAudioMixController` for bus volume, camera-focus/zoom attenuation, low-pass, and subtle far-source reverb, so keep them low-volume/spatial when adding more residents or faster simulation speeds.
- Lumberjack work keeps the same camp worksite component but chooses the nearest available tree/processable wood on the map; it tests nearby work cells for real path reachability before starting tree/log/plant movement, only starts planting when the camp can accept future Logs, and includes tree chopping, trunk bucking, Logs delivery, and sapling planting.
- Resident woodcut sprites are generated for every male/female visual variant and should stay in sync with readability outline mirroring.
- Stonecutter work keeps the same camp worksite component but chooses the nearest available finite Stone deposit on the map and does not plant/regrow Stone.
- Resident stonecut sprites are generated for every male/female visual variant and should stay in sync with readability outline mirroring.
- Mine work follows the local worksite assignment model but keeps miners hidden underground during the timed work loop, reserves walkable underground Iron indicators, and stores produced Iron locally at the Mine.
- Coal Pit work follows the local worksite assignment model but keeps coal miners visible inside the pit during the timed work loop, reserves walkable underground Coal indicators, and stores produced Coal locally at the Coal Pit.
- Clay Pit work follows the local worksite assignment model but keeps clay diggers visible inside the pit during the timed work loop, reserves walkable near-water Clay fields, and stores produced Clay locally at the Clay Pit.
- Sawmill work follows the local worksite assignment model, waits for Hauler-delivered Logs from Storage Yard stock, keeps Sawyers visible inside the Sawmill during the timed work loop, and stores produced Planks locally at the Sawmill.
- Kiln work follows the local worksite assignment model, waits for Hauler-delivered Clay and Coal from Storage Yard stock, keeps Potters visible during the timed firing loop, and stores produced Pottery locally at the Kiln.
- Hunter work keeps the same camp worksite component but reserves the nearest available adult rabbit through `StrategyWildlifeController` by default, can reserve adult deer after the Hunter Camp production upgrade, chooses a reachable roughly 2-3 tile bow stand cell, uses a 20% arrow miss chance, and stores produced `Game` locally at the hunter camp on hits.
- Resident bow and butchering sprites are generated for every male/female visual variant and should stay in sync with readability outline mirroring.
- Fisher work keeps the same hut worksite component but reserves the nearest available fish through `StrategyWildlifeController`, requires a valid land/shore stand cell around the target, abandons casts when the fish leaves cast range during cast/wait/reel phases, and stores produced `Fish` locally at the fisher hut for now.
- Resident fishing sprites are generated for every male/female visual variant and should stay in sync with readability outline mirroring.
- Granary food logistics is serviced by shared Haulers, moving food from production buildings into food storage after normal storage-resource hauling checks.
- Settlement Haulers move Logs, Stone, Iron, Coal, Clay, Planks, Pottery, and Tools outputs from production worksites into Storage Yard stock when a yard exists, deliver non-food production inputs from Storage Yard stock into production nodes, and can fallback-deliver construction materials before Storage Yard construction; Householders deliver Pottery from Storage Yards into houses for Dish cooking. Coal, Clay, Planks, Pottery, and Tools use their own carried sprite and return/drop cleanup paths.
- Construction assignment is an exclusive active task for settlement Builders; there is no construction-site builder cap, balanced dispatch spreads free Builders across active sites first, and workplace assignment skips residents already attached to a construction site. Construction assignment does not block home/family assignment.
- Construction hammer work must reserve an individual unlocked build-hit unit on `StrategyConstructionSite` before the resident enters the hammer animation; reset, assignment clear, site completion, and site destruction paths must release those reservations.
- Builder construction pickup path failures include start/pickup walkability details in `debug.log`; repeated pickup path failures drop that builder's current site assignment so another builder can retry.
- Worker and builder assignment must check `StrategyResidentAgent.CanWork`; children under age 3 remain inside assigned homes, and older children can use age-weighted ambient actions or carry raw household food home at displayed age 6+, but cannot work/build.
- Resident construction sprites are generated for every male/female visual variant and should stay in sync with readability outline mirroring.
- Resident crying sprites are generated for adult and child funeral mourning/waiting states and should stay in sync with readability outline mirroring.
- Chickens use the same local path style as before; their animation and standalone coop night sheltering are visual-only.
- House construction no longer consumes residents as builders; after completion, the finished house tries to pull one homeless adult male and one homeless adult female from the starter camp/free pool, regardless of workplace or construction role.
- Male/female household pair creation and partner move-in rename the wife to the husband's family name; this is a current display/name rule, not a separate explicit marriage entity yet.
- Assigning a home should not cancel active workplace/construction tasks; idle residents can walk home immediately, and busy residents keep the home binding for later idle/home behavior.
- If no free pair exists, the completed house is available for adult-child migration and partner lookup.
- House occupation consumes the finite free-resident pool from the starter camp while it exists; later household births and adult-child migration are the first internal population growth path.
- Resident death must continue to go through the centralized population cleanup path; direct `Destroy` on accepted residents risks stale worksite, construction, home, HUD, or kinship state.
- Resident helper methods for settlement role assignment, carried-resource return, construction work, hauler construction delivery fallback, workplace clearing, readability sync, refugee path following, tree movement, fishing cast/reel flow, production-input delivery, trail movement, building-route trail capture, ranged hunt stand selection, reachable forestry work-cell selection, reachable construction dropoff selection, worker-triggered visual effects, day/night work scheduling, night home sleep, night lamp lighting, Clay work/logistics, Kiln/Pottery work/logistics, household Pottery delivery, Forge/Tools work/logistics, production-upgrade speed helpers, homeless campfire sleep, forager work, and child play are split across `StrategyResidentAgent` partial files to keep source files below the 500-line limit.
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
- Keep Garden Beds and Chicken Coop hidden from the selected-house HUD.
- Expose Tools-based production upgrade actions in eligible selected-building HUDs.
- Show selected-house resident portraits/names/age/life stage/statuses up to house capacity, including the Householder marker, prepared dish recipe summaries, Pottery, ingredient rations, and resource icons/counts.
- Show selected worksite status/resource context without worker assignment controls, except for the Scout Lodge's single direct Assign/Remove slot.
- Show selected Storage Yards with a dedicated icon-led logistics dashboard for Haulers, builders, available sources, resource stock, and readiness status.
- Show selected starter Caravan Carts with compact temporary construction and food stock context.
- Show selected Trading Posts with settlement Coins, caravan status/ETA, and active buy/sell offer buttons.
- Show selected lumberjack/stonecutter/sawmill/kiln/forge/hunter/fisher/mine/coal pit/clay pit/granary/storage stock and nearby source/target counts.
- Show selected-construction-site cost, delivered resources, builder count, and progress/status context.
- Show selected-resident full name, portrait, profile, age/life stage, current activity, and home/camp assignment.
- Show selected-grave deceased name, epitaph, age, final profession, family role, and memory text.
- Listen for `Delete` on selected construction sites/buildings and open the reusable confirmation dialog before cancellation or demolition.
- Use shared button feedback for selection actions and the shared protected modal transition for confirmation prompts.

Primary files/assets:

- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.ScoutWorkers.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.Part09.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.Part10.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.Part12.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldSelectionController.Part13.cs`
- `Assets/Scripts/Runtime/Selection/IStrategyWorldInspectable.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldInspectInfo.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldInspectInfoFactory.cs`
- `Assets/Scripts/Runtime/Selection/StrategyWorldInspectHudController.cs`
- `Assets/Scripts/Runtime/Selection/StrategyStaticWorldInspectable.cs`
- `Assets/Scripts/Runtime/UI/StrategyConfirmationDialogController.cs`
- `Assets/Scripts/Runtime/Build/StrategyPlacedBuilding.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildingUpgradeController.cs`
- `Assets/Scripts/Runtime/Build/StrategyProductionUpgradeCost.cs`
- `Assets/Scripts/Runtime/Build/StrategyProductionBuildingUpgrade.cs`
- `Assets/Scripts/Runtime/Build/StrategyProductionBuildingUpgradeCatalog.cs`
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
- `Assets/Scripts/Runtime/Build/StrategyTradingPost.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.cs`
- `Assets/Scripts/Runtime/Population/StrategyGraveMarker.cs`
- `Assets/Scripts/Runtime/Economy/StrategyHouseResourceStore.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceIconFactory.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceIconFactory.Part02.cs`
- `Assets/Scripts/Runtime/Economy/StrategySettlementTreasury.cs`
- `Assets/Scripts/Runtime/Economy/StrategyTradeCaravanController.cs`
- `Assets/Scripts/Runtime/Economy/StrategyTradeTransactionService.cs`
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
- Building selection links are visual-only world overlays: Houses use `StrategyPlacedBuilding.Residents`; worksites use their assigned worker lists; Storage Yards show stock/logistics context without owning Hauler/Builder links; selected construction sites link to their assigned builders.
- House resident rows use the assigned resident references stored on `StrategyPlacedBuilding` and grow to the current house capacity.
- Worksite context uses references/counts stored on the selected worksite component, but player assignment/removal is owned by the Profession HUD.
- Construction site context uses cost/progress/builder data stored on `StrategyConstructionSite`.
- Current HUD layout is code-built with Unity UI primitives; future HUD shell work should decide whether this remains local or moves into a shared UI view layer.

### Input Foundation

Responsibilities:

- Own canonical Global/Camera/Gameplay/Build/Debug/UI action definitions.
- Route all runtime controls through typed actions without direct device polling.
- Own modal channel blocking, cancel handling, and secondary-pointer ownership.
- Keep exactly one shared EventSystem/Input System UI module per scene.

Primary files/assets:

- `Assets/InputSystem_Actions.inputactions`
- `Assets/Scripts/Runtime/Input/StrategyInputRouter.cs`
- `Assets/Scripts/Runtime/Input/StrategyInputContext.cs`
- `Assets/Scripts/Runtime/UI/StrategyUiInputModuleBootstrap.cs`
- `Assets/Tests/EditMode/StrategyInputActionsContractTests.cs`
- `Assets/Tests/EditMode/StrategyInputRouterTests.cs`
- `Packages/manifest.json`

Impact hints:

- Action names, IDs, canonical UI bindings, and control schemes are contracts enforced by tests.
- Modal UI must own/dispose a scoped context so blocked gameplay/camera/build input cannot leak after close or scene unload.
- Add runtime controls to the router/action asset rather than reading `Keyboard.current`, `Mouse.current`, or `KeyControl` in consumers.

### Shared Resource Stores And Queries

Responsibilities:

- Own physical resource amounts and reservations across settlement storage, production, houses, the starter cart, loose construction piles, and loose carried-resource piles.
- Aggregate resource availability for HUD, construction affordability, logistics, and seasonal readiness without counting carried stock.
- Adapt existing source-specific reserve/commit/release behavior to the common query contract.
- Preserve exact prepared-dish recipe/count/leftover payloads while loose and restore them when a Householder delivers the pile.

Primary files:

- `Assets/Scripts/Runtime/Economy/StrategyResourceStore.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceQueryService.cs`
- `Assets/Scripts/Runtime/Economy/StrategyResourceReservationProviders.cs`
- `Assets/Scripts/Runtime/Economy/StrategyHouseResourceStore.cs`
- `Assets/Scripts/Runtime/Economy/StrategyHouseResourceStore.Dishes.cs`
- `Assets/Scripts/Runtime/Economy/StrategyLooseCarriedResourcePile.cs`
- `Assets/Scripts/Runtime/Economy/StrategyLooseCarriedResourcePile.Logistics.cs`
- `Assets/Scripts/Runtime/Economy/StrategyLooseCarriedResourcePile.PreparedDishes.cs`
- `Assets/Scripts/Runtime/Build/StrategyProductionConstructionResources.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.LooseStoragePickup.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.LoosePreparedDish.cs`

Impact hints:

- New stock-owning buildings should expose a common store and declare the correct query scope.
- Keep resources removed from a source while carried so settlement totals never double-count in-transit stock.

### First Winter Progression

Responsibilities:

- Extend starter onboarding into food/fuel preparation and first-winter endurance goals.
- Show live current-days/target-days progress bars for first-winter Food and Firewood preparation using `StrategySeasonReadiness` values.
- Apply house warmth, resident cold exposure, seasonal movement/mortality consequences, and season/housing-aware refugee pressure.
- Clear the winter goal and continue normal sandbox simulation after the first winter; do not create victory or defeat outcomes at this stage.

Primary files:

- `Assets/Scripts/Runtime/Core/StrategyFirstWinterController.cs`
- `Assets/Scripts/Runtime/Core/StrategyFirstYearBalance.cs`
- `Assets/Scripts/Runtime/Core/StrategySeasonReadiness.cs`
- `Assets/Scripts/Runtime/Economy/StrategyHouseWarmthState.Cold.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentColdState.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.Cold.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.Cold.cs`

Impact hints:

- Balance food, fuel, temperature, and refugee changes against the seven-day season length together.
- Keep the winter controller as progression orchestration; physical stock remains owned by resource stores and cold remains resident/house state.

### Persistence

Responsibilities:

- Capture and restore versioned runtime settlement snapshots with stable IDs and no raw Unity object references.
- Materialize resident-carried stock into the save snapshot as loose resources at each resident's current cell because active tasks and carried state are intentionally rebuilt rather than serialized.
- Write saves atomically and coordinate restoration only after runtime bootstrap has created all required systems.
- Preserve F5 save and F8 load/restart controls while exposing read/validate/pending-load entry points to the intro menu Continue flow.
- Preserve the founding profile, stable answer pairs, exact camp cell, current starter-cart origin, stable point-of-interest state, and exact loose prepared-dish payloads across save version 5.

Primary files:

- `Assets/Scripts/Runtime/Persistence/StrategySaveData.cs`
- `Assets/Scripts/Runtime/Persistence/StrategySaveSystem.cs`
- `Assets/Scripts/Runtime/Persistence/StrategySaveMigration.cs`
- `Assets/Scripts/Runtime/Persistence/StrategySaveSystem.Files.cs`
- `Assets/Scripts/Runtime/Persistence/StrategySaveSystem.Validation.cs`
- `Assets/Scripts/Runtime/Persistence/StrategySaveSystem.Validation.LooseResources.cs`
- `Assets/Scripts/Runtime/Persistence/StrategySaveSystem.Capture.cs`
- `Assets/Scripts/Runtime/Persistence/StrategySaveSystem.Apply.cs`
- `Assets/Scripts/Runtime/Persistence/StrategySaveSystem.Founding.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.Persistence.cs`
- `Assets/Scripts/Runtime/Core/StrategyGameBootstrap.Founding.cs`
- `Assets/Scripts/Runtime/Build/StrategyBuildPlacementController.Persistence.cs`
- `Assets/Scripts/Runtime/Build/StrategyConstructionSite.Persistence.cs`
- `Assets/Scripts/Runtime/Population/StrategyPopulationController.Persistence.cs`
- `Assets/Scripts/Runtime/Population/StrategyResidentAgent.PersistenceResources.cs`
- `Assets/Scripts/Runtime/Map/StrategyFogOfWarController.Persistence.cs`
- `Assets/Scripts/Runtime/Map/StrategyTrailController.Persistence.cs`
- `Assets/Scripts/Runtime/Map/StrategyPointOfInterestController.Persistence.cs`
- `Assets/Tests/EditMode/StrategySaveSystemTests.cs`
- `Assets/Tests/EditMode/StrategySaveLooseResourceTests.cs`

Impact hints:

- Current persistence is version 5 with v1/v2/v3/v4 migration. Increment the version and add an explicit migration whenever persisted DTO shape changes; validate migrated data before applying it.
- Keep primary/temp/backup replacement and backup recovery in the file seam so interrupted writes do not destroy the last valid save.
- Reject save files above 32 MiB before reading and keep top-level plus resident-child/prepared-dish/point-of-interest collection limits in validation.
- Stable IDs are serialization contracts; never replace them with scene-instance IDs or object references.

### Verification

Responsibilities:

- Run deterministic logic checks from the Unity Editor.
- Enforce source/config quality and assembly/project-file parity before Unity verification.
- Enter real Play Mode to verify menu isolation, prepared menu-to-founding-to-gameplay launch with founding-layout invariants, and direct gameplay bootstrap.
- Apply wall-clock progress/stall watchdogs and collect unexpected Unity runtime errors with only narrow batch-mode infrastructure exceptions.
- Verify explicit map seeds and procedural plus production-16px catalog terrain golden output.
- Run a 45-game-second `QuickSoak` covering 3 in-game hours for pull requests and `main`, writing `Logs/QuickSoakSmoke.txt`.
- Run the deterministic 720-game-second full soak covering 2 in-game days only for the nightly 01:23 UTC schedule, manual `workflow_dispatch`, `release`/`release/**` branches, and `v*` tags; keep its modal input-context invariants, final/peak memory budgets, and `Logs/SoakSmoke.txt` evidence.
- Render the menu at 1600x900 for visual layout inspection.
- Render deterministic Noon, Spring, Autumn, Night, and Winter gameplay frames for visual comparison on a real graphics device.

Primary files:

- `Assets/Editor/StrategyVerificationRunner.cs`
- `Assets/Editor/StrategyVerificationRunner.Errors.cs`
- `Assets/Editor/StrategyVerificationRunner.Visual.cs`
- `Assets/Editor/StrategyVerificationRunner.Input.cs`
- `Assets/Editor/StrategyVerificationRunner.Map.cs`
- `Assets/Editor/StrategyVerificationRunner.Quality.cs`
- `Assets/Editor/StrategyVerificationRunner.Soak.cs`
- `Assets/Editor/StrategyVerificationRunner.Watchdog.cs`
- `Assets/Editor/StrategyVerificationRunner.FoundingJourney.cs`
- `Assets/Tests/EditMode/`
- `Tools/Verification/Invoke-TechnicalGates.ps1`
- `Tools/Verification/Invoke-UnityVerification.ps1`
- `.github/workflows/technical-gates.yml`
- `Logs/EditModeVerification.txt`
- `Logs/MainMenuSmoke.txt`
- `Logs/MainMenuLaunchSmoke.txt`
- `Logs/PlayModeSmoke.txt`
- `Logs/GameplayVisualCapture.txt`
- `Logs/QuickSoakSmoke.txt`
- `Logs/SoakSmoke.txt`

Impact hints:

- Extend deterministic/NUnit checks when calendar, resources, resident priority, lifecycle, input, persistence, logging, or refugee balance changes.
- Keep Play Mode checks focused on scene ownership and system readiness so they remain reliable in batch mode.
- Let fast PR/main concurrency cancel stale superseded runs, but never cancel an in-progress full-soak run.
- Unity verification deletes stale artifacts, requires fresh PASS/XML output, and must refuse to start while the same project is open in a main Editor process.
- Do not run gameplay `Camera.Render()` capture with `-nographics`; URP requires a non-Null graphics device for this path.

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

- No zoning owner is present yet.
- Victory and defeat outcome ownership is intentionally deferred.

When a new system appears, add an owner card with responsibilities, primary files/assets, and impact hints.
