# Chronoforge — Security & Robustness Audit

**Date:** 2026-07-25 · **Scope:** `com.nekuzaky.chronoforge` runtime + editor
(28 files) · **Method:** manual static review (no Unity compile in the audit
environment — runtime behaviour unverified). **Threat model:** a local Unity
developer importing untrusted JSON/CSV build orders and authoring in the editor.

## Verdict

No exploitable vulnerabilities: no network, no process execution, no unsafe
deserialization, no secrets in source. Attack surface is local file import;
parsers are hardened and fail closed. One critical data-integrity defect found
and fixed this pass.

| Severity | Count |
| --- | --- |
| Critical | 1 (fixed) |
| High | 0 |
| Low | 2 (1 fixed, 1 open) |
| Advisory | 3 |
| Passed checks | 7 |

## Findings

### CRITICAL — Snapshots grew exponentially · FIXED
`ExportJson(asset)` serialized the whole asset including its snapshot list; a
snapshot stores that JSON, so each new snapshot re-embedded every prior one —
payload roughly doubling per snapshot. Enough snapshots freeze the editor and
bloat VCS. **Fix:** `ExportJson(asset, includeHistory:false)` strips
`m_Snapshots` before serializing; `TakeSnapshot` uses it.
Refs: `Editor/Panels/BuildOrderExportPanel.cs` · `Runtime/Serialization/BuildOrderSerializer.cs`.

### LOW — Numeric CSV type mapped to wrong category · FIXED
`Enum.TryParse` accepts a bare number, so a `type` cell `"3"` became `Economy`
instead of `Custom`. **Fix:** reject numeric tokens before the enum parse.
Ref: `Runtime/Serialization/BuildOrderCsvImporter.cs` — `ParseType`.

### LOW — Reorder is not an atomic undo step · OPEN
Drag-reorder mutates the bound list then only flags dirty; no `Undo.RecordObject`
before the move, so undo may not restore order. Minor; no data loss.
Refs: `Editor/Panels/BuildOrderListView.cs` · `Editor/BuildOrderEditorContext.cs`.

### ADVISORY — Compare rehydrates a ScriptableObject every refresh
`BuildOrderComparePanel` does `CreateFromJson` + `DestroyImmediate` per refresh.
Correct (cleanup in `finally`, no leak) but churny. Suggest caching the baseline
by snapshot id. Ref: `Editor/Panels/BuildOrderComparePanel.cs`.

### ADVISORY — Guard CSV export formula injection (preventive)
Chronoforge imports CSV but does not export it. If a CSV export is added, prefix
cells starting with `= + - @`. Text/JSON exports are unaffected.

### ADVISORY — Public `m_` fields carry no invariants (by design)
Data models expose public mutable fields per project convention; callers can set
inconsistent state, which the validator then catches. Conscious trade-off.

## Passed checks

- Safe deserialization — `JsonUtility` only, no polymorphic type resolution / RCE vector.
- No secrets in source (`ghp_`/`github_pat_` grep clean; the chat-shared token never entered the repo).
- No network calls (`UnityWebRequest`/`System.Net` absent).
- No process execution (`Process.Start` absent).
- Schema gate rejects newer-schema JSON instead of partially applying it.
- Parsers fail closed — malformed CSV/JSON returns an error string, never throws.
- No dangling references on delete — tag removal strips step refs; deleted prerequisite targets are flagged.
