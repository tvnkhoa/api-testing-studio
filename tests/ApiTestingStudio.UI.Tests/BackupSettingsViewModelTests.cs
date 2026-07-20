using ApiTestingStudio.Application.Backup;
using ApiTestingStudio.Application.Settings;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.UI.ViewModels.Dialogs;
using FluentAssertions;

namespace ApiTestingStudio.UI.Tests;

public sealed class BackupSettingsViewModelTests
{
    private static (BackupSettingsViewModel Vm, FakeAppSettingsService Settings, FakeBackupService Backups, FakeWorkspaceSession Session, FakeFileDialogService Dialog) Create()
    {
        var settings = new FakeAppSettingsService();
        var backups = new FakeBackupService();
        var recovery = new FakeRecoveryService();
        var session = new FakeWorkspaceSession();
        var dialog = new FakeFileDialogService();
        var vm = new BackupSettingsViewModel(settings, backups, recovery, session, dialog);
        return (vm, settings, backups, session, dialog);
    }

    [Fact]
    public async Task Load_populates_settings_and_backups_for_the_open_workspace()
    {
        var (vm, settings, backups, session, _) = Create();
        var workspace = new Workspace { Name = "Demo" };
        session.Open(workspace, @"C:\temp\demo.atsdb");
        settings.Settings = new AppSettings { AutoBackupOnClose = true, BackupRetention = 5 };
        backups.Backups.Add(new BackupEntry("b.apistudio", workspace.Id, "Demo", DateTimeOffset.UnixEpoch, 1024));

        await vm.LoadAsync();

        vm.AutoBackupOnClose.Should().BeTrue();
        vm.BackupRetention.Should().Be(5);
        vm.Backups.Should().ContainSingle();
    }

    [Fact]
    public async Task Save_persists_the_settings()
    {
        var (vm, settings, _, _, _) = Create();
        await vm.LoadAsync();
        vm.AutoBackupOnClose = true;
        vm.BackupRetention = 7;

        await vm.SaveCommand.ExecuteAsync(null);

        settings.Settings.AutoBackupOnClose.Should().BeTrue();
        settings.Settings.BackupRetention.Should().Be(7);
    }

    [Fact]
    public async Task Restore_when_target_chosen_marks_workspace_changed()
    {
        var (vm, _, _, session, dialog) = Create();
        session.Open(new Workspace { Name = "Demo" }, @"C:\temp\demo.atsdb");
        dialog.CreateResult = @"C:\temp\restored.atsdb";
        var item = new BackupItemViewModel(new BackupEntry("b.apistudio", Guid.NewGuid(), "Demo", DateTimeOffset.UnixEpoch, 1024));

        await vm.RestoreCommand.ExecuteAsync(item);

        vm.WorkspaceChanged.Should().BeTrue();
    }

    [Fact]
    public async Task Restore_when_target_cancelled_does_nothing()
    {
        var (vm, _, _, _, dialog) = Create();
        dialog.CreateResult = null;
        var item = new BackupItemViewModel(new BackupEntry("b.apistudio", Guid.NewGuid(), "Demo", DateTimeOffset.UnixEpoch, 1024));

        await vm.RestoreCommand.ExecuteAsync(item);

        vm.WorkspaceChanged.Should().BeFalse();
    }
}
