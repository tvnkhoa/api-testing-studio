using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> to create the context when generating and
/// applying migrations. It is NOT used at runtime — the app builds its options from DI.
/// </summary>
public sealed class WorkspaceDbContextFactory : IDesignTimeDbContextFactory<WorkspaceDbContext>
{
    public WorkspaceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WorkspaceDbContext>()
            .UseSqlite("Data Source=apistudio-design.db")
            .Options;

        return new WorkspaceDbContext(options);
    }
}
