# Chronoforge — Documentation

## Overview

Chronoforge is an editor tool for authoring build orders. It separates a pure,
testable runtime layer from a UI Toolkit editor workspace.

## Data model (runtime)

| Type | Role |
| --- | --- |
| `BuildOrderAsset` | The document: metadata, steps, branches, tags, resource model, snapshots. |
| `BuildOrderStep` | A single planned action. Id-stable, self-describing. |
| `BuildOrderBranch` | A named variation lane, gated by a `BuildOrderCondition`. |
| `BuildOrderCondition` | A boolean gate (`variable operator value`). |
| `BuildOrderRequirement` | A prerequisite pointing at a step / resource / key. |
| `BuildOrderResourceCost` | Per-resource cost lines, plus the economy model rate type. |
| `BuildOrderTag` | Reusable label for filtering. |
| `BuildOrderBenchmark` | A target checkpoint (supply and/or required-step-by-time). |
| `BuildOrderSnapshot` | Timestamped serialized copy for history / comparison. |

## Business logic (runtime, UI-agnostic)

- `BuildOrderValidator.Validate(asset)` → `List<BuildOrderValidationIssue>`.
- `BuildOrderSimulator.Evaluate(asset)` → `BuildOrderEvaluationResult`
  (timeline, total time, final supply, shortfall issues).
- `BuildOrderBenchmarkEvaluator.Evaluate(asset, result)` — checks benchmarks, writing
  per-checkpoint results and issues into the result.
- `BuildOrderEvaluation.Run(asset)` — one call = simulate + validate + benchmark.
- `BuildOrderSerializer` — `ExportJson` / `ImportJson` (schema-versioned) and
  `ExportText`.

## Editor

`BuildOrderEditorWindow` hosts the workspace. State is centralised in
`BuildOrderEditorContext`; panels (`BuildOrderListView`, `BuildOrderDetailsPanel`,
`BuildOrderTimelineView`, `BuildOrderBenchmarkPanel`, `BuildOrderValidationPanel`,
`BuildOrderExportPanel`, `BuildOrderQuickAddBar`, `BuildOrderSearchBar`) subscribe to its
`Changed` and `SelectionChanged` events and rebuild themselves.

### Shortcuts

| Key | Action |
| --- | --- |
| `1`–`6` | Add Unit / Building / Upgrade / Economy / Tech / Note |
| `Ctrl+D` | Duplicate selected step |
| `Del` | Delete selected step |
| Drag | Reorder (when no filter is active) |

## Extending

- **New step type** — add a value to `BuildOrderActionType` and a colour in
  `BuildOrderPalette`.
- **New validation rule** — add a `BuildOrderValidationCode` and a check in
  `BuildOrderValidator`; the panel renders it automatically.
- **Schema change** — bump `BuildOrderAsset.k_SchemaVersion`; the serializer
  gates newer payloads and is the migration point.
