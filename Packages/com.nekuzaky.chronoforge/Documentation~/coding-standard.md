# Chronoforge — Coding Standard

Chronoforge's analysis layer follows the **NASA/JPL "Power of 10"** rules for
safety-critical code, adapted to C# / Unity. The rules apply strictly to
`src/Runtime` (data + evaluation) — the code that must be deterministic and
testable. Editor UI follows them in spirit where they make sense.

| # | JPL rule | How Chronoforge applies it |
| --- | --- | --- |
| 1 | Simple control flow; no `goto`, no recursion | **Literal.** No recursion anywhere in evaluation; flat loops and early returns only. |
| 2 | All loops have a statically provable upper bound | **Literal.** Every loop iterates a known collection (`for i < list.Count`); no `while (true)`, no unbounded scanning. |
| 3 | No dynamic allocation after initialization | **Adapted.** C# cannot forbid allocation, so instead: analyzers allocate working state once per call and never inside loops, callers pass in the output list, and per-frame code (overlay/demo) caches styles and textures. Hot paths allocate zero. |
| 4 | No function longer than one sheet (~60 lines) | **Literal.** Analysis methods are decomposed; each does one check. |
| 5 | Minimum two assertions per function; side-effect free | **Adapted.** Public analysis entry points validate every parameter and assert invariants via `Debug.Assert` (stripped in release, no side effects). Trivial one-line accessors are exempt rather than padded with ceremony. |
| 6 | Declare data at the smallest possible scope | **Literal.** Loop-local state stays in the loop; no ambient mutable fields in analyzers (all are `static` pure). |
| 7 | Check the return value of non-void functions; validate parameters | **Literal.** Every `TryParse` / `TryGetValue` result is checked; every public method null-guards its arguments and returns a defined result instead of throwing. |
| 8 | Limit the preprocessor | **Literal.** No conditional compilation in runtime logic beyond Unity's own assertion define. |
| 9 | Restrict pointers | **N/A.** No `unsafe`, no pointers. Read as: no deep indirection — analyzers take data in and return results out, with no callback graphs. |
| 10 | Compile with all warnings on, zero warnings | **Intent.** Code is written warning-clean; enable "Error on warning" in the project when the package is vendored. |

## Additional project rules

- **Zero coroutines.** No `IEnumerator` / `StartCoroutine` / `yield` anywhere.
  Timed work uses `Update`, `VisualElement.schedule`, or `EditorApplication.update`.
- **Runtime never references `UnityEditor`.** Enforced by CI.
- Public fields prefixed `m_`, private/protected `_`; namespace matches assembly.
- Analysis is **pure**: same asset in → same result out, no editor state, no time
  dependency, no `Random`.
