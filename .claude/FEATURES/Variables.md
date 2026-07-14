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

Application service (`ApiTestingStudio.Application`):

- A variable-resolution service builds a merged, precedence-ordered lookup (Global → Workspace →
  Environment → Workflow → Local → WorkflowOutput) and substitutes `{{...}}` tokens. Dynamic tokens
  (`$guid`, `$random`, `$now`) are evaluated per substitution; `$now` uses the `IClock` port so it
  is testable and deterministic under test.

Resolution is invoked by the request runner and the workflow engine before a request is sent.

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
