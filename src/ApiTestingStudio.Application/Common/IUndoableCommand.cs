namespace ApiTestingStudio.Application.Common;

/// <summary>
/// A single reversible edit (a memento/command). <see cref="Execute"/> applies the change and is also
/// used to <em>redo</em> it; <see cref="Undo"/> reverses it. Implementations capture whatever state
/// they need to round-trip. Kept UI-agnostic so <see cref="IUndoRedoService"/> is reusable.
/// </summary>
public interface IUndoableCommand
{
    /// <summary>Short human-readable label (e.g. "Add node", "Move node") for tooltips/history.</summary>
    string Description { get; }

    /// <summary>Applies the edit (also invoked to redo it).</summary>
    void Execute();

    /// <summary>Reverses the edit.</summary>
    void Undo();
}
