using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Workspaces;

/// <summary>
/// Typed, transport-agnostic errors for workspace lifecycle operations. Returning these via
/// <see cref="Result"/> keeps failures explicit and testable instead of surfacing raw exceptions.
/// </summary>
public static class WorkspaceErrors
{
    public static Error InvalidName { get; } =
        new("workspace.invalid_name", "Workspace name must not be empty.");

    public static Error NoneOpen { get; } =
        new("workspace.none_open", "No workspace is currently open.");

    public static Error NotFound(string location) =>
        new("workspace.not_found", $"No workspace found at '{location}'.");

    public static Error AlreadyExists(string location) =>
        new("workspace.already_exists", $"A workspace already exists at '{location}'.");

    public static Error Locked(string location) =>
        new("workspace.locked", $"The workspace at '{location}' is locked by another process.");

    public static Error Corrupt(string location) =>
        new("workspace.corrupt", $"The file at '{location}' is not a valid workspace.");

    public static Error SchemaTooNew(int fileVersion, int appVersion) =>
        new(
            "workspace.schema_too_new",
            FormattableString.Invariant(
                $"The workspace uses schema version {fileVersion}, which is newer than this build supports ({appVersion}). Update the application to open it."));

    public static Error CreateFailed(string location) =>
        new("workspace.create_failed", $"Failed to create a workspace at '{location}'.");

    public static Error OpenFailed(string location) =>
        new("workspace.open_failed", $"Failed to open the workspace at '{location}'.");
}
