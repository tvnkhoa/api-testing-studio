using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Workflows;

/// <summary>
/// Typed errors for the workflow engine, surfaced on <see cref="NodeRunResult"/> /
/// <see cref="WorkflowRunResult"/> or returned via <see cref="Result"/>. Codes are namespaced
/// under <c>workflow.*</c>.
/// </summary>
public static class WorkflowErrors
{
    public static Error NoWorkspaceOpen { get; } =
        new("workflow.no_workspace", "No workspace is currently open.");

    public static Error NotFound(Guid id) =>
        new("workflow.not_found", $"Workflow '{id}' was not found.");

    public static Error NoHandler(WorkflowNodeKind kind) =>
        new("workflow.no_handler", $"No handler is registered for node kind '{kind}'.");

    public static Error NodeTimeout(string nodeName) =>
        new("workflow.node_timeout", $"Node '{nodeName}' timed out.");

    public static Error Cancelled { get; } =
        new("workflow.cancelled", "The workflow run was cancelled.");

    public static Error LoopLimitExceeded(string nodeName, int max) =>
        new("workflow.loop_limit_exceeded", $"Loop node '{nodeName}' exceeded the maximum of {max} iterations.");

    public static Error NodeFailed(string nodeName, string message) =>
        new("workflow.node_failed", $"Node '{nodeName}' failed: {message}");

    public static Error InvalidConfig(string nodeName, string message) =>
        new("workflow.invalid_config", $"Node '{nodeName}' has invalid configuration: {message}");
}
