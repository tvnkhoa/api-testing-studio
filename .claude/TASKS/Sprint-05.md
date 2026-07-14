# Sprint 05 — Service Explorer

## Goal
Provide the Service Explorer panel: a hierarchical tree of services and endpoints with full CRUD, search/filter, and context menus — the primary navigation surface for the workspace's API catalog.

## Scope
- Service tree (services -> folders -> endpoints) as a dockable panel.
- Endpoint CRUD (create, rename, edit, delete, duplicate, move).
- Service/folder CRUD and drag-to-reorganize (uncertain — drag reorg may slip).
- Incremental search / filter across the tree.
- Context menus with contextual actions (open in Runner, rename, delete, etc.).

## Requirements
- Tree state persists (expansion, selection) per workspace.
- CRUD operations are transactional against the workspace DB.
- Search is fast on large trees (virtualized `TreeView`).
- Selecting an endpoint raises an event later consumed by the API Runner (Sprint 06).

## Architecture Impact
- Introduces Service/Endpoint domain entities and repositories.
- Adds a messaging/event channel (CommunityToolkit `IMessenger`) for cross-panel selection.

## Projects (which solution projects change)
- Domain — Service, Folder, Endpoint entities.
- Application — explorer service, CRUD commands, search.
- Infrastructure — repositories + migration.
- UI — Service Explorer panel, view models, tree controls.
- Tests: Domain.Tests, Application.Tests.

## Classes
- `Service`, `EndpointFolder`, `Endpoint` entities.
- `ServiceExplorerViewModel`, `ServiceNodeViewModel`, `EndpointNodeViewModel`.
- `ServiceExplorerService`, `EndpointCrudService`, `TreeSearchService`.
- `ServiceRepository`, `EndpointRepository`.

## Interfaces
- `IServiceExplorerService`, `IEndpointCrudService`.
- `IServiceRepository`, `IEndpointRepository`.
- `ITreeSearch` (uncertain — may be a plain method).

## Database Changes
- `Services`/`Endpoints` tables already existed (`InitialCreate`); this sprint adds the
  `EndpointFolders` table (nestable via `ParentFolderId`) plus `Endpoints.FolderId`,
  `Endpoints.SortOrder`, `Services.SortOrder` columns and supporting indexes.
- Migration: `AddServiceCatalogHierarchy` (renamed from the proposed `AddServiceCatalog` since it
  extends existing tables rather than creating them). `Workspace.CurrentSchemaVersion` bumped 1 → 2.

## Plugin Changes
- None. (Import plugins will *write* into this catalog in Sprint 07.)

## UI Changes
- New dockable Service Explorer panel with tree, search box, toolbar, context menus.
- New/edit endpoint dialogs (basic; full request editing is Sprint 06).

## Acceptance Criteria
- Create/rename/delete services, folders, endpoints; changes persist.
- Search filters the tree and highlights matches.
- Context menu actions work and are enabled/disabled by context.
- Selecting an endpoint publishes a selection event.
- Tree expansion/selection restored on reopen.

## Out of Scope
- Request building/sending (Sprint 06).
- Import (Sprint 07).
- Bulk operations / tagging.
- **Drag-and-drop reorganize** — moved to Future Improvements (backlog, unscheduled). Delivered this
  sprint as Move + up/down ordering via context menu / toolbar instead.

## Risks
- TreeView virtualization + drag/drop reliability in WPF.
- Ordering/move semantics with concurrent edits.
- Performance on very large catalogs.

## Future Improvements
- **Drag-and-drop tree reorganize** (backlog — not yet assigned to a sprint). Move + up/down ordering
  ship this sprint; drag-drop is the polish follow-up.
- Tags, favorites, and saved filters.
- Multi-select bulk edit/delete.
- Grouping by tag/environment.

## Checklist
- [x] Domain entities + repositories + migration.
- [x] Explorer panel with virtualized tree.
- [x] Endpoint/service/folder CRUD.
- [x] Search/filter (client-side, case-insensitive; auto-expands to reveal matches).
- [x] Context menus + selection messaging (`IMessenger` → `EndpointSelectedMessage`).
- [x] Per-workspace tree state (expansion/selection) persisted in the `Settings` table.
- [x] Move + up/down ordering via context menu (drag-drop deferred to backlog).
