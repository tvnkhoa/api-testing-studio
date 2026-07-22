using System.Globalization;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Profiles;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Workflows.Handlers;

/// <summary>
/// Executes an <see cref="WorkflowNodeKind.Api"/> node: resolves the request template, applies the
/// node's optional "Run As" profile, sends it via the shared <see cref="IRequestExecutor"/> (reused
/// from Sprint 06), and publishes <c>status</c>/<c>reason</c>/<c>body</c> outputs into the context
/// for downstream nodes.
/// </summary>
public sealed class RequestNodeHandler : INodeHandler
{
    private readonly IRequestExecutor _executor;
    private readonly IProfileRepository _profiles;
    private readonly IAuthApplicator _authApplicator;

    public RequestNodeHandler(
        IRequestExecutor executor,
        IProfileRepository profiles,
        IAuthApplicator authApplicator)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _authApplicator = authApplicator ?? throw new ArgumentNullException(nameof(authApplicator));
    }

    public WorkflowNodeKind Kind => WorkflowNodeKind.Api;

    public async Task<NodeRunResult> ExecuteAsync(NodeHandlerContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var node = context.Node;

        var config = NodeConfigSerializer.Deserialize<RequestNodeConfig>(node.Config);
        if (config is null || string.IsNullOrWhiteSpace(config.Url))
        {
            return Failed(node, WorkflowErrors.InvalidConfig(node.Name, "a request URL is required.").Message);
        }

        // Collect any {{tokens}} that could not be resolved against the run's variable context so the
        // node reports them as warnings rather than silently sending empty values.
        var unresolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var request = new HttpRequestModel
        {
            Method = config.Method,
            Url = context.Resolver.Resolve(config.Url, context.Context, unresolved),
            QueryParams = Resolve(config.QueryParams, context, unresolved),
            Headers = Resolve(config.Headers, context, unresolved),
            BodyKind = config.BodyKind,
            Body = string.IsNullOrEmpty(config.Body) ? null : context.Resolver.Resolve(config.Body, context.Context, unresolved),
        };

        request = await ApplyProfileAsync(request, config.ProfileId, cancellationToken).ConfigureAwait(false);

        var result = await _executor.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Failed(node, result.Error.Message);
        }

        var response = result.Value.Response;
        var outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["status"] = response.StatusCode.ToString(CultureInfo.InvariantCulture),
            ["reason"] = response.ReasonPhrase,
            ["body"] = response.Body ?? string.Empty,
        };
        context.Context.SetNodeOutputs(node.Name, outputs);

        return new NodeRunResult
        {
            NodeId = node.Id,
            NodeName = node.Name,
            Kind = Kind,
            Status = RunStatus.Passed,
            Outputs = outputs,
            Warnings = unresolved.Count > 0 ? [.. unresolved] : [],
        };
    }

    private async Task<HttpRequestModel> ApplyProfileAsync(HttpRequestModel request, Guid? profileId, CancellationToken cancellationToken)
    {
        if (profileId is not { } id)
        {
            return request;
        }

        var profile = await _profiles.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return _authApplicator.Apply(request, profile);
    }

    private static List<QueryParam> Resolve(IReadOnlyList<QueryParam> parameters, NodeHandlerContext context, ICollection<string> unresolved) =>
        parameters.Select(p => p with { Value = context.Resolver.Resolve(p.Value, context.Context, unresolved) }).ToList();

    private static List<HttpHeader> Resolve(IReadOnlyList<HttpHeader> headers, NodeHandlerContext context, ICollection<string> unresolved) =>
        headers.Select(h => h with { Value = context.Resolver.Resolve(h.Value, context.Context, unresolved) }).ToList();

    private NodeRunResult Failed(WorkflowNode node, string error) => new()
    {
        NodeId = node.Id,
        NodeName = node.Name,
        Kind = Kind,
        Status = RunStatus.Failed,
        Error = error,
    };
}
