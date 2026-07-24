# Chronoforge

**Premium build order editor for Unity.** Author, simulate, validate, compare and
export build orders for RTS, colony sims, tactical and management games — faster
than a spreadsheet or a text file.

Chronoforge is a dedicated editor tool, not a runtime gameplay system. It gives
designers a keyboard-first workspace to plan opening sequences with live
validation and a visual timeline, and a clean, generic data model that no single
game is baked into.

## Highlights

- **Three-column workspace** — step list · step details · timeline + validation.
- **Keyboard-first** — add steps with `1`–`6`, duplicate with `Ctrl+D`, delete
  with `Del`, drag to reorder.
- **Live validation** — 12 rules (timing, supply, cost, prerequisites, branches,
  duplicates, incomplete data) surfaced without blocking editing.
- **Lightweight simulation** — timeline, cumulative supply, and resource shortfall
  against an optional per-resource economy model.
- **Import / export** — schema-versioned JSON round-trip and a readable text share
  format, both driven by the runtime serializer.
- **Snapshots** — capture state for history and planned-vs-actual comparison.
- **11 native step types** — Unit, Building, Upgrade, Economy, Scout, Attack,
  Expand, Tech, Defense, Note, Custom.

## Getting started

1. Create an asset: `Assets ▸ Create ▸ Chronoforge ▸ Build Order`
   (or right-click in the Project window).
2. Double-click it, or press **Open in Chronoforge** in the inspector.
3. Add steps from the toolbar or with the number keys, edit in the details panel,
   watch validation and the timeline update live.

## Architecture

Strict runtime / editor separation:

- `Chronoforge.Runtime` — data models plus all business logic (`BuildOrderValidator`,
  `BuildOrderSimulator`, `BuildOrderSerializer`). No editor dependency, unit-testable,
  reusable by a future overlay or companion app.
- `Chronoforge.Editor` — UI Toolkit workspace. Panels are decoupled `VisualElement`s
  wired through a single `BuildOrderEditorContext`; no build-order logic lives here.

## Requirements

Unity 6 (6000.0+). No external dependencies.
