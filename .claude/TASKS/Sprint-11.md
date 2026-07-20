# Sprint 11 — Assertions & Test Cases

## Goal
Add assertions and test cases with the `Assertion.*` plugins (JSON, Regex, Schema), organized into test suites with pass/fail reporting — turning requests and workflows into repeatable tests.

## Scope
- `IAssertion` implementations in Assertion.Json, Assertion.Regex, Assertion.Schema.
- Assertion authoring against responses (status, headers, body, timing).
- Test cases (request + assertions) grouped into test suites.
- Suite execution with aggregated pass/fail results and reporting.
- Integrate assertions into workflow Request nodes (S08) and the Runner (S06).

## Requirements
- Assertions are discovered plugins (S03) exposing a uniform evaluation contract.
- JSON assertions support JSONPath-style selection and comparisons.
- Schema assertions validate against JSON Schema.
- Regex assertions match against body/headers with capture support.
- Results are structured, explaining why an assertion passed/failed.

## Architecture Impact
- Defines the `IAssertion` contract in Plugin.Abstractions and an assertion-runner service.
- Test suites become a first-class persisted concept; results reused by Dashboard (S13).

## Projects (which solution projects change)
- Plugin.Abstractions — `IAssertion`, assertion context/result contracts.
- plugins/Assertion.Json, Assertion.Regex, Assertion.Schema.
- Domain — TestCase, TestSuite, Assertion, TestResult models.
- Application — assertion runner, suite execution, reporting.
- Infrastructure — persistence + migration.
- UI — assertion editor, suite runner, results view.
- Tests: PluginHost.Tests, Application.Tests, Domain.Tests.

## Classes
- `JsonAssertion`, `RegexAssertion`, `SchemaAssertion`.
- `TestCase`, `TestSuite`, `AssertionDefinition`, `AssertionResult`, `TestRunResult`.
- `AssertionRunner`, `TestSuiteExecutor`, `TestReportBuilder`.
- `AssertionEditorViewModel`, `TestResultsViewModel`.

## Interfaces
- `IAssertion` (evaluate against an assertion context).
- `IAssertionRunner`, `ITestSuiteExecutor`, `ITestReportBuilder`.
- `ITestCaseRepository`, `ITestSuiteRepository`.

## Database Changes
- New tables: `TestSuites`, `TestCases`, `Assertions`, `TestResults`.
- Migration: `AddTestsAndAssertions`.

## Plugin Changes
- Implement Assertion.Json / Assertion.Regex / Assertion.Schema against `IAssertion`.
- Each declares `IAssertionPlugin` capability in its manifest.

## UI Changes
- Assertion editor (attach to request/node; pick type, configure).
- Test suite runner panel with pass/fail tree and summary.
- Results/report view.

## Acceptance Criteria
- Attach JSON/regex/schema assertions to a request and evaluate them.
- A test suite runs all cases and reports aggregate pass/fail.
- Failed assertions include a clear reason/diff.
- Assertions run inside a workflow Request node.
- Assertion plugins load and appear in the editor's type list.

## Out of Scope
- CI/headless test runner CLI (possible future).
- Non-HTTP assertions.
- Data-driven test matrices (future).

## Risks
- JSONPath/JSON Schema library choice and offline bundling.
- Consistent assertion-context shape across Runner vs Workflow.
- Report format scope creep.

## Future Improvements
- Data-driven / parameterized tests.
- Export reports (JUnit/HTML) — ties into S14.
- Custom assertion scripting.

## Checklist
- [x] `IAssertion` contract + assertion context/result. *(scaffolded in S03; reused unchanged)*
- [x] JSON / Regex / Schema assertion plugins. *(json-everything: JsonPath.Net + JsonSchema.Net)*
- [x] Test case/suite models + migration. *(`AddTestsAndAssertions`, schema v7)*
- [x] Suite executor + report builder. *(`AssertionRunner`, `TestSuiteExecutor`, `TestReportBuilder`)*
- [x] Assertion editor + results UI + Runner/Workflow integration. *(Test Cases panel, Test Results doc, `AssertionNodeHandler`)*
