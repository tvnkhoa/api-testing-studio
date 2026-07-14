namespace ApiTestingStudio.Application.Settings;

/// <summary>
/// The visual theme applied to the shell. Following the OS preference automatically is deferred
/// (see <c>UI/Themes.md</c>); this sprint ships a manual light/dark choice only.
/// </summary>
public enum ThemeMode
{
    /// <summary>Light Material Design palette.</summary>
    Light = 0,

    /// <summary>Dark Material Design palette.</summary>
    Dark = 1,
}
