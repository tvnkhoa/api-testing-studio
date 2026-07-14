using ApiTestingStudio.Core.Plugins;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels;

/// <summary>
/// View model for the application shell. Kept intentionally small: it exposes only what the
/// empty Phase 1 shell needs. Feature view models are added under their own sprints.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    public MainViewModel(IPluginRegistry pluginRegistry)
    {
        ArgumentNullException.ThrowIfNull(pluginRegistry);
        PluginCount = pluginRegistry.Plugins.Count;
        StatusMessage = $"Ready — {PluginCount} plugin(s) loaded.";
    }

    [ObservableProperty]
    private string _title = "API Testing Studio";

    [ObservableProperty]
    private string _statusMessage;

    /// <summary>Number of plugin modules discovered and registered at startup.</summary>
    public int PluginCount { get; }
}
