# Chronoforge Demo

A worked example of the whole tool: a rich sample build order, played back at runtime by
the in-game overlay, and editable in the Chronoforge window while you watch.

## Create it

Menu: **Chronoforge ▸ Create Demo Scene**. Everything is generated for you — sample asset,
runtime theme, PanelSettings, and a scene wired end to end. Press Play.

Under `Assets/Chronoforge Demo/`:

| Asset | What it is |
| --- | --- |
| `SO_DemoBuildOrder.asset` | The sample build order — 19 steps, tags, a branch, benchmarks, an economy model and clean-build settings. |
| `Chronoforge Demo.unity` | Camera, light, and one GameObject carrying `UIDocument` + `BuildOrderOverlay` + `BuildOrderDemoPlayer`. |
| `Chronoforge Overlay Panel.asset` | The `PanelSettings` runtime UI Toolkit needs. |

Re-running the menu rebuilds the scene but **keeps** your `SO_DemoBuildOrder`. Use
**Chronoforge ▸ Regenerate Demo Build Order** to reset the asset itself.

## What to look at

**In Play mode.** The overlay (top right) tracks the clock: the current step is marked,
the next four are previewed, and a warning appears once a benchmark that is already due
has been missed. The transport (bottom left) pauses, restarts, scrubs and changes speed —
scrub back and forth to watch the overlay follow.

**In the editor.** Select `SO_DemoBuildOrder`, press **Open in Chronoforge**, and edit it
while the scene is still playing — press Play again to see the change.

## The sample has two deliberate flaws

A flawless build order would demonstrate none of the analysis, so the sample contains
exactly two problems, both called out in its description:

1. **Worker gap** — no worker is produced between 0:30 and 1:23. The clean-build analysis
   reports the 53-second pause.
2. **Missed benchmark** — the "Factory online" checkpoint targets 3:20, but the factory
   finishes at 4:10. The benchmark row shows a red dot, the timeline marker turns amber,
   and the overlay warns once the clock passes the checkpoint.

Everything else is clean: no supply blocks, no idle production, no stockpiling, no
validation errors. Break something on purpose — delete a supply depot, move a step
earlier, point a prerequisite at nothing — and watch the panels react.

## How the pieces fit

```
BuildOrderDemoPlayer  ── owns the clock ──▶  BuildOrderOverlay  ──▶  UIDocument
   (transport, guide)      CurrentTime          (renders)             (UI Toolkit)
                                  │
                                  ▼
                        BuildOrderEvaluation.Run(asset)
                     simulate + validate + benchmarks + clean build
```

This is the integration pattern for a real game: **the host owns the clock and pushes it
in**; the overlay only renders. Swap `BuildOrderDemoPlayer` for your match code and the
overlay works unchanged:

```csharp
private void Update() => m_Overlay.CurrentTime = MyMatch.ElapsedSeconds;
```

The transport and guide are drawn with `OnGUI` on purpose — they are a development
harness, not shipping UI. The overlay itself, the part a game would ship, is runtime
UI Toolkit. No coroutines anywhere.
