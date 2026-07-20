using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Persists <see cref="TestCaseDefinition"/>s and their child <see cref="AssertionDefinition"/>s for
/// the open workspace, hydrating a case into a runtime <see cref="TestCase"/> aggregate. Operations
/// require an open workspace; EF Core types never cross this port.
/// </summary>
public interface ITestCaseRepository
{
    /// <summary>Loads a case with its ordered assertions, or null when not found.</summary>
    Task<TestCase?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lists the case definitions (without assertions) in a suite.</summary>
    Task<IReadOnlyList<TestCaseDefinition>> ListBySuiteAsync(Guid suiteId, CancellationToken cancellationToken = default);

    /// <summary>Lists all case definitions (without assertions) in a workspace, including ungrouped.</summary>
    Task<IReadOnlyList<TestCaseDefinition>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>Inserts or updates a case and replaces its assertion rows wholesale.</summary>
    Task SaveAsync(TestCase testCase, CancellationToken cancellationToken = default);

    /// <summary>Deletes a case and its assertions.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
