# Sprint 09 — Workflow Designer

## Goal
Deliver the visual Workflow Designer on Nodify: a canvas for building workflow graphs with drag & drop, zoom, minimap, undo/redo, and two-way mapping to the Sprint 08 engine model.

## Scope
- Nodify-based canvas hosting workflow nodes and connections.
- Node palette + drag-and-drop node creation.
- Connect/disconnect edges with validation (compatible ports).
- Zoom, pan, fit-to-view, minimap.
- Undo/redo for all graph edits.
- Visual model <-> domain graph mapping and live save.

## Requirements
- Designer edits the same `Workflow` model the engine executes (no drift).
- Undo/redo covers add/remove/move nodes and edges and property edits.
- Node property editing panel bound to selected node.
- Run-from-designer triggers the S08 engine and reflects live node status (basic; full live viz in S13).

## Architecture Impact
- Adds a UI-side view-model graph layer mapped to the domain graph.
- Introduces an undo/redo (command/mementos) service reusable elsewhere.

## Projects (which solution projects change)
- UI — designer document pane, Nodify canvas, node/edge/port view models, palette, property panel,
  `GraphMapper` (VM ↔ domain; references UI VM types so it lives here, not Application), the Workflows
  tool panel, and shell wiring.
- Application — `NodePortCatalog`/`WorkflowPorts`, `ConnectorValidator`, `UndoRedoService`,
  `WorkflowCatalogService` (UI-facing CRUD), and the `IProgress<NodeRunResult>` engine seam. All pure
  and unit-tested.
- Domain — visual metadata on `WorkflowNode` (`Width`/`Height`/`Color`); schema bump v4→v5.
- Tests: Application.Tests (validator, undo/redo, port catalog, catalog service, engine progress).

## Classes
- UI: `WorkflowEditorViewModel` (document pane), `NodeViewModel`, `ConnectionViewModel`,
  `PortViewModel`, `NodePaletteViewModel`, `NodePropertiesViewModel`, `WorkflowsPanelViewModel`
  (tool panel), `NodeViewModelFactory`, `GraphMapper`, per-edit `IUndoableCommand`s,
  `RunStatusToBrushConverter`.
- Application: `WorkflowPorts` (port constants), `NodePortCatalog`, `ConnectorValidator`,
  `UndoRedoService`, `WorkflowCatalogService` (+ `WorkflowListItem`).

## Interfaces
- Application: `IConnectorValidator`, `IUndoRedoService`, `IUndoableCommand`, `IWorkflowCatalogService`.
- UI: `INodeViewModelFactory`, `IWorkflowEditorViewModelFactory` (multi-instance document factory).

## Database Changes
- Add nullable visual metadata to `WorkflowNodes` (`Width`/`Height` double, `Color` string) via
  migration `AddNodeVisualMetadata`; schema bump **v4→v5**. (Position `PositionX`/`PositionY` already
  exist from Sprint 08.)
- Otherwise reuses Sprint 08 workflow tables.

## Plugin Changes
- None. (Palette lists built-in node types; plugin nodes are future work.)

## UI Changes
- New **Workflows tool panel** (dockable anchorable) listing workflows with New/Rename/Delete;
  selecting one opens the designer (mirrors the Service Explorer → Runner selection path — not a
  Service Explorer root, whose CRUD switches are hard-coded to service/folder/endpoint).
- New **Workflow Designer document pane** (one per open workflow) with a Nodify canvas, palette,
  minimap, zoom controls, and a typed property inspector.
- Toolbar: run, save, undo/redo, zoom-to-fit.

## Acceptance Criteria
- Drag nodes from palette, connect them, and save; reopen restores layout.
- Zoom/pan/minimap function; fit-to-view works.
- Undo/redo reliably reverses/replays edits.
- Invalid connections are rejected.
- Running from the designer executes the underlying engine graph.

## Out of Scope
- Full live execution animation/telemetry (Sprint 13).
- Collaborative/multi-user editing.
- Auto-layout algorithms (may be future).

## Risks
- Nodify performance/behavior with large graphs.
- Keeping VM graph and domain graph in sync (source-of-truth discipline).
- Undo/redo correctness across compound operations.

## Future Improvements
- Auto-layout and alignment guides.
- Copy/paste and node grouping.
- Plugin-contributed custom nodes in the palette.

## Checklist
- [x] Nodify canvas + node/edge view models. (`WorkflowEditorView` + `NodeViewModel`/`ConnectionViewModel`/
  `PortViewModel`; connect validated by `ConnectorValidator`; disconnect via connector/connection.)
- [x] Node palette + drag & drop. (`NodePaletteViewModel` + palette strip; click-to-add and drag-onto-canvas.)
- [x] Zoom/pan/minimap. (Nodify `NodifyEditor` pan/zoom + `Minimap` overlay + `EditorCommands.FitToScreen`.)
- [x] Undo/redo service. (Application `UndoRedoService` + per-edit `IUndoableCommand`s; covers add/remove/
  move/connect/disconnect and property edits.)
- [x] Graph mapping + save + visual metadata migration. (`GraphMapper` VM↔domain; `WorkflowRepository`
  save; `AddNodeVisualMetadata` migration, schema v5. Live run status via engine `IProgress`.)

## Deviations from the original plan (design decisions)
- **Mapper/VM placement:** `GraphMapper` and the node/connection/port view-models live in **UI**
  (they reference UI VM types; Clean Architecture forbids Application → UI). The pure, reusable pieces
  — `ConnectorValidator`, `UndoRedoService`, `NodePortCatalog`, `WorkflowCatalogService` — stay in
  **Application** with unit tests. (The original table placed the mapper/undo-redo in Application.)
- **Entry point:** a dedicated **Workflows tool panel** rather than a "Workflows" root inside the
  Service Explorer, because the Explorer's CRUD/reorder/context-menu logic is hard-coded switches over
  service/folder/endpoint node types. Opening is **selection-driven** (mirroring the endpoint→Runner
  path), not double-click.
- **Live run status:** an optional `IProgress<NodeRunResult>` was added to `IWorkflowEngine.RunAsync`
  (backward-compatible) so nodes reflect Pending→Running→Passed/Failed during a run. Full telemetry
  remains Sprint 13.
- **Property inspector:** full typed per-kind editors bound to the public `*NodeConfig` records;
  `NodeConfigSerializer` was promoted to `public` so the UI round-trips config through the same
  serializer the engine handlers use (no drift).
- **Multi-instance documents:** a new `IWorkflowEditorViewModelFactory` seam creates one designer pane
  per workflow (the existing Runner is a single shared singleton; there was no prior factory pattern).
