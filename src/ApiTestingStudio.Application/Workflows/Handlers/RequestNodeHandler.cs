using System.Globalization;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Workflows.Handlers;

/// <summary>
/// Executes an <see cref="WorkflowNodeKind.Api"/> node: resolves the request template, sends it via
/// the shared <see cref="IRequestExecutor"/> (reused from Sprint 06), and publishes
/// <c>status</c>/<c>reason</c>/<c>body</c> outputs into the context for downstream nodes.
/// </summary>
public sealed class RequestNodeHandler : INodeHandler
{
    private readonly IRequestExecutor _executor;

    public RequestNodeHandler(IRequestExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
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

        var request = new HttpRequestModel
        {
            Method = config.Method,
            Url = context.Resolver.Resolve(config.Url, context.Context),
            QueryParams = Resolve(config.QueryParams, context),
            Headers = Resolve(config.Headers, context),
            BodyKind = config.BodyKind,
            Body = string.IsNullOrEmpty(config.Body) ? null : context.Resolver.Resolve(config.Body, context.Context),
        };

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
        };
    }

    private static List<QueryParam> Resolve(IReadOnlyList<QueryParam> parameters, NodeHandlerContext context) =>
        parameters.Select(p => p with { Value = context.Resolver.Resolve(p.Value, context.Context) }).ToList();

    private static List<HttpHeader> Resolve(IReadOnlyList<HttpHeader> headers, NodeHandlerContext context) =>
        headers.Select(h => h with { Value = context.Resolver.Resolve(h.Value, context.Context) }).ToList();

    private NodeRunResult Failed(WorkflowNode node, string error) => new()
    {
        NodeId = node.Id,
        NodeName = node.Name,
        Kind = Kind,
        Status = RunStatus.Failed,
        Error = error,
    };
}
