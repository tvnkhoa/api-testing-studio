using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Persists <see cref="TestSuite"/>s for the currently open workspace. Operations require an open
/// workspace; EF Core types never cross this port. See <c>.claude/DATABASE_GUIDELINES.md</c>.
/// </summary>
public interface ITestSuiteRepository
{
    /// <summary>Lists the suites in a workspace, ordered by sort order then name.</summary>
    Task<IReadOnlyList<TestSuite>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>Loads a single suite, or null when not found.</summary>
    Task<TestSuite?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new suite.</summary>
    Task AddAsync(TestSuite suite, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing suite.</summary>
    Task UpdateAsync(TestSuite suite, CancellationToken cancellationToken = default);

    /// <summary>Deletes a suite; its cases are detached (moved to ungrouped), not deleted.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
