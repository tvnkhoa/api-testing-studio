# Sprint 03 — Plugin Infrastructure

## Goal
Harden the Phase 1 plugin foundation into a production-grade, directory-based dynamic plugin system with discovery, a registry, version-compatibility checks, and a well-defined plugin lifecycle.

## Scope
- Upgrade the existing `PluginLoader` / `IPluginRegistry` / `AddPluginHost` (Core, Phase 1).
- Directory-based discovery of plugins from a `plugins/` folder.
- Dynamic loading via `AssemblyLoadContext` (isolation + collectible, unload support — uncertain if unload ships this sprint).
- Version compatibility: host API version vs. plugin's declared min/max supported version.
- Plugin lifecycle: discovered -> loaded -> initialized -> started -> stopped -> unloaded.
- Base plugin contracts and a manifest format.

## Requirements
- Plugins declare metadata via manifest (id, name, version, host-compat range, entry type).
- Incompatible or failed plugins are quarantined, not crashing the host.
- Registry exposes query by capability/category (importer, assertion, runner, exporter).
- DI integration: plugin-provided services registered into a child/scoped container.
- 100% offline; no network resolution of dependencies.

## Architecture Impact
- Defines the extensibility backbone for all `plugins/*` projects.
- `Plugin.Abstractions` becomes the stable public contract surface (SemVer-governed).
- Introduces `AssemblyLoadContext` isolation boundary.

## Projects (which solution projects change)
- Core — loader, registry, host builder extensions, ALC management.
- Plugin.Abstractions — base contracts, manifest model, capability interfaces.
- Shared — version compatibility helpers.
- Host — wire plugin directory into startup.
- Tests: PluginHost.Tests.

## Classes
- `PluginLoader` (harden), `PluginRegistry` (harden), `PluginLoadContext : AssemblyLoadContext`.
- `PluginManifest`, `PluginDescriptor`, `PluginCompatibilityChecker`.
- `PluginLifecycleManager`, `PluginCatalog`.
- `AddPluginHost` host-builder extension (harden).

## Interfaces
- `IPlugin` (base contract: Initialize/Start/Stop).
- `IPluginRegistry` (existing, extend), `IPluginLoader`.
- `IPluginMetadata`, capability markers: `IImporterPlugin`, `IAssertionPlugin`, `IRunnerPlugin`, `IExporterPlugin` (uncertain — may live in Plugin.Abstractions as marker interfaces only).

## Database Changes
- None. (Installed-plugin metadata persistence handled via Sprint 02 PackageMetadata if needed.)

## Plugin Changes
- Establish manifest.json convention for every `plugins/*` project.
- No functional plugin logic yet — contracts and stubs only.

## UI Changes
- None. (A plugin-management UI is deferred; may surface a read-only list in Shell later.)

## Acceptance Criteria
- Host discovers and loads a sample plugin from the plugins directory at startup.
- An incompatible-version plugin is rejected with a logged, typed reason.
- A throwing plugin does not crash the host; it is quarantined.
- Registry returns plugins filtered by capability.
- Lifecycle transitions are observable/logged.

## Out of Scope
- Hot-reload / live plugin swap (unless unload lands early).
- Plugin marketplace / signing / trust.
- Concrete importer/assertion/runner logic (later sprints).

## Risks
- `AssemblyLoadContext` unload leaks (event handlers, static state) preventing collection.
- Dependency/version conflicts between plugin and host assemblies.
- Contract churn in Plugin.Abstractions breaking plugins (needs SemVer discipline).

## Future Improvements
- Plugin signing and integrity verification.
- Hot-reload during development.
- Per-plugin isolated settings and permissions.

## Checklist
- [x] Directory discovery + manifest parsing. (`PluginDirectoryScanner`, `PluginManifestReader`, `PluginManifest`)
- [x] AssemblyLoadContext-based loading. (`PluginLoadContext`, `PluginLoader.Load`)
- [x] Version compatibility checks + quarantine. (`PluginCompatibilityChecker`, `VersionCompatibility`, `AddPluginHost` quarantine)
- [x] Lifecycle manager + registry query API. (`PluginLifecycleManager`/`IPluginLifecycle`, `IPluginRegistry.GetByCapability`)
- [x] Sample plugin + PluginHost.Tests green. (`plugins/Sample.HelloWorld`, 9 tests passing)

## Outcome (2026-07-14)
- Decisions: hybrid seam (compile-time + directory sources into one pipeline), optional lifecycle
  (`IPluginModule` unchanged), collectible ALC with a verified unload GC test. See
  `DECISIONS/ADR-0007-Dynamic-Plugin-Loading.md`.
- Acceptance criteria verified by `PluginHost.Tests` (dynamic load, typed-reason compatibility
  quarantine, throwing-plugin quarantine, capability query, lifecycle transitions, ALC unload).
- Build clean (0 warnings); all 48 solution tests green.
