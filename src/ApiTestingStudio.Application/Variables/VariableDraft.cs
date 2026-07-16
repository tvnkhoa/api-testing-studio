using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Variables;

/// <summary>
/// Editable projection of a variable. <see cref="Value"/> is plaintext; when <see cref="IsSecret"/>
/// is true the service encrypts it before persistence. On update, a null <see cref="Value"/> for a
/// secret variable leaves the stored ciphertext unchanged.
/// </summary>
public sealed record VariableDraft
{
    public VariableScope Scope { get; init; } = VariableScope.Workspace;

    /// <summary>Required when <see cref="Scope"/> is <see cref="VariableScope.Environment"/>.</summary>
    public Guid? EnvironmentId { get; init; }

    public required string Key { get; init; }

    public string? Value { get; init; }

    public bool IsSecret { get; init; }
}
