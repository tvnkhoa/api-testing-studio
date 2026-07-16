namespace ApiTestingStudio.Application.Common;

/// <inheritdoc />
public sealed class UndoRedoService : IUndoRedoService
{
    private readonly Stack<IUndoableCommand> _undo = new();
    private readonly Stack<IUndoableCommand> _redo = new();

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public event EventHandler? StateChanged;

    public void Execute(IUndoableCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Execute();
        _undo.Push(command);
        _redo.Clear();
        RaiseStateChanged();
    }

    public void Undo()
    {
        if (_undo.Count == 0)
        {
            return;
        }

        var command = _undo.Pop();
        command.Undo();
        _redo.Push(command);
        RaiseStateChanged();
    }

    public void Redo()
    {
        if (_redo.Count == 0)
        {
            return;
        }

        var command = _redo.Pop();
        command.Execute();
        _undo.Push(command);
        RaiseStateChanged();
    }

    public void Clear()
    {
        if (_undo.Count == 0 && _redo.Count == 0)
        {
            return;
        }

        _undo.Clear();
        _redo.Clear();
        RaiseStateChanged();
    }

    private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
