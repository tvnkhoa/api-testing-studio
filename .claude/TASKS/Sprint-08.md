# Sprint 08 — Workflow Engine

## Goal
Build the headless workflow execution engine: a node/edge graph executed with a shared context supporting request, condition, loop, parallel, and delay nodes — the "Workflow-first" heart of the product.

## Scope
- Execution engine that traverses nodes along edges with a `WorkflowContext`.
- Node types: Request, Condition (branch), Loop (for-each/while), Parallel (fan-out/in), Delay.
- Context: variables, previous-response access, data passing between nodes.
- Cancellation, per-node timeout, and error/failure propagation policies.
- Deterministic, testable engine independent of any UI.

## Requirements
- Nodes are pluggable/registered; new node types can be added without engine changes.
- Reuses the S06 `IRequestExecutor` for Request nodes.
- Parallel nodes bounded by a configurable degree of parallelism.
- Execution produces a structured run result (per-node status, outputs, timings).
- Loops guard against infinite iteration.

## Architecture Impact
- Introduces the workflow domain model (graph) and an engine service reused by the Designer (S09) and Dashboard (S13).
- Node-handler registry pattern; strategy per node type.

## Projects (which solution projects change)
- Domain — Workflow, Node, Edge, WorkflowContext, RunResult models.
- Application — execution engine, node handlers, run orchestration.
- Infrastructure — workflow persistence + migration; run-result store.
- Tests: Domain.Tests, Application.Tests.

## Classes
- `Workflow`, `WorkflowNode`, `WorkflowEdge`, `WorkflowContext`, `WorkflowRunResult`, `NodeRunResult`.
- `WorkflowEngine`, `NodeHandlerRegistry`.
- `RequestNodeHandler`, `ConditionNodeHandler`, `LoopNodeHandler`, `ParallelNodeHandler`, `DelayNodeHandler`.
- `VariableResolver` (expression/interpolation).

## Interfaces
- `IWorkflowEngine`, `INodeHandler`, `INodeHandlerRegistry`.
- `IWorkflowContext`, `IVariableResolver`.
- `IWorkflowRepository`, `IWorkflowRunStore`.

## Database Changes
- New tables: `Workflows`, `WorkflowNodes`, `WorkflowEdges` (graph persistence).
- Optional `WorkflowRuns` / `WorkflowRunNodes` for run history (uncertain — may defer to S13).
- Migration: `AddWorkflows`.

## Plugin Changes
- None yet. (Node types may become plugin-contributed later; core node types are built-in this sprint.)

## UI Changes
- None. (Visual designer is Sprint 09; engine is headless and tested via unit tests.)

## Acceptance Criteria
- A linear request->request workflow runs and passes data via context.
- Condition node branches correctly on response values.
- Loop iterates a collection and aggregates results.
- Parallel node executes branches concurrently within the degree limit.
- Cancellation stops execution promptly; per-node errors follow the failure policy.

## Out of Scope
- Visual editing / canvas (Sprint 09).
- Assertions inside nodes (Sprint 11 integration).
- Distributed/multi-machine execution.

## Risks
- Concurrency correctness in parallel/loop nodes and context mutation.
- Expression/variable language scope creep.
- Persisting/versioning graph schema as node types evolve.

## Future Improvements
- Sub-workflows / reusable node groups.
- Retry/backoff and circuit-breaker nodes.
- Pluggable custom node types.

## Checklist
- [x] Graph domain model + persistence + migration. (`WorkflowNode`/`WorkflowEdge` entities + runtime
  `Workflow` aggregate; `WorkflowRepository`; `AddWorkflows` migration, schema **v4**. Kept the
  existing `WorkflowDefinition`/`Workflows` root; child tables are additive.)
- [x] Engine traversal + context + variable resolver. (`WorkflowEngine` walks entry→edges; thread-safe
  `WorkflowContext`; `VariableResolver` does `{{vars.x}}`/`{{Node.key}}` interpolation with JSON-path.)
- [x] Request/Condition/Loop/Parallel/Delay handlers. (Built-in `INodeHandler`s dispatched via
  `NodeHandlerRegistry` keyed by `WorkflowNodeKind`; `RequestNodeHandler` reuses the S06
  `IRequestExecutor`. Container nodes drive their `body` branch through a `BranchExecutor`, so the
  engine stays closed to new kinds.)
- [x] Cancellation + timeout + failure policies. (Linked `CancellationTokenSource` + `CancelAfter`
  per leaf node; `NodeFailurePolicy` Stop/Continue; loop iteration cap; caller-cancel distinguished
  from timeout via the `when (ct.IsCancellationRequested)` filter.)
- [x] Run result model + comprehensive unit tests. (`NodeRunResult` (+`Children`)/`WorkflowRunResult`;
  in-memory `IWorkflowRunStore` — durable tables deferred to S13. 25 new tests: resolver, registry,
  and engine acceptance criteria. Full suite green: 188 tests, build clean at 0 warnings.)

## Deviations from the original plan
- The engine dispatches through an Application-layer `INodeHandler` (built-in), not the plugin-facing
  `IWorkflowNode`; the latter is retained for future plugin-contributed node kinds.
- Node handlers receive a `NodeHandlerContext` (node + workflow + context + resolver + options +
  `RunBranch`) rather than a bare `(node, context)` pair, so Loop/Parallel can execute sub-branches
  without the engine knowing specific kinds.
- Condition branching uses a small fixed operator set on the node config (`ConditionOperator`),
  keeping comparison logic out of the (interpolation-only) `VariableResolver`.
- Run-result persistence tables (`WorkflowRuns`/`WorkflowRunNodes`) deferred to Sprint 13 per the
  doc's own "uncertain" note; only an in-memory run store ships now.
