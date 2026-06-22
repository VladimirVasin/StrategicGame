# Tutorial Scenario

Last updated: 2026-06-19

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

- On normal strategy startup, the left-side Goals HUD shows `Build 3 Houses (0/3)`.
- Dawn counts as settlement work time on every day, so auto-assigned builders and haulers can begin starter construction immediately.
- While this goal is active, the Build menu allows only `House`; all other building categories/items are locked.
- Completed construction, not placed construction sites, advances goal progress.
- After the third completed House, the next goal stage starts.
- The second stage shows `Build Lumberjack Camp` and `Build Stonecutter Camp`.
- While the second stage is active, the Build menu allows only `Lumberjack Camp` and `Stonecutter Camp`; other buildings remain locked.
- After both raw-resource camps are completed, the starter goal sequence completes and the full Build menu catalog unlocks.
