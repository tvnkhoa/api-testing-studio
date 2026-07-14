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

- **Node types:** `API`, `Condition`, `Loop`, `Delay`, `Parallel`, `Switch`, `Variable`,
  `Assertion`.
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

Domain records (`ApiTestingStudio.Domain`):

- `Workflow` — id, name, workspace id, `IReadOnlyList<WorkflowNode> Nodes`,
  `IReadOnlyList<NodeConnection> Connections`.
- `WorkflowNode` — id, `NodeKind` (enum: `Api`, `Condition`, `Loop`, `Delay`, `Parallel`,
  `Switch`, `Variable`, `Assertion`), position, and a node-specific config payload.
- `NodeConnection` — source node/port → target node/port; carries the data mapping expression.

Plugin contract (`ApiTestingStudio.Plugin.Abstractions`):

- `IWorkflowNode` — one implementation per node kind: `NodeKind Kind`, plus
  `Task<NodeResult> ExecuteAsync(NodeContext context, CancellationToken cancellationToken)`.
- The **workflow engine** in Core walks the graph, resolves connections/variables, dispatches to
  the registered `IWorkflowNode` for each node, and records steps.

Node kinds are discovered through the plugin registry, so new node types are added as plugins
without touching the engine.

## UI

- Visual node editor built with **Nodify** (canvas, pannable/zoomable, connectable ports).
- A node palette (toolbox) supplies draggable node types; the property panel edits the selected
  node's config; a connection drag creates a `NodeConnection`.
- Hosted in an **AvalonDock** document pane; runs stream live status onto each node (pending /
  running / passed / failed).
- MVVM throughout (CommunityToolkit.Mvvm); the canvas binds to a `WorkflowEditorViewModel`, no
  business logic in code-behind.

## Sprint

- **Sprint 08** — workflow **engine** (graph model, execution, node dispatch, run tree).
- **Sprint 09** — visual **node editor** UI via Nodify, drag-and-drop mapping.

## Open Questions / Future

- Optional advanced **scripting node** (sandboxed) for power users — deliberately deferred.
- Sub-workflows / reusable node groups.
- Loop strategies beyond count/collection (while-condition, retry-with-backoff).
- Visual diff of two workflow runs.
- Live collaborative editing is out of scope (100% offline product).
