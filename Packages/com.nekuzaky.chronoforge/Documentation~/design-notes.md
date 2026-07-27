# Chronoforge — Design Notes

Research-backed rationale for what Chronoforge is and where it goes. Sources at the
bottom.

## What the domain actually needs

Build orders in competitive RTS are recorded as a **supply-anchored sequence**, with
**timestamps added when the benchmark must be tighter than supply alone**
(Liquipedia). Chronoforge already stores both `m_Supply` and `m_TimeSeconds` per step,
so it matches how pros notate — no reinvention.

The signal that separates a tool from a text file is **benchmarks**: not just the
sequence, but the *target state* you should have reached at a checkpoint — e.g.
"Factory at 23 supply / 2:24 with 21 workers", "attack timing at 60 supply / 4:35".
Lower-league players fail when a single checkpoint slips ~5 s and the delay cascades
(Liquipedia / ByuN vs Dark example). A tool that flags checkpoint slippage does
something a spreadsheet cannot do cheaply. → **We ship a benchmark system.**

"Clean build" quality (Day9): worker production never stops, no supply blocks,
minimal stockpiling, precise sequencing. These are real validation targets, but most
require modelling production facilities and a supply *cap* — not just supply *used*.
We deliberately **defer fuzzy clean-build heuristics** until that model exists, to
avoid shipping noisy false positives. User-defined benchmarks give the same value now
with zero false positives.

Timing is **relative to conditions** — the same move is early/on-time/late only versus
scouting info, and a deviation is correct or wrong depending on what the opponent is
doing. → validates our **conditional branches** and the planned-vs-actual direction.

## What production tools teach us

- **Central data model owning undo / copy-paste / save / view-binding**, with
  standardized data↔view so views are reusable (GDC Tools Summit, *Writing Tools
  Faster*, The Machinery). → We already centralise all state and mutation in
  `BuildOrderEditorContext`; panels are thin views over it. Keep pushing state out of
  panels.
- **Design around complete workflows, not isolated widgets** (Remedy, *Workflow
  Driven Tools Design*). Chronoforge's workflow: draft opener → set benchmarks →
  validate → compare planned vs actual → share. Actions stay context-sensitive to the
  selected step.
- **Rapid iteration needs a safety net** (Bungie, *Tooling for Small Team
  Workflows*). → snapshots/history exist for exactly this.
- **"When spreadsheets aren't enough"** and data-driven studios (Supercell live
  telemetry / A-B, Riot) → the migration path matters: import from a sheet, and later
  import *actual* runs (replay/telemetry) into the `Actual` lane for comparison.

## Decisions taken now

1. **Benchmarks** — first-class runtime feature: user-defined checkpoints (supply
   and/or time) that the evaluator compares against the simulated state and flags on
   slippage. Generic (no SC2-specific metric hardcoded): checks projected supply at a
   time, and optional "this step must complete by T".
2. **Single runtime evaluation entry point** `BuildOrderEvaluation.Run(asset)` =
   simulate + validate + benchmark. Keeps the editor dumb and the logic testable.

## Roadmap implied by the research

- Benchmarks authoring panel (data + evaluation land first; UI next).
- Production-facility model → real clean-build checks (idle production, supply block,
  stockpiling).
- Planned-vs-actual comparison view; import actual runs.
- CSV import (spreadsheet migration path).

## Sources

- Liquipedia — [Timings and Benchmarks](https://liquipedia.net/starcraft2/Timings_and_Benchmarks)
- Spawning Tool — [How to Read a Build Order](http://blog.spawningtool.com/?page_id=168)
- GDC Vault — [Tools Summit: Writing Tools Faster](https://www.gdcvault.com/play/1026729/Tools-Summit-Writing-Tools)
- GDC Vault — [Workflow Driven Tools Design (Remedy)](https://gdcvault.com/play/1025289/Tools-Tutorial-Day-Workflow-Driven)
- GDC Vault — [Tooling for Small Team Workflows (Bungie)](https://gdcvault.com/browse/gdc-19/play/1025807)
- GDC Vault — [Game Design Tools: For When Spreadsheets and Flowcharts Aren't Enough](https://gdcvault.com/play/1024644/Game-Design-Tools-For-When)
- Databricks — [How Supercell evolved player experiences](https://www.databricks.com/dataaisummit/session/leveling-gaming-analytics-how-supercell-evolved-player-experiences)
