# DATABASE_GUIDELINES.md

## Store & ORM

- **SQLite** is the Phase 1 backing store, accessed through **EF Core 10**.
- Business code depends on **`IStorageProvider`** (in `Plugin.Abstractions.Storage`), never on
  `WorkspaceDbContext` directly. The SQLite implementation is `SqliteStorageProvider`
  (`Infrastructure/Persistence`). Alternative providers plug in without business-logic changes.

## Where the database lives

- One SQLite file **per workspace**. There is **no shared/global database** — the file is chosen
  at runtime when the user creates or opens a workspace. A `.apistudio` package embeds the file as
  `database.sqlite` (see ADR-0003 and `FEATURES/` packaging notes).
- Because the file path is a runtime choice, there is **no fixed `AddDbContext`**. A
  `WorkspaceSession` (singleton) holds the open workspace's connection string, and
  `WorkspaceContextFactory` builds a short-lived `WorkspaceDbContext` per unit of work. Contexts
  are disposed immediately so connection pooling manages the OS file handle.
- `AddInfrastructure(appDataDirectory)` takes the app data directory (used for the MRU store and
  logs), **not** a connection string.

## Workspace lifecycle & session (Sprint 02)

- `IStorageProvider` is a **location-based lifecycle** contract: `IsOpen`, `CreateAsync`,
  `OpenAsync`, `CloseAsync`, `DeleteAsync`, plus `GetWorkspaceAsync` / `SaveWorkspaceAsync` for the
  currently open workspace. A `location` is an opaque provider-specific locator (a file path for
  SQLite). Recoverable failures come back as `Result` (see `WorkspaceErrors`), not exceptions.
- Exactly **one workspace is open at a time**; `IWorkspaceService.CreateAsync`/`OpenAsync`
  auto-close the current workspace first. The rest of the app observes the open workspace through
  the read-only `IWorkspaceSession` port.
- On close/delete the provider calls `SqliteConnection.ClearAllPools()` so Windows releases the
  file handle, allowing reopen and delete.
- The **recent-workspaces (MRU)** list lives **outside** any workspace database, as JSON at
  `%LocalAppData%/ApiTestingStudio/recent-workspaces.json` (capped, most-recent-first, deduped by
  location). Pinning is deferred.

## Entity model (Phase 1)

Entities are **immutable `record`s** in `ApiTestingStudio.Domain.Entities`, mapped as a **flat
relational model**: each child references its owner by a `*Id` foreign-key column (e.g.
`Service.WorkspaceId`, `Endpoint.ServiceId`). There are no embedded navigation collections yet —
this keeps EF mapping and migrations simple and predictable.

Mapped entities & tables (`WorkspaceDbContext`):
`Workspace`, `Service`, `EndpointFolder`, `Endpoint`, `ProfileDefinition`, `EnvironmentDefinition`,
`Variable`, `WorkflowDefinition`, `WorkflowNode`, `WorkflowEdge`, `TestCaseDefinition`, `Run`,
`RunStep`, `Attachment`, `WorkspaceSetting`, `LogEntry`, `PackageMetadata`, `RequestHistoryEntry`.

The Service Explorer catalog (Sprint 05) is `Service` → `EndpointFolder` (nestable via
`ParentFolderId`) → `Endpoint`. Endpoints reference their owner by `ServiceId` plus an optional
`FolderId` (null = directly under the service). Services, folders, and endpoints each carry a
`SortOrder` for sibling ordering. Indexes exist on `Service.WorkspaceId`, `Endpoint.ServiceId`,
`Endpoint.FolderId`, `EndpointFolder.ServiceId`, `EndpointFolder.ParentFolderId`. The catalog is
read/written via `IServiceRepository`, `IEndpointFolderRepository`, `IEndpointRepository`; the
per-workspace `Settings` table is accessed via `IWorkspaceSettingRepository` (used for the
Explorer's tree expansion/selection state).

`PackageMetadata` (`Packages` table) records which plugins a workspace depends on
(`PluginId`, `PluginName`, `Version`, `InstalledUtc`), with a **unique index on `PluginId`** so
upserts key off the plugin id. It is read/written via `IPackageMetadataRepository`.

`WorkflowNode` / `WorkflowEdge` (`WorkflowNodes` / `WorkflowEdges` tables, Sprint 08) hold the graph
of a workflow, each keyed to its owner `WorkflowDefinition` by `WorkflowId` (indexed). A node carries
`WorkflowNodeKind Kind` (enum-as-int), a name, canvas `PositionX`/`PositionY`, and a nullable JSON
`Config` payload (`System.Text.Json`, interpreted by the node's engine handler). An edge carries
`SourceNodeId`/`TargetNodeId` (both indexed), optional `SourcePort`/`TargetPort`, and an optional
`Mapping` expression. The flat root `Workflows` table (from `InitialCreate`) is unchanged; the
repository (`IWorkflowRepository`) hydrates the three tables into a runtime `Workflow` aggregate and
replaces the child rows wholesale on save.

`RequestHistoryEntry` (`RequestHistory` table, Sprint 06) records one API Runner send against an
endpoint: `EndpointId` (indexed FK), denormalized `Method`/`Url`/`StatusCode` and timing
(`TotalMs`, nullable `DnsMs`/`ConnectMs`/`TimeToFirstByteMs`) for cheap list rendering, plus full
`RequestSnapshot`/`ResponseSnapshot` JSON text (via `System.Text.Json`) used for replay, and a
`TimestampUtc`. Ordering by timestamp is done client-side (SQLite cannot `ORDER BY` a
`DateTimeOffset`). Read/written via `IRequestHistoryRepository`. Sprint 06 also extends `Endpoint`
with nullable `DefaultHeaders` (JSON array) and `DefaultBody` columns the runner pre-fills.

Conventions:
- Primary key: `Id` (`Guid`). Configured explicitly in `OnModelCreating`.
- Enums are stored as integers.
- Secrets on `ProfileDefinition` are `Protected*` string columns holding **ciphertext only**.
- `Attachment` stores metadata + a `RelativePath`; bytes live under the workspace
  `attachments/` folder, never in the database.

## Repository pattern

`IStorageProvider` is the coarse persistence port for Phase 1. As query needs grow, introduce
focused repository interfaces in `Application` (e.g. `IEndpointRepository`) implemented in
`Infrastructure`. Keep EF Core types out of `Application`/`Domain`.

## Migrations

- Migrations live in `Infrastructure/Persistence/Migrations`. Applied so far: `InitialCreate`,
  `AddPackageMetadata`, `AddServiceCatalogHierarchy`, `AddRequestHistory`, `AddWorkflows`.
  - `AddServiceCatalogHierarchy` (Sprint 05) adds the `EndpointFolders` table and the
    `Endpoints.FolderId`, `Endpoints.SortOrder`, `Services.SortOrder` columns + indexes. It is
    additive/back-compatible (the `Services`/`Endpoints` base tables already existed from
    `InitialCreate`, so this migration extends rather than creates them — the Sprint doc's proposed
    `AddServiceCatalog` name predates that fact). Paired with a `Workspace.CurrentSchemaVersion` bump
    to **2**; existing v1 workspaces self-provision the new schema via `MigrateAsync` on open and stay
    compatible.
  - `AddRequestHistory` (Sprint 06) creates the `RequestHistory` table (indexed on `EndpointId`) and
    adds the nullable `Endpoints.DefaultHeaders` / `Endpoints.DefaultBody` columns. Additive/
    back-compatible. Paired with a `Workspace.CurrentSchemaVersion` bump to **3**; v2 workspaces
    self-provision on open via `MigrateAsync`.
  - `AddWorkflows` (Sprint 08) creates the `WorkflowNodes` and `WorkflowEdges` tables with indexes on
    their `WorkflowId` (and the edge's `SourceNodeId`/`TargetNodeId`). Purely additive — the existing
    `Workflows` table is untouched. Paired with a `Workspace.CurrentSchemaVersion` bump to **4**; v3
    workspaces self-provision the two new tables on open via `MigrateAsync` and stay compatible.
- Create: `dotnet ef migrations add <Name> --project src/ApiTestingStudio.Infrastructure --output-dir Persistence/Migrations`
- Apply (dev): `dotnet ef database update --project src/ApiTestingStudio.Infrastructure`
- At runtime the provider runs `Database.MigrateAsync()` when a workspace is **created or opened**
  (not at startup), so each workspace file self-provisions its schema on first use.
- A `WorkspaceDbContextFactory` (`IDesignTimeDbContextFactory`) supports the EF tooling.

### Migration rules

- **Never edit an applied migration.** Add a new one.
- **Never break backward compatibility** of existing workspaces. Additive changes preferred.
- Every schema change is paired with a **workspace `SchemaVersion` bump** when it affects the
  logical model, plus an upgrade path documented in the sprint doc.

## Workspace versioning

- `Workspace.SchemaVersion` (int) tracks the logical workspace schema. On open, the app compares
  the file's version to the app's expected version and runs migrations / upgrades as needed.
  Opening a **newer** workspace than the app supports must fail safely with a clear message.

## Native SQLite

The native SQLite stack is pinned to `SQLitePCLRaw 3.x` in `Directory.Packages.props` (via CPM
transitive pinning) because the version EF Core pulls by default carries a security advisory.
See ADR-0003.
