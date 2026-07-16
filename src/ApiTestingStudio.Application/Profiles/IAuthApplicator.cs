using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Profiles;

/// <summary>
/// Applies a profile's authorization to an outgoing request by injecting the appropriate header,
/// decrypting the profile's secret material on demand. The transport layer stays auth-agnostic —
/// auth is just headers on the model by the time it reaches the executor.
/// </summary>
public interface IAuthApplicator
{
    /// <summary>
    /// Returns a copy of <paramref name="request"/> with the profile's authorization header applied.
    /// A null profile, or <c>AuthScheme.None</c>/<c>Custom</c>, returns the request unchanged.
    /// </summary>
    HttpRequestModel Apply(HttpRequestModel request, ProfileDefinition? profile);
}
