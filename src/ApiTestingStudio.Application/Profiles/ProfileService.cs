using System.Globalization;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Profiles;

/// <summary>
/// Profile CRUD over <see cref="IProfileRepository"/>. Encrypts plaintext secrets from the draft via
/// <see cref="ISecretProtector"/> so the domain only ever persists ciphertext. Workspace scope comes
/// from <see cref="IWorkspaceSession"/>. The active "Run As" profile id is stored as a per-workspace
/// setting (mirrors <c>EnvironmentService</c>; no schema change).
/// </summary>
public sealed class ProfileService : IProfileService
{
    /// <summary>Settings key holding the active "Run As" profile id for the open workspace.</summary>
    public const string ActiveProfileSettingKey = "active-profile-id";

    private readonly IProfileRepository _profiles;
    private readonly ISecretProtector _protector;
    private readonly IWorkspaceSession _session;
    private readonly IWorkspaceSettingRepository _settings;

    public ProfileService(
        IProfileRepository profiles,
        ISecretProtector protector,
        IWorkspaceSession session,
        IWorkspaceSettingRepository settings)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(protector);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(settings);
        _profiles = profiles;
        _protector = protector;
        _session = session;
        _settings = settings;
    }

    public async Task<Result<IReadOnlyList<ProfileDefinition>>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (_session.Current is not { } workspace)
        {
            return Result.Failure<IReadOnlyList<ProfileDefinition>>(IdentityErrors.NoWorkspaceOpen);
        }

        var profiles = await _profiles.GetByWorkspaceAsync(workspace.Id, cancellationToken).ConfigureAwait(false);
        return Result.Success(profiles);
    }

    public async Task<Result<ProfileDefinition>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure<ProfileDefinition>(IdentityErrors.NoWorkspaceOpen);
        }

        var profile = await _profiles.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return profile is null
            ? Result.Failure<ProfileDefinition>(IdentityErrors.ProfileNotFound(id))
            : Result.Success(profile);
    }

    public async Task<Result<ProfileDefinition>> CreateAsync(ProfileDraft draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (_session.Current is not { } workspace)
        {
            return Result.Failure<ProfileDefinition>(IdentityErrors.NoWorkspaceOpen);
        }

        if (string.IsNullOrWhiteSpace(draft.Name))
        {
            return Result.Failure<ProfileDefinition>(IdentityErrors.NameRequired);
        }

        var profile = new ProfileDefinition
        {
            WorkspaceId = workspace.Id,
            Name = draft.Name.Trim(),
            Kind = draft.Kind,
            Auth = draft.Auth,
            ApiKeyHeaderName = Normalize(draft.ApiKeyHeaderName),
            Username = Normalize(draft.Username),
            Email = Normalize(draft.Email),
            Tenant = Normalize(draft.Tenant),
            UserId = Normalize(draft.UserId),
            ProtectedAccessToken = ProtectNew(draft.AccessToken),
            ProtectedRefreshToken = ProtectNew(draft.RefreshToken),
            ProtectedPassword = ProtectNew(draft.Password),
            ProtectedApiKey = ProtectNew(draft.ApiKey),
            ProtectedSecret = ProtectNew(draft.Secret),
        };

        await _profiles.AddAsync(profile, cancellationToken).ConfigureAwait(false);
        return Result.Success(profile);
    }

    public async Task<Result<ProfileDefinition>> UpdateAsync(Guid id, ProfileDraft draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (!_session.IsOpen)
        {
            return Result.Failure<ProfileDefinition>(IdentityErrors.NoWorkspaceOpen);
        }

        if (string.IsNullOrWhiteSpace(draft.Name))
        {
            return Result.Failure<ProfileDefinition>(IdentityErrors.NameRequired);
        }

        var existing = await _profiles.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return Result.Failure<ProfileDefinition>(IdentityErrors.ProfileNotFound(id));
        }

        var updated = existing with
        {
            Name = draft.Name.Trim(),
            Kind = draft.Kind,
            Auth = draft.Auth,
            ApiKeyHeaderName = Normalize(draft.ApiKeyHeaderName),
            Username = Normalize(draft.Username),
            Email = Normalize(draft.Email),
            Tenant = Normalize(draft.Tenant),
            UserId = Normalize(draft.UserId),
            ProtectedAccessToken = MergeSecret(draft.AccessToken, existing.ProtectedAccessToken),
            ProtectedRefreshToken = MergeSecret(draft.RefreshToken, existing.ProtectedRefreshToken),
            ProtectedPassword = MergeSecret(draft.Password, existing.ProtectedPassword),
            ProtectedApiKey = MergeSecret(draft.ApiKey, existing.ProtectedApiKey),
            ProtectedSecret = MergeSecret(draft.Secret, existing.ProtectedSecret),
        };

        await _profiles.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);
        return Result.Success(updated);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure(IdentityErrors.NoWorkspaceOpen);
        }

        var existing = await _profiles.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return Result.Failure(IdentityErrors.ProfileNotFound(id));
        }

        await _profiles.DeleteAsync(id, cancellationToken).ConfigureAwait(false);

        // Clear the active selection if it pointed at the deleted profile.
        var active = await GetActiveIdAsync(cancellationToken).ConfigureAwait(false);
        if (active == id)
        {
            await _settings.SetAsync(ActiveProfileSettingKey, null, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    public async Task<Guid?> GetActiveIdAsync(CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return null;
        }

        var raw = await _settings.GetAsync(ActiveProfileSettingKey, cancellationToken).ConfigureAwait(false);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public async Task<Result> SetActiveAsync(Guid? profileId, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure(IdentityErrors.NoWorkspaceOpen);
        }

        if (profileId is { } id)
        {
            var profile = await _profiles.GetAsync(id, cancellationToken).ConfigureAwait(false);
            if (profile is null)
            {
                return Result.Failure(IdentityErrors.ProfileNotFound(id));
            }
        }

        var value = profileId?.ToString(null, CultureInfo.InvariantCulture);
        await _settings.SetAsync(ActiveProfileSettingKey, value, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Encrypts a plaintext secret on create; null/empty stores nothing.</summary>
    private string? ProtectNew(string? plaintext) =>
        string.IsNullOrEmpty(plaintext) ? null : _protector.Protect(plaintext);

    /// <summary>
    /// On update: null draft value keeps the existing ciphertext; empty string clears it; any other
    /// value is re-encrypted.
    /// </summary>
    private string? MergeSecret(string? draftPlaintext, string? existingCipher) => draftPlaintext switch
    {
        null => existingCipher,
        "" => null,
        _ => _protector.Protect(draftPlaintext),
    };
}
