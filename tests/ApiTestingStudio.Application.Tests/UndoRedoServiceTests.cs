using ApiTestingStudio.Application.Common;
using FluentAssertions;
using Xunit;

namespace ApiTestingStudio.Application.Tests;

public sealed class UndoRedoServiceTests
{
    [Fact]
    public void Execute_runs_the_command_and_enables_undo()
    {
        var service = new UndoRedoService();
        var log = new List<string>();

        service.Execute(new DelegateCommand(() => log.Add("do"), () => log.Add("undo")));

        log.Should().Equal("do");
        service.CanUndo.Should().BeTrue();
        service.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void Undo_then_redo_round_trips()
    {
        var service = new UndoRedoService();
        var log = new List<string>();
        service.Execute(new DelegateCommand(() => log.Add("do"), () => log.Add("undo")));

        service.Undo();
        service.CanUndo.Should().BeFalse();
        service.CanRedo.Should().BeTrue();

        service.Redo();
        service.CanUndo.Should().BeTrue();
        service.CanRedo.Should().BeFalse();
        log.Should().Equal("do", "undo", "do");
    }

    [Fact]
    public void Executing_a_new_command_clears_the_redo_stack()
    {
        var service = new UndoRedoService();
        service.Execute(new DelegateCommand(() => { }, () => { }));
        service.Undo();
        service.CanRedo.Should().BeTrue();

        service.Execute(new DelegateCommand(() => { }, () => { }));

        service.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void Undo_and_redo_are_no_ops_on_empty_stacks()
    {
        var service = new UndoRedoService();

        service.Undo();
        service.Redo();

        service.CanUndo.Should().BeFalse();
        service.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void StateChanged_is_raised_on_execute()
    {
        var service = new UndoRedoService();
        var raised = 0;
        service.StateChanged += (_, _) => raised++;

        service.Execute(new DelegateCommand(() => { }, () => { }));

        raised.Should().Be(1);
    }

    private sealed class DelegateCommand : IUndoableCommand
    {
        private readonly Action _execute;
        private readonly Action _undo;

        public DelegateCommand(Action execute, Action undo)
        {
            _execute = execute;
            _undo = undo;
        }

        public string Description => "test";

        public void Execute() => _execute();

        public void Undo() => _undo();
    }
}
