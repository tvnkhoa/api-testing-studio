using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Domain.Entities;

/// <summary>A logical API service (a base URL grouping related endpoints).</summary>
public sealed record Service
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public required string Name { get; init; }

    public string? BaseUrl { get; init; }

    public string? Description { get; init; }
}

/// <summary>A single callable operation belonging to a <see cref="Service"/>.</summary>
public sealed record Endpoint
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid ServiceId { get; init; }

    public required string Name { get; init; }

    public HttpVerb Method { get; init; } = HttpVerb.Get;

    public required string Path { get; init; }

    public string? Description { get; init; }
}
