# Chronoforge Overlay

`BuildOrderOverlay` is the in-game companion HUD: it shows the current step of a build
order, previews the next few, and warns when a benchmark that is already due has been
missed. Rendered with **runtime UI Toolkit**.

## Setup

1. Run **Chronoforge ▸ Create Overlay Setup** once — it creates the `PanelSettings`
   asset runtime UI Toolkit requires. (Runtime UI Toolkit renders nothing without one;
   if the project has no `ThemeStyleSheet` yet, the dialog tells you how to make one.)
2. Add **Build Order Overlay** to a GameObject. Unity adds the required `UIDocument`;
   assign the PanelSettings asset to it.
3. Assign your `BuildOrderAsset` to the overlay.

## Driving it from your game

The overlay does not assume anything about your game loop — feed it match time:

```csharp
[SerializeField] private BuildOrderOverlay m_Overlay;

private void Update()
{
    // Push authoritative match time (pauses, replays and rewinds all just work).
    m_Overlay.CurrentTime = MyMatch.ElapsedSeconds;
}
```

Alternatives: `Advance(deltaSeconds)` to step it forward, `ResetClock()` on a new match,
`SetBuildOrder(asset)` to swap plans mid-session. For a quick look without wiring
anything, tick `m_UseInternalClock`.

`Demo/BuildOrderDemoPlayer` is a worked example of the same pattern: it owns the clock
and pushes it into the overlay.

## Notes

- The visual tree is built once in C# with inline styles — no USS, prefab or scene asset
  needed, and colours come from `BuildOrderColors`, shared with the editor window.
- Per-frame work is a clock label update; step rows are reused from a fixed pool and only
  repointed when the current step changes. No per-frame allocation, no coroutines.
