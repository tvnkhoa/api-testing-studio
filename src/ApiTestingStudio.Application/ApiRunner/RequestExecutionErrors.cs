using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.ApiRunner;

/// <summary>
/// Typed, transport-agnostic errors for API Runner request execution and history. Returned via
/// <see cref="Result"/> so failures stay explicit and testable.
/// </summary>
public static class RequestExecutionErrors
{
    public static Error NoWorkspaceOpen { get; } =
        new("request.no_workspace", "No workspace is currently open.");

    public static Error UrlRequired { get; } =
        new("request.url_required", "A request URL is required.");

    public static Error InvalidUrl(string url) =>
        new("request.invalid_url", $"'{url}' is not a valid absolute URL.");

    public static Error Timeout { get; } =
        new("request.timeout", "The request timed out.");

    public static Error Cancelled { get; } =
        new("request.cancelled", "The request was cancelled.");

    public static Error RequestFailed(string message) =>
        new("request.failed", $"The request failed: {message}");

    public static Error EndpointNotFound(Guid id) =>
        new("request.endpoint_not_found", $"Endpoint '{id}' was not found.");

    public static Error HistoryEntryNotFound(Guid id) =>
        new("request.history_not_found", $"History entry '{id}' was not found.");
}
