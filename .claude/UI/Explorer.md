# Explorer

## Overview

The **Explorer** is a dockable tool window that presents the workspace's Services and Endpoints
as a hierarchical tree. It is the primary navigation surface: selecting an Endpoint opens it in
the Runner document pane, and it is the entry point for all Service/Endpoint CRUD. It is an
`IToolWindow` (`ToolWindowId = "explorer"`, `Title = "Explorer"`) hosted in an AvalonDock tool
pane, docked to the left by default (see `DockLayout.md`).

## Scope / Capabilities

- **Service tree** — a two-level tree: `Service` → `Endpoint`, bound via `HierarchicalDataTemplate`.
  Endpoints show their `HttpVerb` (colour-coded badge) and relative path.
- **Endpoint CRUD** — add / rename / duplicate / delete Services and Endpoints. Mutations flow
  through the Application layer (`IWorkspaceService`) and persist via the storage provider; the
  tree refreshes from the resulting domain state.
- **Search / filter** — an incremental filter box over Service and Endpoint names and paths;
  non-matching nodes are collapsed/hidden. Filtering is client-side and offline.
- **Context menu** — right-click actions: New Endpoint, New Service, Rename, Duplicate, Delete,
  and (later sprints) Import into Service, Send to Workflow, Copy as cURL.
- **Drag reordering** and drag-to-canvas (feeding the Workflow Designer, Sprint 09) are future.

## Domain & Contracts

Domain records (`ApiTestingStudio.Domain`, `ServiceCatalog.cs`):

- `Service` — id, workspace id, name, description, ordered `Endpoints`.
- `Endpoint` — id, service id, name, `HttpVerb`, relative path/URL template, headers, body.

The Explorer is a consumer only; it holds no business logic. All reads/writes go through
`IWorkspaceService` (Application) which owns validation and persistence.

## UI

- MVVM (CommunityToolkit.Mvvm). `ExplorerViewModel` exposes an `ObservableCollection` of
  `ServiceNodeViewModel` / `EndpointNodeViewModel`, a `SelectedNode`, a `FilterText`, and
  `RelayCommand`s for each CRUD/context action. No logic in code-behind.
- View is a `TreeView` with `HierarchicalDataTemplate`s; Material Design icons distinguish
  Services from Endpoints and encode the HTTP verb.
- Selection raises navigation (opens the Endpoint in the Runner document pane in later sprints).

## Sprint

- **Sprint 05** — Service tree, Endpoint CRUD, search, context menu.
- Depends on Sprint 02 (workspace storage) and Sprint 04 (shell + docking).

## Open Questions / Future

- Drag-and-drop reordering and drag-to-canvas onto the Workflow Designer.
- Multi-select bulk operations (delete/move).
- Grouping/tagging Endpoints beyond the Service level.
- Virtualisation for very large workspaces.
