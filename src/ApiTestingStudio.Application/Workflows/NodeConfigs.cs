using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Workflows;

/// <summary>
/// Configuration for an <see cref="WorkflowNodeKind.Api"/> node: the request template the handler
/// builds and sends via <c>IRequestExecutor</c>. String fields may contain <c>{{token}}</c>
/// expressions resolved against the run context.
/// </summary>
public sealed record RequestNodeConfig
{
    public HttpVerb Method { get; init; } = HttpVerb.Get;

    public string Url { get; init; } = string.Empty;

    public IReadOnlyList<QueryParam> QueryParams { get; init; } = [];

    public IReadOnlyList<HttpHeader> Headers { get; init; } = [];

    public BodyKind BodyKind { get; init; } = BodyKind.Json;

    public string? Body { get; init; }

    /// <summary>Optional "Run As" profile whose authorization is applied to this node's request.</summary>
    public Guid? ProfileId { get; init; }
}

/// <summary>Comparison a <see cref="WorkflowNodeKind.Condition"/> node applies to its resolved operands.</summary>
public enum ConditionOperator
{
    /// <summary>True when the left operand is non-empty and not a falsy literal (false/0/null).</summary>
    IsTruthy,

    /// <summary>True when the left operand is null or empty.</summary>
    IsEmpty,

    Equals,

    NotEquals,

    Contains,
}

/// <summary>
/// Configuration for a <see cref="WorkflowNodeKind.Condition"/> node. <see cref="Left"/> and
/// <see cref="Right"/> are resolved (interpolation only) before <see cref="Operator"/> is applied;
/// the boolean outcome selects the outgoing <c>true</c>/<c>false</c> edge.
/// </summary>
public sealed record ConditionNodeConfig
{
    public string Left { get; init; } = string.Empty;

    public ConditionOperator Operator { get; init; } = ConditionOperator.IsTruthy;

    public string? Right { get; init; }
}

/// <summary>
/// Configuration for a <see cref="WorkflowNodeKind.Loop"/> node. Either iterate the elements of a
/// resolved JSON array (<see cref="CollectionExpression"/>) or a fixed <see cref="Count"/>. The
/// current element/index are published as variables for the loop body.
/// </summary>
public sealed record LoopNodeConfig
{
    public string? CollectionExpression { get; init; }

    public int? Count { get; init; }

    public string ItemVariable { get; init; } = "item";

    public string IndexVariable { get; init; } = "index";

    /// <summary>Optional per-node override of <see cref="WorkflowRunOptions.MaxLoopIterations"/>.</summary>
    public int? MaxIterations { get; init; }
}

/// <summary>Configuration for a <see cref="WorkflowNodeKind.Parallel"/> node.</summary>
public sealed record ParallelNodeConfig
{
    /// <summary>Optional per-node override of <see cref="WorkflowRunOptions.DefaultMaxDegreeOfParallelism"/>.</summary>
    public int? MaxDegreeOfParallelism { get; init; }
}

/// <summary>Configuration for a <see cref="WorkflowNodeKind.Delay"/> node.</summary>
public sealed record DelayNodeConfig
{
    public int DelayMs { get; init; }
}

/// <summary>
/// Configuration for a <see cref="WorkflowNodeKind.Assertion"/> node: evaluates one or more
/// assertions against the response outputs (<c>status</c>/<c>reason</c>/<c>body</c>) published by an
/// upstream node named <see cref="SourceNode"/>. The node passes only when every assertion passes.
/// </summary>
public sealed record AssertionNodeConfig
{
    /// <summary>Name of the upstream node whose published response outputs are asserted.</summary>
    public string SourceNode { get; init; } = string.Empty;

    public IReadOnlyList<AssertionSpec> Assertions { get; init; } = [];
}

/// <summary>
/// One assertion inside an <see cref="AssertionNodeConfig"/> — the workflow-node mirror of the
/// persisted <see cref="AssertionDefinition"/> (no test-case ownership).
/// </summary>
public sealed record AssertionSpec
{
    public string Kind { get; init; } = "json";

    public AssertionSource Source { get; init; } = AssertionSource.Body;

    public string? Target { get; init; }

    public string? Expression { get; init; }

    public string? Operator { get; init; }

    public string Expected { get; init; } = string.Empty;

    public bool Enabled { get; init; } = true;
}
