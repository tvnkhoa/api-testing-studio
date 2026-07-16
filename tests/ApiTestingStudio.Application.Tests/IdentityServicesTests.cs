using ApiTestingStudio.Application.Environments;
using ApiTestingStudio.Application.Profiles;
using ApiTestingStudio.Application.Variables;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class AuthApplicatorTests
{
    private readonly FakeSecretProtector _protector = new();
    private readonly AuthApplicator _sut;

    public AuthApplicatorTests() => _sut = new AuthApplicator(_protector);

    private static HttpRequestModel Request() => new() { Url = "https://api.example.com" };

    [Fact]
    public void Apply_bearer_sets_authorization_header()
    {
        var profile = new ProfileDefinition
        {
            Name = "P",
            Auth = AuthScheme.Bearer,
            ProtectedAccessToken = _protector.Protect("token-123"),
        };

        var result = _sut.Apply(Request(), profile);

        result.Headers.Should().ContainSingle(h => h.Name == "Authorization" && h.Value == "Bearer token-123");
    }

    [Fact]
    public void Apply_basic_sets_base64_credentials()
    {
        var profile = new ProfileDefinition
        {
            Name = "P",
            Auth = AuthScheme.Basic,
            Username = "alice",
            ProtectedPassword = _protector.Protect("s3cret"),
        };

        var result = _sut.Apply(Request(), profile);

        var expected = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("alice:s3cret"));
        result.Headers.Should().ContainSingle(h => h.Name == "Authorization" && h.Value == expected);
    }

    [Fact]
    public void Apply_apikey_uses_configured_header_name()
    {
        var profile = new ProfileDefinition
        {
            Name = "P",
            Auth = AuthScheme.ApiKey,
            ApiKeyHeaderName = "X-Api-Key",
            ProtectedApiKey = _protector.Protect("abc"),
        };

        var result = _sut.Apply(Request(), profile);

        result.Headers.Should().ContainSingle(h => h.Name == "X-Api-Key" && h.Value == "abc");
    }

    [Fact]
    public void Apply_none_or_null_leaves_request_unchanged()
    {
        var none = new ProfileDefinition { Name = "P", Auth = AuthScheme.None };

        _sut.Apply(Request(), none).Headers.Should().BeEmpty();
        _sut.Apply(Request(), null).Headers.Should().BeEmpty();
    }
}

public sealed class VariableScopeSeederTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private readonly InMemoryVariableRepository _variables = new();
    private readonly InMemoryEnvironmentRepository _environments = new();
    private readonly InMemoryWorkspaceSettingRepository _settings = new();
    private readonly FakeSecretProtector _protector = new();
    private readonly FakeWorkspaceSession _session = new()
    {
        Current = new Workspace { Id = WorkspaceId, Name = "WS" },
    };

    private VariableScopeSeeder CreateSut() =>
        new(_variables, new EnvironmentService(_environments, _settings, _session), _protector, _session);

    [Fact]
    public async Task Environment_scope_overrides_workspace_scope_when_active()
    {
        var env = new EnvironmentDefinition { Id = Guid.NewGuid(), WorkspaceId = WorkspaceId, Name = "QA", Kind = EnvironmentKind.QA };
        _environments.Items.Add(env);
        _variables.Items.Add(new Variable { WorkspaceId = WorkspaceId, Scope = VariableScope.Workspace, Key = "baseUrl", Value = "https://ws" });
        _variables.Items.Add(new Variable { WorkspaceId = WorkspaceId, Scope = VariableScope.Environment, EnvironmentId = env.Id, Key = "baseUrl", Value = "https://qa" });
        await new EnvironmentService(_environments, _settings, _session).SetActiveAsync(env.Id);

        var context = await CreateSut().BuildContextAsync();

        context.TryGetVariable("baseUrl", out var value).Should().BeTrue();
        value.Should().Be("https://qa");
    }

    [Fact]
    public async Task Workspace_scope_used_when_no_active_environment()
    {
        _variables.Items.Add(new Variable { WorkspaceId = WorkspaceId, Scope = VariableScope.Workspace, Key = "baseUrl", Value = "https://ws" });

        var context = await CreateSut().BuildContextAsync();

        context.TryGetVariable("baseUrl", out var value).Should().BeTrue();
        value.Should().Be("https://ws");
    }

    [Fact]
    public async Task Secret_variables_are_decrypted_when_seeded()
    {
        _variables.Items.Add(new Variable
        {
            WorkspaceId = WorkspaceId,
            Scope = VariableScope.Workspace,
            Key = "apiKey",
            Value = _protector.Protect("plaintext-key"),
            IsSecret = true,
        });

        var context = await CreateSut().BuildContextAsync();

        context.TryGetVariable("apiKey", out var value).Should().BeTrue();
        value.Should().Be("plaintext-key");
    }
}

public sealed class ProfileServiceTests
{
    private readonly InMemoryProfileRepository _profiles = new();
    private readonly FakeSecretProtector _protector = new();
    private readonly FakeWorkspaceSession _session = new() { Current = new Workspace { Name = "WS" } };
    private readonly ProfileService _sut;

    public ProfileServiceTests() => _sut = new ProfileService(_profiles, _protector, _session);

    [Fact]
    public async Task Create_encrypts_secret_fields()
    {
        var result = await _sut.CreateAsync(new ProfileDraft { Name = "Admin", AccessToken = "tok" });

        result.IsSuccess.Should().BeTrue();
        result.Value.ProtectedAccessToken.Should().NotBeNull();
        result.Value.ProtectedAccessToken.Should().NotBe("tok");
        _protector.Unprotect(result.Value.ProtectedAccessToken!).Should().Be("tok");
    }

    [Fact]
    public async Task Update_with_null_secret_keeps_existing_ciphertext()
    {
        var created = (await _sut.CreateAsync(new ProfileDraft { Name = "Admin", Password = "pw" })).Value;

        var updated = (await _sut.UpdateAsync(created.Id, new ProfileDraft { Name = "Admin Renamed", Password = null })).Value;

        updated.Name.Should().Be("Admin Renamed");
        updated.ProtectedPassword.Should().Be(created.ProtectedPassword);
    }

    [Fact]
    public async Task Update_with_empty_secret_clears_ciphertext()
    {
        var created = (await _sut.CreateAsync(new ProfileDraft { Name = "Admin", Password = "pw" })).Value;

        var updated = (await _sut.UpdateAsync(created.Id, new ProfileDraft { Name = "Admin", Password = "" })).Value;

        updated.ProtectedPassword.Should().BeNull();
    }
}

public sealed class VariableServiceTests
{
    private readonly InMemoryVariableRepository _variables = new();
    private readonly FakeSecretProtector _protector = new();
    private readonly FakeWorkspaceSession _session = new() { Current = new Workspace { Name = "WS" } };
    private readonly VariableService _sut;

    public VariableServiceTests() => _sut = new VariableService(_variables, _protector, _session);

    [Fact]
    public async Task Create_secret_variable_encrypts_value()
    {
        var result = await _sut.CreateAsync(new VariableDraft { Key = "token", Value = "raw", IsSecret = true });

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().NotBe("raw");
        _protector.Unprotect(result.Value.Value!).Should().Be("raw");
    }

    [Fact]
    public async Task Create_environment_scope_without_environment_fails()
    {
        var result = await _sut.CreateAsync(new VariableDraft { Key = "k", Scope = VariableScope.Environment });

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.environment_required");
    }
}
