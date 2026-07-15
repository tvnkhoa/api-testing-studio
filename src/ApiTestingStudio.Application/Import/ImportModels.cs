using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions.Importing;

namespace ApiTestingStudio.Application.Import;

/// <summary>
/// A request to import an API definition. Exactly one of <see cref="Content"/> (pasted text or file
/// text) or <see cref="Uri"/> (a URL to fetch) is populated. <see cref="Format"/> is an optional
/// explicit format; when null the orchestrator auto-detects. <see cref="FileName"/> is an optional
/// hint used by detection (file extension).
/// </summary>
public sealed record ImportRequest
{
    public string? Format { get; init; }

    public string? Content { get; init; }

    public string? Uri { get; init; }

    public string? FileName { get; init; }
}

/// <summary>Whether a previewed item would be created new or merged into an existing catalog row.</summary>
public enum ImportChangeKind
{
    Create,
    Update,
}

/// <summary>A single parsed endpoint as shown in the import preview.</summary>
public sealed record ParsedEndpoint(
    string Name,
    HttpVerb Method,
    string Path,
    ImportChangeKind Change);

/// <summary>A parsed service and its endpoints as shown in the import preview.</summary>
public sealed record ParsedService(
    string Name,
    string? BaseUrl,
    ImportChangeKind Change,
    IReadOnlyList<ParsedEndpoint> Endpoints);

/// <summary>
/// The result of parsing a source before commit: a human-readable diff for the wizard plus the raw
/// <see cref="ImportResult"/> carried through so <c>CommitAsync</c> can merge without re-parsing.
/// </summary>
public sealed record ImportPreview
{
    public required string Format { get; init; }

    public required IReadOnlyList<ParsedService> Services { get; init; }

    public int EndpointCount { get; init; }

    /// <summary>The domain entities produced by the importer, carried through to the merge step.</summary>
    public required ImportResult Result { get; init; }
}

/// <summary>Options controlling how a parsed result is merged into the catalog.</summary>
public sealed record ImportOptions
{
    /// <summary>
    /// When true, endpoints that match an existing one (same method + path within the service) are
    /// updated in place; when false they are skipped.
    /// </summary>
    public bool OverwriteExisting { get; init; }
}

/// <summary>A summary of what a merge committed, for the wizard's result step.</summary>
public sealed record ImportSummary(
    int ServicesCreated,
    int ServicesUpdated,
    int EndpointsCreated,
    int EndpointsUpdated,
    int EndpointsSkipped);

/// <summary>An API definition fetched from a URL, with the path that actually responded.</summary>
public sealed record FetchedDefinition(string Content, string ResolvedUri, string? Format);
