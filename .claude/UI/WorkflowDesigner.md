# Workflow Designer

> UX rationale & benchmark (n8n / Node-RED): see `../UI_BENCHMARK.md` (Feature Mapping, Create/Execute Workflow journeys).

## Overview

The **Workflow Designer** is the visual canvas for composing a workflow as a graph of nodes and
edges — the product's core differentiator (see `FEATURES/Workflow.md`). It is built on **Nodify**
and hosted in an AvalonDock **document** pane (one document per open workflow). A user assembles a
scenario — log in, capture a token, create an order, assert the result — entirely by placing nodes
and wiring data between them, with **no scripting**.

## Scope / Capabilities

- **Nodes** — one visual node per `WorkflowNodeKind` (`Api`, `Condition`, `Loop`, `Delay`,
  `Parallel`, `Switch`, `Variable`, `Assertion`). Each node exposes typed input/output ports.
- **Edges** — connections drag from an output port to an input port, creating a `NodeConnection`.
- **Drag & drop** — a node palette (toolbox) supplies draggable node types onto the canvas;
  Endpoints can also be dragged from the Service Explorer onto the canvas to create a pre-configured
  `Api` node (method, resolved URL, default headers and body pulled from the saved endpoint).
- **Visual data mapping** — an output field of one node (e.g. `Login.token`) is wired to a later
  node's input without code; mappings resolve through the variable substitution engine.
- **Zoom / pan** and a **minimap** for large graphs (both Nodify features).
- **Undo / redo** over all canvas edits (add/move/delete node, connect/disconnect).
- **Live run status** — during execution each node reflects Pending / Running / Passed / Failed.

## Domain & Contracts

Domain records (`ApiTestingStudio.Domain`):

- `Workflow` — id, name, workspace id, `Nodes`, `Connections`.
- `WorkflowNode` — id, `WorkflowNodeKind`, canvas position, node-specific config payload.
- `NodeConnection` — source node/port → target node/port, plus the data-mapping expression.

Plugin contract: `IWorkflowNode` (`Kind: WorkflowNodeKind`, `ExecuteAsync(NodeExecutionContext)`).
The designer edits the graph; the **Sprint 08 engine** in Core executes it by dispatching each
node to its registered `IWorkflowNode`. New node kinds arrive as plugins without designer changes.

## UI

- MVVM (CommunityToolkit.Mvvm). The Nodify `NodifyEditor` binds to a `WorkflowEditorViewModel`
  (a `DocumentPanelViewModel`, one pane per workflow, deterministic `ContentId =
  "document.workflow.{id}"` so AvalonDock restores it) exposing `Nodes`, `Connections`,
  `SelectedNode`, `PendingConnection`, and a command surface (AddNode/DeleteSelection/Connect/
  Disconnect/Undo/Redo/Save/Run/ZoomToFit). `NodePropertiesViewModel` provides typed per-kind editors
  for the selected node's config (bound to the public `*NodeConfig` records via `NodeConfigSerializer`);
  edits route through the undo stack. No business logic in code-behind. The **Assertion** node's
  inspector edits its source node plus a list of assertions (add/edit/remove), reusing the shared
  `IDialogService.PromptAssertion` dialog and the assertion kinds contributed by the loaded
  `IAssertion` plugins; each list change is committed as an undoable `EditNodeCommand`.
- **Entry point:** a dockable **Workflows tool panel** (`WorkflowsPanelViewModel`) lists workflows
  (New/Rename/Delete via `IWorkflowCatalogService`); selecting one publishes `OpenWorkflowMessage`,
  which the shell handles by opening/focusing the designer pane (created via
  `IWorkflowEditorViewModelFactory`).
- Graph ↔ domain mapping is done by the UI-side `GraphMapper` (+ `INodeViewModelFactory`), using the
  Application `NodePortCatalog` for port names so the designer can only produce engine-traversable
  edges. `ConnectorValidator` (Application) rejects self/duplicate/unknown-port/wrong-direction edges.
- **Endpoint drop:** the Service Explorer starts an endpoint drag (payload = endpoint id, via the
  shared `DragFormats.EndpointRef` key); the canvas `Drop` handler calls
  `WorkflowEditorViewModel.AddApiNodeFromEndpointAsync`, which resolves the endpoint + its service
  (`IEndpointRepository`/`IServiceRepository`), builds a `RequestNodeConfig` (URL composed from the
  service base URL + endpoint path) and adds the node through the undo stack. Drag-source and
  drop-target views share the format-key contract via `DragFormats` (no magic strings).
- Material Design iconography per node kind; verb colour coding via `HttpVerbToBrushConverter` and
  live per-node status via `RunStatusToBrushConverter` (`Semantic.*` tokens); status always shows a
  label, never colour alone.

## Sprint

- **Sprint 09** — Nodify canvas, nodes/edges, drag & drop, zoom, minimap, undo/redo, visual mapping,
  typed property inspector, Workflows panel entry point, and run-from-designer with live per-node
  status streamed via `IWorkflowEngine.RunAsync`'s optional `IProgress<NodeRunResult>`.
- Depends on Sprint 08 (workflow engine, graph model, execution, run tree).

## Open Questions / Future

- Sub-workflows / reusable node groups; auto-layout of imported graphs.
- Visual diff of two workflow runs.
- Optional sandboxed scripting node (deliberately deferred).
