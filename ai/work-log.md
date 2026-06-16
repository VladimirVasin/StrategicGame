# Work Log

Last updated: 2026-06-16

## Active

- None.

## Done

### 2026-06-16 - Production input logistics through Storage Yard

- Added `IStrategyProductionLogisticsNode` so production buildings can expose non-food input requests and output pickup requests without making production workers move resources between buildings.
- Sawmill now requests input `Logs` through Storage Yard Haulers; Sawyers only work when Logs are already buffered at the Sawmill and no longer fetch Logs from Storage Yards or Lumberjack Camps.
- Storage Yard now reserves production-input deliveries from local stock, tracks those reservations separately from construction/logistics reservations, and counts production-input backlog for auto workforce logistics demand.
- Construction no longer reserves or picks up Stone directly from Stonecutter Camps; Stone must be hauled into Storage Yards before it can satisfy construction reservations.
- `Logs` is now a technical `StrategyResourceType` with a runtime resource icon/title for shared logistics/debug fallback paths.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; full `.cs` line-count scan found no files over 500 lines; `git diff --check` reported no whitespace errors and only the existing CRLF normalization warning for `Assembly-CSharp.csproj`.

### 2026-06-16 - House Stone cost and Sawyer autoassign stability

- House construction cost is now explicitly `2 Logs / 3 Stone`, bypassing the shared Stone multiplier so the player-facing House Stone cost lands exactly on 3.
- Analyzed `debug.log`: `Sawyer` workers were assigned for `planks_shortage`, then repeatedly cleared by `AutoWorkforceReleasedForDemand sourceProfession=Sawyer` before any `PlanksStoredAtSawmill` event occurred.
- Demand-driven auto workforce rebalancing now refuses to use a profession as a donor while its assigned count is at or below its desired target, so target-filled production roles like `Sawyer` can finish their work cycle instead of being stolen by Construction/Logistics/Wood/Stone demands.
- Rebalance locks now use real time rather than scaled time, so x2/x3 simulation speed no longer shortens the lock window in real seconds.

### 2026-06-16 - Residents HUD sorting and auto workforce rebalance lock

- Residents roster column headers are now clickable on every filter tab; each click sorts the visible rows by that column and toggles ascending/descending order with a compact header indicator.
- Moved roster sort/header behavior into `StrategyPopulationRosterHudController.Part02.cs` so the main roster controller stays below the 500-line limit.
- Analyzed `debug.log`: Lumberjacks were repeatedly assigned for `logs_shortage`, then immediately released by the next tick's Construction/Logistics demands, causing a Wood/Builder/Hauler role oscillation.
- Demand-driven auto workforce rebalancing now temporarily locks both the donor and target professions after a forced transfer, preventing the next few ticks from stealing the same workers back and forth.

### 2026-06-16 - Stone construction cost tuning

- Build catalog construction costs now apply a shared 1.2x multiplier to Stone requirements.
- Integer rounding keeps tiny Stone costs stable when a 10-20% increase would otherwise be impossible without a much larger jump; Stone costs of 3+ now rise through the shared helper.
- No Stone processing/resource chain was added.

### 2026-06-16 - Auto workforce shortage rebalance

- Analyzed `debug.log`: auto workforce repeatedly detected `Lumberjack` demand from `logs_shortage`, but every tick ended with `freeAdults=0`, `released=0`, `assigned=0`, so no worker could move into wood production.
- Auto workforce now has a limited demand-rebalance pass: when no free adult is available, a higher-scored demand can release an auto-managed worker from a lower-priority held role through the normal worksite unassign API and immediately reuse the worker if they become idle.
- Resource shortages now receive a shared priority bonus for Food, Wood, Stone, Planks, Iron, and Coal, so real shortages outrank ordinary logistics backlog instead of only boosting Logs.
- Added `AutoWorkforceReleasedForDemand`, `AutoWorkforceRebalanceSkipped`, and `demandReleased` diagnostics to make future `debug.log` analysis explicit.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; full `.cs` line-count scan found no files over 500 lines.

### 2026-06-16 - Wildlife structure avoidance

- Land wildlife now treats placed buildings, active construction sites, and the campfire as structure buffers: deer/rabbit/wolf spawn, birth, migration, relaxed targets, flee targets, wolf roam, and wolf prey lookup avoid cells within 4 cells of those structures.
- Deer, rabbits, and wolves now use the same wildlife travel predicate for local BFS, so routes avoid passing through settlement buffers while still allowing River crossings and allowing an animal already inside a buffer to escape.
- Wolf target acquisition now only reserves rabbit/deer surplus prey and no longer falls back to ordinary resident targeting when animal surplus is unavailable.
- Added throttled `WolfPreySearchSkipped` diagnostics plus `targetTravelSafe` in wolf path-failure logs, and split new wildlife/wolf helpers into small partial files to preserve the 500-line C# limit.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; full `.cs` line-count scan found no files over 500 lines; `git diff --check` reported no whitespace errors and only the existing CRLF normalization warning for `Assembly-CSharp.csproj`.

### 2026-06-16 - Auto workforce target-count assignment fix

- Auto workforce priority numbers now act as desired worker targets for available compatible worksites instead of only creating assignments during stock shortages.
- Food, Wood, Stone, Planks, Iron, Coal, Logistics, and Construction demands now fill available vacancies up to the configured target when buildings can currently accept work.
- Shortages still affect demand score/urgency, but zero shortage no longer prevents unemployed residents from filling below-target professions.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-16 - Auto workforce surplus rebalance

- Auto workforce now computes desired worker targets for all managed professions, not only free-worker demand.
- Each auto tick releases surplus Lumberjacks, Stonecutters, Miners, Coal Miners, Sawyers, Hunters, Fishers, Haulers, and Builders through the normal worksite unassign APIs before filling higher-scored current demands.
- Construction worker targets are capped by the Construction priority value, so default `Construction = 4` no longer auto-grows into 6+ Builders when multiple sites exist.
- Released workers are only reused immediately when they are truly idle; workers returning carried resources become normal candidates on a later tick after the return flow finishes.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; full `.cs` line-count scan found no files over 500 lines.

### 2026-06-16 - Marriage surname update on household pairing

- Adult male/female household pair formation now applies the husband's family name to the wife when the pair first occupies a House or when a partner moves into a single-resident house.
- Added a resident-level family-name change helper that preserves the given name, updates the GameObject name, refreshes the persistent family record, and logs `MarriageSurnameChanged`.
- Biological parent/child IDs are unchanged, so Family Trees keeps birth-family links even after the visible current surname changes.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; full `.cs` line-count scan found no files over 500 lines; `git diff --check` passed with only the existing CRLF normalization warning for `Assembly-CSharp.csproj`.

### 2026-06-16 - Family Trees relationship hover labels

- Added always-visible gender symbols to Family Trees member cards.
- Hovering a member card now shows relationship labels directly inside other recorded relatives' cards, including parent, child, sibling, co-parent/spouse inferred from shared children, grandparent, grandchild, aunt/uncle, niece/nephew, cousin, and generic kin fallback.
- Family columns still size dynamically from generation row width and depth, and family-column order now uses a relationship-affinity pass so more closely linked family groups can appear next to each other when cross-family records exist.
- Added `StrategyFamilyTreeHudController.Part03.cs` for relationship and affinity logic so Family Trees source files remain below the 500-line limit.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-16 - Family Trees column layout

- Family Trees now lays family groups out as left-to-right family columns instead of vertical full-width bands.
- Each family column uses compact generation rows, keeping parents on the same row and children on lower rows so parent-child links remain meaningful.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-16 - Compact Family Trees cards

- Corrected the Family Trees family section width so ordinary family groups render as compact vertical cards instead of full-width horizontal bands.
- Family groups now use compact column widths so the bottom scrollbar is used to browse across families.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-16 - Family tree card layout and deceased markers

- Family Trees now has permanent vertical and horizontal scrollbars, leaving clear bottom/right scrollbar space around the viewport.
- Family sections render as framed cards, preparing the layout for future visual links between related families.
- Deceased relatives remain visible in the trees with muted monochrome cards, greyed portraits/text, and a generated pixel skull marker.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; full `.cs` line-count scan found no files over 500 lines.

### 2026-06-16 - Family tree surname collision fix

- Family Trees now groups recorded residents by connected kinship components instead of unique family-name strings, so unrelated families with the same surname render as separate trees.
- Duplicate surname trees receive numbered headers such as `Wintermere family 1` and `Wintermere family 2`.
- New startup and refugee family blocks now reserve surnames from the least-used name pool, avoiding repeats until all built-in family names have been used.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; touched `.cs` files stayed below 500 lines.

### 2026-06-16 - Family tree scene HUD

- Added a fullscreen modal `StrategyFamilyTreeHudController` opened from the Residents HUD through a `Family Trees` button.
- Family Trees uses `StrategyTimeScaleController` pause locks while open, supports Escape/Back close, and provides horizontal/vertical scrolling for large family layouts.
- The tree view groups recorded members by connected kinship components, lays them out by generation, draws parent-child connection lines, and shows portrait/name/status cards for living and deceased family members.
- Raised the Family Trees canvas above the Profession HUD, made the modal background opaque, added a permanent right-side vertical scrollbar, increased smooth scroll responsiveness, and added center-pivot hover scaling for member cards.
- Moved `StrategyResidentFamilyRecord` into its own file and extended family records with full name, age, life stage, and visual variant data so deceased relatives can still appear in family trees with portraits.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; full `.cs` line-count scan found no files over 500 lines.

### 2026-06-16 - Population roster HUD

- Added a runtime `StrategyPopulationRosterHudController` opened from the top population bar.
- The roster shows settlement-wide resident stats plus filterable resident rows with name, age, home/camp state, role, current status, and food status.
- Added shared `StrategyResidentHudText` formatting so the roster and selection HUD use the same resident role/status labels; logistics residents now display as `Hauler` instead of split storage/granary worker labels.
- The compact top population HUD remains the count display, but its population panel is now clickable and toggles the larger roster HUD.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; full `.cs` line-count scan found no files over 500 lines.

### 2026-06-16 - Smart auto workforce assignment

- Added `StrategyAutoWorkforceController` with demand scoring for construction, food, logistics, Wood, Stone, Planks, Iron, and Coal priorities.
- Auto workforce ticks every few seconds, gathers eligible free adults, scores work demands by player priority, shortage, urgency, storage backlog, construction readiness, and resident distance, then assigns the nearest free adults through existing worksite APIs.
- Profession HUD now includes an `Auto Assign` toggle plus compact priority steppers; manual worker removal creates a short profession override so auto-fill does not immediately undo the player's action.
- Auto builders are hired through Storage Yards and then dispatched by the existing balanced construction-site builder routing; Haulers remain the single logistics profession for storage resources and Granary food hauling.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; full `.cs` line-count scan found no files over 500 lines; no duplicate `.csproj` compile items were found; `git diff --check` reported only the existing CRLF normalization warning for `Assembly-CSharp.csproj`.

### 2026-06-16 - Land wildlife river swimming

- Added river-crossing support for land wildlife without changing global map walkability: deer, rabbits, and wolves can path through generated River water cells, while Lake water remains water-only.
- Deer/rabbit/wolf movement now slows while crossing river cells and uses a swimming visual: slower movement animation plus a generated water-ripple child sprite.
- Wildlife migration connection checks now treat River water as passable so herds, rabbit groups, and wolf packs can migrate across both river sides while still choosing land cells as homes/targets.
- Added `StrategyWildlifeRiverCrossing.cs` plus small deer/rabbit/wolf swim partials; kept all `.cs` files at or below the 500-line limit.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-16 - Wolf population-control hunting

- Analyzed `debug.log`: wolves were not movement-stalled, but were continuously hunting available rabbits/deer and killed 18 rabbits plus 8 deer in about 150 seconds of logged simulation.
- Changed wolf prey reservation so wolves only hunt rabbit/deer surplus above high control thresholds, subtracting active predator reservations and hunter-reserved rabbits from the available surplus.
- Added a short fast pounce phase: wolves stalk at normal stalking speed, then switch to `Chasing` only inside the pounce band and close at higher speed.
- Wolf pack spawn selection now alternates preferred river sides when a generated river route exists, with a logged fallback if one side has no valid safe candidate.
- Added `StrategyWildlifeController.Part09.cs` for wolf population-control prey lookup and river-side wolf-pack placement so source files stay below the 500-line limit.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; full `.cs` line-count scan found no files over 500 lines; `git diff --check` only reported the existing CRLF normalization warning for `Assembly-CSharp.csproj`.

### 2026-06-16 - Unity recovery import guard

- Moved the untracked `Assets/_Recovery` scene recovery folder out of `Assets` into a local ignored backup folder so Unity stops importing the stale recovery scene as a project asset.
- Added ignore rules for Unity `_Recovery` artifacts under `Assets` and the local `.unity-recovery-backup` folder.
- Investigation note: `Editor.log` showed `Assets/_Recovery/0.unity` imports before `Internal error - unexpected guid mismatch`, and no duplicate `.meta` GUIDs were found under `Assets`.

### 2026-06-16 - Wolf path refresh movement fix

- Fixed wolves visually stalling during stalking by removing the current-cell center from rebuilt path waypoints, so frequent target refreshes no longer pull the wolf back to its own cell center.
- Stalking wolves now move directly toward the target's exact world position only after the path has brought them into the same map cell as the target.
- Split resident death-drop helpers into `StrategyResidentAgent.Part34.cs` and moved a forestry shadow helper into the existing forestry partial so all `.cs` files stay within the 500-line limit.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; full `.cs` line-count scan found no files over 500 lines; `git diff --check` passed with only the existing CRLF normalization warning for `Assembly-CSharp.csproj`.

### 2026-06-16 - Death-dropped construction reservation restore

- Fixed a construction stall where a builder could die after picking up a reserved construction resource, causing the construction site to lose that reserved unit while the dropped resource was recovered as generic Storage Yard stock.
- Death-dropped loose construction resource piles now restore the carried resource reservation for the original construction site when that site still needs the resource.
- `CarriedConstructionResourcesDroppedOnDeath` now reports `reservation=restored`, `none`, or `unrestored` for diagnostics.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-15 - Single Sawyer and Chicken Coop cost tuning

- Reduced Sawmill staffing to 1 assigned Sawyer and centered the active worker inside the Sawmill.
- Added a Sawmill input-Logs buffer cap of 4 so the shared 6-resource production cap cannot deadlock the `1 Log -> 2 Planks` work cycle.
- Removed Logs from Chicken Coop upgrade costs; Chicken Coop now costs 1 Stone and 2 Planks.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-15 - Tree yield and production capacity tuning

- Small generated trees now yield 3 Logs, while large generated/planted mature trees yield 6 Logs, and lumberjacks still carry the full tree yield in one trip.
- Increased Large Tree visual scale and kept the leaf overlay aligned with the larger sprite scale.
- Raised the shared production-building local stock cap from 5 to 6 resources so all production worksites can hold a full large-tree Logs batch.
- Slowed Sawmill Log-to-Planks work by increasing the worker cycle duration and reducing the active saw overlay frame rate.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-15 - Balanced construction builder dispatch

- Replaced per-site builder dispatch that could assign every free builder to the requesting construction site with balanced dispatch across all active construction sites.
- Builder assignment now favors empty and lower-builder-count sites first, while still allowing extra builders to stack onto sites after active sites have coverage.
- Added `StrategyStorageYard.Part06.cs` for builder dispatch scoring and kept all touched `.cs` files below the 500-line limit.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-15 - Service burial fallback

- Funeral processes with no living family/household participants now skip mourning and use a service burial flow.
- Service burials pick one adult carrier randomly from the nearest eligible adult pool, move the corpse to a reachable grave, and suppress crying sprites during waiting/burial duty.
- Added `StrategyFuneralController.Part02.cs` for service-burial helpers and kept all touched `.cs` files below the 500-line limit.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-15 - Wolf movement diagnostics

- Added throttled wolf diagnostics for state changes, target acquisition/release, path readiness/failures, roam failures, and movement stalls while attempting path/direct movement.
- Wolf logs now include wolf entity id, pack id, state, current cell/world, target kind/name/cell/world, speed, and path index/count where relevant.
- Added `StrategyWolfAgent.Part03.cs` for diagnostics so all wolf source files remain below the 500-line limit.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; `.cs` line-count scan found no wolf files over 500 lines; `git diff --check` reported only the existing CRLF normalization warning for `Assembly-CSharp.csproj`.

### 2026-06-15 - Funeral participant death guard

- Added a centralized resident-death guard: residents in active funeral duty cannot die from annual mortality, malnutrition mortality, or wolf attacks until the funeral duty ends.
- The guard logs `ResidentDeathBlockedByFuneral` and leaves the funeral state untouched, preventing carrier death from freezing an active procession.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; `.cs` line-count scan found no files over 500 lines.

### 2026-06-15 - Mineral build blocking

- Iron and Coal deposit footprints now remain walkable but mark their cells as not buildable through a separate map buildability overlay.
- Normal buildings, bridge bank endpoints, starter storage placement, and house upgrades reject Iron/Coal build-blocked cells; Mine and Coal Pit placement can still use their matching mineral cells.
- Mineral generation now treats existing build-blocked mineral cells as occupied for future nature props, preventing multi-cell Iron/Coal deposits from receiving overlapping props.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; `.cs` line-count scan found no files over 500 lines.

### 2026-06-15 - Building microHUD removal

- World inspect microHUD no longer opens for placed buildings, construction sites, or house upgrades; those clicks use the right-side selection HUD only.
- Kept resident, grave, resource, nature-prop, wildlife, and loose-pile inspect behavior intact.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; `.cs` line-count scan found no files over 500 lines.

### 2026-06-15 - Construction crews, haulers, and population event log

- Construction sites no longer cap active assigned builders at 2; all available hired Storage Yard builders can be dispatched, and selected construction sites now draw linked-resident markers/lines for assigned builders.
- Hunter, fisher, lumberjack, and stonecutter target lookup now chooses the nearest available map target instead of rejecting everything outside the old work radius; fisher cast validation now keeps fish range checks active during waiting/reeling.
- Added a compact top event log for births, deaths, and child adoption events.
- Added orphan adoption: minor children with no living parents can be adopted into an adult household, preferring their current home or close relatives before the nearest eligible house.
- Replaced the player-facing `Storekeepers`/`Granary Workers` split with one `Haulers` profession; Storage Yard haulers move both storage resources and food to Granaries.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; `.cs` line-count scan found no files over 500 lines.

### 2026-06-15 - Younger starter-family ages

- Reduced starter parent ages so initial families no longer begin close to the old-age mortality band.
- Starter fathers now spawn around ages 34-40, starter mothers around ages 33-38, and adult children around ages 16-18 while preserving a minimum 17-year parent-child age gap.
- Kept the 1-2 adult children per starter family rule unchanged.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; `.cs` line-count scan found no files over 500 lines; `git diff --check` passed.

### 2026-06-15 - Slightly faster active resident movement

- Added a 15% movement-speed multiplier for residents while they are in non-idle activities.
- Kept `Idle` and `TendingHousehold` movement at the existing base speed so casual wandering around home/camp does not speed up.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; `.cs` line-count scan found no files over 500 lines; `git diff --check` passed.

### 2026-06-15 - Gentler pre-50 mortality curve

- Reduced the base annual mortality curve so residents stay much safer before age 50.
- The base annual risk is now about 0.02% at age 1, 0.2% at age 40, and 4% at age 50, with faster capped old-age growth after 50.
- Kept the annual age-tick system and malnutrition mortality multipliers unchanged.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; `.cs` line-count scan found no files over 500 lines; `git diff --check` passed.

### 2026-06-15 - Starter families with adult children

- Changed startup population generation from 6 unrelated adults to 3 family blocks.
- Each starter family now spawns with a father, a mother, and 1-2 adult children, all sharing a family name and parent/child links.
- Parent ages are generated older than the adult children so initial kinship is plausible, and the camp fallback spawn ring now accounts for up to 12 starting residents.
- Split startup-family spawning into `StrategyPopulationController.Part05.cs` to keep source files below the 500-line limit.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; `.cs` line-count scan found no files over 500 lines; `git diff --check` passed.

### 2026-06-15 - Wildlife, mineral-field, and fishing fixes

- Fixed wolf chase behavior so wolves continue moving toward the exact target position after reaching the target's map cell, preventing stalled hunts beside prey.
- Changed Iron and Coal generation to use multi-cell walkable underground fields instead of single-cell spots, scale their visual surface marks by footprint, and reject adjacent opposite-mineral fields.
- Tightened Fisher Hut stand-cell selection to land/shore cells with orthogonal adjacent water and added fisher cast validation so fishers abandon a cast if the reserved fish leaves cast range before the hook sequence.
- Moved fisher cast/reel/deposit methods into `StrategyResidentAgent.Part33.cs` to keep source files below the 500-line limit.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; `.cs` line-count scan found no files over 500 lines; `git diff --check` passed.

### 2026-06-15 - Planks in late construction costs

- Extended construction resource costs, reservations, loose construction piles, builder carry/delivery states, cancellation cleanup, death drops, and storage-worker recovery to support Planks alongside Logs and Stone.
- Added Plank costs only to later-stage options: Mine costs 5 Logs/5 Stone/3 Planks, Coal Pit costs 4 Logs/4 Stone/2 Planks, and Chicken Coop currently costs 1 Stone/2 Planks.
- Left starter buildings and Sawmill construction without Plank requirements so the Plank production chain can be established before Planks are required.
- Updated construction and upgrade HUD/status text to display only non-zero resource costs, including Planks when needed.
- Split construction/logistics/resident helper methods into new partial files so runtime `.cs` files remain at or below the 500-line limit.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; `.cs` line-count scan found no files over 500 lines; `git diff --check` passed.

### 2026-06-15 - Production stock caps and shared haulers

- Added a shared `StrategyProductionStorage` local stock cap for production buildings; the current cap is 6 resources.
- Lumberjack Camps, Stonecutter Camps, Mines, Coal Pits, Hunter Camps, Fisher Huts, and Sawmills now stop starting new production when their local stock would exceed the cap; Sawmill counts Logs, Planks, and pending Planks against the same cap.
- Storage Yards and Granaries remain uncapped storage buildings.
- Storage Yard workers now also service Granaries by hauling reserved `Game`/`Fish` from Hunter Camps/Fisher Huts or loose food piles to the nearest Granary.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; `.cs` line-count scan found no files over 500 lines.

### 2026-06-15 - Sawmill and Planks production chain

- Added player-buildable `Sawmill` under Production with procedural 2.5D art, stock visuals, and an animated sawing work overlay.
- Added `Planks` as a technical resource with HUD/resource icon, carried/loose visuals, Sawmill-local stock, and Storage Yard stock.
- Added `Sawyer` as a Profession HUD role; assigned sawyers fetch reserved Logs from Storage Yards or Lumberjack Camps, carry them into the Sawmill, saw them into Planks while visible inside the building, and store Planks at the Sawmill.
- Storage Yard workers now reserve Planks from Sawmills, haul them to Storage Yards, and return/drop Planks safely during cancellation/death cleanup.
- Split new Sawmill/resident/selection/storage behavior into partial files to keep `.cs` source files at or below 500 lines.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; `.cs` line-count scan found no files over 500 lines; `git diff --check` passed.

### 2026-06-15 - Coal Pit mining and Coal logistics

- Added player-buildable `Coal Pit` under Production with procedural 2.5D art, 2x2 footprint, Coal-under-footprint placement validation, and completed-building `StrategyCoalPit` worksite setup.
- Added `Coal Miner` as a Profession HUD role; assigned coal miners enter the pit, remain visible inside the pit while working, mine reserved underground Coal deposits, and add Coal to the pit's local stock.
- Storage Yard workers now reserve Coal from Coal Pits, haul it to Storage Yards, and Storage Yard HUD/visual stockpiles include Coal alongside Logs, Stone, and Iron.
- Tightened Mine placement/extraction to require an available underground Iron deposit under the Mine footprint, matching the Coal Pit deposit rule.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; `.cs` line-count scan found no files over 500 lines; `git diff --check` passed.

### 2026-06-15 - Coal resource groundwork

- Added `Coal` as a technical `StrategyResourceType` with a generated HUD/resource icon, but did not connect it to food, construction costs, storage, logistics, worker production, or Mine behavior.
- Added runtime Coal resource registry/deposit components and generated walkable underground Coal indicators as dark dust ground and coal seam markings during nature generation.
- Coal deposits are inspectable and registered technically, but are explicitly not mineable yet and do not block walkability.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; `.cs` line-count scan found no files over 500 lines; `git diff --check` passed.

### 2026-06-15 - Mine building and Miner profession

- Added player-buildable `Mine` under Production with procedural 2.5D art, 2x2 footprint, Iron-access placement validation, and completed-building `StrategyMine` worksite setup.
- Added `Miner` as a Profession HUD role; assigned miners enter the mine, become hidden underground while working, mine reserved underground Iron deposits, and add Iron to the mine's local stock.
- Storage Yard workers now reserve Iron from Mines, haul it to Storage Yards, and Storage Yard HUD/visual stockpiles include Iron alongside Logs and Stone.
- Selection HUD, resident final-profession cleanup, death/drop cleanup, carried Iron visuals, and `.csproj` entries were updated for Mine/Miner/Iron logistics.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; `.cs` line-count scan found no files over 500 lines; `git diff --check` passed.

### 2026-06-15 - Iron resource groundwork

- Added `Iron` as a technical `StrategyResourceType` with a generated HUD/resource icon, but did not connect it to food, construction costs, storage, logistics, or worker production.
- Added runtime Iron resource registry/deposit components and generated walkable underground Iron indicators as rust-stained ground and shallow vein markings during nature generation.
- Iron deposits are inspectable and registered technically, but are explicitly not mineable yet and do not block walkability.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors; `.cs` line-count scan found no files over 500 lines.

### 2026-06-15 - C# 500-line rule recorded

- Added the ongoing 500-line hard limit for `.cs` source files to `AGENTS.md`, `ai/README.md`, and `ai/prompt-templates.md`.
- Updated stable AI memory to describe `.PartNN.cs` partial files as the current same-owner structure for files that would otherwise exceed the limit.
- Verification: runtime `.cs` line-count scan found no files over 500 lines.

### 2026-06-15 - Runtime class line-limit partial refactor

- Split oversized runtime C# classes into same-owner `.PartNN.cs` partial files so every runtime script file stays at or below 500 lines.
- Kept behavior unchanged; this is a physical file-size refactor, not a semantic service extraction.
- Updated `Assembly-CSharp.csproj` and added Unity `.meta` files for the generated partial scripts.
- Verification: runtime `.cs` line-count scan found no files over 500 lines, and `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-15 - Householder Granary food pickup

- Householders can now fetch one reserved `Fish` or `Game` unit from the nearest reachable Granary when their home's ration value is below the household reserve target.
- Houses can store local `Fish` and `Game`; household ration consumption and selected-house HUD now include those resources alongside crops, Eggs, and forage.
- Granaries now reserve household pickup food separately so daily ration fallback consumption does not eat a unit already claimed by a householder.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-15 - Food resource ration values

- Added resource-specific ration values so one food unit no longer equals one daily ration: light crops/forage contribute less, Fish contributes more, and `Game` is the strongest current food.
- Household daily ration resolution now consumes house-local food and Granary food by ration value while still tracking consumed physical units for HUD/debug context.
- Selected house and Granary HUD text now surfaces stock units alongside total ration value.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-15 - Daily household ration and resident hunger state

- Reworked household food from periodic household starvation into one evening daily ration resolved from the day/night cycle after a one-day settling grace.
- Resident food needs now scale by life stage; short rations create per-resident nutrition debt and hungry/starving status for house and resident HUDs.
- Houses consume local Eggs/crops/forage first, then Granary `Game`/`Fish`; sustained shortages block births and resident malnutrition severity drives mortality multipliers.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-15 - More distributed compact wildlife groups

- Rebalanced wildlife generation toward more numerous smaller groups: deer now use up to 8 compact herds, rabbits up to 10 compact groups, lake fish up to 12 compact shoals, and wolves 3-4 compact packs.
- Kept only the first few rabbit groups near the starter camp for early hunting while allowing later rabbit groups to distribute map-wide.
- Removed the deer initial-spawn dependency on the rabbit near-camp maximum distance, so deer avoid the camp but can populate suitable habitat across the map.
- Added per-herd and per-shoal reproduction caps for deer and lake fish, matching the existing per-rabbit-group cap pattern.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-14 - Wildlife group migration

- Added controller-owned migration state for deer herds, rabbit groups, wolf packs, decorative birds, and lake fish shoals.
- Wildlife groups now periodically pick new habitat centers and move toward them in gradual retargeting steps instead of staying permanently bound to their initial spawn zone.
- Land migrations avoid dense settlement pressure and use a short walkable-connection check so herd/pack centers do not jump through unwalkable water or blockers.
- Wolves now migrate around their current pack roam center rather than being softly pulled back toward the original den cell.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-14 - World inspect performance fix

- World inspect microHUD no longer opens for empty terrain cells; empty clicks hide the microHUD instead of showing map-cell data.
- Removed the new mass trigger-collider path for non-selectable inspect objects to avoid expanding 2D Physics workload.
- Non-selectable inspect objects are now resolved from visible `SpriteRenderer.bounds` only at click time; existing selection colliders remain limited to residents, buildings, construction sites, graves, and pre-existing selectable agents.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-14 - World inspect microHUD

- Added a bottom-right world inspect microHUD that updates on left-click while leaving the existing right-side selected-object HUD unchanged.
- Added a shared `IStrategyWorldInspectable` contract plus static inspectable support for non-selected world props.
- Clicks show object info for residents, inspectable objects, buildings, construction sites, and graves.
- Trees, bushes/forest thickets, Stone deposits, forage nodes, house upgrades, loose resource piles, chickens, deer, rabbits, fish, birds, and wolves provide inspect data.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-14 - General carried-resource death drop

- Resident death now drops all currently carried resources instead of only construction Logs/Stone.
- Logs and Stone still use loose construction resource piles so builders and storage workers can recover them through the existing construction/logistics flow.
- Game, Fish, Berries, Roots, and Mushrooms now drop as loose carried-resource piles; Granary workers recover Game/Fish, and household foragers recover nearby forage food for their home.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-14 - Construction carried-resource death drop

- Residents who die while carrying Logs or Stone now drop those materials as loose construction resource piles at the death cell instead of losing them during death cleanup.
- Death-dropped construction piles restore the previous construction reservation binding when the original site still needs that resource; otherwise, any still-needing construction site can reserve them again.
- Construction pickup search now falls back to dynamically reserving a nearby free loose pile after existing reserved loose piles, Storage Yards, and Stonecutter Camp Stone are unavailable.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-14 - Stone logistics construction fallback

- `debug.log` showed Stone was mined into Stonecutter Camp local stock while build affordability stayed at Storage Yard `Stone: 0`.
- Stonecutter Camps now expose reserved Stone as a construction-resource source, so builders can pick up Stone directly from camp stock when a construction site reserved it.
- Construction affordability/reservation now includes available Stone from Stonecutter Camps in addition to Storage Yard stock and loose construction resource piles.
- Storage workers now prioritize Stone hauling when Stone is needed by active construction or when yard Stone stock is lower than Logs, preventing perpetual Logs-first starvation.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Building selection resident links

- Selecting a completed building now also shows a lightweight world overlay for its linked residents without changing the selected-object HUD.
- Houses link to current residents; worksites link to assigned workers, and Storage Yards link to both storage workers and builders.
- Linked residents get a small gold ground marker and a thin dynamic line from the selected building anchor to the resident.
- Selection links update while the building remains selected, so worker assignment/removal and resident cleanup are reflected without reopening the HUD.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Wolf packs predator MVP

- Added autonomous wolf packs to the wildlife generator with safe land spawning away from the startup camp and dense settlement pressure.
- Added generated 2.5D wolf sprites and a wolf agent with idle, roaming, stalking, chasing, attack, feeding, avoiding-settlement, resting, and howling states.
- Added predator reservation/death hooks to rabbits and deer so wolves can hunt them without conflicting with hunter reservations.
- Wolves can rarely reserve vulnerable adult residents away from settlement safety and trigger normal `wolf_attack` resident death/funeral flow through `StrategyPopulationController`.
- Updated `Assembly-CSharp.csproj` and Unity `.meta` files for the new wolf runtime scripts.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Household foraging MVP

- Added generated forage nodes for Berries, Roots, and Mushrooms using seeded terrain placement, starter-area guarantees, procedural node sprites, reservations, depletion, and timed regrowth.
- Added house-local forage food resources/icons and included them in household food consumption before Granary food.
- Houses now attach a household foraging state that dispatches non-householder, unemployed adults and older children during daytime to gather forage and carry it back to their own home.
- Resident foraging has explicit move/gather/carry/deposit states, visible carried baskets, cancellation cleanup for death/funerals/home changes, and debug events for node/resident flow.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Gradual fishing reel-in animation

- Fish hooked by Fisher Hut workers now keep hook start, reel target, and reel progress state instead of resolving catch purely by reel hit count.
- Resident fishing animation now sends reel pulls toward a near-shore rod target while the fishing line follows the fish's current hook point.
- Hooked fish visibly move toward the fisher over multiple reel pulls, keep thrashing while pulled, and only become caught once fully reeled in and close to the target.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Rabbit group population cap

- Added a per-group rabbit population cap so a single rabbit group cannot grow past 5 living rabbits through reproduction.
- Initial rabbit generation now uses the same group cap instead of a hard-coded group-size limit.
- Rabbit population debug output now includes the group cap and per-birth group count.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Construction material reservation return fix

- `debug.log` showed a Stonecutter Camp stuck at 3/4 Stone after a builder picked up reserved Stone, was removed from Builder duty, returned that Stone to the Storage Yard, and the returned Stone became generic stock instead of the construction site's reserved stock.
- Residents now remember the construction site/resource for carried construction Logs/Stone and restore that reservation when returning the material to a Storage Yard or loose construction pile.
- Cleared stale carried-resource return state when a resident is still in a return activity but no longer carries Logs, Stone, Game, or Fish, preventing repeated zero-resource retry warnings.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Rain visual readability fix

- Replaced the first full-map low-resolution rain texture with a pooled camera-area rain-drop renderer.
- Rain now renders as thin moving diagonal streaks influenced by wind direction instead of large blocky checker/noise artifacts on terrain.
- The existing cloud-shadow, mist, wet-ground, water-ripple, wind, and audio weather integration remains unchanged.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Runtime weather effects MVP

- Added a runtime `StrategyWeatherController` with random Clear, Cloudy, LightRain, HeavyRain, Fog, and Storm states, smooth intensity transitions, weather-driven wind influence, and `Weather` debug events.
- Added procedural weather visuals for cloud shadows, rain streaks, mist/fog, and wet-ground darkening using dedicated world overlay sorting bands.
- Wired ambience rain/wind layers to the actual weather state instead of the previous hidden rain sine wave.
- Water animation now intensifies ripples and rain hits while raining or storming.
- Bootstrap now creates and configures weather before ambience audio so visuals, water, wind, and audio share one source of truth.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Chicken Coop production cycle sync

- Reworked Chicken Coop egg production from random independent delays into a cycle-driven production timer around 22 seconds with small per-cycle jitter.
- Chicken Coop upgrade animation now uses the same production progress as egg generation, so the final visual egg phase matches the moment Eggs enter the house resource store.
- Chicken Coop sprites now stage nest/egg visibility across production frames instead of looping unrelated animation frames.
- Added `ChickenCoopCycleStarted` and `ChickenCoopEggStored` debug events with house origin, coop origin, cycle length/progress, and egg count.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Clickable grave HUD

- Added `StrategyGraveMarker` so completed graves keep the deceased resident snapshot, click collider, epitaph, final profession, family role, and selection bounds.
- Resident death snapshots now preserve final profession and family role before assignment cleanup removes workplace/home context.
- `StrategyCemeteryController` now creates clickable grave marker components when graves are created.
- World selection can select graves and shows a right-side HUD with the deceased name, epitaph, age/profession/family role, and memory text.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Funeral corpse delivery and work assignment fix

- `debug.log` showed a second funeral carrier being assigned to construction one frame after starting `CarryingCorpseToCemetery`, so the funeral controller still counted him as a carrier while normal builder logic took over.
- Added resident work-assignment gating so residents in funeral activities cannot be assigned to professions, builders, or construction sites until funeral duty ends.
- Burial now checks that the corpse is actually near the reserved grave before starting burial; family gather timeouts can still prevent deadlocks, but they no longer teleport an undelivered corpse into a grave.
- Added `FuneralProcessionDeliveryFailed`, `BurialDelayed`, and `BurialRejected` debug events for future funeral-state diagnostics.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Cemetery founding hitch fix

- Optimized first cemetery selection so placed-building origins and the starter camp cell are collected once before scanning candidate grave cells.
- Removed repeated `FindObjectsByType<StrategyPlacedBuilding>()` calls from the per-cell cemetery scoring loop, which matched the death/funeral hitch seen in `debug.log` around `CemeteryFounded`.
- `CemeteryFounded` debug output now includes tested cell count, building count, and search milliseconds for future performance checks.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Professions HUD scroll fix

- Professions HUD list content now rebuilds its layout immediately after visible profession rows change.
- Added direct mouse-wheel handling while the pointer is inside the professions panel so the list scrolls reliably even when Unity UI scroll events are not delivered.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Rabbit spawn near starter camp

- Initial rabbit groups now search for group centers in a ring near the starter camp before any map-wide fallback.
- Rabbit spawn cells require a camp distance from 7 to 30 cells when the camp cell is known, keeping huntable rabbits close to the settlement without placing them directly on the campfire.
- Wildlife generation debug output now includes the configured rabbit camp max distance and logs a warning if no near-camp rabbit group center can be found.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Procedural world shadows

- Added a shared `StrategyShadowCaster2D` and procedural shadow sprite factory for soft/cast ground shadows on runtime world sprites.
- Completed buildings, construction sites, house upgrades, loose construction resource piles, forest groups, bushes, forestry trees, Stone deposits, and chickens now attach tuned runtime shadows.
- Day/night now exposes shadow opacity and length multipliers so shadows become shorter/stronger in daytime and longer/softer around dawn/dusk/night.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Accepted refugee family housing fix

- Accepted refugee families now first try to occupy an empty House as a whole family instead of only joining the generic homeless resident pool.
- If no empty House exists at acceptance time, the accepted family remains in camp idle and later gets priority for the next empty House before normal single-adult migration or random pair assignment.
- Reset accepted homeless residents to camp idle so stale refugee paths cannot keep pulling them away from the settlement.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Strict unexplored fog visibility

- Unexplored fog cells now render as fully black and no longer fade from low edge visibility strength before being marked explored.
- Explored-but-currently-hidden cells keep the dim grey fog state, while currently visible cells still fade toward clear.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-13 - Refugee arrival bank-side routing

- Refugee arrival now builds a reachable camp-side target set before choosing map-edge entry routes.
- Arrival target selection prefers the connected walkable component around the camp that already contains accepted residents, preventing families from arriving on the opposite river bank when no Bridge makes that side reachable.
- If a Bridge or other walkable route connects both banks, the normal pathfinder can still use that side because it is genuinely reachable.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Starter storage resource buffer

- Increased starter Storage Yard resources from 13 Logs / 9 Stone to 16 Logs / 12 Stone, adding a small early-game buffer above the exact first-build sequence.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Carried resource return on role removal

- Residents removed from a workplace while carrying Logs, Stone, Game, or Fish now keep the carried resource and first return it to the appropriate Storage Yard or Granary before continuing with a newly assigned role.
- Construction builders who are unassigned after taking reserved Logs/Stone now return those materials instead of clearing `carriedLogAmount` / `carriedStoneAmount`, preventing construction resources from disappearing.
- Added immediate safe storage/drop fallback for hard interruptions or unreachable storage so carried resources are preserved instead of silently zeroed.
- Selected-resident status text now shows the new carried-resource return activities.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Mortality acceleration age raised

- Resident annual mortality now keeps the low-risk curve until age 40 instead of age 30, reducing deaths in the 30-40 range.
- Kept the age-1 mortality start, about 30% annual risk by age 50, the late-age cap, and starvation mortality multiplier behavior unchanged.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Funeral dragging, grave gathering, and crying animation

- Corpse processions now drag the dead behind a primary carrier with a visible rope, clamping the corpse within one map-cell distance from the carrier instead of averaging between carriers.
- Funeral flow now enters a grave-gathering phase after carriers reach the reserved grave; burial starts only after all reachable expected attendees arrive or a timeout prevents a deadlock.
- Mourning and waiting funeral residents now use generated crying sprite frames for adult and child variants, while burial keeps its separate burial pose.
- Added debug context for expected burial attendees, rejected burial paths, grave gathering start, and burial start reasons.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Build menu single-item category visibility fix

- `Housing` no longer appears locked by house count when the single `House` item is unaffordable.
- Single-item build categories now still open their item tray so the player can see the tool card and resource cost.
- Unaffordable tools still cannot be activated by mouse or hotkey; the block is resource affordability only, not building count.
- Verified there is no implemented house-count cap; the only current 3-house threshold gates the first refugee arrival.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Delete confirmation, demolition, and construction cancellation

- Selected construction sites and completed buildings can now be acted on with `Delete`, opening a reusable confirmation dialog before destructive action.
- Cancelling a construction site releases its reserved occupied/walkability state, clears builders, releases unused Storage Yard reservations, and drops already delivered or carried Logs/Stone as visible loose construction resource piles at the site.
- Loose construction resource piles count toward construction affordability, can be reserved by new construction sites, can be picked up directly by builders, and can also be hauled back to Storage Yards by storage workers.
- Completed building demolition releases occupied/walkability cells; bridges also remove their river walkability span, and demolished houses detach residents safely.
- Construction completion now releases the temporary construction-site map blocker before creating the final building, preventing stale blocked cells after later demolition.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Householder garden duty state bugfix

- Householders now enter a real `TendingHousehold` resident activity instead of relying on `Idle` status text for home duty.
- Assigning a Householder clears external workplace/builder assignments through the owning worksite APIs, preventing stale worker slots while keeping the one-householder-per-house rule.
- Householders now prioritize their home's Garden Beds from `TendingHousehold`/`Idle`, use a cooldown instead of the old random-chance gate, and return to `TendingHousehold` after garden work completes.
- Added debug events for householder external-work cleanup, garden start/block reasons, and garden completion.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Funeral pathing and carried corpse bugfix

- `debug.log` showed Maerwynn Grimwald's funeral selecting a grave at `28,62` and dispatching carriers to it; the resident funeral movement path could fall back to a direct world-space path, which allowed river crossing without a Bridge.
- Funeral movement now fails with `ResidentFuneralMoveFailed` instead of using direct movement when no walkable grid path exists.
- Cemetery grave selection now filters candidates by cells reachable from the selected carriers, so spontaneous graves are not placed across an unbridged river for that funeral.
- Corpse processions now switch the corpse to a carried shroud/stretcher sprite above ground instead of sliding the lying death sprite along the terrain.
- Residents stuck in `WaitingAtFuneral` now auto-release after a timeout and funeral duty release is logged.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Lower young mortality

- Reduced the base annual mortality curve before the acceleration threshold from 0.2%-3.0% to 0.04%-0.8%.
- The acceleration threshold was later raised to age 40; age-1 mortality start and starvation mortality multiplier behavior stayed unchanged.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Automatic householder assignment

- Houses now automatically assign their oldest adult female resident as `Householder`; if no adult woman lives in the house, no householder is assigned.
- `StrategyResidentAgent.HasWorkplace` now treats the householder role as home work, and only the householder can perform Garden Beds work when she has no external workplace.
- Selected-house resident rows mark the `Householder`, and the selected-resident HUD shows `householder` as the resident role.
- Growing children trigger a householder refresh so newly adult women can take over the household role.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Household edible resources and garden harvest ticks

- House-local Eggs and crop resources now count as edible food and are consumed by `StrategyHouseholdFoodState` before any `Game`/`Fish` is taken from Granaries.
- Garden Beds now produce their selected crop on a fixed 8-second harvest timer, enough to sustain a two-adult household at the current food consumption rate.
- Garden Beds visuals now use growth-progress frames synced to the harvest timer; resident garden work boosts the current growth cycle instead of directly adding a free crop.
- Selected-house food HUD now shows home food and Granary food, and the fed detail reports how much of the last meal came from home stock versus Granary stock.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Default Garden Beds for houses

- Added a free default Garden Beds installation path in `StrategyBuildingUpgradeController` that reuses normal upgrade placement, sprite animation, crop selection, and house registration without spending Logs/Stone.
- Wired `StrategyBuildPlacementController` to install Garden Beds automatically whenever a House is placed/completed.
- Moved upgrade-controller bootstrap before placement configuration so default house upgrades are available for the first completed house.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Animal visual scale reduction

- Reduced generated deer, rabbit, fish, bird, and chicken sprites to 60% of their previous world size by increasing their sprite factory pixels-per-unit values.
- Scaled separate readability shadows, bird flight shadows, and fish surface ripples to match the smaller animal visuals.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - House resources HUD polish

- Replaced the selected-house Resources debug-style text line with a structured visual UI section.
- House Resources now shows a colored food status row, last-meal meter, Granary stock row, crop row with crop icon when available, and a clean empty state for local household stock.
- Household food state now exposes a structured `StrategyHouseholdFoodStatus` for UI instead of requiring the HUD to display compact debug text.
- Kept household resource entries filtered to amounts greater than 1.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Immediate family funeral recall

- Funeral creation now immediately recalls close family/household participants when a resident dies, instead of waiting for the corpse death animation or the funeral queue to become active.
- Recalled family members use the resident funeral movement hook, which temporarily cancels active work/construction tasks and releases active reservations while keeping workplace roles intact for after the funeral.
- Increased the close-family participant cap from 10 to 24 so larger related households can be recalled together.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Corpse self-motion visual bugfix

- Removed the fallback that moved a corpse toward the cemetery when no live carriers were available; funerals now delay/retry carrier selection instead of letting corpses visually travel by themselves.
- Made lying corpse death frames visually static by removing the subtle per-frame head offset that made corpses appear to breathe/move.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Debug-driven food, construction, cemetery, and fish-log fixes

- Added a household food grace/no-supply path so starvation does not activate before the settlement food chain exists; once Granary food has existed, later shortages still increase starvation.
- Replaced the early x16 starvation mortality curve with a gentler capped multiplier curve and blocked household births while a house is starving.
- Prevented free-house/one-resident family formation while that house is currently starving, without changing the one-family-per-house rule.
- Added per-builder construction pickup claims so multiple builders do not target the same reserved Logs/Stone unit; claims release on cancellation and resources are still physically removed only on pickup.
- Added resident path-start recovery from blocked cells plus more detailed construction pickup `no_path` logging and repeated-failure assignment drop.
- Tuned spontaneous cemetery placement to prefer a moderate settlement distance and penalize extreme map-edge cells.
- Throttled `FishLakeBirthBlocked` debug logging per lake region to reduce repeated cap spam.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Homebound toddlers

- Children younger than 3 years old now stay inside their assigned home instead of idling/walking outdoors.
- Homebound young children hide their world sprite/collider, keep their household/family records, skip funeral travel/activity calls, and reappear at a nearby walkable home exit once they turn 3.
- Selected-house resident rows now show the `inside home` status for these children.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Visual day/night cycle MVP

- Added `StrategyDayNightCycleController`, a runtime world overlay that cycles through dawn, day, dusk, and night over a 220-second scaled-time loop.
- The overlay renders above world sprites and below placement preview/fog/UI, so it tints the game world without darkening HUD panels.
- The controller also blends the camera background color with the same phase and logs major phase changes.
- Runtime bootstrap now creates/configures the day/night controller after camera setup.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Household food consumption and starvation mortality

- Added `StrategyHouseholdFoodState` to completed houses: each household periodically consumes food based on resident count and tracks starvation level when settlement Granary food is insufficient.
- Added settlement food consumption APIs to `StrategyGranary`; households consume nearby Granary stock, preferring `Game` before `Fish`, and Granary visuals update after food is eaten.
- Resident annual mortality now applies a household starvation multiplier (`x2`/`x4`/`x8`/`x16`, capped) when the resident's home is short on food.
- Selected-house HUD now shows compact household food status, last meal need/served values, settlement Granary food total, and crop info in the Resources block.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-12 - Looser construction-site placement validation

- Split normal building placement validation into strict construction foundation checks, softer final 2.5D blocker reservation checks, and a required nearby builder work-access check.
- Construction sites now block walking only on their technical foundation while reserving the future final blocker cells for placement collision; completed buildings still apply their full final 2.5D walk blocker.
- Added clearer placement rejection reasons for foundation, final-block reservation, and builder-access failures.
- Changed `CityMapController` runtime walkability blockers from a boolean layer to per-cell block counts so overlapping blockers cannot accidentally release each other when one temporary blocker disappears.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Temporary campfire walkability blocker

- The startup campfire now blocks its own map cell while burning so residents and animals path around the fire instead of walking through it.
- The campfire gradually burns out after a short delay, fades/shrinks away, destroys its visual object, and releases the campfire cell back to walkable.
- Added debug log events for campfire cell blocking, burnout start, cell release, and final extinguish.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Funeral and cemetery MVP

- Added death-to-funeral flow: resident death now creates an animated corpse snapshot instead of simply disappearing from the world.
- Added a funeral controller that waits for the death animation, gathers close family/household participants, runs mourning, moves a procession toward a spontaneous cemetery, and completes burial.
- Added runtime-generated corpse/death frames and grave sprites; completed burials create graves and mark grave cells as not walkable.
- Added temporary resident funeral activities so participants can attend mourning/procession/burial without permanently losing their workplace roles.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Resident mortality MVP

- Added annual resident mortality rolls starting at age 1, with a gentle youth curve, faster risk growth after the acceleration threshold, about 30% annual risk by age 50, and a capped late-age risk after that.
- Added centralized resident death cleanup through `StrategyPopulationController`: dead residents leave live population counts, homes, worksite roles, construction assignments, active reservations, selection HUD targets, and click colliders.
- Added persistent runtime family records so kinship checks still see dead parents/ancestors and close-relative couple blocking survives resident death.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Professions button top-HUD overlap fix

- Moved the `Professions` top button into the left HUD column below the x1/x2/x3 speed controls so it no longer overlaps the top population panel on narrow/scaled views.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Unity built-in font bootstrap fix

- Replaced the remaining runtime HUD `Resources.GetBuiltinResource<Font>("Arial.ttf")` calls in the top population HUD and refugee dialog with `LegacyRuntime.ttf`, matching Unity 6 built-in font requirements.
- Verification: `rg "Arial\.ttf" Assets/Scripts` found no remaining references, and `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - HUD speed buttons under resources

- Added compact x1/x2/x3 speed buttons directly beneath the top-left Logs/Stone resource panel in the Build HUD canvas.
- Exposed `StrategyTimeScaleController.SetRequestedScale` so F1/F2/F3 and the new HUD buttons share the same time-scale path and logging.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - English-first project language pass

- Translated runtime-facing strategy UI strings to English across the Build catalog, placement-created object names, selection HUD, profession HUD, refugee dialog, top population HUD, construction-site status, and worksite stock/status summaries.
- Updated AI collaboration rules and memory entry points with an English-first language standard for UI, debug/log labels, code comments, AI memory, documentation, commits, and future development notes.
- Replaced current AI memory references to Russian building/resource/profession names with English project terminology.
- Verification: project text scan for Cyrillic/escaped Cyrillic passed, and `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Bridge building and river crossing placement

- Added Bridge to the Build catalog under Infrastructure with Logs/Stone construction cost.
- Added two-click bridge placement: the first click selects a valid explored river-bank cell, the game highlights opposite-bank candidates across contiguous River water, and the second click creates a construction site.
- Bridge construction sites store custom span cells and use bank endpoint cells for builder delivery/build work so builders do not stand in water.
- Completed bridges store their span on the placed-building record and make River water cells walkable through a map bridge overlay without changing water kind.
- Added dynamic runtime bridge sprites plus staged bridge construction sprites sized to the selected span.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Resident adulthood age lowered

- Lowered the resident adulthood threshold from 18 to 16 years.
- Child-to-adult growth, adult/child population counts, worker eligibility, household migration, and partner search now all follow the same life-stage threshold.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - First refugee arrival gated by three houses

- Changed the refugee arrival schedule so the first family no longer uses a random initial timer.
- The first refugee family now starts as soon as the settlement has 3 completed registered houses.
- Repeat refugee families still use the existing randomized interval after the first family is accepted or rejected.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Refugee family arrival event and population counter

- Added a runtime refugee-arrival controller: a random family periodically spawns from a map edge, walks to the starter campfire, and opens a settlement decision dialog on arrival.
- Refugee families contain one adult man, one adult woman, and 1-3 random-gender children with shared family name, ages, visual variants, and parent/child kinship links.
- Added a modal refugee decision HUD; accepting registers the family as normal residents, rejecting sends them back off-map and destroys the temporary agents.
- Added pause-lock support to `StrategyTimeScaleController` so the decision dialog pauses simulation without letting F1/F2/F3 accidentally unpause it.
- Added a top status HUD population counter showing total residents, adults, and children.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Quieter footsteps and river ambience

- Reduced resident grass footstep one-shot volumes to `0.16` for adults and `0.095` for children.
- Reduced spatial river ambience target volume to `0.075` near the river.
- Added short custom `AudioReverbFilter` settings to resident footstep sources and the river ambience source for softer tails without a strong cave-like wash.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Profession HUD scroll

- Made the Profession HUD list visibly scrollable with a vertical scrollbar.
- Kept the header and bottom free-worker/status line outside the scrolling viewport so profession rows no longer get visually cut off behind the footer.
- Opening the panel resets the scroll position to the top.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Lake and river fish behavior split

- Added `CityMapController.RiverFlowDirection` so generated rivers expose a technical current direction; the water overlay now animates river flow along that direction while lakes keep the calmer local shimmer.
- Split fish behavior by habitat: lake fish belong to a lake water-region, stay on lake water, reproduce only inside that region, and count fry/adults against a hard per-lake cap.
- Added a single-timer river fish spawner: adult river fish spawn at the upstream route start, swim with the river current along the generated route, and despawn at the far end instead of reproducing.
- Fisher huts can still reserve both lake and river fish through the existing fishing API.
- Added structured debug events for fish water-region setup, lake birth cap blocks, river fish spawn, and river fish despawn.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - River and lake water identity

- Added `CityMapWaterKind` to map cells so generated water and shore cells are technically tagged as `River` or `Lake`.
- River water comes from the generated river corridor; lake water comes from generated water blobs.
- Added `CityMapCell.WaterKind`, `IsRiver`, `IsLake`, and `CityMapController` water-kind helper methods for future systems.
- Map generation debug logs now include river/lake water and shore cell counts.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Household second-child kinship fix

- Fixed household birth checks treating existing parents as close relatives after their first child was born.
- `StrategyKinshipUtility` now measures close blood relation through parent/ancestor chains and shared ancestors instead of traversing through shared descendants.
- This keeps parent/child, sibling, cousin, and other close blood-relative couple blocks while allowing the same adult house pair to have additional children while the house has free resident slots.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Louder resident footsteps

- Increased resident footstep one-shot volume from `0.075` to `0.32` for adults and from `0.045` to `0.2` for children.
- Reduced footstep spatial attenuation by lowering spatial blend and widening min/max distance so steps remain audible from the game camera.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Music focus pause/resume

- Changed `StrategyMusicController` so losing application/game focus pauses the current music `AudioSource` instead of advancing the playlist.
- Restoring focus resumes the same clip from the paused playback position via `AudioSource.UnPause()`.
- `Update` now ignores paused-for-focus music so `AudioSource.isPlaying == false` does not get mistaken for track completion during focus loss.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - House assignment ignores construction work

- Fixed completed houses rejecting homeless adults who were already hired as builders or assigned to active construction work.
- House-pair selection and later home migration now treat "free" as no current home/pair, independent from workplace or construction assignment.
- Assigning a home no longer cancels the resident's current work/construction task; idle residents still start walking to the new home immediately, while busy residents keep the home binding and return there after their current task flow allows it.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - In-game music volume and reverb

- Lowered `StrategyMusicController` target volume from `0.22` to `0.12`.
- Added a custom `AudioReverbFilter` to the in-game music source for a soft atmospheric reverb tail.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - In-game music playlist folder

- Added `Assets/Resources/Audio/Music/` as the drop folder for in-game music clips.
- Added `StrategyMusicController`, loaded by bootstrap, to play all AudioClips in `Audio/Music` as a random playlist.
- The playlist avoids repeating the same track twice in a row when 2+ tracks exist; with one track it naturally repeats after finishing.
- Added a folder instruction file with recommended names like `Music_01.mp3`, `Music_02.mp3`, and `Music_03.ogg`.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Gruzovichky ambience audio transfer

- Copied the non-generated `Gruzovichky` ambience assets into `Assets/Resources/Audio`: nature loops and grass walking footsteps.
- Added `StrategyAmbientAudioController` for runtime forest birds, cicadas, night, rain, wind, and spatial river ambience loaded from Resources.
- Added `StrategyResidentFootstepAudio` and wired residents to play quiet spatial grass footsteps on walk animation step frames.
- Runtime bootstrap now configures ambience audio after camera setup and logs loaded footstep clip count.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Unlimited storage workers/builders

- Removed the per-Storage-Yard assignment caps for storage workers and hired builders; both roles are now limited by available adult residents and existing workplace/construction assignments.
- Profession HUD and selected Storage Yard context now show storage workers/builders as unlimited capacity, while production roles and granary workers still use their worksite slot caps.
- Kept the per-construction-site active builder cap at 2 so each site still stages construction with a small visible crew.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Profession assignment HUD

- Added a runtime `Professions` HUD opened from a top-menu button, with generated profession icons, dynamic rows for available professions, assigned/capacity counters, and `-`/`+` controls.
- Moved worker assignment/removal out of selected-building microHUDs; selected production/storage buildings now keep status/resource context only.
- Profession HUD aggregates current built worksites and routes assignment through existing worksite APIs for lumberjacks, stonecutters, hunters, fishers, storage workers, builders, and granary workers.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Build menu partial-class refactor

- Refactored the runtime Build menu so the public `StrategyBuildMenuController` remains the same MonoBehaviour API while its implementation lives in a non-partial `StrategyBuildMenuControllerDriver`.
- Converted the build catalog/icon helper from a `partial` controller file into the non-partial `StrategyBuildMenuCatalog` helper.
- Verified there are no remaining `partial class` declarations under `Assets`, so the maximum partial-class file size is `0 <= 900` characters.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Granary food storage MVP

- Added `Granary` as a buildable 3x2 food-storage building under `Storage`, with Logs/Stone construction cost, generated 2.5D art, food stockpile visuals, and 2 assignable worker slots.
- Added `StrategyGranary` local food storage for `Game` and `Fish`, with selected-building HUD support showing workers, stored food, and available food sources.
- Added food logistics for granary workers: reserve `Game` from Hunter Camps or `Fish` from Fisher Huts, walk to the source, pick up a reserved batch, carry it to the granary, and deposit it into granary stock.
- Extended Hunter Camp and Fisher Hut stock APIs with reservation/take/release methods so multiple granary workers do not target the same local food stock.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Fisher hut and fishing MVP

- Added `Fisher Hut` as a buildable 2x2 production camp under `Production`, with Logs/Stone construction cost, shoreline placement validation, generated 2.5D hut art, worker slots, and a visual local `Fish` stockpile.
- Added fisher work loop for up to 2 adult workers: reserve a nearby fish, move to a walkable shore cell, cast a fishing line, wait for a bite, reel the fish through hit-driven animation, carry `Fish` back, and deposit it at the hut.
- Extended fish with fishing reservation, hooked/caught states, hooked sprite animation, and fish yield; reserved fish pause normal shoal movement so the cast/reel sequence is stable.
- Added `Fish` as a resource type with HUD icon/future economy identity, plus carried-fish sprites and selected fisher-hut HUD support.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Hunter camp and rabbit hunting MVP

- Added `Hunter Camp` as a buildable 2x2 production camp under `Production`, with construction cost, generated 2.5D camp art, local worker slots, and visual `Game` stockpile.
- Added hunter work loop for up to 2 adult workers: reserve a nearby adult rabbit, move into bow range, animate aiming/shooting with an arrow projectile, wait for the carcass, butcher it over several animated hits, carry `Game` back, and deposit it at the hunter camp.
- Extended rabbits with hunt reservation, hit/death/carcass states, generated hit/death/carcass sprites, and butchering yield; reserved rabbits stop normal idle/flee behavior so the shot sequence is stable.
- Added `Game` as a resource type with HUD icon/future economy identity, plus carried game sprites and selected hunter-camp HUD support.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Resident aging cadence tune

- Changed resident age progression in `StrategyResidentAgent` from 120 seconds per year to 100 seconds per year.
- Children now take roughly 30 minutes at x1 speed to grow from birth to adulthood, or about 10 minutes at x3 speed.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Decorative bird wildlife MVP

- Added ambient decorative birds as a runtime wildlife layer alongside deer, rabbits, and fish.
- Birds spawn as Sparrows, Crows, and Ducks on species-appropriate terrain: meadows/grass for sparrows, forest/near-forest land for crows, and water/shore cells for ducks.
- Added procedural bird sprites with idle, pecking, hopping, flying, landing, and duck swimming frames, plus lightweight flight shadows.
- Bird agents roam inside loose home ranges, react to nearby residents/noisy work by flying away, and remain visual-only: no reproduction, resources, fog reveal, save data, or walkability blocking.
- Added structured debug events for bird spawn and flee reactions.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Fish wildlife MVP

- Added ambient fish as a water-only runtime wildlife species alongside deer and rabbits.
- Fish spawn in 18-28 initial adults across small shoals on generated `Water` cells with nearby water neighbors.
- Added three procedural fish species visuals: Minnow, Carp, and Perch, with idle, swim, dart/flee, turn, and feed sprite frames plus lightweight surface ripple renderers.
- Fish agents use local water-cell paths, do not block map cells, do not reveal fog, and react to nearby residents/noisy work by darting away through water.
- Adult fish can reproduce when another adult of the same species is nearby in the same shoal, up to a hard population cap of 60; fry grow into adults after scaled simulation time.
- Added structured debug events for fish spawn/birth/growth, population changes, and flee reactions.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-11 - Rabbit wildlife MVP

- Added ambient rabbits as a second runtime wildlife species alongside deer.
- Rabbits spawn in 12-18 initial adults across small groups on walkable meadow/grass/forest-edge land away from the starter camp.
- Added procedural 2.5D rabbit sprites for male/female visuals with idle, hopping, nibbling, alert, fleeing, grooming, and resting animation frames.
- Rabbit agents use local walkable-cell paths, do not block map cells, do not reveal fog, and react to residents/noisy work by becoming alert or fleeing.
- Adult female rabbits can reproduce with nearby adult males in the same group up to a hard population cap of 36; kits grow into adults after scaled simulation time.
- Added structured debug events for rabbit spawn/birth/growth, population changes, alert, and flee reactions.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

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

- Added construction-resource costs for house upgrades; current costs are Garden Beds 2 Logs + 1 Stone and Chicken Coop 1 Stone + 2 Planks.
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
- Follow-up on 2026-06-11 also made construction assignment independent from house pickup; home assignment is based on family/home state, not job state.
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

- Added `Stonecutter Camp` as a `Production` Build menu item with a 2x2 footprint, 3 procedural 2.5D camp variants, and a growing local Stone stockpile.
- Added worker assignment for `StrategyStonecutterCamp`: up to 2 residents can become stonecutters from the selected building HUD.
- Residents assigned to a stonecutter camp now reserve nearby Stone deposits, path to a walkable adjacent cell, play frame-based pickaxe animation, mine Stone chunks after several hits, carry Stone to camp, and deposit it into local camp stock.
- Stone deposits now support hit-driven mining with shake, crack overlay, grey chip/dust effects, per-kind hit thresholds, chunk amounts, reservation release between chunks, and depletion cleanup.
- Storage Yard logistics now reserve Stone from stonecutter camps, send storage workers to pick it up, carry it to the yard, and add it to Storage Yard Stone stock.
- Selection HUD now shows stonecutter camp worker slots/status, resident stonecutter role/statuses, and the stonecutter camp building title/context.
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

- Added `Lumberjack Camp` as a new Build catalog tool under `Production`, with procedural 2.5D camp sprites and a 2x2 footprint.
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
- Each placed `House` now spawns two residents: one male and one female.
- Added `StrategyResidentAgent` for simple idle movement around the linked home.
- Added `StrategyResidentSpriteFactory` for runtime male/female resident sprites.
- Runtime bootstrap now creates/configures the population controller and wires it into placement.
- Updated `Assembly-CSharp.csproj` for the new runtime scripts.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Build menu single-item category activation

- Changed single-item Build categories to directly activate their only build tool on click.
- `Housing` now selects/toggles `House` immediately, so the player does not need a second click on the item card.
- Disabled raycast targeting on the item cost badge background so decorative UI cannot intercept item-card clicks.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - Larger House sprite and housing category

- Renamed the current Build category from `Medieval` to `Housing`.
- Enlarged the generated House world sprite by lowering sprite pixels-per-unit and cropping transparent padding.
- Increased the Build item card/icon area so the House icon reads larger in the menu.
- Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` passed with 0 warnings and 0 errors.

### 2026-06-10 - First medieval building: House

- Added the first requested medieval building Build catalog entry: `House`.
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
