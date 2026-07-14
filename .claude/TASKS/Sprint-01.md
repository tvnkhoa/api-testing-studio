# Sprint 01 — Foundation (Bootstrap)

## Goal

Bootstrap a **production-ready, buildable** solution implementing Clean Architecture, DI,
logging, configuration, the plugin host, and an empty WPF shell — with **no business features**.
The solution must compile and run before any feature work begins.

## Scope

- Solution structure (8 `src` projects, 9 plugins, 4 test projects) + `.slnx`.
- Clean Architecture layering with enforced references.
- Dependency Injection composition root (`Host`).
- Logging (Serilog → console + rolling file under `%LocalAppData%/ApiTestingStudio/logs`).
- Configuration & central build (`Directory.Build.props`, `Directory.Packages.props` (CPM),
  `.editorconfig`, `global.json`, `NuGet.config`).
- Plugin infrastructure: contracts, `PluginLoader`, `IPluginRegistry`, `AddPluginHost`, nine
  empty plugins each with a `*PluginModule` + placeholder impl.
- Workspace domain model (immutable records) + Infrastructure EF Core/SQLite skeleton
  (`WorkspaceDbContext`, `SqliteStorageProvider`, `ISecretProtector` placeholder, `SystemClock`)
  + `InitialCreate` migration.
- Empty AvalonDock shell window + `MainViewModel`.
- Documentation set under `.claude/`.

## Requirements

- Build treats warnings as errors; ship with **0 warnings**.
- App launches, applies migrations, and logs that all 9 plugins were discovered.

## Architecture Impact

Materializes ADR-0001…0005. No feature logic.

## Projects

All 21 projects created and referenced per the `ARCHITECTURE.md` dependency table.

## Classes (representative)

- Domain: `Workspace`, `Service`, `Endpoint`, `ProfileDefinition`, `Variable`, `Run`, …
- Shared: `Result`, `Result<T>`, `Error`.
- Core: `PluginLoader`, `PluginRegistry`, `PluginDescriptor`,
  `PluginHostServiceCollectionExtensions`.
- Infrastructure: `WorkspaceDbContext`, `SqliteStorageProvider`, `SystemClock`,
  `PlaceholderSecretProtector`, `InfrastructureServiceCollectionExtensions`.
- UI: `MainViewModel`. Host: `App`, `MainWindow`, `PluginCatalog`.

## Interfaces

`IPluginModule`, `IImporter`, `IExporter`, `IWorkspaceSerializer`, `IAssertion`,
`IWorkflowNode`, `IStressRunner`, `IStorageProvider`, `IDashboardWidget`, `IToolWindow`,
`IPluginRegistry`, `IClock`, `ISecretProtector`, `IWorkspaceService`.

## Database Changes

- `WorkspaceDbContext` with 13 entity tables; `InitialCreate` migration. Native SQLite pinned to
  3.x (ADR-0003).

## Plugin Changes

- Nine plugins scaffolded (Import.Curl/OpenApi/Scalar/Postman, Assertion.Json/Regex/Schema,
  Runner.Stress, Export.ApiStudio). Each registers one placeholder contract impl; real behavior
  is deferred to its feature sprint.

## UI Changes

- Empty AvalonDock shell (`MainWindow`) with menu + status bar bound to `MainViewModel`.

## Acceptance Criteria

- `dotnet build ApiTestingStudio.slnx` → **0 warnings, 0 errors**.
- `dotnet test` → all tests green (incl. plugin-discovery = 9).
- `dotnet run --project src/ApiTestingStudio.Host` opens the empty shell; log confirms 9 plugins
  active and storage initialized; `workspace.db` created.
- `Core`/`Domain`/`Application` reference no plugin or UI package.

## Out of Scope

- Any business feature (import, workflow, runner, dashboard, real crypto). Deferred to Sprints 02+.

## Risks

- Package availability/versions on .NET 10 → mitigated (probed versions, deferred LiveCharts).
- Warnings-as-errors friction → mitigated via documented `.editorconfig` rule tuning.

## Future Improvements

- Automated architecture test; CI pipeline; app packaging/installer.

## Checklist

- [x] Solution + all projects build
- [x] DI + Serilog wired in Host
- [x] Plugin host discovers 9 modules (verified by test + runtime log)
- [x] EF Core/SQLite skeleton + InitialCreate migration (applies at runtime)
- [x] Empty AvalonDock shell launches
- [x] Test projects green
- [x] `.claude/` documentation authored
- [x] Git initialized with bootstrap commit
