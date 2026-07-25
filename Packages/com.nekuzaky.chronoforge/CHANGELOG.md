# Changelog

All notable changes to Chronoforge are documented here. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning is
[SemVer](https://semver.org/).

## [Unreleased]

### Added
- **Snapshot restore & history** — `BuildOrderHistoryPanel` lists snapshots with restore
  (behind a confirm, undoable, history preserved) and delete. `RestoreSnapshot` re-attaches
  the current history after overwriting.
- **Companion overlay** — `BuildOrderOverlay` (`Chronoforge.Overlay`): an in-game HUD driven
  by the host game's clock (`CurrentTime`/`Advance`) that highlights the current step, previews
  upcoming ones, and warns when a due benchmark is missed. Update/OnGUI only, no scene assets.
- **Edit-mode test suite** (`Chronoforge.Tests`) — NUnit coverage for time parsing, validator
  rules, simulation, benchmarks, comparison, CSV import, and JSON round-trip / snapshot-history
  exclusion / schema gating.

### Added (earlier)
- **Benchmarks** — user-defined checkpoints (`BuildOrderBenchmark`) that the evaluator
  compares against the simulated timeline, flagging supply/timing slippage. Research-backed
  (Liquipedia timings & benchmarks). Generic; no game-specific metric hardcoded.
- `BuildOrderEvaluation.Run(asset)` — single runtime entry point that simulates, validates
  and checks benchmarks in one call.
- **Benchmarks editor panel** — author checkpoints (label, time, supply target, required
  step) with a live pass/fail dot and computed detail per row, plus vertical checkpoint
  markers drawn over the timeline (green pass / amber miss).
- Side column is now scrollable to host timeline, benchmarks, validation and export.
- Text/numeric fields commit on Enter/blur (`isDelayed`) — no focus loss while typing.
- **CSV import** (`BuildOrderCsvImporter`) — spreadsheet migration path. Auto-detects
  delimiter (comma/semicolon/tab), optional header mapping, quoted fields; replace or
  append. Surfaced in the export panel.
- **Branches & tags authoring** (`BuildOrderStructurePanel`) — create/edit/remove branches
  (key, name, colour, gating condition) and tag definitions; per-step prerequisites editor
  and tag chips added to the details panel.
- **Planned-vs-actual comparison** (`BuildOrderComparer` + `BuildOrderComparePanel`) — diff
  the current build against any snapshot; added / removed / time / supply / type shifts,
  click a row to jump to the step.
- `Documentation~/design-notes.md` — research synthesis (GDC tools talks, RTS benchmark
  theory, data-driven studios) mapping findings to product decisions.

- **Demo scene** — `Chronoforge ▸ Create Demo Scene` generates a sample build order
  asset and a scene wired to a `BuildOrderDemoPlayer` that plays the build order in Play
  mode (Update-driven overlay: play/pause/restart/speed/scrub, current-step highlight).
  Built programmatically so the scene/asset are always valid.

### Fixed
- Snapshots no longer embed their own history — `ExportJson(includeHistory:false)` used
  for snapshot payloads (was exponential blob growth).
- CSV `type` cells that are bare numbers no longer map onto an enum by value.

### Changed
- Schema bumped to v2 (adds `m_Benchmarks`). v1 payloads still import cleanly.

## [0.2.0] - Unreleased

### Added
- Runtime foundation with strict runtime/editor separation.
- Extensible data model: steps, branches, conditions, requirements, resource
  costs, tags, snapshots, and an optional per-resource economy model.
- Business logic in runtime (UI-agnostic, testable):
  - `BuildOrderValidator` — 12 rules (empty step, duplicate id, negative
    time/cost, non-monotonic time, impossible/suspicious supply, missing &
    duplicate prerequisites, broken branch, incomplete data).
  - `BuildOrderSimulator` — timeline, cumulative supply, resource shortfall.
  - `BuildOrderSerializer` — schema-versioned JSON round-trip + readable text
    export.
- 11 native step types: Unit, Building, Upgrade, Economy, Scout, Attack,
  Expand, Tech, Defense, Note, Custom.

### Changed
- Restructured package into `src/Runtime` and `src/Editor` with per-feature
  folders.
- Data fields migrated to the `m_` public / `_` private naming convention.

## [0.1.0]

### Added
- Initial prototype: `BuildOrderAsset`, flat steps, custom inspector, text/JSON
  export.
