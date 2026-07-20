# Assertions & Test Cases

## Overview

Sprint 11 turns requests and workflows into **repeatable, verifiable tests**. Assertions evaluate a
response (status / headers / body / timing); test cases pair a target with a set of assertions; test
suites group cases and run them together with aggregate pass/fail reporting. The same assertion
evaluation is reused inside workflow **Assertion nodes**.

Everything belongs to a Workspace.

## Assertions (plugins)

Assertions are discovered **plugins** (Sprint 03) implementing `IAssertion`
(`Plugin.Abstractions/Assertions/IAssertion.cs`), each with a stable `Kind` and an `EvaluateAsync`
taking a flat `AssertionContext(Actual, Expected, Options)` → `AssertionEvaluation(Outcome, Message)`.
The plugin contract is unchanged from its Sprint 03 scaffold; Sprint 11 fills in the evaluation logic.

- **`Assertion.Json`** (`json`) — parses the actual value as JSON, optionally selects a node via a
  JSONPath (`Options["path"]`, `JsonPath.Net`), and compares to `Expected` using `Options["operator"]`
  (`equals` / `notEquals` / `contains` / `exists` / `notExists` / `gt` / `gte` / `lt` / `lte`;
  default `equals`).
- **`Assertion.Regex`** (`regex`) — matches the actual value against `Options["pattern"]` (falling
  back to `Expected`); `Options["operator"]` is `matches` (default) or `notMatches`;
  `Options["ignoreCase"]` toggles case-insensitivity; capture groups are surfaced in the message.
- **`Assertion.Schema`** (`schema`) — validates the actual JSON against the JSON Schema in `Expected`
  (`JsonSchema.Net`), reporting the validation errors on failure.

Malformed input never throws — it becomes a `Failed` (or `Skipped`) evaluation with a clear reason.
The JSONPath / JSON Schema libraries are the **json-everything** suite (`JsonPath.Net`,
`JsonSchema.Net`), System.Text.Json-native and fully offline (pinned in `Directory.Packages.props`).

## The assertion runner (consistent context)

`IAssertionRunner` / `AssertionRunner` (`Application/Testing/`) is the single mapper from an
`HttpExecutionResult` to the assertion `Actual`: it resolves each assertion's `AssertionSource`
(`StatusCode` / `ReasonPhrase` / `Header` (by `Target`) / `Body` / `TimingTotalMs`) into a string,
picks the plugin by `Kind`, and evaluates. Both the test-suite executor and the workflow Assertion
node call this runner, so the assertion context is identical in both — addressing the "consistent
context across Runner vs Workflow" requirement. Disabled assertions and unknown kinds come back as
`Skipped` with a reason.

## Domain model

Immutable records in `Domain/Entities/Automation.cs`:

- `TestSuite` — a named grouping of cases.
- `TestCaseDefinition` — a case targeting **either** an endpoint request (`EndpointId`) **or** a
  workflow (`WorkflowId`); belongs to an optional `TestSuiteId`.
- `TestCase` — the runtime aggregate (a `TestCaseDefinition` + its ordered `AssertionDefinition`s),
  mirroring the `Workflow` aggregate.
- `AssertionDefinition` — a persisted assertion (`Kind`, `Source`, `Target`, `Expression`,
  `Operator`, `Expected`, `Enabled`, `SortOrder`).
- `AssertionResult` — one assertion's runtime outcome (serialized into `TestRunResult.DetailsJson`).
- `TestRunResult` — one case execution: denormalized aggregate (status, passed/failed/skipped counts,
  duration, timestamp) + a JSON `DetailsJson` blob of per-assertion results, mirroring
  `RequestHistoryEntry`.

`AssertionSource` and the reused `AssertionOutcome` / `RunStatus` live in `Domain/Enums/DomainEnums.cs`.

## Execution & reporting

- `ITestSuiteExecutor` / `TestSuiteExecutor` (`Application/Testing/`) runs a case (endpoint request via
  the Sprint 06 `IRequestExecutionService`, or a workflow via the Sprint 08 `IWorkflowEngine`),
  evaluates assertions through `IAssertionRunner`, builds a `TestRunResult`, and persists it. A suite
  run executes each case and reports per-case progress via `IProgress<TestRunResult>`. For workflow
  cases, assertions evaluate against the last API node's published `status`/`reason`/`body` outputs
  (`SyntheticHttpResponse`).
- `ITestReportBuilder` / `TestReportBuilder` aggregates results into a `TestSuiteReport` (case + assertion tallies).
- Typed errors in `TestingErrors` (`test.*`).

## Workflow integration

`AssertionNodeHandler` (`Application/Workflows/Handlers/`) implements `INodeHandler` for
`WorkflowNodeKind.Assertion` (registered in `AddApplication`). It reads a named upstream node's
response outputs (`AssertionNodeConfig.SourceNode`), runs its `Assertions` through the shared
`IAssertionRunner`, and passes only when every assertion passes.

## Persistence

Tables `TestSuites`, `Assertions`, `TestResults` (+ new `TestCases` columns) via migration
`AddTestsAndAssertions`; workspace schema version bumped to **7**. Repositories `ITestSuiteRepository`,
`ITestCaseRepository` (hydrates the case+assertions aggregate; replaces child rows wholesale),
`ITestResultRepository`. See `DATABASE_GUIDELINES.md`.

## UI

- **Test Cases** tool panel (`TestCasesPanelViewModel` + `TestCasesPanelView`): suites / cases /
  assertions with New/Edit/Delete and Run Case / Run Suite. The assertion editor's **type list is
  driven from the loaded assertion plugins**. Follows the Sprint 10 Profiles-panel pattern.
- **Test Results** document (`TestResultsViewModel` + `TestResultsView`): an aggregate summary plus a
  pass/fail tree of cases → assertions, opened on a completed run via `ShowTestResultsMessage`.
- Modal editors `TestCaseEditorDialog` and `AssertionEditorDialog` (via `IDialogService`).

## Sprint

- **Sprint 11** — assertion plugin logic, test case/suite domain + persistence, suite executor +
  report builder, the Assertion workflow node, and the Test Cases / results UI.

## Out of scope / future

- CI/headless test runner CLI; non-HTTP assertions; data-driven / parameterized test matrices.
- Report export (JUnit / HTML) — ties into a later sprint; custom assertion scripting.
