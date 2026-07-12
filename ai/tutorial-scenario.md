# Tutorial Scenario

Last updated: 2026-07-10

## Current State

- A lightweight starter onboarding goal sequence exists in the normal strategy runtime.
- No `GameStartMode.Tutorial`-style mode is documented yet.
- No separate tutorial scene or mode is documented yet.

## Maintenance Rule

When a real tutorial or onboarding flow is implemented:

- Document the player-facing sequence here in plain text.
- Include prerequisites, unlock order, HUD entry points, required actions, and completion conditions.
- Keep this file aligned with implemented behavior.
- If code/assets and this file disagree, trust code/assets first, then update either implementation or this scenario.

## Current Scenario

- Application startup first opens `MainMenu`: `Continue` is enabled only for a valid save, while `New Settlement` starts the normal onboarding flow.
- The menu prepares the likely saved/new map seed in the background and shows actual preparation progress when launch is requested.
- After `New Settlement` reaches the gameplay scene, the left-side Goals HUD shows `Build 3 Houses (0/3)`.
- Normal startup places a temporary Caravan Cart near the campfire instead of a prebuilt Storage Yard; it starts with 20 Logs, 20 Stone, and randomized raw food covering 3 days for the initial families.
- Dawn counts as settlement work time on every day, so auto-assigned builders and haulers can begin starter construction immediately.
- Residents cannot die from any cause during calendar days 1-2; normal mortality begins at the start of day 3.
- While this goal is active, the Build menu allows only `House`; all other building categories/items are locked.
- Completed construction, not placed construction sites, advances goal progress.
- After the third completed House, the next goal stage starts.
- The second stage shows `Build Forager Camp`.
- While the second stage is active, the Build menu allows only `Extraction` / `Food` / `Forager Camp`; other buildings remain locked.
- After the Forager Camp is completed, the third goal stage starts.
- The third stage shows `Build Lumberjack Camp` and `Build Stonecutter Camp`.
- While the third stage is active, the Build menu allows only `Extraction` / `Camps` / `Lumberjack Camp` and `Stonecutter Camp`; other buildings remain locked.
- After both raw-resource camps are completed, the starter goal sequence completes and the full Build menu catalog unlocks.
- The Goals HUD then asks the settlement to stock seven days of food and seven days of firewood before the first Winter.
- Winter readiness counts physical stock across eligible settlement stores and production buildings but excludes resources currently carried by residents.
- When Winter starts, the preparation goals are replaced by `Endure the first winter`.
- Occupied houses consume stored Logs for warmth during winter nights; residents accumulate cold exposure in underheated houses, and homeless residents accumulate exposure at Dawn.
- Cold exposure can slow residents and eventually increase mortality risk, so food, fuel, and housing remain active concerns throughout Winter.
- After the first Winter ends, the winter goal is cleared and the same settlement continues as an unrestricted sandbox.
- There is deliberately no victory screen, defeat screen, forced pause, or terminal game state at this stage.
- F5 saves the current settlement snapshot and F8 restarts the scene into the saved state.
