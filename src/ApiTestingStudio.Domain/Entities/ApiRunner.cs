using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Domain.Entities;

/// <summary>
/// A single request header (or a cookie carried as a <c>Cookie</c> header). <see cref="Enabled"/>
/// lets the builder keep a row while excluding it from the outgoing request. Also used for
/// response headers, where <see cref="Enabled"/> is always <c>true</c>.
/// </summary>
public sealed record HttpHeader(string Name, string Value, bool Enabled = true);

/// <summary>
/// A single URL query-string parameter. <see cref="Enabled"/> excludes the row from the built URL
/// without deleting it from the builder.
/// </summary>
public sealed record QueryParam(string Name, string Value, bool Enabled = true);

/// <summary>
/// An outgoing HTTP request assembled by the runner. Runtime/DTO model (not persisted directly);
/// history stores a serialized snapshot of it. Auth/profiles/environments arrive in Sprint 10, so
/// values here are literal for now.
/// </summary>
public sealed record HttpRequestModel
{
    public HttpVerb Method { get; init; } = HttpVerb.Get;

    /// <summary>Absolute request URL (base URL + path already resolved by the caller).</summary>
    public required string Url { get; init; }

    public IReadOnlyList<QueryParam> QueryParams { get; init; } = [];

    public IReadOnlyList<HttpHeader> Headers { get; init; } = [];

    public BodyKind BodyKind { get; init; } = BodyKind.Json;

    /// <summary>Raw request body text, or null for bodyless verbs (GET/HEAD).</summary>
    public string? Body { get; init; }
}

/// <summary>
/// A received HTTP response captured by the runner. Runtime/DTO model; history stores a serialized
/// snapshot of it.
/// </summary>
public sealed record HttpResponseModel
{
    public int StatusCode { get; init; }

    public required string ReasonPhrase { get; init; }

    public IReadOnlyList<HttpHeader> Headers { get; init; } = [];

    /// <summary>Response body decoded as text, or null when there is no body.</summary>
    public string? Body { get; init; }

    /// <summary>Size of the response body in bytes.</summary>
    public long ContentLengthBytes { get; init; }
}

/// <summary>
/// Timing breakdown for one request. Low-level phases are nullable because they are only available
/// when the transport surfaces them (see <c>SocketsHttpHandler</c> metrics); <see cref="Total"/> is
/// always measured.
/// </summary>
public sealed record RequestTiming
{
    public TimeSpan Total { get; init; }

    public TimeSpan? Dns { get; init; }

    public TimeSpan? Connect { get; init; }

    public TimeSpan? TimeToFirstByte { get; init; }
}

/// <summary>
/// Result of executing a request: the response plus its timing. Returned by the HTTP execution port
/// so the same abstraction can be reused by Workflow (Sprint 08) and Stress (Sprint 12).
/// </summary>
public sealed record HttpExecutionResult
{
    public required HttpResponseModel Response { get; init; }

    public required RequestTiming Timing { get; init; }
}

/// <summary>
/// A persisted record of one request send against an endpoint, re-runnable via replay. The full
/// request/response are stored as JSON snapshots (<c>System.Text.Json</c>); the denormalized
/// method/url/status/timing columns let the history list render without deserializing.
/// </summary>
public sealed record RequestHistoryEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid EndpointId { get; init; }

    public HttpVerb Method { get; init; } = HttpVerb.Get;

    public required string Url { get; init; }

    public int StatusCode { get; init; }

    /// <summary>Total round-trip time in milliseconds.</summary>
    public long TotalMs { get; init; }

    public long? DnsMs { get; init; }

    public long? ConnectMs { get; init; }

    public long? TimeToFirstByteMs { get; init; }

    /// <summary>JSON snapshot of the <see cref="HttpRequestModel"/> that was sent.</summary>
    public required string RequestSnapshot { get; init; }

    /// <summary>JSON snapshot of the <see cref="HttpResponseModel"/> that was received.</summary>
    public required string ResponseSnapshot { get; init; }

    public DateTimeOffset TimestampUtc { get; init; }
}
