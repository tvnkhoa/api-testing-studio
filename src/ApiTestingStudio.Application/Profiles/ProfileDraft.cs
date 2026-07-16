using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Profiles;

/// <summary>
/// Editable projection of a profile carrying secret material as <b>plaintext</b> — the service
/// encrypts it through <c>ISecretProtector</c> before persistence. On update, a null secret field
/// means "leave the stored ciphertext unchanged" (so editing non-secret fields never wipes secrets);
/// an empty string clears the secret.
/// </summary>
public sealed record ProfileDraft
{
    public required string Name { get; init; }

    public ProfileKind Kind { get; init; } = ProfileKind.Custom;

    public AuthScheme Auth { get; init; } = AuthScheme.None;

    public string? ApiKeyHeaderName { get; init; }

    public string? Username { get; init; }

    public string? Email { get; init; }

    public string? Tenant { get; init; }

    public string? UserId { get; init; }

    // Plaintext secret material (null = unchanged on update, empty = cleared).
    public string? AccessToken { get; init; }

    public string? RefreshToken { get; init; }

    public string? Password { get; init; }

    public string? ApiKey { get; init; }

    public string? Secret { get; init; }
}
