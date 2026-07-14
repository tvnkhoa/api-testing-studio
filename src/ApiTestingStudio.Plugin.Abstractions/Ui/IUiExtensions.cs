namespace ApiTestingStudio.Plugin.Abstractions.Ui;

/// <summary>
/// A dashboard widget contributed by a plugin. Phase 1 defines identity/metadata only;
/// the view contract is fleshed out in the Dashboard sprint.
/// </summary>
public interface IDashboardWidget
{
    /// <summary>Stable widget identifier.</summary>
    string WidgetId { get; }

    /// <summary>Display title shown in the dashboard.</summary>
    string Title { get; }
}

/// <summary>
/// A dockable tool window contributed by a plugin. Phase 1 defines identity/metadata only;
/// the view/host contract is fleshed out in the Shell UI sprint.
/// </summary>
public interface IToolWindow
{
    /// <summary>Stable tool-window identifier.</summary>
    string ToolWindowId { get; }

    /// <summary>Display title shown on the dock tab.</summary>
    string Title { get; }
}
