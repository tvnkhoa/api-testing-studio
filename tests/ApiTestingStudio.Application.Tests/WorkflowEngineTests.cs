using ApiTestingStudio.Application.Variables;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;
using FluentAssertions;
using static ApiTestingStudio.Application.Tests.WorkflowTestHarness;

namespace ApiTestingStudio.Application.Tests;

public sealed class WorkflowEngineTests
{
    // --- Acceptance: linear request -> request passing data via context ---

    [Fact]
    public async Task Linear_workflow_passes_data_via_context()
    {
        var executor = new FakeRequestExecutor
        {
            ResultToReturn = Result.Success(new HttpExecutionResult
            {
                Response = new HttpResponseModel
                {
                    StatusCode = 200,
                    ReasonPhrase = "OK",
                    Body = "{\"token\":\"abc\"}",
                },
                Timing = new RequestTiming { Total = TimeSpan.Zero },
            }),
        };
        var engine = CreateEngine(executor);

        var login = Node("Login", WorkflowNodeKind.Api, ConfigJson(new RequestNodeConfig
        {
            Method = HttpVerb.Post,
            Url = "https://api.example.com/login",
        }));
        var orders = Node("Orders", WorkflowNodeKind.Api, ConfigJson(new RequestNodeConfig
        {
            Method = HttpVerb.Get,
            Url = "https://api.example.com/orders",
            Headers = [new HttpHeader("Authorization", "Bearer {{Login.body.token}}")],
        }));
        var workflow = Graph([login, orders], [Edge(login, orders)]);

        var run = await engine.RunAsync(workflow);

        run.Status.Should().Be(RunStatus.Passed);
        run.Nodes.Should().HaveCount(2);
        executor.LastRequest!.Headers.Should().ContainSingle()
            .Which.Value.Should().Be("Bearer abc");
    }

    // --- Acceptance: condition node branches on a response value ---

    [Fact]
    public async Task Condition_node_branches_on_value()
    {
        var executor = new FakeRequestExecutor
        {
            ResultToReturn = Result.Success(new HttpExecutionResult
            {
                Response = new HttpResponseModel
                {
                    StatusCode = 200,
                    ReasonPhrase = "OK",
                    Body = "{\"role\":\"admin\"}",
                },
                Timing = new RequestTiming { Total = TimeSpan.Zero },
            }),
        };
        var fake = new FakeNodeHandler(WorkflowNodeKind.Assertion);
        var engine = CreateEngine(executor, extraHandlers: fake);

        var check = Node("Check", WorkflowNodeKind.Api, ConfigJson(new RequestNodeConfig { Url = "https://api.example.com/me" }));
        var condition = Node("IsAdmin", WorkflowNodeKind.Condition, ConfigJson(new ConditionNodeConfig
        {
            Left = "{{Check.body.role}}",
            Operator = ConditionOperator.Equals,
            Right = "admin",
        }));
        var adminPath = Node("AdminPath", WorkflowNodeKind.Assertion);
        var guestPath = Node("GuestPath", WorkflowNodeKind.Assertion);

        var workflow = Graph(
            [check, condition, adminPath, guestPath],
            [
                Edge(check, condition),
                Edge(condition, adminPath, sourcePort: "true"),
                Edge(condition, guestPath, sourcePort: "false"),
            ]);

        var run = await engine.RunAsync(workflow);

        run.Status.Should().Be(RunStatus.Passed);
        fake.CalledNodes.Should().Contain("AdminPath");
        fake.CalledNodes.Should().NotContain("GuestPath");
    }

    // --- Acceptance: loop iterates a collection and aggregates results ---

    [Fact]
    public async Task Loop_node_iterates_collection_and_aggregates()
    {
        var fake = new FakeNodeHandler(WorkflowNodeKind.Assertion);
        var engine = CreateEngine(extraHandlers: fake);

        var loop = Node("Loop", WorkflowNodeKind.Loop, ConfigJson(new LoopNodeConfig
        {
            CollectionExpression = "{{vars.items}}",
        }));
        var body = Node("Body", WorkflowNodeKind.Assertion);
        var workflow = Graph([loop, body], [Edge(loop, body, sourcePort: "body")]);

        var context = new WorkflowContext();
        context.SetVariable("items", "[10,20,30]");

        var run = await engine.RunAsync(workflow, context: context);

        run.Status.Should().Be(RunStatus.Passed);
        fake.CallCount.Should().Be(3);

        var loopResult = run.Nodes.Single(n => n.Kind == WorkflowNodeKind.Loop);
        loopResult.Children.Should().HaveCount(3);
        loopResult.Outputs["count"].Should().Be("3");
    }

    [Fact]
    public async Task Loop_node_guards_against_runaway_iteration()
    {
        var fake = new FakeNodeHandler(WorkflowNodeKind.Assertion);
        var engine = CreateEngine(extraHandlers: fake);

        var loop = Node("Loop", WorkflowNodeKind.Loop, ConfigJson(new LoopNodeConfig
        {
            Count = 100,
            MaxIterations = 5,
        }));
        var body = Node("Body", WorkflowNodeKind.Assertion);
        var workflow = Graph([loop, body], [Edge(loop, body, sourcePort: "body")]);

        var run = await engine.RunAsync(workflow);

        run.Status.Should().Be(RunStatus.Failed);
        run.Nodes[0].Error.Should().Contain("exceeded the maximum");
        fake.CallCount.Should().Be(0);
    }

    // --- Acceptance: parallel node runs branches concurrently within the degree limit ---

    [Fact]
    public async Task Parallel_node_runs_branches_bounded_by_degree()
    {
        var fake = new FakeNodeHandler(WorkflowNodeKind.Assertion) { Delay = TimeSpan.FromMilliseconds(60) };
        var engine = CreateEngine(extraHandlers: fake);

        var parallel = Node("Fan", WorkflowNodeKind.Parallel, ConfigJson(new ParallelNodeConfig
        {
            MaxDegreeOfParallelism = 2,
        }));
        var branches = Enumerable.Range(0, 5)
            .Select(i => Node($"B{i}", WorkflowNodeKind.Assertion))
            .ToList();
        var edges = branches.Select(b => Edge(parallel, b, sourcePort: "body")).ToList();

        var workflow = Graph([parallel, .. branches], edges);

        var run = await engine.RunAsync(workflow);

        run.Status.Should().Be(RunStatus.Passed);
        fake.CallCount.Should().Be(5);
        fake.MaxConcurrency.Should().BeLessThanOrEqualTo(2);

        var parallelResult = run.Nodes.Single(n => n.Kind == WorkflowNodeKind.Parallel);
        parallelResult.Children.Should().HaveCount(5);
    }

    // --- Acceptance: cancellation stops execution promptly ---

    [Fact]
    public async Task Cancellation_stops_the_run_promptly()
    {
        using var cts = new CancellationTokenSource();
        var fake = new FakeNodeHandler(WorkflowNodeKind.Assertion)
        {
            OnExecute = async (_, token) =>
            {
                cts.Cancel();
                await Task.Delay(Timeout.Infinite, token);
            },
        };
        var engine = CreateEngine(extraHandlers: fake);

        var node = Node("LongRunning", WorkflowNodeKind.Assertion);
        var workflow = Graph([node], []);

        var run = await engine.RunAsync(workflow, cancellationToken: cts.Token);

        run.Status.Should().Be(RunStatus.Cancelled);
        run.Error.Should().Be("The workflow run was cancelled.");
    }

    // --- Acceptance: per-node error follows the failure policy ---

    [Fact]
    public async Task Stop_on_error_halts_subsequent_nodes()
    {
        var fake = new FakeNodeHandler(WorkflowNodeKind.Assertion);
        fake.StatusByNode["A"] = RunStatus.Failed;
        var engine = CreateEngine(extraHandlers: fake);

        var a = Node("A", WorkflowNodeKind.Assertion);
        var b = Node("B", WorkflowNodeKind.Assertion);
        var workflow = Graph([a, b], [Edge(a, b)]);

        var run = await engine.RunAsync(workflow, new WorkflowRunOptions { FailurePolicy = NodeFailurePolicy.StopOnError });

        run.Status.Should().Be(RunStatus.Failed);
        fake.CalledNodes.Should().ContainSingle().Which.Should().Be("A");
    }

    [Fact]
    public async Task Continue_on_error_proceeds_to_next_node()
    {
        var fake = new FakeNodeHandler(WorkflowNodeKind.Assertion);
        fake.StatusByNode["A"] = RunStatus.Failed;
        var engine = CreateEngine(extraHandlers: fake);

        var a = Node("A", WorkflowNodeKind.Assertion);
        var b = Node("B", WorkflowNodeKind.Assertion);
        var workflow = Graph([a, b], [Edge(a, b)]);

        var run = await engine.RunAsync(workflow, new WorkflowRunOptions { FailurePolicy = NodeFailurePolicy.ContinueOnError });

        run.Status.Should().Be(RunStatus.Failed);
        fake.CalledNodes.Should().BeEquivalentTo(["A", "B"]);
    }

    // --- Per-node timeout ---

    [Fact]
    public async Task Node_that_exceeds_timeout_fails_with_timeout()
    {
        var fake = new FakeNodeHandler(WorkflowNodeKind.Assertion) { Delay = TimeSpan.FromSeconds(10) };
        var engine = CreateEngine(extraHandlers: fake);

        var node = Node("Slow", WorkflowNodeKind.Assertion);
        var workflow = Graph([node], []);

        var run = await engine.RunAsync(workflow, new WorkflowRunOptions { DefaultNodeTimeoutMs = 40 });

        run.Status.Should().Be(RunStatus.Failed);
        run.Nodes[0].Error.Should().Contain("timed out");
    }

    // --- Delay node uses the scheduler seam (no real wait) ---

    [Fact]
    public async Task Delay_node_defers_to_the_scheduler()
    {
        var scheduler = new RecordingDelayScheduler();
        var engine = CreateEngine(scheduler: scheduler);

        var delay = Node("Wait", WorkflowNodeKind.Delay, ConfigJson(new DelayNodeConfig { DelayMs = 5000 }));
        var workflow = Graph([delay], []);

        var run = await engine.RunAsync(workflow);

        run.Status.Should().Be(RunStatus.Passed);
        scheduler.Requested.Should().ContainSingle()
            .Which.Should().Be(TimeSpan.FromMilliseconds(5000));
    }

    // --- Missing handler ---

    [Fact]
    public async Task Node_without_a_handler_fails()
    {
        var engine = CreateEngine();

        var node = Node("Unknown", WorkflowNodeKind.Switch);
        var workflow = Graph([node], []);

        var run = await engine.RunAsync(workflow);

        run.Status.Should().Be(RunStatus.Failed);
        run.Nodes[0].Error.Should().Contain("No handler is registered");
    }

    [Fact]
    public async Task Empty_workflow_passes_with_no_nodes()
    {
        var engine = CreateEngine();

        var run = await engine.RunAsync(Graph([], []));

        run.Status.Should().Be(RunStatus.Passed);
        run.Nodes.Should().BeEmpty();
    }

    // --- Sprint 16: the engine seeds the run context when the caller supplies none ---

    [Fact]
    public async Task Run_seeds_variable_context_when_no_context_is_supplied()
    {
        var executor = new FakeRequestExecutor
        {
            ResultToReturn = Result.Success(new HttpExecutionResult
            {
                Response = new HttpResponseModel { StatusCode = 200, ReasonPhrase = "OK" },
                Timing = new RequestTiming { Total = TimeSpan.Zero },
            }),
        };
        var seeder = new StubScopeSeeder(("host", "https://seeded.example.com"));
        var engine = CreateEngine(executor, scopeSeeder: seeder);

        var call = Node("Call", WorkflowNodeKind.Api, ConfigJson(new RequestNodeConfig
        {
            Url = "{{vars.host}}/orders",
        }));
        var run = await engine.RunAsync(Graph([call], []));

        run.Status.Should().Be(RunStatus.Passed);
        executor.LastRequest!.Url.Should().Be("https://seeded.example.com/orders");
    }

    [Fact]
    public async Task Request_node_reports_unresolved_variables_as_warnings()
    {
        var executor = new FakeRequestExecutor
        {
            ResultToReturn = Result.Success(new HttpExecutionResult
            {
                Response = new HttpResponseModel { StatusCode = 200, ReasonPhrase = "OK" },
                Timing = new RequestTiming { Total = TimeSpan.Zero },
            }),
        };
        var engine = CreateEngine(executor);

        var call = Node("Call", WorkflowNodeKind.Api, ConfigJson(new RequestNodeConfig
        {
            Url = "{{vars.missingHost}}/orders",
        }));
        var run = await engine.RunAsync(Graph([call], []));

        run.Status.Should().Be(RunStatus.Passed);
        run.Nodes[0].Warnings.Should().Contain("vars.missingHost");
    }

    private sealed class StubScopeSeeder(params (string Key, string Value)[] variables) : IVariableScopeSeeder
    {
        public Task SeedAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            foreach (var (key, value) in variables)
            {
                context.SetVariable(key, value);
            }

            return Task.CompletedTask;
        }

        public async Task<IWorkflowContext> BuildContextAsync(CancellationToken cancellationToken = default)
        {
            var context = new WorkflowContext();
            await SeedAsync(context, cancellationToken).ConfigureAwait(false);
            return context;
        }
    }
}
