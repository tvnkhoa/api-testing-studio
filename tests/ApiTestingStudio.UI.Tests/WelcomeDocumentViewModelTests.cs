using ApiTestingStudio.UI.Messaging;
using ApiTestingStudio.UI.ViewModels.Panels;
using CommunityToolkit.Mvvm.Messaging;
using FluentAssertions;

namespace ApiTestingStudio.UI.Tests;

public sealed class WelcomeDocumentViewModelTests
{
    private readonly WeakReferenceMessenger _messenger = new();
    private readonly WelcomeDocumentViewModel _sut;

    public WelcomeDocumentViewModelTests() => _sut = new WelcomeDocumentViewModel(_messenger);

    [Fact]
    public void OpenSample_is_always_enabled_and_sends_the_action()
    {
        WelcomeAction? received = null;
        _messenger.Register<WelcomeActionMessage>(this, (_, m) => received = m.Action);

        _sut.OpenSampleCommand.CanExecute(null).Should().BeTrue();
        _sut.OpenSampleCommand.Execute(null);

        received.Should().Be(WelcomeAction.OpenSample);
    }

    [Fact]
    public void Import_and_AddService_are_gated_on_an_open_workspace()
    {
        _sut.IsWorkspaceOpen = false;
        _sut.ImportCommand.CanExecute(null).Should().BeFalse();
        _sut.AddServiceCommand.CanExecute(null).Should().BeFalse();

        _sut.IsWorkspaceOpen = true;
        _sut.ImportCommand.CanExecute(null).Should().BeTrue();
        _sut.AddServiceCommand.CanExecute(null).Should().BeTrue();
    }
}
