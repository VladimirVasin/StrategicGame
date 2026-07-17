# Tutorial Scenario

Last updated: 2026-07-17

## Current State

- A lightweight starter onboarding goal sequence exists in the normal strategy runtime.
- A separate pre-game `FoundingJourney` onboarding scene now precedes normal New Settlement gameplay.
- No `GameStartMode.Tutorial`-style mode is documented yet.
- No dedicated gameplay tutorial scene or mode exists beyond the founding onboarding and normal starter goals.

## Maintenance Rule

When a real tutorial or onboarding flow is implemented:

- Document the player-facing sequence here in plain text.
- Include prerequisites, unlock order, HUD entry points, required actions, and completion conditions.
- Keep this file aligned with implemented behavior.
- If code/assets and this file disagree, trust code/assets first, then update either implementation or this scenario.

## Current Scenario

- Application startup first opens `MainMenu`: `Continue` is enabled only for a valid save and opens gameplay directly, while `New Settlement` opens `FoundingJourney`.
- The menu prepares the likely saved/new map seed in the background. The same New Settlement candidate continues preparing while the founding story is shown; no second map is generated.
- Four story panels describe the families leaving a war-torn home, crossing the long road, discovering a quiet valley, and gathering for their first decision. Each uses an authored cover-cropped shot, cinematic shot/atmosphere crossfade, staged reveal, normalized artwork-bound effects, and scene-local wind/rain/fire ambience; persistent reduced motion removes the travel/particle motion and shortens transitions. The player can move Back, Skip the story, or use normal UI submit/navigation controls.
- The player answers four questions: preferred water landmark (River/Lake/High Dry Ground), surrounding landscape (Forest Edge/Open Meadow/Mixed), first livelihood (Hunting/Fishing/Foraging), and immediate priority (Construction/Resources/Balanced). `Use balanced defaults` skips individual answers with a neutral profile.
- The answers rank safe cells on the already prepared map. They never override water clearance, connected-land, resident-spawn, buildability, or Caravan Cart reservation requirements; explicit relaxed/legacy fallbacks keep unusually constrained maps playable.
- The summary lets the player change answers or begin. Beginning selects one deterministic camp cell and an exact `3x3` reserved Caravan Cart blocker, then opens gameplay.
- Gameplay places the campfire, initial families, camera focus, nature/forage exclusions, and visible `3x2` Caravan Cart from that selected layout. A new settlement begins at Dawn on Spring day 1. The profile and exact geometry are included in save version 3; Continue does not replay the story or create temporary new-game residents/cart.
- An optional `City Inventory` launcher in the top bar opens a read-only special-item chest with an English empty state and descriptive item rows. Opening the HUD blocks map/build input without pausing and does not alter founding, starter-build, or First Winter goals.
- After `New Settlement` reaches the gameplay scene, the left-side Goals HUD shows `Build 3 Houses (0/3)`.
- Normal startup places a temporary Caravan Cart near the campfire instead of a prebuilt Storage Yard; it starts with 20 Logs, 20 Stone, and randomized raw food covering 3 days for the initial families.
- Dawn counts as settlement work time on every day, so auto-assigned builders and haulers can begin starter construction immediately.
- Residents cannot die from any cause during calendar days 1-2; normal mortality begins at the start of day 3.
- At `Dusk` on Day 1, the settlement fauna enters its first-night stage: three mice are created in the same population refresh around the Caravan Cart and other food buildings, while cats remain completely blocked. The gnawed supplies are narrative and visual only; this event does not subtract food from the economy.
- At the first `Night` boundary, the presentation waits for any higher-priority modal to close, forces the requested simulation speed to x1, then pauses simulation and blocks all input. A reusable in-engine cinematic selects a nearby adult deterministically, temporarily reveals/stages that adult if everyone is sleeping or hidden, and creates a transient rat on a walkable 4-6-cell path.
- Before either actor moves, pulsing gold ground rings mark the resident and mouse while the camera smoothly focuses on both and black bars slide in to a 2.39:1 aperture. After a short hold, the standard settlement-scale mouse dashes past with subtle squash/tilt motion, the resident recoils through a dedicated eight-frame startle, and the scene restores the resident's exact prior transform/renderers.
- The cinematic hands directly into the three-frame fullscreen chronicle before releasing its camera/input ownership. The frames show the first rustling, the mice feasting on the stores, and the cats that quietly followed the caravan into the valley.
- Completing the third frame or choosing `Skip story` grants the unique `Cats` City Inventory item, completes the narrative stage, and creates the first world cat near a food building. Cats remain enabled only while the completed story and permanent item entitlement agree.
- The chronicle hands directly into a simulation-pausing `Cats` reward card with generated pixel art, concise story/effect copy, Reduced Motion support, and one required confirmation. The card then flies into the actual top-bar chest icon and triggers its arrival pulse. City Inventory keeps the item as a normal descriptive row rather than a reward card.
- Before gameplay resumes, the reward hands directly into a second gameplay-space cinematic: the camera smoothly focuses on a highlighted standard cat and mouse while the 2.39:1 bars return, the cat stalks and pounces, the mouse disappears at the catch, and the cat finishes with its non-looping joyful animation. The transient shot does not consume or reposition the three live settlement mice; exact camera/input/time ownership is released only after the cinematic returns to the prior view, and gameplay continues at x1.
- Save version 9 retains the first-night fauna stage and deterministic City Inventory stacks rather than individual runtime animals. The v8-to-v9 migration silently adds `Cats` to completed stories without replaying the reward; loading an unresolved `MiceVisible` state reconstructs the mice and replays the rat prelude, chronicle, and fresh reward flow.
- Until the starter sequence completes, the Build menu allows the seven-building base catalog: `House`, `Lumberjack Camp`, `Stonecutter Camp`, `Forager Camp`, `Scout Lodge`, `Storage Yard`, and `Granary`; all other building items remain locked.
- Completed construction, not placed construction sites, advances goal progress.
- Base buildings may be completed ahead of their displayed goal; the sequence remembers them and skips goals whose requirement is already satisfied.
- After the third completed House, the next goal stage starts.
- The second stage shows `Build Forager Camp`.
- After the Forager Camp is completed, the third goal stage starts.
- The third stage shows `Build Lumberjack Camp` and `Build Stonecutter Camp`.
- After both raw-resource camps are completed, the fourth stage shows `Build Scout Lodge`.
- Completing the first live Scout Lodge forces the requested speed to x1, smoothly focuses and zooms the camera onto it, pauses simulation, and opens the `First Expedition` assignment board. When the board resolves, the camera smoothly returns to its pre-cinematic position and zoom, releases programmatic focus, and gameplay continues at x1; later manual Scout picker openings preserve the current requested speed.
- The board explains that Scouts travel continuously through day and night, reveal unknown territory, investigate landmarks, and report distant Iron/Coal sites. Its introduction holds three stable random adult candidates, prioritizing current Haulers and Builders; an explicit appointment safely transfers the exact selected resident from an ordinary profession, while truly unavailable candidates remain visible with their blocking reason.
- If at least one adult is eligible, the introductory board requires a selection. If nobody can take the role, `Decide Later` closes the board safely, selects the Lodge, and leaves its ordinary Assign action available for later.
- The selected Scout Lodge HUD and the settlement Profession HUD both reopen the same exact-resident picker with the full adult roster; Scout assignment is manual-only and is not managed by Auto Workforce.
- Scout appointment is not a starter-goal completion requirement: the storage stage begins as soon as construction completes. A restored existing Lodge does not replay the first-expedition cinematic, and worksite assignments remain transient across save/load like the other current workplace roles.
- After the Scout Lodge is completed, the fifth stage shows `Build Storage Yard` and `Build Granary`.
- After both storage buildings are completed, the starter goal sequence completes and the full Build menu catalog unlocks.
- The Goals HUD then asks the settlement to stock seven days of food and seven days of firewood before the first Winter.
- Both preparation goals show live filled bars and current reserve days out of seven, using the same readiness calculation that completes the goals.
- Winter readiness counts physical stock across eligible settlement stores and production buildings but excludes resources currently carried by residents.
- When Winter starts, the preparation goals are replaced by `Endure the first winter`.
- Occupied houses consume stored Logs for warmth during winter nights; residents accumulate cold exposure in underheated houses, and homeless residents accumulate exposure at Dawn.
- Cold exposure can slow residents and eventually increase mortality risk, so food, fuel, and housing remain active concerns throughout Winter.
- After the first Winter ends, the winter goal is cleared and the same settlement continues as an unrestricted sandbox.
- There is deliberately no victory screen, defeat screen, forced pause, or terminal game state at this stage.
- When no higher-priority modal or HUD owns Cancel, `Escape` opens the in-game pause menu and pauses simulation without changing the requested x1/x2/x3 speed. `Escape` returns from Settings before closing the menu; Resume continues the existing onboarding or sandbox state.
- The pause menu exposes Save Game, master/music/effects/fullscreen settings, and confirmed Main Menu/Quit actions without changing tutorial goals or unlocks. F5 still saves the current settlement snapshot and F8 restarts the scene into the saved state.
