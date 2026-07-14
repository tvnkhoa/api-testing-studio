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
- UI — designer panel, canvas, node/edge view models, palette, property panel.
- Application — graph mapping, undo/redo service, save orchestration.
- Domain — minor additions (node position/visual metadata).
- Tests: Application.Tests (mapping + undo/redo).

## Classes
- `WorkflowDesignerViewModel`, `NodeViewModel`, `ConnectionViewModel`, `NodePaletteViewModel`, `NodePropertiesViewModel`.
- `GraphMapper` (VM <-> domain), `UndoRedoService`, `EditCommand` (memento).
- `PortViewModel`, `ConnectorValidator`.

## Interfaces
- `IUndoRedoService`, `IGraphMapper`.
- `IConnectorValidator`, `INodeViewModelFactory`.

## Database Changes
- Add visual metadata to `WorkflowNodes` (x/y, size, color) via migration `AddNodeVisualMetadata`.
- Otherwise reuses Sprint 08 workflow tables.

## Plugin Changes
- None. (Palette lists built-in node types; plugin nodes are future work.)

## UI Changes
- New dockable Workflow Designer panel with Nodify canvas, palette, minimap, zoom controls, property inspector.
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
- [ ] Nodify canvas + node/edge view models.
- [ ] Node palette + drag & drop.
- [ ] Zoom/pan/minimap.
- [ ] Undo/redo service.
- [ ] Graph mapping + save + visual metadata migration.
