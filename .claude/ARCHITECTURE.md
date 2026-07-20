# ARCHITECTURE.md

## Clean Architecture

Dependencies point **inward only**. Inner layers know nothing about outer layers. The domain is
the center; frameworks (WPF, EF Core, Serilog) live at the edges and are reached through
abstractions.

```
            ┌─────────────────────────────────────────────┐
            │                    Host                      │  composition root (WPF WinExe)
            │        (wires Infrastructure + UI + plugins) │
            └───────────────┬───────────────┬─────────────┘
                            │               │
                    ┌───────▼─────┐   ┌─────▼───────┐
                    │     UI      │   │Infrastructure│  EF Core, SQLite, crypto, time
                    │ (WPF/MVVM)  │   └─────┬───────┘
                    └──────┬──────┘         │
                           │        ┌───────▼───────┐
                           └────────►     Core      │  plugin host/loader, orchestration
                                    └───────┬───────┘
                                    ┌───────▼───────┐
                                    │  Application  │  use cases, ports (interfaces)
                                    └───────┬───────┘
                        ┌───────────────────┼───────────────────┐
                ┌───────▼────────┐  ┌───────▼───────┐   ┌────────▼────────┐
                │ Plugin.Abstr.  │  │    Domain     │   │     Shared      │
                │  (contracts)   │  │ (entities)    │   │  (primitives)   │
                └────────────────┘  └───────────────┘   └─────────────────┘
```

## Projects & responsibilities

| Project | TFM | Responsibility | May reference |
|---|---|---|---|
| **Domain** | net10.0 | Immutable entity records, enums. No framework deps. | — |
| **Shared** | net10.0 | Cross-cutting primitives (`Result`, `Error`, `VersionCompatibility`). | — |
| **Plugin.Abstractions** | net10.0 | All plugin contracts + `PluginManifest`, `IPluginLifecycle`, `PluginApiVersion`, `PluginCapability`. | Domain, Shared |
| **Application** | net10.0 | Use cases + ports (`IClock`, `ISecretProtector`, `IWorkspaceService`). | Domain, Shared, Plugin.Abstractions |
| **Core** | net10.0 | Plugin host: loader, registry, lifecycle, `PluginLoadContext` (ALC), `AddPluginHost`. | Application, Domain, Shared, Plugin.Abstractions |
| **Infrastructure** | net10.0 | EF Core `WorkspaceDbContext`, `SqliteStorageProvider`, `SystemClock`, secret protector. | Application, Core, Domain, Shared, Plugin.Abstractions |
| **UI** | net10.0-windows | WPF Views + ViewModels (MVVM). | Application, Core, Domain, Shared, Plugin.Abstractions |
| **Host** | net10.0-windows (WinExe) | Composition root; the only project that knows concrete infrastructure + plugins. | everything + all plugins |

### The dependency rule that matters most

- **Core never references a concrete plugin.** It only knows `Plugin.Abstractions`.
- **UI never references Infrastructure.** ViewModels depend on Application/Core ports; the Host
  binds the Infrastructure implementations.
- **Only Host** composes Infrastructure + plugins into the container.

These rules are what keep every capability swappable. Do not add a reference that violates the
table above.

## Plugin architecture

Every capability is a plugin behind an abstraction. Contracts live in `Plugin.Abstractions`:

- `IPluginModule` — the entry point each plugin assembly exposes (`Name`, `Version`,
  `ConfigureServices(IServiceCollection)`).
- `IImporter`, `IExporter`, `IWorkspaceSerializer`, `IAssertion`, `IWorkflowNode`,
  `IStressRunner`, `IStorageProvider`, `IDashboardWidget`, `IToolWindow`.

**Two discovery sources feed one pipeline** (`Core`, hardened in Sprint 03 — see ADR-0007):

1. **Compile-time.** `Host` provides referenced plugin assemblies (`Composition/PluginCatalog.cs`);
   `PluginLoader.Discover` reflects over them and instantiates every `IPluginModule`.
2. **Directory (dynamic).** `PluginDirectoryScanner` scans the `plugins/` folder next to the
   executable; each subfolder holds a `manifest.json` + entry assembly. `PluginCompatibilityChecker`
   gates the manifest against `PluginApiVersion.Current`, then `PluginLoader.Load` loads the entry
   assembly into an isolated collectible `PluginLoadContext : AssemblyLoadContext`.

`AddPluginHost` runs both sources through the same steps: call `module.ConfigureServices(services)`,
infer the plugin's `PluginCapability` set by diffing the service collection, and record a
`PluginDescriptor`. Incompatible, unreadable, or throwing plugins are **quarantined** (logged, typed
reason) instead of crashing the host. It registers an `IPluginRegistry` (query by capability) and a
`PluginLifecycleManager` hosted service that drives optional `IPluginLifecycle` hooks
(Initialize/Start/Stop) and unloads directory contexts on shutdown.

**Isolation boundary:** shared contract assemblies (`Plugin.Abstractions`, `Domain`, `Shared`,
`Microsoft.Extensions.DependencyInjection.Abstractions`) always resolve from the default context so
plugin contract types have a single identity across host and plugin. See `PLUGIN_DEVELOPMENT.md`.

## Storage (provider pattern)

```
IWorkspaceService  →  IStorageProvider  →  SQLite (SqliteStorageProvider)
       │                     │
       └── IWorkspaceSession ┘   (read-only view of the one open workspace)
```

Business logic depends on `IStorageProvider`, never on EF Core directly. Adding SQL Server /
PostgreSQL / a cloud store later means adding a new provider and one DI line in the Host — no
business-logic change.

`IStorageProvider` is a **location-based lifecycle** contract (`IsOpen`, `Create/Open/Close/Delete`,
`Get/Save` for the open workspace); a `location` is an opaque provider-specific locator (a file
path for SQLite). **Each workspace is a self-contained file chosen at runtime — there is no global
DB and no fixed connection string.** A singleton `WorkspaceSession` holds the open workspace;
`WorkspaceContextFactory` builds a short-lived `WorkspaceDbContext` per unit of work from it. The
rest of the app observes what is open via the read-only `IWorkspaceService`/`IWorkspaceSession`
ports; exactly one workspace is open at a time. `DATABASE_GUIDELINES.md` and ADR-0006 cover the
schema, lifecycle, and migration strategy.

## Packaging, backup & recovery (Sprint 14, ADR-0012)

```
IWorkspacePackageService ──► IWorkspaceMaintenance   (checkpoint + VACUUM INTO, Infrastructure)
        │                └──► IWorkspaceSerializer    (pure ZIP pack/unpack, Export.ApiStudio plugin)
        │                └──► IStorageProvider         (open the imported/restored workspace)
IBackupService / IRecoveryService ──► IWorkspaceSerializer (a backup is a timestamped .apistudio)
```

The portable **`.apistudio`** package is `ZIP(manifest.json + database.sqlite + attachments/)`. The
**plugin** does dependency-free byte I/O behind `IWorkspaceSerializer`; the **Application**
`IWorkspacePackageService` orchestrates (DB maintenance → build `PackageManifest` → serialize; on
import: validate manifest → install → open); **Infrastructure** implements the SQLite maintenance and
the app-data backup store. Secrets stay machine-bound (ADR-0010): the manifest records a
non-reversible master-key fingerprint and import **flags** undecryptable secrets for re-entry rather
than exporting or blanking them. See `FEATURES/Packaging.md`.

## Composition root

`src/ApiTestingStudio.Host/App.xaml.cs` builds a `Microsoft.Extensions.Hosting` container:
configures Serilog → `AddApplication()` → `AddInfrastructure(appDataDirectory)` →
`AddPluginHost(PluginCatalog.GetPluginAssemblies(), pluginsDirectory)` → registers `MainViewModel` + `MainWindow`
→ shows the window. **No workspace is opened at startup** (no global DB); EF migrations run when a
workspace is created or opened via `IWorkspaceService`.

## Solution file

`ApiTestingStudio.slnx` (the .NET 10 XML solution format). Central build settings live in
`Directory.Build.props`; package versions in `Directory.Packages.props` (Central Package
Management); restore is pinned to nuget.org via `NuGet.config`.
