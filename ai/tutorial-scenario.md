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
- After `New Settlement` reaches the gameplay scene, the left-side Goals HUD shows `Build 3 Houses (0/3)`.
- Normal startup places a temporary Caravan Cart near the campfire instead of a prebuilt Storage Yard; it starts with 20 Logs, 20 Stone, and randomized raw food covering 3 days for the initial families.
- Dawn counts as settlement work time on every day, so auto-assigned builders and haulers can begin starter construction immediately.
- Residents cannot die from any cause during calendar days 1-2; normal mortality begins at the start of day 3.
- At `Dusk` on Day 1, the settlement fauna enters its first-night stage: at least three mice begin appearing around the Caravan Cart and other food buildings, while cats remain completely blocked. The gnawed supplies are narrative and visual only; this event does not subtract food from the economy.
- At the first `Night` boundary, a three-frame fullscreen chronicle waits for any higher-priority modal to close, then pauses simulation and blocks all input. The frames show the first rustling, the mice feasting on the stores, and the cats that quietly followed the caravan into the valley.
- Completing the third frame or choosing `Skip story` closes the chronicle and immediately creates the first world cat near a food building. Further mouse/cat population follows the ordinary settlement-fauna rules, but the first-night minimum remains available after load.
- Save version 7 persists the first-night fauna stage rather than individual runtime animals. Loading a pre-night save reconstructs the appropriate mouse/story state; legacy saves already at or beyond the first Night migrate as completed and do not replay the chronicle.
- Until the starter sequence completes, the Build menu allows the seven-building base catalog: `House`, `Lumberjack Camp`, `Stonecutter Camp`, `Forager Camp`, `Scout Lodge`, `Storage Yard`, and `Granary`; all other building items remain locked.
- Completed construction, not placed construction sites, advances goal progress.
- Base buildings may be completed ahead of their displayed goal; the sequence remembers them and skips goals whose requirement is already satisfied.
- After the third completed House, the next goal stage starts.
- The second stage shows `Build Forager Camp`.
- After the Forager Camp is completed, the third goal stage starts.
- The third stage shows `Build Lumberjack Camp` and `Build Stonecutter Camp`.
- After both raw-resource camps are completed, the fourth stage shows `Build Scout Lodge`.
- Completing the first live Scout Lodge smoothly focuses and zooms the camera onto it, pauses simulation without changing the requested speed, and opens the `First Expedition` assignment board. When the board resolves, the camera smoothly returns to its pre-cinematic position and zoom and releases programmatic focus.
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
