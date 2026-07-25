# Chronoforge Demo

A runtime playback of a build order — the seed of a future in-game overlay, and a
quick way to test the tool end to end.

## Create the demo scene

Menu: **Chronoforge ▸ Create Demo Scene**.

This generates, under `Assets/Chronoforge Demo/`:

- `SO_DemoBuildOrder.asset` — a sample build order (steps, a benchmark, a resource
  model).
- `Chronoforge Demo.unity` — a scene with a `BuildOrderDemoPlayer` wired to that
  asset.

The scene and asset are built programmatically, so they are always valid for your
Unity version — nothing is hand-authored.

## Try it

1. Press **Play**. The overlay lists the steps and highlights the current one as the
   clock advances; use Play/Pause, Restart, the speed slider, and the scrubber.
2. Select `SO_DemoBuildOrder.asset` and press **Open in Chronoforge** (or double-click)
   to edit it — add steps, set benchmarks, author branches — then Play again.

`BuildOrderDemoPlayer` is Update-driven (no coroutines) and reads the runtime
simulation directly, demonstrating that the runtime layer works outside the editor.
