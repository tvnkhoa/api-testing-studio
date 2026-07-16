using ApiTestingStudio.Application.Profiles;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Dialogs;

/// <summary>
/// Backs the new/edit profile dialog. Secret fields are entered as plaintext and only ever leave the
/// dialog inside the produced <see cref="ProfileDraft"/>, which the service encrypts. When editing,
/// secret boxes start blank; leaving one blank keeps the stored ciphertext (see
/// <see cref="ProfileService"/>).
/// </summary>
public sealed partial class ProfileEditorViewModel : ObservableObject
{
    private readonly bool _isEdit;

    public ProfileEditorViewModel(string title, ProfileDefinition? existing)
    {
        Title = title;
        _isEdit = existing is not null;
        _name = existing?.Name ?? string.Empty;
        _kind = existing?.Kind ?? ProfileKind.Custom;
        _auth = existing?.Auth ?? AuthScheme.None;
        _apiKeyHeaderName = existing?.ApiKeyHeaderName ?? "X-Api-Key";
        _username = existing?.Username ?? string.Empty;
        _email = existing?.Email ?? string.Empty;
        _tenant = existing?.Tenant ?? string.Empty;
        _userId = existing?.UserId ?? string.Empty;
    }

    public string Title { get; }

    public IReadOnlyList<ProfileKind> Kinds { get; } = Enum.GetValues<ProfileKind>();

    public IReadOnlyList<AuthScheme> Schemes { get; } = Enum.GetValues<AuthScheme>();

    /// <summary>Hint shown next to secret boxes when editing (blank keeps the stored value).</summary>
    public string SecretHint => _isEdit ? "Leave blank to keep the current value." : string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private string _name;

    [ObservableProperty]
    private ProfileKind _kind;

    [ObservableProperty]
    private AuthScheme _auth;

    [ObservableProperty]
    private string _apiKeyHeaderName;

    [ObservableProperty]
    private string _username;

    [ObservableProperty]
    private string _email;

    [ObservableProperty]
    private string _tenant;

    [ObservableProperty]
    private string _userId;

    // Secret plaintext (blank = keep on edit / none on create).
    [ObservableProperty]
    private string _accessToken = string.Empty;

    [ObservableProperty]
    private string _refreshToken = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _secret = string.Empty;

    /// <summary>Toggles secret fields between masked (PasswordBox) and revealed (TextBox).</summary>
    [ObservableProperty]
    private bool _revealSecrets;

    public bool CanConfirm => !string.IsNullOrWhiteSpace(Name);

    public ProfileDraft ToDraft() => new()
    {
        Name = Name.Trim(),
        Kind = Kind,
        Auth = Auth,
        ApiKeyHeaderName = string.IsNullOrWhiteSpace(ApiKeyHeaderName) ? null : ApiKeyHeaderName.Trim(),
        Username = Username,
        Email = Email,
        Tenant = Tenant,
        UserId = UserId,
        AccessToken = SecretOrKeep(AccessToken),
        RefreshToken = SecretOrKeep(RefreshToken),
        Password = SecretOrKeep(Password),
        ApiKey = SecretOrKeep(ApiKey),
        Secret = SecretOrKeep(Secret),
    };

    // On edit, a blank box means "keep" (null). On create, a blank box also maps to null (no secret).
    private static string? SecretOrKeep(string value) => string.IsNullOrEmpty(value) ? null : value;
}
