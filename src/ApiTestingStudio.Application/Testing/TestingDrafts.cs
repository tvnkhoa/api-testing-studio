using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Testing;

/// <summary>Editor payload for creating/updating a test case (name, description, and its target).</summary>
public sealed record TestCaseDraft(string Name, string? Description, Guid? EndpointId, Guid? WorkflowId);

/// <summary>
/// A selectable target for a test case in the editor: either an endpoint request or a workflow.
/// Exactly one of <see cref="EndpointId"/> / <see cref="WorkflowId"/> is set.
/// </summary>
public sealed record TestCaseTargetOption(string Display, Guid? EndpointId, Guid? WorkflowId);

/// <summary>Editor payload for creating/updating a single assertion attached to a test case.</summary>
public sealed record AssertionDraft(
    string Kind,
    AssertionSource Source,
    string? Target,
    string? Expression,
    string? Operator,
    string Expected,
    bool Enabled);
