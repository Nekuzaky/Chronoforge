<div align="center">

# ⚔️ Chronoforge

**A premium build order editor for Unity.**

Author, simulate, validate, compare and export build orders for RTS, colony sims,
tactical and management games — faster than a spreadsheet or a text file.

[![Unity](https://img.shields.io/badge/Unity-6000.0%2B-000000?logo=unity)](https://unity.com/releases/unity-6)
[![Release](https://img.shields.io/badge/release-v0.2.0-4C9AFF)](https://github.com/Nekuzaky/Chronoforge/releases)
[![License](https://img.shields.io/badge/license-MIT-8C9BAB)](LICENSE.md)
[![UPM](https://img.shields.io/badge/UPM-git%20url-35C46A)](#installation)
[![Code style](https://img.shields.io/badge/style-runtime%2Feditor%20split-6C5CE7)](CONTRIBUTING.md)

</div>

---

Chronoforge is a **dedicated editor tool**, not a runtime gameplay system. It gives
designers a keyboard-first workspace to plan opening sequences with live validation
and a visual timeline, on top of a clean, generic data model that no single game is
baked into.

> Stop iterating build orders in a spreadsheet. Forge them.

## ✨ Features

| | |
| --- | --- |
| 🗂️ **Three-column workspace** | Step list · step details · timeline + validation, all live. |
| ⌨️ **Keyboard-first** | Add steps with `1`–`6`, duplicate with `Ctrl+D`, delete with `Del`, drag to reorder. |
| ✅ **Live validation** | 12 rules — timing, supply, cost, prerequisites, branches, duplicates, incomplete data — surfaced without blocking editing. |
| 📊 **Lightweight simulation** | Timeline, cumulative supply, and resource shortfall against an optional per-resource economy model. |
| 🔀 **Conditional branches** | Named variation lanes gated by designer-authored conditions. |
| 🏷️ **Tags & filtering** | Reusable labels and instant search over the step list. |
| 💾 **Import / export** | Schema-versioned JSON round-trip + a readable text share format. |
| 🕓 **Snapshots** | Capture state for history and planned-vs-actual comparison. |
| 🧩 **11 native step types** | Unit, Building, Upgrade, Economy, Scout, Attack, Expand, Tech, Defense, Note, Custom. |

## 📸 Screenshots

> _Coming soon — open the package in Unity 6 and drop editor captures here._

## 📦 Installation

**Via Unity Package Manager (git URL)** — `Window ▸ Package Manager ▸ + ▸ Add package from git URL`:

```
https://github.com/Nekuzaky/Chronoforge.git?path=/Packages/com.nekuzaky.chronoforge
```

**Or** clone this repository and open it directly in Unity 6 (6000.0+). The package
lives at `Packages/com.nekuzaky.chronoforge/`. No external dependencies.

## 🚀 Quick start

1. Create an asset: `Assets ▸ Create ▸ Chronoforge ▸ Build Order`
   (or right-click in the Project window).
2. Double-click it, or press **Open in Chronoforge** in the inspector.
3. Add steps from the toolbar or with the number keys, edit in the details panel,
   and watch validation and the timeline update live.
4. Export to JSON or text to share — or import an existing JSON build order.

### Keyboard shortcuts

| Key | Action |
| --- | --- |
| `1` – `6` | Add Unit / Building / Upgrade / Economy / Tech / Note |
| `Ctrl` + `D` | Duplicate selected step |
| `Del` | Delete selected step |
| Drag | Reorder steps (when no filter is active) |

## 🏗️ Architecture

Chronoforge enforces a **strict runtime / editor separation** so its logic is testable
and reusable by a future overlay or companion app.

```
Packages/com.nekuzaky.chronoforge/
├── src/
│   ├── Runtime/          Chronoforge.Runtime  (no UnityEditor dependency)
│   │   ├── Data/         Asset, Step, Branch, Condition, Requirement,
│   │   │                 ResourceCost, Tag, Snapshot
│   │   ├── Evaluation/   Validator, Simulator, results & issues
│   │   └── Serialization/ schema-versioned JSON + text
│   └── Editor/           Chronoforge.Editor  (UI Toolkit)
│       ├── Panels/       list, details, timeline, validation, export, quick-add
│       └── UI/           USS theme + palette
├── Samples~/             JSON starter templates
└── Documentation~/
```

- **`Chronoforge.Runtime`** — data models plus all business logic
  (`BuildOrderValidator`, `BuildOrderSimulator`, `BuildOrderSerializer`). UI-agnostic
  and unit-testable.
- **`Chronoforge.Editor`** — the UI Toolkit workspace. Panels are decoupled
  `VisualElement`s wired through a single `BuildOrderEditorContext`; no build-order
  logic lives here.

### Data model

| Type | Role |
| --- | --- |
| `BuildOrderAsset` | The document: metadata, steps, branches, tags, resource model, snapshots. |
| `BuildOrderStep` | A single planned action — id-stable and self-describing. |
| `BuildOrderBranch` / `BuildOrderCondition` | A named variation lane and its gating condition. |
| `BuildOrderRequirement` | A prerequisite pointing at a step / resource / key. |
| `BuildOrderResourceCost` | Per-resource cost lines + the economy rate model. |
| `BuildOrderSnapshot` | Timestamped serialized copy for history / comparison. |

## 🗺️ Roadmap

- [x] Runtime foundation — data model, validation, simulation, serialization
- [x] UI Toolkit editor workspace — list, details, timeline, validation, export
- [ ] Full branch / tag / prerequisite editors
- [ ] Planned-vs-actual comparison view
- [ ] Snapshot restore from history
- [ ] Edit-mode test suite
- [ ] In-game overlay companion

## 🤝 Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for the branch model, commit convention, and
code style. In short: `feature/*` → PR into `develop` → release into `main`; runtime
stays free of `UnityEditor`; public fields use `m_`, private use `_`.

## 📄 License

[MIT](LICENSE.md) © nekuzaky
