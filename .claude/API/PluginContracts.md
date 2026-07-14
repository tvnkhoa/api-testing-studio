# Plugin Contracts

Every plugin contract lives in **`ApiTestingStudio.Plugin.Abstractions`**. The core never
references a concrete plugin — coupling flows only through these interfaces, which keeps every
capability replaceable. Plugins are discovered by `PluginLoader` and wired via `AddPluginHost`
(see `ExtensionPoints.md`). Signatures below match the abstractions as implemented.

## IPluginModule

The single entry point every plugin assembly exposes. The host discovers all implementations,
instantiates each via its parameterless constructor, and calls `ConfigureServices` to let the
plugin register its own capabilities into the shared container.

```csharp
public interface IPluginModule
{
    string Name { get; }        // e.g. "Import.Curl"
    Version Version { get; }
    void ConfigureServices(IServiceCollection services);
}
```

*Implemented by:* every plugin (e.g. `CurlImportPluginModule`, `JsonAssertionPluginModule`,
`StressRunnerPluginModule`, `ApiStudioExportPluginModule`).

## IImporter

Converts an external API description into workspace entities (`Service` / `Endpoint`).

```csharp
public interface IImporter
{
    string Format { get; }                                  // "curl", "openapi", "scalar", "postman"
    bool CanImport(ImportSource source);                    // ImportSource(Format, Content?, Uri?)
    Task<ImportResult> ImportAsync(ImportSource source, CancellationToken cancellationToken = default);
}
```

*Implemented by:* `Import.Curl` (`curl`), `Import.OpenApi` (`openapi`), `Import.Scalar` (`scalar`),
`Import.Postman` (`postman`).

## IExporter

Writes a workspace to an external package on disk.

```csharp
public interface IExporter
{
    string Format { get; }                                  // "apistudio"
    Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken = default);
}
```

*Implemented by:* `Export.ApiStudio` (`apistudio`). `ExportRequest(WorkspaceId, TargetPath)` →
`ExportResult(PackagePath, SizeBytes)`.

## IWorkspaceSerializer

Reads/writes the portable `.apistudio` package (`manifest.json` + `database.sqlite` + `attachments/`).

```csharp
public interface IWorkspaceSerializer
{
    string Format { get; }                                  // "apistudio"
    Task SaveAsync(Guid workspaceId, string packagePath, CancellationToken cancellationToken = default);
    Task<Guid> LoadAsync(string packagePath, CancellationToken cancellationToken = default);
}
```

*Implemented by:* `Export.ApiStudio`.

## IAssertion

Evaluates one kind of assertion against an actual value.

```csharp
public interface IAssertion
{
    string Kind { get; }                                    // "json", "regex", "schema"
    Task<AssertionEvaluation> EvaluateAsync(AssertionContext context, CancellationToken cancellationToken = default);
}
```

*Implemented by:* `Assertion.Json` (`json`), `Assertion.Regex` (`regex`), `Assertion.Schema`
(`schema`). Context: `AssertionContext(Actual, Expected, Options)` →
`AssertionEvaluation(AssertionOutcome, Message?)`.

## IWorkflowNode

Executes one kind of workflow node; one implementation per `WorkflowNodeKind`.

```csharp
public interface IWorkflowNode
{
    WorkflowNodeKind Kind { get; }                          // Api, Condition, Loop, Delay, Parallel, Switch, Variable, Assertion
    Task<NodeResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default);
}
```

*Implemented by:* workflow node plugins (Sprints 08/09). The engine dispatches each graph node to
its registered `IWorkflowNode`.

## IStressRunner

Executes a stress plan and collects aggregate performance metrics.

```csharp
public interface IStressRunner
{
    Task<StressMetrics> RunAsync(StressPlan plan, CancellationToken cancellationToken = default);
}
```

*Implemented by:* `Runner.Stress`. `StressPlan(Mode, Iterations, Concurrency)` →
`StressMetrics(AverageMs, MinMs, MaxMs, MedianMs, P95Ms, P99Ms, RequestsPerSecond, FailureRate)`.

## IStorageProvider

Storage-agnostic workspace **lifecycle** + persistence (SQLite is the Phase 1 provider). A
`location` is an opaque provider-specific locator (a file path for SQLite). Exactly one workspace
is open at a time; recoverable failures come back as `Result`. See ADR-0006.

```csharp
public interface IStorageProvider
{
    string ProviderName { get; }                            // "sqlite"
    bool IsOpen { get; }
    Task<Result> CreateAsync(string location, Workspace metadata, CancellationToken cancellationToken = default);
    Task<Result> OpenAsync(string location, CancellationToken cancellationToken = default);
    Task CloseAsync(CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(string location, CancellationToken cancellationToken = default);
    Task<Workspace?> GetWorkspaceAsync(CancellationToken cancellationToken = default);  // the open workspace
    Task SaveWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default);
}
```

*Implemented by:* `SqliteStorageProvider` (Infrastructure), driven by the Application-layer
`IWorkspaceService` and observed through the read-only `IWorkspaceSession`. Additional stores can
be added without touching business logic.

## IDashboardWidget

A dashboard widget contributed by a plugin (see `UI/Dashboard.md`). Phase 1 is identity/metadata.

```csharp
public interface IDashboardWidget
{
    string WidgetId { get; }
    string Title { get; }
}
```

*Implemented by:* first-party Dashboard widgets and any analytics plugin (Sprint 13).

## IToolWindow

A dockable tool window contributed by a plugin (see `UI/DockLayout.md`). Phase 1 is
identity/metadata.

```csharp
public interface IToolWindow
{
    string ToolWindowId { get; }
    string Title { get; }
}
```

*Implemented by:* first-party tool panes (Explorer, Logs) and any UI plugin (Sprint 04+).
