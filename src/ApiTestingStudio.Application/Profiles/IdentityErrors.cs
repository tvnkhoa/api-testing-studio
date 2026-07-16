using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Profiles;

/// <summary>
/// Typed, transport-agnostic errors for profile / environment / variable operations (Sprint 10).
/// Returned via <see cref="Result"/> so failures stay explicit and testable.
/// </summary>
public static class IdentityErrors
{
    public static Error NoWorkspaceOpen { get; } =
        new("identity.no_workspace", "No workspace is currently open.");

    public static Error NameRequired { get; } =
        new("identity.name_required", "A name is required.");

    public static Error KeyRequired { get; } =
        new("identity.key_required", "A variable key is required.");

    public static Error EnvironmentRequired { get; } =
        new("identity.environment_required", "An environment-scoped variable must reference an environment.");

    public static Error ProfileNotFound(Guid id) =>
        new("identity.profile_not_found", $"Profile '{id}' was not found.");

    public static Error EnvironmentNotFound(Guid id) =>
        new("identity.environment_not_found", $"Environment '{id}' was not found.");

    public static Error VariableNotFound(Guid id) =>
        new("identity.variable_not_found", $"Variable '{id}' was not found.");
}
