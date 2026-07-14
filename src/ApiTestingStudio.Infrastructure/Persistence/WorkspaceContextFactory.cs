using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// Creates <see cref="WorkspaceDbContext"/> instances bound to a specific SQLite file at runtime.
/// Because the open workspace (and therefore the connection string) is chosen at runtime, we
/// cannot register a context with a fixed connection string; callers build a short-lived context
/// per unit of work and dispose it, which also lets connection pooling manage file handles.
///
/// <para>A single-purpose factory (not a grab-bag utility), so a static method is appropriate.</para>
/// </summary>
public static class WorkspaceContextFactory
{
    public static WorkspaceDbContext Create(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var options = new DbContextOptionsBuilder<WorkspaceDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new WorkspaceDbContext(options);
    }
}
