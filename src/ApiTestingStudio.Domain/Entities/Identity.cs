using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Domain.Entities;

/// <summary>
/// A simulated user identity ("Run As"). All secret-bearing fields are stored in an
/// already-encrypted form — the domain never holds plaintext secrets at rest. Encryption
/// is applied through the <c>ISecretProtector</c> port before persistence.
/// </summary>
public sealed record ProfileDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public required string Name { get; init; }

    public ProfileKind Kind { get; init; } = ProfileKind.Custom;

    public string? Username { get; init; }

    public string? Email { get; init; }

    public string? Tenant { get; init; }

    public string? UserId { get; init; }

    // --- Encrypted-at-rest secret material (ciphertext only) --------------------
    public string? ProtectedAccessToken { get; init; }

    public string? ProtectedRefreshToken { get; init; }

    public string? ProtectedPassword { get; init; }

    public string? ProtectedApiKey { get; init; }

    public string? ProtectedSecret { get; init; }
}

/// <summary>A named environment (Development / QA / Staging / Production).</summary>
public sealed record EnvironmentDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public required string Name { get; init; }

    public EnvironmentKind Kind { get; init; } = EnvironmentKind.Development;
}

/// <summary>A substitution variable resolved at execution time by scope precedence.</summary>
public sealed record Variable
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public VariableScope Scope { get; init; } = VariableScope.Workspace;

    public required string Key { get; init; }

    public string? Value { get; init; }

    /// <summary>When true, <see cref="Value"/> is stored as ciphertext.</summary>
    public bool IsSecret { get; init; }
}
