using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Settings;
using MaterialDesignThemes.Wpf;

namespace ApiTestingStudio.UI.Services;

/// <summary>
/// Material Design implementation of <see cref="IThemeManager"/>. Uses <see cref="PaletteHelper"/>
/// to swap the base (light/dark) theme on the running application's merged resource dictionaries,
/// and persists the selected <see cref="ThemeMode"/> through <see cref="IAppSettingsService"/>.
/// Awaits continue on the UI context because applying a theme mutates WPF resources.
/// </summary>
public sealed class ThemeManager : IThemeManager
{
    private readonly IAppSettingsService _appSettings;
    private readonly PaletteHelper _paletteHelper = new();

    public ThemeManager(IAppSettingsService appSettings)
    {
        ArgumentNullException.ThrowIfNull(appSettings);
        _appSettings = appSettings;
    }

    public ThemeMode Current { get; private set; } = ThemeMode.Light;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _appSettings.LoadAsync(cancellationToken).ConfigureAwait(true);
        ApplyCore(settings.Theme);
    }

    public Task ToggleAsync(CancellationToken cancellationToken = default)
    {
        var next = Current == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
        return SetAsync(next, cancellationToken);
    }

    public async Task SetAsync(ThemeMode mode, CancellationToken cancellationToken = default)
    {
        ApplyCore(mode);

        var settings = await _appSettings.LoadAsync(cancellationToken).ConfigureAwait(true);
        await _appSettings.SaveAsync(settings with { Theme = mode }, cancellationToken).ConfigureAwait(true);
    }

    private void ApplyCore(ThemeMode mode)
    {
        var theme = _paletteHelper.GetTheme();
        theme.SetBaseTheme(mode == ThemeMode.Dark ? BaseTheme.Dark : BaseTheme.Light);
        _paletteHelper.SetTheme(theme);
        Current = mode;
    }
}
