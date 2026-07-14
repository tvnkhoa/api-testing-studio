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
- New tables: `Services`, `EndpointFolders`, `Endpoints` (name, method, url template, parent id, ordering).
- Migration: `AddServiceCatalog`.

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

## Risks
- TreeView virtualization + drag/drop reliability in WPF.
- Ordering/move semantics with concurrent edits.
- Performance on very large catalogs.

## Future Improvements
- Tags, favorites, and saved filters.
- Multi-select bulk edit/delete.
- Grouping by tag/environment.

## Checklist
- [ ] Domain entities + repositories + migration.
- [ ] Explorer panel with virtualized tree.
- [ ] Endpoint/service/folder CRUD.
- [ ] Search/filter.
- [ ] Context menus + selection messaging.
