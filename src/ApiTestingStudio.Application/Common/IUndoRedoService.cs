namespace ApiTestingStudio.Application.Common;

/// <summary>
/// A generic two-stack undo/redo manager. Reusable anywhere edits need reversing (the Sprint 09
/// designer is the first consumer). Not thread-safe by design — it is driven from a single UI thread.
/// </summary>
public interface IUndoRedoService
{
    /// <summary>Whether there is at least one edit that can be undone.</summary>
    bool CanUndo { get; }

    /// <summary>Whether there is at least one undone edit that can be redone.</summary>
    bool CanRedo { get; }

    /// <summary>Raised whenever the undo/redo availability changes, so callers can refresh commands.</summary>
    event EventHandler? StateChanged;

    /// <summary>Runs the command, pushes it onto the undo stack, and clears the redo stack.</summary>
    void Execute(IUndoableCommand command);

    /// <summary>Undoes the most recent command (no-op when <see cref="CanUndo"/> is false).</summary>
    void Undo();

    /// <summary>Redoes the most recently undone command (no-op when <see cref="CanRedo"/> is false).</summary>
    void Redo();

    /// <summary>Discards all history (e.g. after a fresh load).</summary>
    void Clear();
}
