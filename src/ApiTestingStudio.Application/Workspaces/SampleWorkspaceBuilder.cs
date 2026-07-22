using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Variables;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Workspaces;

/// <summary>
/// Builds a small, ready-to-explore sample workspace on demand so the Welcome "Open sample" CTA has
/// something real to open. Creates the workspace (which seeds a default environment + <c>baseUrl</c>),
/// then adds one service with a few endpoints and a demo workflow that resolves <c>{{baseUrl}}</c> —
/// exercising the Sprint-16 variable-seeding fix end to end. No fixture file ships in the repo.
/// </summary>
public interface ISampleWorkspaceBuilder
{
    /// <summary>Creates and populates the sample workspace at <paramref name="location"/>, leaving it open.</summary>
    Task<Result<Workspace>> BuildAsync(string location, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class SampleWorkspaceBuilder : ISampleWorkspaceBuilder
{
    private const string SampleBaseUrl = "https://jsonplaceholder.typicode.com";

    private readonly IWorkspaceService _workspaces;
    private readonly IWorkspaceSession _session;
    private readonly IServiceRepository _services;
    private readonly IEndpointRepository _endpoints;
    private readonly IWorkflowRepository _workflows;
    private readonly IVariableService _variables;

    public SampleWorkspaceBuilder(
        IWorkspaceService workspaces,
        IWorkspaceSession session,
        IServiceRepository services,
        IEndpointRepository endpoints,
        IWorkflowRepository workflows,
        IVariableService variables)
    {
        _workspaces = workspaces;
        _session = session;
        _services = services;
        _endpoints = endpoints;
        _workflows = workflows;
        _variables = variables;
    }

    public async Task<Result<Workspace>> BuildAsync(string location, CancellationToken cancellationToken = default)
    {
        var created = await _workspaces.CreateAsync(location, "Sample API Workspace", "A guided sample to explore the product.", cancellationToken).ConfigureAwait(false);
        if (created.IsFailure)
        {
            return created;
        }

        if (_session.Current is not { } workspace)
        {
            return created;
        }

        await PointBaseUrlAtSampleApiAsync(cancellationToken).ConfigureAwait(false);
        await AddSampleServiceAsync(workspace.Id, cancellationToken).ConfigureAwait(false);
        await AddDemoWorkflowAsync(workspace.Id, cancellationToken).ConfigureAwait(false);

        return created;
    }

    /// <summary>Retargets the seeded <c>baseUrl</c> at a real public sample API so requests actually work online.</summary>
    private async Task PointBaseUrlAtSampleApiAsync(CancellationToken cancellationToken)
    {
        var list = await _variables.ListAsync(cancellationToken).ConfigureAwait(false);
        if (list.IsFailure)
        {
            return;
        }

        var baseUrl = list.Value.FirstOrDefault(v => string.Equals(v.Key, WorkspaceService.DefaultBaseUrlKey, StringComparison.OrdinalIgnoreCase));
        if (baseUrl is not null)
        {
            await _variables.UpdateAsync(
                baseUrl.Id,
                new VariableDraft { Scope = baseUrl.Scope, EnvironmentId = baseUrl.EnvironmentId, Key = baseUrl.Key, Value = SampleBaseUrl },
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task AddSampleServiceAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var service = new Service
        {
            WorkspaceId = workspaceId,
            Name = "JSONPlaceholder",
            BaseUrl = SampleBaseUrl,
            Description = "A free sample REST API for exploring requests.",
        };
        await _services.AddAsync(service, cancellationToken).ConfigureAwait(false);

        var endpoints = new[]
        {
            new Endpoint { ServiceId = service.Id, Name = "List posts", Method = HttpVerb.Get, Path = "/posts", SortOrder = 0 },
            new Endpoint { ServiceId = service.Id, Name = "Get post", Method = HttpVerb.Get, Path = "/posts/1", SortOrder = 1 },
            new Endpoint
            {
                ServiceId = service.Id,
                Name = "Create post",
                Method = HttpVerb.Post,
                Path = "/posts",
                SortOrder = 2,
                DefaultBody = "{\n  \"title\": \"hello\",\n  \"body\": \"from API Testing Studio\",\n  \"userId\": 1\n}",
            },
        };

        foreach (var endpoint in endpoints)
        {
            await _endpoints.AddAsync(endpoint, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>A one-node workflow whose URL uses <c>{{baseUrl}}</c>, demonstrating variable resolution.</summary>
    private async Task AddDemoWorkflowAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var workflowId = Guid.NewGuid();
        var node = new WorkflowNode
        {
            WorkflowId = workflowId,
            Kind = WorkflowNodeKind.Api,
            Name = "Get first post",
            PositionX = 120,
            PositionY = 120,
            Config = NodeConfigSerializer.Serialize(new RequestNodeConfig
            {
                Method = HttpVerb.Get,
                Url = "{{baseUrl}}/posts/1",
            }),
        };

        var workflow = new Workflow
        {
            Id = workflowId,
            WorkspaceId = workspaceId,
            Name = "Fetch a post",
            Description = "Demonstrates variable resolution: the request URL uses {{baseUrl}}.",
            Nodes = [node],
            Edges = [],
        };

        await _workflows.SaveAsync(workflow, cancellationToken).ConfigureAwait(false);
    }
}
