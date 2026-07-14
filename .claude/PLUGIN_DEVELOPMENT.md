# PLUGIN_DEVELOPMENT.md

Everything in API Testing Studio is a plugin behind an abstraction. The core never references a
concrete plugin — coupling flows only through `Plugin.Abstractions`.

## Anatomy of a plugin

A plugin is a class library that references **`ApiTestingStudio.Plugin.Abstractions`** (plus
`Domain`/`Shared`, and `Application` if it needs application ports). It exposes exactly one
**`IPluginModule`** as its entry point and one or more contract implementations.

```csharp
public sealed class CurlImportPluginModule : IPluginModule
{
    public string Name => "Import.Curl";
    public Version Version => new(1, 0, 0);

    public void ConfigureServices(IServiceCollection services)
        => services.AddSingleton<IImporter, CurlImporter>();
}

public sealed class CurlImporter : IImporter
{
    public string Format => "curl";
    public bool CanImport(ImportSource source) => /* ... */;
    public Task<ImportResult> ImportAsync(ImportSource source, CancellationToken ct = default) => /* ... */;
}
```

## Contracts you can implement

| Contract | Purpose | Example plugin |
|---|---|---|
| `IImporter` | Parse an external API description into workspace entities | `Import.Curl`, `Import.OpenApi`, `Import.Scalar`, `Import.Postman` |
| `IExporter` | Write a workspace to a package | `Export.ApiStudio` |
| `IWorkspaceSerializer` | Read/write the `.apistudio` package | `Export.ApiStudio` |
| `IAssertion` | Evaluate one kind of assertion | `Assertion.Json`, `Assertion.Regex`, `Assertion.Schema` |
| `IWorkflowNode` | Execute one workflow node kind | workflow node plugins |
| `IStressRunner` | Run a stress plan, collect metrics | `Runner.Stress` |
| `IStorageProvider` | Persist a workspace | `Infrastructure` (SQLite) / future providers |
| `IDashboardWidget` | Contribute a dashboard widget | dashboard plugins |
| `IToolWindow` | Contribute a dockable tool window | UI plugins |

## Lifecycle

1. **Provide** — the Host lists plugin assemblies in `Composition/PluginCatalog.cs`.
2. **Discover** — `PluginLoader` (in `Core`) reflects over the assemblies and instantiates each
   `IPluginModule` via its parameterless constructor.
3. **Register** — `AddPluginHost` calls `module.ConfigureServices(services)` for each module and
   registers an `IPluginRegistry` describing everything that loaded.
4. **Resolve** — the app resolves contract implementations (`IEnumerable<IImporter>`, etc.) from
   DI wherever it needs them.

## Registration & discovery rules

- A plugin module **must** have a public parameterless constructor.
- Register your services **only** inside `ConfigureServices`. Do not touch global state.
- Register against the **interface** (`AddSingleton<IImporter, CurlImporter>()`), not the
  concrete type, so consumers depend on the abstraction.
- Multiple plugins may register the same contract; consumers receive them all via
  `IEnumerable<T>` and select by `Format` / `Kind`.

## Versioning & compatibility

- `IPluginModule.Version` is the plugin's own version.
- Contract evolution follows **backward compatibility**: add new members via new interfaces or
  optional parameters; never break an existing contract. Breaking a contract is an ADR-level
  decision. Compatibility gates (host ↔ plugin API version) are hardened in **Sprint 03**.

## Future: dynamic loading

Phase 1 references plugins at compile time and passes their assemblies to the loader. Because
`PluginLoader.Discover` accepts **any** assembly enumeration, a future directory-based loader
(scanning `/plugins` with `AssemblyLoadContext`, enabling drop-in third-party plugins) changes
**only** `PluginCatalog` — the loader, contracts, and all business code stay the same.

## Testing a plugin

Assert discovery + registration like `tests/ApiTestingStudio.PluginHost.Tests`: build a
`ServiceCollection`, call `AddPluginHost(pluginAssemblies)`, then verify the `IPluginRegistry`
count and that the expected contracts resolve.
