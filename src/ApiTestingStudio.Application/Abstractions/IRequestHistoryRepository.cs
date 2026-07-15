using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Persists <see cref="RequestHistoryEntry"/> rows for the currently open workspace. Operations
/// require an open workspace. EF Core types never cross this port. See
/// <c>.claude/DATABASE_GUIDELINES.md</c>.
/// </summary>
public interface IRequestHistoryRepository
{
    /// <summary>Returns an endpoint's history, most-recent first.</summary>
    Task<IReadOnlyList<RequestHistoryEntry>> GetByEndpointAsync(Guid endpointId, CancellationToken cancellationToken = default);

    /// <summary>Returns the history entry with the given id, or null.</summary>
    Task<RequestHistoryEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new history entry.</summary>
    Task AddAsync(RequestHistoryEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Deletes all history for an endpoint.</summary>
    Task DeleteByEndpointAsync(Guid endpointId, CancellationToken cancellationToken = default);
}
