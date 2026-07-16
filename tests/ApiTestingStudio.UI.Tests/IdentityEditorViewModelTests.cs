using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.UI.ViewModels.Dialogs;
using FluentAssertions;

namespace ApiTestingStudio.UI.Tests;

public sealed class ProfileEditorViewModelTests
{
    [Fact]
    public void ToDraft_maps_fields_and_secrets()
    {
        var vm = new ProfileEditorViewModel("New Profile", existing: null)
        {
            Name = "  Admin  ",
            Auth = AuthScheme.Bearer,
            AccessToken = "tok",
        };

        var draft = vm.ToDraft();

        draft.Name.Should().Be("Admin");
        draft.Auth.Should().Be(AuthScheme.Bearer);
        draft.AccessToken.Should().Be("tok");
    }

    [Fact]
    public void ToDraft_blank_secret_maps_to_null_to_keep_on_edit()
    {
        var existing = new ProfileDefinition { Name = "Admin", ProtectedPassword = "cipher" };
        var vm = new ProfileEditorViewModel("Edit Profile", existing);

        // Password left blank by the user.
        var draft = vm.ToDraft();

        draft.Password.Should().BeNull();
    }

    [Fact]
    public void CanConfirm_requires_a_name()
    {
        var vm = new ProfileEditorViewModel("New Profile", existing: null) { Name = "" };
        vm.CanConfirm.Should().BeFalse();

        vm.Name = "P";
        vm.CanConfirm.Should().BeTrue();
    }
}

public sealed class VariableEditorViewModelTests
{
    private static readonly EnvironmentDefinition Env =
        new() { Id = Guid.NewGuid(), Name = "QA", Kind = EnvironmentKind.QA };

    [Fact]
    public void Environment_scope_requires_a_selected_environment()
    {
        var vm = new VariableEditorViewModel("New", existing: null, [Env])
        {
            Key = "k",
            Scope = VariableScope.Environment,
            SelectedEnvironment = null,
        };

        vm.CanConfirm.Should().BeFalse();

        vm.SelectedEnvironment = Env;
        vm.CanConfirm.Should().BeTrue();
    }

    [Fact]
    public void ToDraft_sets_environment_id_only_for_environment_scope()
    {
        var vm = new VariableEditorViewModel("New", existing: null, [Env])
        {
            Key = "baseUrl",
            Scope = VariableScope.Workspace,
            Value = "https://x",
        };

        vm.ToDraft().EnvironmentId.Should().BeNull();

        vm.Scope = VariableScope.Environment;
        vm.SelectedEnvironment = Env;
        vm.ToDraft().EnvironmentId.Should().Be(Env.Id);
    }

    [Fact]
    public void Secret_variable_value_starts_blank_on_edit()
    {
        var existing = new Variable { Key = "token", Value = "cipher", IsSecret = true };

        var vm = new VariableEditorViewModel("Edit", existing, [Env]);

        vm.Value.Should().BeEmpty();
        vm.IsSecret.Should().BeTrue();
    }
}
