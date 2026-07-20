using ApiTestingStudio.Application.Dashboard;
using ApiTestingStudio.Plugin.Abstractions.Ui;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Dashboard;

/// <summary>
/// A dashboard widget that renders from a <see cref="DashboardSnapshot"/> (Sprint 13). Built-in widgets
/// derive from <see cref="DashboardWidgetViewModel"/>; a plugin widget contributes an
/// <see cref="IDashboardWidget"/> and, to be data-driven, also implements this UI-side contract.
/// </summary>
public interface IDashboardWidgetContent
{
    /// <summary>Applies the latest aggregate snapshot to the widget's bound state.</summary>
    void Update(DashboardSnapshot snapshot);
}

/// <summary>
/// Base for the first-party dashboard widgets. Carries the plugin identity/metadata
/// (<see cref="IDashboardWidget"/>) so widgets are enumerated uniformly, plus the data contract
/// (<see cref="IDashboardWidgetContent"/>) the <c>DashboardViewModel</c> drives on refresh.
/// </summary>
public abstract class DashboardWidgetViewModel : ObservableObject, IDashboardWidget, IDashboardWidgetContent
{
    protected DashboardWidgetViewModel(string widgetId, string title, int order)
    {
        WidgetId = widgetId;
        Title = title;
        Order = order;
    }

    /// <inheritdoc />
    public string WidgetId { get; }

    /// <inheritdoc />
    public string Title { get; }

    /// <summary>Display order within the dashboard grid (ascending).</summary>
    public int Order { get; }

    /// <inheritdoc />
    public abstract void Update(DashboardSnapshot snapshot);
}
