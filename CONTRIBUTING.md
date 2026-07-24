# Contributing to Chronoforge

## Branch model

- `main` — stable, tagged releases only. Never commit directly.
- `develop` — integration branch. Features merge here first.
- `feature/*` — one branch per feature or fix, cut from `develop`.
- `release/*` — stabilisation before a `main` merge + tag (optional for small releases).
- `hotfix/*` — urgent fixes cut from `main`, merged back to `main` and `develop`.

Flow: `feature/x` → PR into `develop` → (release) → merge into `main` → tag `vX.Y.Z`.

## Commit convention

[Conventional Commits](https://www.conventionalcommits.org/):
`feat`, `fix`, `refactor`, `docs`, `chore`, `test`. Scope in parentheses,
e.g. `feat(editor): add timeline zoom`.

## Code conventions

- Strict runtime / editor separation. Runtime never references `UnityEditor`.
- Public fields prefixed `m_`, private/protected prefixed `_`.
- Namespace matches the assembly (`Chronoforge`, `Chronoforge.Editor`).
- Expression bodies for one-line methods; named arguments for booleans.
- No empty methods; keep regions tidy.
- No logic hardcoded to a single game.

## Before opening a PR

- Update `CHANGELOG.md`.
- Bump `BuildOrderAsset.k_SchemaVersion` if the serialized shape changed.
- Verify the change in the Unity 6 editor.
