# Variables

## Overview

Variables let requests and workflows stay data-driven and portable across environments. A value
written once (a base URL, a token, an id captured from a previous response) is referenced by name
and resolved at execution time. Variables also carry data **between workflow nodes** without
scripting — the mechanism behind drag-and-drop mapping (see `Workflow.md`).

## Scope / Capabilities

**Scopes**, in precedence order from broadest to narrowest (a narrower scope overrides a broader
one when the same name exists):

1. **Global** — across all Workspaces.
2. **Workspace** — the current Workspace.
3. **Environment** — the selected environment.
4. **Workflow** — variables defined on a workflow.
5. **Local** — a single request/step.
6. **WorkflowOutput** — values captured from node outputs during a run (narrowest; e.g.
   `Login.token`).

**Substitution** uses `{{ ... }}` tokens:

- Reference captured output — `{{Login.token}}`, `{{CreateOrder.id}}`.
- Built-in dynamic values — `{{$guid}}`, `{{$random}}`, `{{$now}}`.

**Environments:** Development, QA, Staging, Production. Switching the active environment re-resolves
environment-scoped variables. Sensitive values that live in Profiles are protected separately (see
`Profiles.md`).

## Domain & Contracts

Domain records (`ApiTestingStudio.Domain`):

- `Variable` — id, name, value, `VariableScope` (enum: `Global`, `Workspace`, `Environment`,
  `Workflow`, `Local`, `WorkflowOutput`).
- `Environment` — id, name (`EnvironmentKind`: `Development`, `QA`, `Staging`, `Production`),
  associated variables.

Application services (`ApiTestingStudio.Application`):

- `IVariableService` — CRUD for variables per scope; environment-scoped variables carry an
  `EnvironmentId`; secret variables (`IsSecret`) have their value encrypted via `ISecretProtector`.
- `IEnvironmentService` — environment CRUD plus the active-environment selection (persisted as the
  per-workspace `Settings` row `active-environment-id`).
- `IVariableScopeSeeder` (Sprint 10) loads the persisted Global/Workspace/Environment(active) scopes,
  **decrypting secret values**, and seeds them into an `IWorkflowContext` in precedence order
  (broadest first, so the narrower scope overwrites). The narrower runtime scopes (Workflow, Local,
  WorkflowOutput) are set later on the same context and therefore win.
- The existing `VariableResolver` (Sprint 08) substitutes `{{...}}` tokens against that context —
  `{{name}}`, `{{vars.x}}`, `{{Node.field}}` with JSON traversal. It was **not** rewritten; Sprint 10
  only enriches the variables that feed it.

Resolution is invoked by the request runner (`RequestExecutionService` builds a seeded context and
resolves URL/headers/query/body) and by the workflow engine (the caller seeds the context it passes
to `RunAsync`) before a request is sent.

> Dynamic tokens (`$guid`, `$random`, `$now`) and inline `{{...}}` autocomplete remain future work.

## UI

- A **Variables** editor grouped by scope; an **Environment** switcher in the main toolbar.
- Inline autocomplete for `{{...}}` tokens (available names + built-ins) in request/URL/body fields,
  rendered in the **Monaco** editor (WebView2).
- MVVM (CommunityToolkit.Mvvm); Material Design; unresolved tokens are highlighted.

## Sprint

- Delivered alongside the workflow/runner work; `WorkflowOutput` capture and `{{Node.field}}`
  mapping land with the workflow engine (**Sprint 08**). Environment management surfaces with the
  broader environment/variable UI.

## Open Questions / Future

- Additional built-in tokens (`$timestamp`, `$randomInt(min,max)`, `$env(NAME)`).
- Secret-typed variables that route through `ISecretProtector` instead of Profiles.
- Nested / computed variables and default-value fallback syntax.
- Per-environment variable diff and bulk edit.
- Escaping literal `{{`/`}}` in payloads.
