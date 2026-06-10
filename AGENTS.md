# AGENTS

This project uses shared AI memory in:

- `ai/`

Quick entry point:

- `AI.md`

Main memory files:

- `ai/README.md`
- `ai/project-overview.md`
- `ai/system-tree.md`
- `ai/systems-map.md`
- `ai/architecture-notes.md`
- `ai/work-log.md`
- `ai/tutorial-scenario.md`
- `ai/release-notes.md`

Reusable prompt templates live in:

- `ai/prompt-templates.md`

Design maps live in:

- `ai/Design/worker-thought-tree.md`
- `ai/Design/worker-thought-influence-matrix.md`

## Template Selection Logic

Primary rule:

- If the user explicitly writes `Use TEMPLATE_NAME`, the agent must load that template from `ai/prompt-templates.md` and follow it.
- At the start of every final response, the agent must state which template was used and whether it was explicitly requested by the user or inferred by the agent.

Fallback rule:

- If no template is specified, infer the most appropriate template from the request.

Inference rules:

- Use `FULL_TEMPLATE` for normal implementation tasks.
- Use `PLAN_TEMPLATE` for large, ambiguous, risky, or cross-system tasks.
- Use `BUGFIX_TEMPLATE` for bug investigation or regression fixes.
- Use `REFACTOR_TEMPLATE` for structural code changes, file splitting, or architectural cleanup.

Safety rule:

- If the correct template is unclear, default to `PLAN_TEMPLATE`.

## Template Lookup Rules

If the user says any of the following:

- `Use FULL_TEMPLATE`
- `Use PLAN_TEMPLATE`
- `Use BUGFIX_TEMPLATE`
- `Use REFACTOR_TEMPLATE`

the agent must:

1. open `ai/prompt-templates.md`
2. load the matching template block
3. follow that workflow for the task

## Default Agent Behaviour

- Read `ai/README.md` first.
- Read only the AI memory files relevant to the task.
- Treat code and assets as source of truth if memory and implementation disagree.
- Identify affected systems before editing.
- For broad, architectural, cross-system, or unclear tasks, consult `ai/system-tree.md` before the owner map.
- Before broad code searches, consult the System Owner Map in `ai/systems-map.md`.
- If a change touches Tutorial mode, `GameStartMode.Tutorial`, onboarding flow, taught systems, required resources/buildings, unlock order, HUD entry points, or tutorial goal text, read `ai/tutorial-scenario.md` before editing and update it if behavior changes.
- If this project later adds worker cognition, worker thoughts, active/pending thought formation, affect states, weakness/trait thought inputs, knowledge/opinion links from thoughts, social signals, thought-state display, or worker thought UI, read `ai/Design/worker-thought-tree.md` before editing.
- If this project later adds explicit thought influence rules, opinion feedback loops, source-thought -> target-thought bias, influence windows/caps, or human-logic links between thoughts, also read `ai/Design/worker-thought-influence-matrix.md` before editing.
- Scan only the necessary code/assets.
- State a short plan before making code changes.
- Avoid unrelated refactors.
- Update `ai/work-log.md` after implementation.
- Update `ai/system-tree.md` when system hierarchy, subsystem responsibilities, feature leaves, or cross-system links change.
- Update `ai/systems-map.md` when ownership, paths, or responsibilities change.
- Update `ai/project-overview.md` when visible project structure or key responsibilities change.
- Update `ai/architecture-notes.md` when implemented architecture, dependencies, hotspots, or refactor seams change.
- Update `ai/release-notes.md` when version labels, release contents, changelogs, or player-facing patch notes change.
- Update design maps only when the corresponding systems actually exist and change.

## System Tree Rules

- Keep `ai/system-tree.md` as the high-level informational tree of the project.
- Update it when adding, removing, renaming, or substantially changing a system, subsystem, player-facing feature, simulation feature, UI surface, or cross-system dependency.
- Keep file ownership details in `ai/systems-map.md`; keep `ai/system-tree.md` conceptual and navigational.
- If a feature crosses systems, update both the relevant tree leaves and the cross-system links section.

## Owner Map Rules

- Use `ai/systems-map.md` -> `System Owner Map` as the first navigation pass for implementation, bugfix, refactor, and investigation tasks.
- Owner cards are starting points, not hard boundaries. If a task crosses systems, inspect every relevant owner card before editing.
- If the owner map disagrees with code/assets, trust the implementation and update the owner map after the work is complete.
- If files are split, moved, or ownership changes, update `ai/systems-map.md` and add a short note to `ai/work-log.md`.

## Memory Rules

- Keep memory concise.
- Avoid bloat and duplicate notes.
- Do not paste code into memory files.
- `ai/work-log.md` is the frequently updated file.
- Stable memory files should only be updated when project reality actually changes.

## Tutorial Scenario Rule

- In this project, Tutorial means only a concrete player-facing tutorial/onboarding mode once implemented.
- Keep `ai/tutorial-scenario.md` as the plain-text scenario for the current tutorial flow.
- For serious gameplay, HUD, staffing, economy, transport, trade, building, worker, or onboarding changes, compare the implementation with `ai/tutorial-scenario.md` when a tutorial exists.
- If the change alters tutorial flow, prerequisites, unlock order, HUD entry points, required buildings/resources, automation/manual-control balance, or goal text, update both the tutorial implementation and `ai/tutorial-scenario.md` in the same task.
- If code and `ai/tutorial-scenario.md` disagree, trust code/assets as behavior, then either fix Tutorial or update the scenario so the next agent sees the real current flow.

## C# Project File Rules

- When adding, moving, or deleting `.cs` files, update the relevant `.csproj` files used by local `dotnet build` if they exist in the workspace.
- For runtime scripts, keep `Assembly-CSharp.csproj` in sync with exactly one `<Compile Include="...">` entry for each compiled `.cs` file when that file is present.
- For editor or test scripts, update `Assembly-CSharp-Editor.csproj` when the file is compiled there.
- If `.csproj` files are generated or ignored locally, still keep the shared workspace copy current when it exists so local verification remains reliable.
- After editing project files, run `dotnet build Assembly-CSharp.csproj -v:minimal` when `Assembly-CSharp.csproj` exists; also run `dotnet build Assembly-CSharp-Editor.csproj -v:minimal` when editor/test code may be affected and that project file exists.
- If a build reports duplicate compile items, remove the duplicate `.csproj` entry before finishing.

## Build And Verification Rules

- Do not run multiple `dotnet build`, `dotnet test`, Unity smoke tests, or broad check scripts in parallel.
- Start one build/test/check command at a time and wait for it to finish before starting another command that may touch `bin/`, `obj/`, generated `.csproj` files, or Unity assemblies.
- Prefer one consolidated verification pass over several overlapping checks.
- If a failure mentions locked DLLs, sharing violations, `MSB3021`, `MSB3026`, `MSB3027`, or files in use, stop starting new checks and first look for already-running `dotnet`, `MSBuild`, `VBCSCompiler`, Unity, or test processes.
- Do not delete `bin/` or `obj/` while any build/test/Unity process is still running.

## Encoding Safety Rules

- Treat all `.cs`, `.md`, `.json`, `.txt`, `.unity`, `.asset`, `.inputactions`, and other project text files as `UTF-8`.
- Prefer `apply_patch` for manual edits, especially in files containing localized UI/tutorial/state text.
- Do not rewrite project files through shell commands unless encoding is explicitly forced to UTF-8.
- After editing localized UI/HUD/tutorial/state text, run a quick scan for mojibake markers such as `вЂ`, `Р`, `С`, or `�`.
- If a diff shows broken Cyrillic or mojibake, stop and fix it before moving on.
