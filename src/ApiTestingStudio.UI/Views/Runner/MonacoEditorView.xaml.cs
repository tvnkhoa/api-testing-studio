using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ApiTestingStudio.UI.Services;
using ApiTestingStudio.UI.ViewModels.Runner;

namespace ApiTestingStudio.UI.Views.Runner;

/// <summary>
/// Hosts the Monaco editor inside a WebView2. Code-behind is limited to the view-only concern of
/// WebView2/JS-interop plumbing (per CODING_STANDARDS): it bridges <see cref="MonacoEditorViewModel"/>
/// text to and from the hosted editor. If the WebView2 runtime is unavailable the control degrades
/// silently; the editor's <c>editor.html</c> itself degrades to a plain editor when the Monaco
/// bundle is absent.
/// </summary>
public partial class MonacoEditorView : UserControl
{
    private MonacoBridge? _bridge;
    private MonacoEditorViewModel? _viewModel;
    private bool _initialized;
    private bool _suppressPush;

    public MonacoEditorView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        try
        {
            var assets = Path.Combine(AppContext.BaseDirectory, "Assets", "monaco");
            _bridge = new MonacoBridge(Web);
            _bridge.TextChanged += OnBridgeTextChanged;
            await _bridge.InitializeAsync(assets);
            await PushAsync();
        }
        catch (Exception ex)
        {
            // WebView2 runtime missing or failed to start — leave the editor blank rather than crash.
            Trace.TraceWarning($"Monaco editor host failed to initialize: {ex.Message}");
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as MonacoEditorViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        _ = PushAsync();
    }

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressPush)
        {
            return;
        }

        await PushAsync();
    }

    private void OnBridgeTextChanged(object? sender, string text)
    {
        if (_viewModel is null)
        {
            return;
        }

        _suppressPush = true;
        _viewModel.Text = text;
        _suppressPush = false;
    }

    private async Task PushAsync()
    {
        if (_bridge is { IsReady: true } bridge && _viewModel is { } vm)
        {
            await bridge.PushAsync(vm.Text, vm.Language, vm.IsReadOnly);
        }
    }
}
