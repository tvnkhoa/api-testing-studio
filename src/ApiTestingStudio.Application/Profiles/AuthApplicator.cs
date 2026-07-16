using System.Text;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Profiles;

/// <summary>
/// Default <see cref="IAuthApplicator"/>. Maps <see cref="AuthScheme"/> to a request header,
/// decrypting the needed <c>Protected*</c> field via <see cref="ISecretProtector"/> at call time.
/// Decrypted values are never logged.
/// </summary>
public sealed class AuthApplicator : IAuthApplicator
{
    private const string AuthorizationHeader = "Authorization";

    private readonly ISecretProtector _protector;

    public AuthApplicator(ISecretProtector protector)
    {
        ArgumentNullException.ThrowIfNull(protector);
        _protector = protector;
    }

    public HttpRequestModel Apply(HttpRequestModel request, ProfileDefinition? profile)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (profile is null)
        {
            return request;
        }

        return profile.Auth switch
        {
            AuthScheme.Bearer => WithHeader(request, AuthorizationHeader, BearerValue(profile)),
            AuthScheme.Basic => WithHeader(request, AuthorizationHeader, BasicValue(profile)),
            AuthScheme.ApiKey => ApplyApiKey(request, profile),
            _ => request, // None / Custom: leave the request as-is.
        };
    }

    private HttpRequestModel ApplyApiKey(HttpRequestModel request, ProfileDefinition profile)
    {
        if (string.IsNullOrWhiteSpace(profile.ApiKeyHeaderName) || profile.ProtectedApiKey is null)
        {
            return request;
        }

        return WithHeader(request, profile.ApiKeyHeaderName, _protector.Unprotect(profile.ProtectedApiKey));
    }

    private string? BearerValue(ProfileDefinition profile) =>
        profile.ProtectedAccessToken is { } cipher
            ? $"Bearer {_protector.Unprotect(cipher)}"
            : null;

    private string? BasicValue(ProfileDefinition profile)
    {
        var username = profile.Username ?? string.Empty;
        var password = profile.ProtectedPassword is { } cipher ? _protector.Unprotect(cipher) : string.Empty;
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        return $"Basic {token}";
    }

    private static HttpRequestModel WithHeader(HttpRequestModel request, string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return request;
        }

        // Replace any existing header with the same name (case-insensitive), then append.
        var headers = request.Headers
            .Where(h => !string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase))
            .Append(new HttpHeader(name, value))
            .ToList();

        return request with { Headers = headers };
    }
}
