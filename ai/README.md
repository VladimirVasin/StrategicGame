# AI Memory System

Purpose: shared project memory for AI agents working in this repository.

This folder is intentionally small. Code and Unity assets remain the source of truth. These files exist to reduce repeated rescans, improve planning, and keep cross-session context consistent.

## Files

- `project-overview.md`
  Stable high-level map of the project: folders, key modules, and main runtime areas.
- `system-tree.md`
  Hierarchical informational tree of project systems, subsystems, feature leaves, and cross-system links.
- `systems-map.md`
  Active systems and their main files, plus impact hints for future changes.
- `architecture-notes.md`
  Actual implemented architecture, complexity hotspots, and likely refactor seams.
- `work-log.md`
  Active and recently completed work. Use this for task state and short implementation notes.
- `release-notes.md`
  Stable baseline for release notes, changelogs, and player-facing patch notes.
- `tutorial-scenario.md`
  Plain-text scenario for the current Tutorial/onboarding flow once one exists.
- `prompt-templates.md`
  Reusable prompt templates and workflow contracts.

Design maps:

- `Design/worker-thought-tree.md`
  Reserved map for worker cognition chains if this project adds worker thoughts, affects, knowledge/opinion effects, social signals, or thought UI.
- `Design/worker-thought-influence-matrix.md`
  Reserved matrix for explicit thought influence rules if this project adds opinion feedback loops or source-thought -> target-thought bias.

## Memory Types

Stable memory:

- `project-overview.md`
- `system-tree.md`
- `systems-map.md`
- `architecture-notes.md`

Release memory:

- `release-notes.md`

Volatile memory:

- `work-log.md`

Rule:

- stable memory is updated rarely
- release memory is updated when version labels, changelogs, release contents, or patch notes change
- volatile memory is updated often
- do not edit stable memory unless project reality actually changed

## Read Order For Agents

1. Read this file.
2. Read `project-overview.md`.
3. Read `system-tree.md` for broad, architectural, cross-system, or unclear tasks.
4. Read `systems-map.md`, especially `System Owner Map`, before broad code/assets search.
5. Read `architecture-notes.md`.
6. Read `work-log.md`.
7. Read `release-notes.md` when the task involves version labels, changelogs, release contents, or patch notes.
8. Read `tutorial-scenario.md` when the task touches Tutorial/onboarding mode or a system currently taught by Tutorial.
9. Read `Design/worker-thought-tree.md` when the task touches worker thoughts, active/pending thought formation, affect states, worker weaknesses/traits as thought inputs, knowledge/opinion links from thoughts, social signals from thoughts, thought-state display, or worker thought UI.
10. Read `Design/worker-thought-influence-matrix.md` when the task touches explicit thought influence rules, opinion feedback loops, source-thought -> target-thought bias, influence windows/caps, or human-logic links between thoughts.
11. Scan only the code/assets relevant to the requested change.

## Workflow Contract

### Before coding

- Read the memory files in the order above.
- Treat memory as a guide, not as authority over code/assets.
- If memory and implementation disagree, trust implementation and update memory after finishing.
- Identify the affected systems before editing.
- For broad, architectural, cross-system, or unclear tasks, use `ai/system-tree.md` to understand conceptual system boundaries before choosing owner files.
- Use `ai/systems-map.md` -> `System Owner Map` to pick the first files/assets to inspect.
- If the affected system is taught by Tutorial/onboarding, compare the change against `ai/tutorial-scenario.md` before editing.
- If the affected system changes worker cognition chain behavior or display, compare the change against `ai/Design/worker-thought-tree.md` before editing.
- If the affected system changes explicit thought influence behavior, compare the change against `ai/Design/worker-thought-influence-matrix.md` before editing.
- Write a short plan before changing code/assets.

### Planning

- Build a short plan using system boundaries, not file-by-file trivia.
- Note likely affected files/assets.
- For risky changes, note dependencies and verification steps before editing.

### During implementation

- Keep notes short.
- Do not paste code into memory files.
- Do not document speculative designs as implemented facts.

### Encoding Safety

- Treat all source and project text files as `UTF-8`.
- Prefer `apply_patch` for code/text edits, especially in files with localization.
- Avoid whole-file rewrite scripts unless the write path is explicitly `UTF-8`.
- After localized UI/tutorial/HUD text edits, run a quick scan for mojibake markers such as `вЂ`, `Р`, `С`, or `�`.
- If encoding corruption appears in diff output, treat it as a bug to fix immediately.

### After implementation

- Update `work-log.md` first.
- Update `system-tree.md` when system hierarchy, subsystem responsibilities, feature leaves, or cross-system dependencies changed.
- Update `tutorial-scenario.md` when changes alter Tutorial/onboarding flow, prerequisites, unlock order, HUD entry points, required buildings/resources, automation/manual-control balance, or goal text.
- Update design maps only when corresponding cognition/influence systems actually exist and change.
- Update `project-overview.md` only if visible project structure or key responsibilities changed.
- Update `systems-map.md` if system ownership, file involvement, owner-map paths, or owner-map responsibilities changed.
- Update `architecture-notes.md` only if the real architecture changed or a new hotspot/refactor seam appeared.

## Writing Rules

- Use concise factual bullets.
- Prefer summaries over narratives.
- Avoid duplicate information across files.
- Record implemented reality, not intention unless clearly marked as planned.
- Keep entries scannable for the next agent.
- Date entries when they describe a task or change.

## Anti-Bloat Rules

- Do not copy full prompts.
- Do not log tiny cosmetic changes unless they affect behavior or workflow.
- Do not repeat the same mechanic in multiple files.
- Do not keep stale "in progress" items; move them to `Done` or remove them.
- If `work-log.md` gets large, keep only active items and a short recent history.

## Quick Template For Future Prompts

Use this workflow:

1. Read `ai/README.md` and the relevant AI memory files.
2. Use memory to identify relevant systems.
3. Scan only the necessary code/assets.
4. Make a short plan before editing.
5. Implement the change.
6. Update AI memory files to match the new implementation.
