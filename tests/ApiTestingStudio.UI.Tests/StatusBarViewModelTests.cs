using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.UI.ViewModels;
using FluentAssertions;

namespace ApiTestingStudio.UI.Tests;

public sealed class StatusBarViewModelTests
{
    [Fact]
    public void Connection_status_is_offline()
    {
        var vm = new StatusBarViewModel(new FakeWorkspaceSession(), new FakeStatusBarService());

        vm.ConnectionStatus.Should().Be("Offline");
    }

    [Fact]
    public void Refresh_shows_placeholder_when_no_workspace_open()
    {
        var vm = new StatusBarViewModel(new FakeWorkspaceSession(), new FakeStatusBarService());

        vm.RefreshWorkspace();

        vm.WorkspaceName.Should().Be("No workspace open");
    }

    [Fact]
    public void Refresh_shows_the_open_workspace_name()
    {
        var session = new FakeWorkspaceSession();
        session.Open(new Workspace { Name = "Payments" }, @"C:\ws\Payments.atsdb");
        var vm = new StatusBarViewModel(session, new FakeStatusBarService());

        vm.RefreshWorkspace();

        vm.WorkspaceName.Should().Be("Payments");
    }

    [Fact]
    public void Background_status_reflects_the_status_service()
    {
        var status = new FakeStatusBarService();
        var vm = new StatusBarViewModel(new FakeWorkspaceSession(), status);

        status.SetMessage("Running…");

        vm.BackgroundStatus.Should().Be("Running…");
    }
}
