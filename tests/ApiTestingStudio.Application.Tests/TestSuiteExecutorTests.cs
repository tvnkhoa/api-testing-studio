using ApiTestingStudio.Application.Testing;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class TestSuiteExecutorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 17, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private sealed class Harness
    {
        public InMemoryTestCaseRepository Cases { get; } = new();
        public InMemoryTestSuiteRepository Suites { get; } = new();
        public InMemoryTestResultRepository Results { get; } = new();
        public FakeCatalogStore Catalog { get; } = new();
        public FakeRequestExecutionService RequestExecution { get; } = new();
        public FakeWorkflowEngine WorkflowEngine { get; } = new();
        public InMemoryWorkflowRepository Workflows { get; } = new();
        public FakeAssertion Assertion { get; } = new("json", AssertionOutcome.Passed);

        public TestSuiteExecutor Build() => new(
            Cases,
            Suites,
            Results,
            new InMemoryEndpointRepository(Catalog),
            new InMemoryServiceRepository(Catalog),
            RequestExecution,
            Workflows,
            WorkflowEngine,
            new AssertionRunner([Assertion]),
            new FixedClock(Now));
    }

    private static Endpoint SeedEndpoint(FakeCatalogStore catalog)
    {
        var service = new Service { WorkspaceId = WorkspaceId, Name = "Svc", BaseUrl = "https://api.example.com" };
        var endpoint = new Endpoint { ServiceId = service.Id, Name = "Get", Method = HttpVerb.Get, Path = "/ping" };
        catalog.Services.Add(service);
        catalog.Endpoints.Add(endpoint);
        return endpoint;
    }

    [Fact]
    public async Task Endpoint_case_runs_request_and_records_passing_result()
    {
        var h = new Harness();
        var endpoint = SeedEndpoint(h.Catalog);
        var testCase = new TestCaseDefinition { WorkspaceId = WorkspaceId, EndpointId = endpoint.Id, Name = "Case" };
        h.Cases.Cases.Add(testCase);
        h.Cases.Assertions.Add(new AssertionDefinition { TestCaseId = testCase.Id, Kind = "json", Source = AssertionSource.StatusCode, Expected = "200" });

        var result = await h.Build().RunCaseAsync(testCase.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(RunStatus.Passed);
        result.Value.PassedCount.Should().Be(1);
        h.RequestExecution.CallCount.Should().Be(1);
        h.RequestExecution.LastRequest!.Url.Should().Be("https://api.example.com/ping");
        h.Results.Results.Should().ContainSingle();
    }

    [Fact]
    public async Task Endpoint_case_with_failing_assertion_is_failed()
    {
        var h = new Harness();
        h.Assertion.Outcome = AssertionOutcome.Failed;
        var endpoint = SeedEndpoint(h.Catalog);
        var testCase = new TestCaseDefinition { WorkspaceId = WorkspaceId, EndpointId = endpoint.Id, Name = "Case" };
        h.Cases.Cases.Add(testCase);
        h.Cases.Assertions.Add(new AssertionDefinition { TestCaseId = testCase.Id, Kind = "json", Source = AssertionSource.Body, Expected = "x" });

        var result = await h.Build().RunCaseAsync(testCase.Id);

        result.Value.Status.Should().Be(RunStatus.Failed);
        result.Value.FailedCount.Should().Be(1);
    }

    [Fact]
    public async Task Endpoint_transport_failure_yields_failed_result_with_execution_note()
    {
        var h = new Harness();
        h.RequestExecution.ResultToReturn = Result.Failure<HttpExecutionResult>(new Error("request.timeout", "timed out"));
        var endpoint = SeedEndpoint(h.Catalog);
        var testCase = new TestCaseDefinition { WorkspaceId = WorkspaceId, EndpointId = endpoint.Id, Name = "Case" };
        h.Cases.Cases.Add(testCase);

        var result = await h.Build().RunCaseAsync(testCase.Id);

        result.Value.Status.Should().Be(RunStatus.Failed);
        var details = TestResultDetails.Deserialize(result.Value.DetailsJson);
        details.Should().ContainSingle().Which.Kind.Should().Be("execution");
    }

    [Fact]
    public async Task Missing_case_returns_failure()
    {
        var h = new Harness();
        var result = await h.Build().RunCaseAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("test.case_not_found");
    }

    [Fact]
    public async Task Workflow_case_evaluates_against_last_api_node_outputs()
    {
        var h = new Harness();
        var workflow = new Workflow { WorkspaceId = WorkspaceId, Name = "WF" };
        h.Workflows.Workflows.Add(workflow);
        h.WorkflowEngine.ResultToReturn = new WorkflowRunResult
        {
            WorkflowId = workflow.Id,
            Status = RunStatus.Passed,
            Duration = TimeSpan.FromMilliseconds(50),
            Nodes =
            [
                new NodeRunResult
                {
                    NodeName = "Login",
                    Kind = WorkflowNodeKind.Api,
                    Status = RunStatus.Passed,
                    Outputs = new Dictionary<string, string> { ["status"] = "200", ["body"] = "{\"t\":1}" },
                },
            ],
        };
        var testCase = new TestCaseDefinition { WorkspaceId = WorkspaceId, WorkflowId = workflow.Id, Name = "WF Case" };
        h.Cases.Cases.Add(testCase);
        h.Cases.Assertions.Add(new AssertionDefinition { TestCaseId = testCase.Id, Kind = "json", Source = AssertionSource.StatusCode, Expected = "200" });

        var result = await h.Build().RunCaseAsync(testCase.Id);

        result.Value.Status.Should().Be(RunStatus.Passed);
        h.Assertion.LastContext!.Actual.Should().Be("200");
    }

    [Fact]
    public async Task Suite_run_aggregates_all_cases()
    {
        var h = new Harness();
        var suite = new TestSuite { WorkspaceId = WorkspaceId, Name = "Suite" };
        h.Suites.Suites.Add(suite);
        var endpoint = SeedEndpoint(h.Catalog);
        for (var i = 0; i < 3; i++)
        {
            var testCase = new TestCaseDefinition { WorkspaceId = WorkspaceId, TestSuiteId = suite.Id, EndpointId = endpoint.Id, Name = $"Case {i}" };
            h.Cases.Cases.Add(testCase);
            h.Cases.Assertions.Add(new AssertionDefinition { TestCaseId = testCase.Id, Kind = "json", Source = AssertionSource.StatusCode, Expected = "200" });
        }

        var result = await h.Build().RunSuiteAsync(suite.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        h.Results.Results.Should().HaveCount(3);
    }
}
