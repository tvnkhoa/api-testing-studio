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

## Two ways a plugin is loaded

1. **Compile-time (first-party).** The Host references the plugin project and lists a representative
   module type in `Composition/PluginCatalog.cs`. `PluginLoader.Discover` reflects over the assembly.
2. **Directory (drop-in / third-party).** A folder under `plugins/` next to the executable, holding a
   `manifest.json` and the entry assembly. `PluginDirectoryScanner` finds it and `PluginLoader.Load`
   loads it into an isolated collectible `PluginLoadContext`. **This is the only path that needs a
   manifest.**

Both sources then run the same steps in `AddPluginHost`: **configure** (`module.ConfigureServices`),
**infer capabilities** (the host diffs the service collection to see which contracts you registered),
**record** a `PluginDescriptor` in `IPluginRegistry`, and **drive lifecycle** via
`PluginLifecycleManager`. The app then **resolves** contract implementations
(`IEnumerable<IImporter>`, etc.) from DI wherever it needs them.

## Directory plugin manifest

A directory plugin ships a `manifest.json` next to its entry assembly:

```json
{
  "id": "Sample.HelloWorld",
  "name": "Hello World Sample",
  "version": "1.0.0",
  "entryAssembly": "ApiTestingStudio.Sample.HelloWorld.dll",
  "entryType": "ApiTestingStudio.Sample.HelloWorld.HelloWorldPluginModule",
  "minHostApiVersion": "1.0.0",
  "maxHostApiVersion": null,
  "description": "..."
}
```

- `entryType` is optional; when omitted, the loader reflects for the first `IPluginModule`. Set it
  when the assembly contains more than one module.
- `minHostApiVersion` / `maxHostApiVersion` gate the plugin against `PluginApiVersion.Current`. A
  plugin outside the range is **quarantined** with a typed reason and never loaded.
- `plugins/Sample.HelloWorld` is the reference implementation; see how the Host's
  `DeploySamplePlugin` MSBuild target copies the DLL + manifest into `bin/.../plugins/`.

## Optional lifecycle

A module MAY also implement `IPluginLifecycle` (`InitializeAsync` → `StartAsync` on startup,
`StopAsync` on shutdown). It is optional: `IPluginModule` alone is enough for a plugin that just
registers services. Keep hooks fast and non-blocking; a hook that throws marks the plugin `Failed`
and is isolated so the host keeps running.

## Isolation & type identity (directory plugins)

Directory plugins load in their own `AssemblyLoadContext`, so their private dependencies are
isolated. **Contract assemblies must stay shared** — `Plugin.Abstractions`, `Domain`, `Shared`, and
`Microsoft.Extensions.DependencyInjection.Abstractions` always resolve from the default context so
`IImporter`, `IPluginModule`, etc. are the same `Type` on both sides. Do not ship private copies of
those assemblies expecting different behaviour.

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
  decision and **bumps `PluginApiVersion.Current`** (see ADR-0007).
- `IPluginModule.Version` and `PluginManifest.version` are the plugin's own version;
  `minHostApiVersion` / `maxHostApiVersion` declare the host range the plugin supports.

## Testing a plugin

Assert discovery + registration like `tests/ApiTestingStudio.PluginHost.Tests`: build a
`ServiceCollection`, call `AddPluginHost(pluginAssemblies)`, then verify the `IPluginRegistry`
count and that the expected contracts resolve.
