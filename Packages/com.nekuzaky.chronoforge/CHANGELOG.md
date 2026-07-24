# Changelog

All notable changes to Chronoforge are documented here. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning is
[SemVer](https://semver.org/).

## [0.2.0] - Unreleased

### Added
- Runtime foundation with strict runtime/editor separation.
- Extensible data model: steps, branches, conditions, requirements, resource
  costs, tags, snapshots, and an optional per-resource economy model.
- Business logic in runtime (UI-agnostic, testable):
  - `BuildOrderValidator` — 12 rules (empty step, duplicate id, negative
    time/cost, non-monotonic time, impossible/suspicious supply, missing &
    duplicate prerequisites, broken branch, incomplete data).
  - `BuildOrderSimulator` — timeline, cumulative supply, resource shortfall.
  - `BuildOrderSerializer` — schema-versioned JSON round-trip + readable text
    export.
- 11 native step types: Unit, Building, Upgrade, Economy, Scout, Attack,
  Expand, Tech, Defense, Note, Custom.

### Changed
- Restructured package into `src/Runtime` and `src/Editor` with per-feature
  folders.
- Data fields migrated to the `m_` public / `_` private naming convention.

## [0.1.0]

### Added
- Initial prototype: `BuildOrderAsset`, flat steps, custom inspector, text/JSON
  export.
