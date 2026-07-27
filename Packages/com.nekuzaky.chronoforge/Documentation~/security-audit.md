# Chronoforge — Security & Robustness Audit (Pass 2)

**Date:** 2026-07-25 · **Scope:** `com.nekuzaky.chronoforge` runtime + editor + demo ·
**Method:** manual static review (no Unity compile in the audit environment — runtime
behaviour unverified). **Threat model:** a local Unity developer importing untrusted
JSON/CSV build orders and authoring in the editor.

## Verdict

**All actionable findings across both passes are resolved.** No exploitable
vulnerabilities: no network, no process execution, no unsafe deserialization, no
secrets in source. Remaining items are a by-design trade-off and a preventive note.

| Severity | Count | State |
| --- | --- | --- |
| Critical | 1 | fixed |
| Medium | 1 | fixed |
| Low | 2 | fixed |
| Advisory | 2 | fixed |
| Accepted (by design / preventive) | 2 | n/a |
| Passed checks | 8 | — |

## Resolved

- **CRITICAL — snapshots grew exponentially.** `ExportJson` embedded the snapshot list
  inside each snapshot. Fix: `ExportJson(asset, includeHistory:false)` for snapshot
  payloads. `BuildOrderSerializer.cs` / `BuildOrderExportPanel.cs`.
- **MEDIUM — demo scene builder discarded unsaved work.** `NewScene` ran without a save
  prompt. Fix: guard with `SaveCurrentModifiedScenesIfUserWantsTo()`.
  `BuildOrderDemoBuilder.cs`.
- **LOW — reorder was not an atomic undo step.** Fix: revert → `RecordUndo` → reapply in
  `itemIndexChanged`. `BuildOrderListView.cs`.
- **LOW — numeric CSV type mapped onto an enum by value.** Fix: reject numeric tokens
  before `Enum.TryParse`. `BuildOrderCsvImporter.cs`.
- **ADVISORY — compare rehydrated a ScriptableObject every refresh.** Fix: cache baseline
  by snapshot id, release on change/detach. `BuildOrderComparePanel.cs`.
- **ADVISORY — demo overlay allocated a GUIStyle per frame.** Fix: cache the style.
  `BuildOrderDemoPlayer.cs`.

## Accepted (not defects)

- Public `m_` fields carry no invariants — project convention; the validator catches the
  resulting states. Conscious trade-off.
- CSV-export formula injection — no CSV export exists, so no surface today. If added,
  prefix cells starting with `= + - @`.

## Passed checks

Safe deserialization (JsonUtility, no RCE vector) · no secrets in source · no network
calls · no process execution · schema gate on import · parsers fail closed · no dangling
refs on delete · zero coroutines package-wide · demo scene built via the Unity API (no
hand-authored YAML).
