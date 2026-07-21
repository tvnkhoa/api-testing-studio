# Workflow

## Overview

The Workflow feature is the product's core differentiator. It lets a user compose an API test
scenario as a **visual graph of nodes** rather than a script. A workflow chains requests,
branching, iteration, and assertions into a single runnable unit. The goal is that a normal
workflow — log in, capture a token, create an order, assert the result — requires **no scripting
at all**; data flows between nodes through drag-and-drop mapping.

Everything belongs to a Workspace. A `Workflow` references Endpoints, Profiles, Environments, and
Variables, and produces a `Run` tree (see `Logging.md`) when executed.

## Scope / Capabilities

- **Node types:** `API`, `Condition`, `Loop`, `Delay`, `Parallel`, `Assertion` are **implemented**
  (each has an engine `INodeHandler`). `Switch` and `Variable` are defined in the `WorkflowNodeKind`
  enum but **not yet implemented** — they have no handler, so the designer palette is driven by the
  registered handlers (`INodeHandlerRegistry.SupportedKinds`) and does not offer them until their
  handlers land. See the recovery backlog.
- **Drag-and-drop data mapping:** an output field of one node (e.g. `Login.token`) is wired to an
  input of a later node without writing code. Mappings resolve through the variable substitution
  engine (see `Variables.md`).
- **Run As:** a workflow (or a single node) can execute under a Profile; authorization is swapped
  automatically (see `Profiles.md`).
- **Deterministic execution order** with explicit parallelism only where a `Parallel` node is used.
- **Cancellation** at any point; every long-running node honours a `CancellationToken`.
- **Replayable output** — each run is persisted as a step tree.

Scripting is intentionally **not** required for normal flows. An advanced scripting node is out of
scope for the first release (see Open Questions).

## Domain & Contracts

Domain records (`ApiTestingStudio.Domain.Entities`):

- `WorkflowDefinition` — the persisted root row (`Workflows` table): id, name, workspace id,
  description. The graph lives in the sibling `WorkflowNode` / `WorkflowEdge` tables keyed by
  `WorkflowId`.
- `WorkflowNode` — id, `WorkflowId`, `WorkflowNodeKind Kind` (`Api`, `Condition`, `Loop`, `Delay`,
  `Parallel`, `Switch`, `Variable`, `Assertion`), name, canvas position, and a node-specific JSON
  `Config` payload.
- `WorkflowEdge` — id, `WorkflowId`, source/target node ids, optional source/target port (branch
  outputs like a Condition's `true`/`false` or a container's `body`), and an optional data-mapping
  expression.
- `Workflow` — the **runtime aggregate** the engine executes: a `WorkflowDefinition` hydrated with
  `IReadOnlyList<WorkflowNode> Nodes` + `IReadOnlyList<WorkflowEdge> Edges`. Assembled by
  `IWorkflowRepository`; not a table of its own.
- Run results: `NodeRunResult` (per-node status/outputs/error/duration, plus `Children` for
  Loop/Parallel branches) and `WorkflowRunResult` (overall status + ordered node results).

Engine contracts (`ApiTestingStudio.Application.Workflows`):

- `IWorkflowEngine` / `WorkflowEngine` — headless, UI-independent. Walks the graph from the entry
  node, resolves per-node timeouts (linked `CancellationTokenSource` + `CancelAfter`), dispatches
  each node to its handler, applies the `NodeFailurePolicy`, and returns a `WorkflowRunResult`.
- `INodeHandler` / `INodeHandlerRegistry` — one built-in handler per kind, resolved by
  `WorkflowNodeKind`. Container handlers (Loop/Parallel) drive their body branch through a
  `BranchExecutor` the engine supplies, so the engine stays closed to new node kinds.
  Built-in handlers this sprint: `RequestNodeHandler` (reuses the S06 `IRequestExecutor`),
  `ConditionNodeHandler`, `LoopNodeHandler`, `ParallelNodeHandler`, `DelayNodeHandler`.
- `IWorkflowContext` / `WorkflowContext` — mutable, thread-safe variables + per-node outputs
  (parallel branches write under distinct node names, so no collisions).
- `IVariableResolver` / `VariableResolver` — `{{vars.name}}` / `{{Node.key}}` interpolation with
  JSON-path access into node outputs (e.g. `{{Login.body.data.token}}`).
- `IWorkflowRepository` (persistence port) and `IWorkflowRunStore` (in-memory run history this
  sprint; durable tables deferred to Sprint 13).

A plugin-facing `IWorkflowNode` contract also exists in `Plugin.Abstractions` for **future**
plugin-contributed node kinds; the Sprint 08 engine dispatches through the built-in `INodeHandler`
strategy and does not yet load node plugins.

## UI

- Visual node editor built with **Nodify** (canvas, pannable/zoomable, connectable ports).
- A node palette (toolbox) supplies draggable node types; a typed property panel edits the selected
  node's config; a connection drag creates a `WorkflowEdge` (validated by `ConnectorValidator`).
- Reached via a dockable **Workflows tool panel** (list + New/Rename/Delete); selecting a workflow
  opens its designer in an **AvalonDock** document pane (one pane per workflow). Runs stream live
  status onto each node (pending / running / passed / failed) via the engine's optional
  `IProgress<NodeRunResult>` — Sprint 09 wires this "basic" live status; richer telemetry is Sprint 13.
- MVVM throughout (CommunityToolkit.Mvvm); the canvas binds to a `WorkflowEditorViewModel`, no
  business logic in code-behind. Undo/redo covers every edit via a reusable Application
  `IUndoRedoService`.

## Sprint

- **Sprint 08** — workflow **engine** (graph model + persistence, execution, node dispatch, run
  tree). Delivered: Domain graph records, `AddWorkflows` migration (schema v4), the headless
  `WorkflowEngine` with Request/Condition/Loop/Parallel/Delay handlers, variable resolver,
  cancellation + per-node timeout + failure policies, and comprehensive unit tests.
- **Sprint 09** — visual **node editor** UI via Nodify, drag-and-drop mapping.

## Open Questions / Future

- Optional advanced **scripting node** (sandboxed) for power users — deliberately deferred.
- Sub-workflows / reusable node groups.
- Loop strategies beyond count/collection (while-condition, retry-with-backoff).
- Visual diff of two workflow runs.
- Live collaborative editing is out of scope (100% offline product).
