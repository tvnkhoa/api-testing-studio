# ADR-0007 — Dynamic plugin loading via AssemblyLoadContext

- **Status:** Accepted
- **Date:** 2026-07-14

## Context

ADR-0002 established a plugin-first design where `PluginLoader` (Core) discovers `IPluginModule`
implementations by reflecting over a supplied set of **assemblies**. Phase 1 (Sprints 01–02) fed
the loader assemblies that the `Host` referenced at compile time via
`Composition/PluginCatalog.cs`. That is enough to prove discovery but is not a real extensibility
story: third parties cannot drop a plugin next to the executable, there is no manifest, no
host↔plugin version gate, no isolation, and a throwing plugin can take down startup.

Sprint 03 hardens this into a production-grade, directory-based dynamic plugin system while
keeping the product **100% offline** (no network resolution of dependencies).

## Decision

Add a **second discovery source** — a `plugins/` directory next to the host — loaded through
`System.Runtime.Loader.AssemblyLoadContext`, **without removing** the compile-time source. Both
sources feed the same registration pipeline (`AddPluginHost`), so first-party plugins keep their
existing compile-time wiring and third-party plugins load dynamically.

Key elements:

1. **Manifest.** Each directory plugin ships a `manifest.json`
   (`PluginManifest` in `Plugin.Abstractions`): `id`, `name`, `version`, `entryAssembly`,
   optional `entryType`, `minHostApiVersion`, optional `maxHostApiVersion`, optional
   `description`.

2. **Host API version.** `Plugin.Abstractions.PluginApiVersion.Current` is the SemVer of the
   contract surface. A plugin declares the host range it supports; `PluginCompatibilityChecker`
   gates load against it and returns a **typed reason** on mismatch.

3. **Isolation boundary.** Each directory plugin loads in its own collectible
   `PluginLoadContext : AssemblyLoadContext` using `AssemblyDependencyResolver`. **Shared
   contract assemblies** (`Plugin.Abstractions`, `Domain`, `Shared`,
   `Microsoft.Extensions.DependencyInjection.Abstractions`) and framework assemblies always
   resolve from the **Default** context, guaranteeing a single `Type` identity for
   `IPluginModule` / `IImporter` / etc. across host and plugin.

4. **Lifecycle.** `PluginLifecycleState` (Discovered → Loaded → Initialized → Started → Stopped →
   Unloaded, plus Quarantined / Failed) is driven by `PluginLifecycleManager`. A module MAY also
   implement the **optional** `IPluginLifecycle` (`InitializeAsync`/`StartAsync`/`StopAsync`); the
   manager invokes it when present. `IPluginModule` is unchanged, so existing plugins compile as-is.

5. **Quarantine, not crash.** An incompatible, unreadable, or throwing plugin is quarantined with
   a logged, typed reason; the host and every other plugin keep running.

6. **Capability query.** `AddPluginHost` diffs the `IServiceCollection` around each module's
   `ConfigureServices` call to infer which contracts it contributes, recording a
   `PluginCapability` set on the `PluginDescriptor`. `IPluginRegistry.GetByCapability` filters by
   it. This works uniformly for compile-time and directory plugins and needs no manifest field.

7. **Unload.** `PluginLoadContext` is collectible; `PluginLifecycleManager.Unload` triggers
   collection. Verified by a weak-reference GC test. This is the foundation for future hot-reload.

## Consequences

- Third-party plugins are drop-in: a folder under `plugins/` with a `manifest.json` and a DLL.
- The compile-time seam (ADR-0002) is preserved; first-party plugins are unaffected.
- The isolation boundary adds a real constraint: **contract assemblies must stay in the shared
  set** or type identity breaks. This set is explicit in `PluginLoadContext`.
- Collectible ALCs can leak if a plugin roots static state or event handlers; unload is
  best-effort and covered by a GC test. Full hot-reload remains out of scope.
- `Plugin.Abstractions` is now the SemVer-governed public contract; breaking it is an ADR-level
  decision and bumps `PluginApiVersion.Current`.
- Acceptance is verified by `PluginHost.Tests` (dynamic load, compatibility rejection, throwing
  quarantine, unload, capability query) plus the existing compile-time discovery tests.
