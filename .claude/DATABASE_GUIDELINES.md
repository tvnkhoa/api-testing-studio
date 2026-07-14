# DATABASE_GUIDELINES.md

## Store & ORM

- **SQLite** is the Phase 1 backing store, accessed through **EF Core 10**.
- Business code depends on **`IStorageProvider`** (in `Plugin.Abstractions.Storage`), never on
  `WorkspaceDbContext` directly. The SQLite implementation is `SqliteStorageProvider`
  (`Infrastructure/Persistence`). Alternative providers plug in without business-logic changes.

## Where the database lives

- One SQLite file **per workspace**. At runtime the Host uses
  `%LocalAppData%/ApiTestingStudio/workspace.db`. A `.apistudio` package embeds the file as
  `database.sqlite` (see ADR-0003 and `FEATURES/` packaging notes).
- Connection string is built by the Host and passed to `AddInfrastructure(connectionString)`.

## Entity model (Phase 1)

Entities are **immutable `record`s** in `ApiTestingStudio.Domain.Entities`, mapped as a **flat
relational model**: each child references its owner by a `*Id` foreign-key column (e.g.
`Service.WorkspaceId`, `Endpoint.ServiceId`). There are no embedded navigation collections yet —
this keeps EF mapping and migrations simple and predictable.

Mapped entities & tables (`WorkspaceDbContext`):
`Workspace`, `Service`, `Endpoint`, `ProfileDefinition`, `EnvironmentDefinition`, `Variable`,
`WorkflowDefinition`, `TestCaseDefinition`, `Run`, `RunStep`, `Attachment`, `WorkspaceSetting`,
`LogEntry`.

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

- Migrations live in `Infrastructure/Persistence/Migrations`. Initial: `InitialCreate`.
- Create: `dotnet ef migrations add <Name> --project src/ApiTestingStudio.Infrastructure --output-dir Persistence/Migrations`
- Apply (dev): `dotnet ef database update --project src/ApiTestingStudio.Infrastructure`
- At runtime the app calls `IStorageProvider.InitializeAsync()` which runs
  `Database.MigrateAsync()` on startup, so a fresh machine self-provisions the schema.
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
