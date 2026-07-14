# ADR-0005 — Central Package Management & reproducible restore

- **Status:** Accepted
- **Date:** 2026-07-14

## Context

A multi-project solution risks version drift when each `.csproj` pins its own package versions.
Builds must also be reproducible across machines that may have private NuGet feeds configured.

## Decision

- Enable **Central Package Management (CPM)**: all versions are declared once in
  `Directory.Packages.props`; project files reference packages **without** a `Version`.
  `CentralPackageTransitivePinningEnabled` is on so transitive versions can be pinned (used for
  the SQLite security fix in ADR-0003).
- Share build settings in `Directory.Build.props`: nullable enabled, implicit usings,
  `LangVersion=latest`, analyzers on, and **`TreatWarningsAsErrors=true`**.
- Pin restore to **nuget.org** via `NuGet.config` (`<clear/>` + single source + source mapping)
  so private user feeds don't affect CI/reproducibility (also avoids NU1507 under CPM).
- Pin the SDK via `global.json` (`10.0.200`, roll-forward latestFeature).

## Consequences

- One place to see/upgrade every dependency; no version drift.
- Warnings-as-errors keeps quality high but requires tuning genuinely-noisy analyzer rules
  (documented in `.editorconfig` with reasons: CA1000/CA1716/CA1848/CA1873, and CA1707/CA1859 in
  tests).
- Introducing an internal feed later means adding the source + a scoped `packageSourceMapping`
  entry in `NuGet.config`.
- `.slnx` (the .NET 10 XML solution format) is used; ensure tooling in use supports it.
