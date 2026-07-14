using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Workspaces;

/// <summary>
/// Validates a workspace file's schema version against the version this build understands.
/// Older files are upgraded by EF migrations on open; newer files must be rejected so a workspace
/// written by a future build is never partially interpreted. See <c>.claude/DATABASE_GUIDELINES.md</c>.
/// </summary>
public static class SchemaVersionValidator
{
    /// <summary>
    /// Succeeds when <paramref name="workspaceSchemaVersion"/> is at or below
    /// <see cref="Workspace.CurrentSchemaVersion"/>; otherwise fails with
    /// <see cref="WorkspaceErrors.SchemaTooNew"/>.
    /// </summary>
    public static Result Validate(int workspaceSchemaVersion)
        => workspaceSchemaVersion > Workspace.CurrentSchemaVersion
            ? Result.Failure(WorkspaceErrors.SchemaTooNew(workspaceSchemaVersion, Workspace.CurrentSchemaVersion))
            : Result.Success();
}
