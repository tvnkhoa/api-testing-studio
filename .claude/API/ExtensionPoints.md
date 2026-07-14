# Extension Points

Every capability in API Testing Studio is replaceable behind an abstraction; the core never
references a concrete plugin. This document summarises **where** a plugin can extend the app and
**how** those extensions are discovered and registered. Contract signatures live in
`PluginContracts.md`.

## What a plugin can extend

| Extension point | Contract | Purpose | First-party plugins |
|---|---|---|---|
| **Importers** | `IImporter` | Turn an external API description into Services/Endpoints | `Import.Curl`, `Import.OpenApi`, `Import.Scalar`, `Import.Postman` |
| **Exporters** | `IExporter` | Write a workspace to an external package | `Export.ApiStudio` |
| **Workspace serializers** | `IWorkspaceSerializer` | Read/write the portable `.apistudio` package | `Export.ApiStudio` |
| **Assertions** | `IAssertion` | Evaluate one kind of assertion | `Assertion.Json`, `Assertion.Regex`, `Assertion.Schema` |
| **Workflow nodes** | `IWorkflowNode` | Execute one `WorkflowNodeKind` | workflow node plugins (Sprints 08/09) |
| **Stress runners** | `IStressRunner` | Run a stress plan, collect metrics | `Runner.Stress` |
| **Storage providers** | `IStorageProvider` | Persist workspaces (storage-agnostic) | `SqliteStorageProvider` (Infrastructure) |
| **Dashboard widgets** | `IDashboardWidget` | Contribute a Dashboard chart/panel | first-party Dashboard widgets (Sprint 13) |
| **Tool windows** | `IToolWindow` | Contribute a dockable tool pane | Explorer, Logs (Sprint 04+) |

Each of these is registered by the plugin into the shared DI container and resolved by the relevant
subsystem by its discriminator (`Format` / `Kind` / `ProviderName` / `WidgetId` / `ToolWindowId`).

## How discovery works

1. **One entry point.** Every plugin assembly exposes an `IPluginModule`
   (`Name`, `Version`, `ConfigureServices(IServiceCollection)`) — the *only* type the host looks for.
2. **Catalog (Host).** `PluginCatalog` (`ApiTestingStudio.Host/Composition`) supplies the set of
   plugin **assemblies**. In Phase 1 the host references the plugin projects directly and names one
   representative module type per assembly. When directory-based dynamic loading arrives, **only this
   catalog changes** — nothing downstream.
3. **Discovery (Core).** `PluginLoader` (`ApiTestingStudio.Core/Plugins`) reflects over the supplied
   assemblies and instantiates every non-abstract `IPluginModule` via its parameterless constructor.
   It tolerates partially-loadable assemblies (a bad plugin doesn't take down the others). This is
   the *only* place assemblies become plugin instances.
4. **Registration (Core).** `AddPluginHost(assemblies)`
   (`PluginHostServiceCollectionExtensions`) runs the loader, calls `ConfigureServices` on each
   discovered module so it registers its own services, records a `PluginDescriptor` per module, and
   registers an `IPluginRegistry` describing what was loaded.
5. **Composition root (Host).** `App.xaml.cs` builds the DI container, calls
   `AddPluginHost(PluginCatalog.GetPluginAssemblies())`, and resolves the shell. `MainViewModel`
   reads `IPluginRegistry` (e.g. the loaded plugin count in the status bar).

## Design guarantees

- **Inward-only dependencies.** Plugins depend on `Plugin.Abstractions`; the core depends on none of
  them. Adding an extension point means adding a contract in `Plugin.Abstractions` plus a resolver in
  the owning subsystem — never a reference to a concrete plugin.
- **Uniformity.** Built-in capabilities (SQLite storage, Explorer/Logs tool windows, Dashboard
  widgets) implement the same contracts as third-party plugins.
- **Forward-compatible loading.** The assembly-list seam in `PluginLoader` lets a future
  `AssemblyLoadContext` / folder-scanning mode drop in without touching discovery or business code.
