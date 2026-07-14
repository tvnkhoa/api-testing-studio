# ADR-0002 — Plugin-first architecture

- **Status:** Accepted
- **Date:** 2026-07-14

## Context

The product must evolve for years and support many independent capabilities (importers,
exporters, assertions, workflow nodes, stress runners, storage providers, UI widgets). Tightly
coupling these into the core would make the codebase brittle and hard to extend.

## Decision

Adopt a **plugin-first design**. All capabilities are defined as **abstractions** in
`ApiTestingStudio.Plugin.Abstractions` and implemented by plugin projects. Each plugin exposes an
**`IPluginModule`** entry point. A **`PluginLoader`** in `Core` discovers modules by reflecting
over a supplied set of assemblies and calls `IPluginModule.ConfigureServices` to register the
plugin's services into the DI container. An `IPluginRegistry` records what loaded.

**The core never references a concrete plugin.** Only the `Host` composition root knows the
concrete plugin set (`Composition/PluginCatalog.cs`).

## Consequences

- New capabilities are added as plugins without modifying the core.
- The loader accepts any assembly source, so directory-based dynamic loading
  (`AssemblyLoadContext`) can be added later by changing only the catalog (see Sprint 03).
- Slightly more indirection up front (contracts + module per capability) — accepted as the price
  of long-term maintainability and testability.
- Discovery is verified by an acceptance test (`PluginHost.Tests`).
