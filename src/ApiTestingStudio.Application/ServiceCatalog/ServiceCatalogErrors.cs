using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.ServiceCatalog;

/// <summary>
/// Typed, transport-agnostic errors for Service Explorer catalog operations (services, folders,
/// endpoints). Returned via <see cref="Result"/> so failures stay explicit and testable.
/// </summary>
public static class ServiceCatalogErrors
{
    public static Error NoWorkspaceOpen { get; } =
        new("service_catalog.no_workspace", "No workspace is currently open.");

    public static Error NameRequired { get; } =
        new("service_catalog.name_required", "A name is required.");

    public static Error PathRequired { get; } =
        new("service_catalog.path_required", "An endpoint path is required.");

    public static Error ServiceNotFound(Guid id) =>
        new("service_catalog.service_not_found", $"Service '{id}' was not found.");

    public static Error FolderNotFound(Guid id) =>
        new("service_catalog.folder_not_found", $"Folder '{id}' was not found.");

    public static Error EndpointNotFound(Guid id) =>
        new("service_catalog.endpoint_not_found", $"Endpoint '{id}' was not found.");
}
