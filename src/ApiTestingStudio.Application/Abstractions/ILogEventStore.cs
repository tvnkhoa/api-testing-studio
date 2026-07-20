using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Filter applied by the Log Viewer when querying <see cref="LogEventRecord"/> rows. All facets are
/// optional and combine with AND; an unset facet does not constrain the result.
/// </summary>
public sealed record LogEventQuery
{
    /// <summary>Level names to include (e.g. Warning/Error/Fatal); null or empty means all levels.</summary>
    public IReadOnlyCollection<string>? Levels { get; init; }

    /// <summary>Exact source (logger category) to include; null means all sources.</summary>
    public string? Source { get; init; }

    /// <summary>Case-insensitive substring the message must contain; null/empty means no text filter.</summary>
    public string? SearchText { get; init; }

    /// <summary>Maximum number of rows to return (most recent first).</summary>
    public int Limit { get; init; } = 1000;
}

/// <summary>
/// Durable store for application (Serilog) log events (Sprint 13), persisted per workspace and read by
/// the Log Viewer. Writes are batched by the Serilog sink; EF Core types stay in Infrastructure.
/// </summary>
public interface ILogEventStore
{
    /// <summary>Appends a batch of log events. Requires an open workspace.</summary>
    Task AppendAsync(IReadOnlyList<LogEventRecord> events, CancellationToken cancellationToken = default);

    /// <summary>Returns matching events for a workspace, most recent first, capped at <see cref="LogEventQuery.Limit"/>.</summary>
    Task<IReadOnlyList<LogEventRecord>> QueryAsync(Guid workspaceId, LogEventQuery query, CancellationToken cancellationToken = default);

    /// <summary>Returns the distinct sources (logger categories) present for a workspace, sorted.</summary>
    Task<IReadOnlyList<string>> GetSourcesAsync(Guid workspaceId, CancellationToken cancellationToken = default);
}
