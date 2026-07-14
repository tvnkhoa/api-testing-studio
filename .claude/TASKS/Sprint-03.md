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
- [ ] Directory discovery + manifest parsing.
- [ ] AssemblyLoadContext-based loading.
- [ ] Version compatibility checks + quarantine.
- [ ] Lifecycle manager + registry query API.
- [ ] Sample plugin + PluginHost.Tests green.
