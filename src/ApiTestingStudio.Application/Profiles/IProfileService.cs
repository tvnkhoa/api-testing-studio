using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Profiles;

/// <summary>
/// Create/read/update/delete identity profiles for the open workspace. Secret material supplied via
/// <see cref="ProfileDraft"/> is encrypted through <c>ISecretProtector</c> before persistence; the
/// returned <see cref="ProfileDefinition"/> carries ciphertext only.
/// </summary>
public interface IProfileService
{
    Task<Result<IReadOnlyList<ProfileDefinition>>> ListAsync(CancellationToken cancellationToken = default);

    Task<Result<ProfileDefinition>> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<ProfileDefinition>> CreateAsync(ProfileDraft draft, CancellationToken cancellationToken = default);

    Task<Result<ProfileDefinition>> UpdateAsync(Guid id, ProfileDraft draft, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
