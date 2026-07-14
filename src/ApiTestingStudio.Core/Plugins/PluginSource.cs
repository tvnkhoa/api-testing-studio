namespace ApiTestingStudio.Core.Plugins;

/// <summary>Where a plugin was discovered from.</summary>
public enum PluginSource
{
    /// <summary>Referenced by the host at compile time and supplied as an assembly.</summary>
    CompileTime,

    /// <summary>Loaded dynamically from the <c>plugins/</c> directory via an isolated load context.</summary>
    Directory,
}
