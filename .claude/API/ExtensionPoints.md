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
   A module may also implement the optional `IPluginLifecycle`.
2. **Two sources.**
   - *Compile-time:* `PluginCatalog` (`ApiTestingStudio.Host/Composition`) supplies referenced
     plugin **assemblies**; `PluginLoader.Discover` reflects over them.
   - *Directory:* `PluginDirectoryScanner` scans the `plugins/` folder next to the executable. Each
     subfolder has a `manifest.json`; `PluginCompatibilityChecker` gates it against
     `PluginApiVersion.Current` and `PluginLoader.Load` loads the entry assembly into an isolated
     collectible `PluginLoadContext`.
3. **Discovery (Core).** `PluginLoader` (`ApiTestingStudio.Core/Plugins`) is the *only* place
   assemblies become plugin instances. It tolerates partially-loadable assemblies and never throws
   for a bad directory plugin (failures come back as a `Result` so the plugin is quarantined).
4. **Registration (Core).** `AddPluginHost(assemblies, pluginsDirectory?)`
   (`PluginHostServiceCollectionExtensions`) runs both sources, calls `ConfigureServices` on each
   module (rolling back and quarantining on failure), infers each plugin's `PluginCapability` set by
   diffing the service collection, records a `PluginDescriptor` per plugin (loaded or quarantined),
   and registers an `IPluginRegistry` (queryable by capability) plus a `PluginLifecycleManager`
   hosted service.
5. **Composition root (Host).** `App.xaml.cs` builds the DI container, calls
   `AddPluginHost(PluginCatalog.GetPluginAssemblies(), pluginsDirectory)`, and resolves the shell.
   `MainViewModel` reads `IPluginRegistry` (e.g. the loaded plugin count in the status bar).

## Design guarantees

- **Inward-only dependencies.** Plugins depend on `Plugin.Abstractions`; the core depends on none of
  them. Adding an extension point means adding a contract in `Plugin.Abstractions` plus a resolver in
  the owning subsystem — never a reference to a concrete plugin.
- **Uniformity.** Built-in capabilities (SQLite storage, Explorer/Logs tool windows, Dashboard
  widgets) implement the same contracts as third-party plugins.
- **Dynamic loading.** Directory plugins load via `AssemblyLoadContext` (Sprint 03 / ADR-0007)
  alongside the compile-time source, with manifest-driven compatibility gating, quarantine on
  failure, and collectible unload — no change to contracts or business code.
