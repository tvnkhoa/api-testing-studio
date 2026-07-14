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
- [ ] Graph domain model + persistence + migration.
- [ ] Engine traversal + context + variable resolver.
- [ ] Request/Condition/Loop/Parallel/Delay handlers.
- [ ] Cancellation + timeout + failure policies.
- [ ] Run result model + comprehensive unit tests.
